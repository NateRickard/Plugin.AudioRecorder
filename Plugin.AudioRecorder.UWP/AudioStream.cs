using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;

namespace Plugin.AudioRecorder
{
	public class AudioStream : IAudioStream
	{
		AudioGraph audioGraph;
		AudioFrameOutputNode outputNode;

		/// <summary>
		/// Occurs when new audio has been streamed.
		/// </summary>
		public event EventHandler<byte []> OnBroadcast;

		/// <summary>
		/// Occurs when the audio stream active status changes.
		/// </summary>
		public event EventHandler<bool> OnActiveChanged;

		/// <summary>
		/// Occurs when there's an error while capturing audio.
		/// </summary>
		public event EventHandler<Exception> OnException;

		/// <summary>
		/// Gets the sample rate.
		/// </summary>
		/// <value>
		/// The sample rate.
		/// </value>
		public int SampleRate { get; private set; } = 44100;

		/// <summary>
		/// Gets the channel count.
		/// </summary>
		/// <value>
		/// The channel count.
		/// </value>
		public int ChannelCount { get; private set; }

		/// <summary>
		/// Gets bits per sample.
		/// </summary>
		public int BitsPerSample { get; private set; } = 16;

		/// <summary>
		/// Gets a value indicating if the audio stream is active.
		/// </summary>
		public bool Active { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AudioStream"/> class.
		/// </summary>
		/// <param name="sampleRate">Sample rate.</param>
		/// <param name="channels">A value representing the number of channels to record.</param>
		public AudioStream (int sampleRate, int channels = 1)
		{
			SampleRate = sampleRate;
			ChannelCount = channels;
		}

		async Task Init ()
		{
			try
			{
				await Stop ();

				var pcmEncoding = AudioEncodingProperties.CreatePcm ((uint) SampleRate, (uint) ChannelCount, (uint) BitsPerSample);
				// apparently this is not _really_ used/supported here, as the audio data seems to come thru as floats (so basically MediaEncodingSubtypes.Float?)
				pcmEncoding.Subtype = MediaEncodingSubtypes.Pcm;

				var graphSettings = new AudioGraphSettings (AudioRenderCategory.Media)
				{
					EncodingProperties = pcmEncoding,
					DesiredRenderDeviceAudioProcessing = AudioProcessing.Raw
					// these do not seem to take effect on certain hardware and MSFT recommends SystemDefault when recording to a file anyway
					//	We'll buffer audio data ourselves to improve RMS calculation across larger samples
					//QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired,
					//DesiredSamplesPerQuantum = 4096
				};

				// create our audio graph... this will be a device input node feeding audio data into a frame output node
				var graphResult = await AudioGraph.CreateAsync (graphSettings);

				if (graphResult.Status == AudioGraphCreationStatus.Success)
				{
					audioGraph = graphResult.Graph;

					// take input from whatever the default communications device is set to me on windows
					var inputResult = await audioGraph.CreateDeviceInputNodeAsync (MediaCategory.Communications, pcmEncoding);

					if (inputResult.Status == AudioDeviceNodeCreationStatus.Success)
					{
						// create the output node
						outputNode = audioGraph.CreateFrameOutputNode (pcmEncoding);

						// wire the input to the output
						inputResult.DeviceInputNode.AddOutgoingConnection (outputNode);

						// Attach to QuantumStarted event in order to receive synchronous updates from audio graph (to capture incoming audio)
						audioGraph.QuantumStarted += Graph_QuantumStarted;
						audioGraph.UnrecoverableErrorOccurred += Graph_UnrecoverableErrorOccurred;
					}
					else
					{
						throw new Exception ($"audioGraph.CreateDeviceInputNodeAsync() returned non-Success status: {inputResult.Status}");
					}
				}
				else
				{
					throw new Exception ($"AudioGraph.CreateAsync() returned non-Success status: {graphResult.Status}");
				}
			}
			catch
			{
				throw;
			}
		}

		/// <summary>
		/// Starts the audio stream.
		/// </summary>
		public async Task Start ()
		{
			try
			{
				if (!Active)
				{
					await Init ();

					// start our constructed audio graph
					audioGraph.Start ();

					Active = true;
					OnActiveChanged?.Invoke (this, true);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in AudioStream.Start(): {0}", ex.Message);

				await Stop ();
				throw;
			}
		}

		/// <summary>
		/// Stops the audio stream.
		/// </summary>
		public Task Stop ()
		{
			if (Active)
			{
				Active = false;

				outputNode?.Stop ();
				audioGraph?.Stop ();

				OnActiveChanged?.Invoke (this, false);
			}

			outputNode?.Dispose ();
			outputNode = null;

			if (audioGraph != null)
			{
				audioGraph.QuantumStarted -= Graph_QuantumStarted;
				audioGraph.UnrecoverableErrorOccurred -= Graph_UnrecoverableErrorOccurred;
				audioGraph.Dispose ();
				audioGraph = null;
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// IMemoryBuferByteAccess is used to access the underlying audioframe for read and write
		/// </summary>
		[ComImport]
		[Guid ("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
		[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
		unsafe interface IMemoryBufferByteAccess
		{
			void GetBuffer (out byte* buffer, out uint capacity);
		}

		const int broadcastSize = 10; // we'll accumulate 10 'quantums' before broadcasting them
		int bufferPosition = 0;
		byte [] audioBytes;

		unsafe void Graph_QuantumStarted (AudioGraph sender, object args)
		{
			// we'll only broadcast if we're actively monitoring audio packets
			if (!Active)
			{
				return;
			}

			try
			{
				// get an audio frame from the output node
				AudioFrame frame = outputNode.GetFrame ();

				if (frame.Duration?.Milliseconds == 0) // discard any empty frames
				{
					return;
				}

				using (var buffer = frame.LockBuffer (AudioBufferAccessMode.Read))
				using (IMemoryBufferReference reference = buffer.CreateReference ())
				{
					// Get the buffer from the AudioFrame
					((IMemoryBufferByteAccess) reference).GetBuffer (out byte* dataInBytes, out uint capacityInBytes);

					// convert the bytes into float
					float* dataInFloat = (float*) dataInBytes;

					if (audioBytes == null)
					{
						audioBytes = new byte [buffer.Length * broadcastSize / 2]; // buffer length * # of frames we want to accrue / 2 (because we're transforming float audio to Int 16)
					}

					for (int i = 0; i < capacityInBytes / sizeof (float); i++)
					{
						// convert the float into a double byte for 16 bit PCM
						var shortVal = AudioFunctions.FloatToInt16 (dataInFloat [i]);
						byte [] chunkBytes = BitConverter.GetBytes (shortVal);

						audioBytes [bufferPosition++] = chunkBytes [0];
						audioBytes [bufferPosition++] = chunkBytes [1];
					}

					// we want to wait until we accrue <broadcastSize> # of frames and then broadcast them
					//	in practice, this will take us from 20ms chunks to 100ms chunks and result in more accurate audio level calculations
					//	we could maybe use the audiograph latency settings to achieve similar results but this seems to work well
					if (bufferPosition == audioBytes.Length || !Active)
					{
						// broadcast the audio data to any listeners
						OnBroadcast?.Invoke (this, audioBytes);

						audioBytes = null;
						bufferPosition = 0;
					}
				}
			}
			catch (Exception ex)
			{
				OnException?.Invoke (this, new Exception ($"AudioStream.QueueInputCompleted() :: Error: {ex.Message}"));
			}
		}

		async void Graph_UnrecoverableErrorOccurred (AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
		{
			await Stop ();

			throw new Exception ($"UnrecoverableErrorOccurred error: {args.Error}");
		}

		/// <summary>
		/// Flushes any audio bytes in memory but not yet broadcast out to any listeners.
		/// </summary>
		public void Flush ()
		{
			// not sure this is _really_ needed, but just in case, if we have bytes buffered in audioBytes, flush them out here

			// do we have leftover bytes to broadcast as we're stopping?
			if (audioBytes != null)
			{
				Debug.WriteLine ("Broadcasting remaining {0} audioBytes", audioBytes.Length);

				// broadcast the audio data to any listeners
				OnBroadcast?.Invoke (this, audioBytes);

				audioBytes = null;
			}
		}
	}
}
