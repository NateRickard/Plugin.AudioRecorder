using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage.Streams;

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


		void Graph_QuantumStarted (AudioGraph sender, object args)
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

				using (AudioBuffer audioBuffer = frame.LockBuffer (AudioBufferAccessMode.Read))
				{
					var buffer = Windows.Storage.Streams.Buffer.CreateCopyFromMemoryBuffer (audioBuffer);
					buffer.Length = audioBuffer.Length;
					var audioBytes = new byte [buffer.Length / 2]; // 1/2 length because we're transforming float audio to Int 16

					using (var dataReader = DataReader.FromBuffer (buffer))
					{
						dataReader.ByteOrder = ByteOrder.LittleEndian;

						int pos = 0;

						while (dataReader.UnconsumedBufferLength > 0)
						{
							// need to convert float audio to short and then get its bytes
							var floatVal = dataReader.ReadSingle ();
							var shortVal = FloatToInt16 (floatVal);
							byte [] chunkBytes = BitConverter.GetBytes (shortVal);
							
							audioBytes [pos++] = chunkBytes [0];
							audioBytes [pos++] = chunkBytes [1];
						}
					}

					// broadcast the audio data to any listeners
					OnBroadcast?.Invoke (this, audioBytes);
				}
			}
			catch (Exception ex)
			{
				OnException?.Invoke (this, new Exception ($"AudioStream.QueueInputCompleted() :: Error: {ex.Message}"));
			}
		}


		/// <summary>
		/// The bytes that we get from audiograph is in IEEE float, we need to covert that to 16 bit
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		static Int16 FloatToInt16 (float value)
		{
			float f = value * Int16.MaxValue;
			if (f > Int16.MaxValue) f = Int16.MaxValue;
			if (f < Int16.MinValue) f = Int16.MinValue;

			return (Int16) f;
		}


		async void Graph_UnrecoverableErrorOccurred (AudioGraph sender, AudioGraphUnrecoverableErrorOccurredEventArgs args)
		{
			await Stop ();

			throw new Exception ($"UnrecoverableErrorOccurred error: {args.Error}");
		}
	}
}
