using AudioToolbox;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	internal class AudioStream : IAudioStream
	{
		const int CountAudioBuffers = 3;
		const int MaxBufferSize = 0x50000; // 320 KB
		const float TargetMeasurementTime = 100F; // milliseconds

		InputAudioQueue audioQueue;

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
		public int SampleRate
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the channel count.  Currently always 1 (Mono).
		/// </summary>
		/// <value>
		/// The channel count.
		/// </value>
		public int ChannelCount => 1;

		/// <summary>
		/// Gets bits per sample.  Currently always 16 (bits).
		/// </summary>
		public int BitsPerSample => 16;

		/// <summary>
		/// Gets a value indicating if the audio stream is active.
		/// </summary>
		public bool Active => audioQueue?.IsRunning ?? false;

		/// <summary>
		/// Wrapper function to run success/failure callbacks from an operation that returns an AudioQueueStatus.
		/// </summary>
		/// <param name="bufferFn">The function that returns AudioQueueStatus.</param>
		/// <param name="successAction">The Action to run if the result is AudioQueueStatus.Ok.</param>
		/// <param name="failAction">The Action to run if the result is anything other than AudioQueueStatus.Ok.</param>
		void BufferOperation (Func<AudioQueueStatus> bufferFn, Action successAction = null, Action<AudioQueueStatus> failAction = null)
		{
			var status = bufferFn ();

			if (status == AudioQueueStatus.Ok)
			{
				successAction?.Invoke ();
			}
			else
			{
				if (failAction != null)
				{
					failAction (status);
				}
				else
				{
					throw new Exception ($"AudioStream buffer error :: buffer operation returned non - Ok status:: {status}");
				}
			}
		}

		/// <summary>
		/// Starts the audio stream.
		/// </summary>
		public Task Start ()
		{
			try
			{
				if (!Active)
				{
					InitAudioQueue ();

					BufferOperation (() => audioQueue.Start (),
						() => OnActiveChanged?.Invoke (this, true),
						status => throw new Exception ($"audioQueue.Start() returned non-OK status: {status}"));
				}

				return Task.FromResult (true);
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in AudioStream.Start(): {0}", ex.Message);

				Stop ();
				throw;
			}
		}

		/// <summary>
		/// Stops the audio stream.
		/// </summary>
		public Task Stop ()
		{
			if (audioQueue != null)
			{
				audioQueue.InputCompleted -= QueueInputCompleted;

				if (audioQueue.IsRunning)
				{
					BufferOperation (() => audioQueue.Stop (true),
						() => OnActiveChanged?.Invoke (this, false),
						status => Debug.WriteLine ("AudioStream.Stop() :: audioQueue.Stop returned non OK result: {0}", status));
				}

				audioQueue.Dispose ();
				audioQueue = null;
			}

			return Task.FromResult (true);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AudioStream"/> class.
		/// </summary>
		/// <param name="sampleRate">Sample rate.</param>
		public AudioStream (int sampleRate)
		{
			SampleRate = sampleRate;
		}

		void InitAudioQueue ()
		{
			// create our audio queue & configure buffers
			var audioFormat = AudioStreamBasicDescription.CreateLinearPCM (SampleRate, (uint) ChannelCount, (uint) BitsPerSample);

			audioQueue = new InputAudioQueue (audioFormat);
			audioQueue.InputCompleted += QueueInputCompleted;

			// calculate our buffer size and make sure it's not too big
			var bufferByteSize = (int) (TargetMeasurementTime / 1000F/*ms to sec*/ * SampleRate * audioFormat.BytesPerPacket);
			bufferByteSize = bufferByteSize < MaxBufferSize ? bufferByteSize : MaxBufferSize;

			for (var index = 0; index < CountAudioBuffers; index++)
			{
				var bufferPtr = IntPtr.Zero;

				BufferOperation (() => audioQueue.AllocateBuffer (bufferByteSize, out bufferPtr), () =>
				 {
					 BufferOperation (() => audioQueue.EnqueueBuffer (bufferPtr, bufferByteSize, null), () => Debug.WriteLine ("AudioQueue buffer enqueued :: {0} of {1}", index + 1, CountAudioBuffers));
				 });
			}
		}

		/// <summary>
		/// Handles iOS audio buffer queue completed message.
		/// </summary>
		/// <param name='sender'>Sender object</param>
		/// <param name='e'> Input completed parameters.</param>
		void QueueInputCompleted (object sender, InputCompletedEventArgs e)
		{
			try
			{
				// we'll only broadcast if we're actively monitoring audio packets
				if (!Active)
				{
					return;
				}

				if (e.Buffer.AudioDataByteSize > 0)
				{
					var audioBytes = new byte [e.Buffer.AudioDataByteSize];
					Marshal.Copy (e.Buffer.AudioData, audioBytes, 0, (int) e.Buffer.AudioDataByteSize);

					// broadcast the audio data to any listeners
					OnBroadcast?.Invoke (this, audioBytes);

					// check if active again, because the auto stop logic may stop the audio queue from within this handler!
					if (Active)
					{
						BufferOperation (() => audioQueue.EnqueueBuffer (e.IntPtrBuffer, null), null, status =>
						 {
							 Debug.WriteLine ("AudioStream.QueueInputCompleted() :: audioQueue.EnqueueBuffer returned non-Ok status :: {0}", status);
							 OnException?.Invoke (this, new Exception ($"audioQueue.EnqueueBuffer returned non-Ok status :: {status}"));
						 });
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("AudioStream.QueueInputCompleted() :: Error: {0}", ex.Message);

				OnException?.Invoke (this, new Exception ($"AudioStream.QueueInputCompleted() :: Error: {ex.Message}"));
			}
		}

		/// <summary>
		/// Flushes any audio bytes in memory but not yet broadcast out to any listeners.
		/// </summary>
		public void Flush ()
		{
			// not needed for this implementation
		}
	}
}
