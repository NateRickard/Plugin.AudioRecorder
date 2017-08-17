using System;
using AudioToolbox;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	internal class AudioStream : IAudioStream
    {
		const int DefaultBufferSize = 640;
		readonly int bufferSize;

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
		public int SampleRate {
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
        /// Starts the audio stream.
        /// </summary>
		public Task Start()
        {
            try
            {
                if (!Active)
                {
					initAudioQueue ();

					var result = audioQueue.Start();

					if (result == AudioQueueStatus.Ok)
                    {
						OnActiveChanged?.Invoke(this, true);
                    }
                    else
                    {
                        throw new Exception($"audioQueue.Start() returned non-OK status: {result}");
                    }
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in AudioStream.Start(): {0}", ex);
                throw;
            }
		}


		/// <summary>
		/// Stops the audio stream.
		/// </summary>
		public Task Stop ()
		{
			audioQueue.InputCompleted -= QueueInputCompleted;

			var result = audioQueue.Stop (true);

			audioQueue.Dispose ();
			audioQueue = null;

			if (result == AudioQueueStatus.Ok)
			{
				OnActiveChanged?.Invoke (this, false);
			}
			else
			{
				System.Diagnostics.Debug.WriteLine ("AudioStream.Stop() :: audioQueue.Stop returned non OK result: {0}", result);
			}

			return Task.FromResult (true);
		}


        /// <summary>
		/// Initializes a new instance of the <see cref="AudioStream"/> class.
		/// </summary>
		/// <param name="sampleRate">Sample rate.</param>
		/// <param name="bufferSize">The buffer size used to read from the stream.  This can typically be left to the default.</param>
		public AudioStream (int sampleRate, int bufferSize = DefaultBufferSize)
		{
			SampleRate = sampleRate;
			this.bufferSize = bufferSize;
		}


		void initAudioQueue ()
		{
			var audioFormat = new AudioStreamBasicDescription
			{
				SampleRate = SampleRate,
				Format = AudioFormatType.LinearPCM,
				FormatFlags = AudioFormatFlags.LinearPCMIsSignedInteger | AudioFormatFlags.LinearPCMIsPacked,
				FramesPerPacket = 1,
				ChannelsPerFrame = 1,
				BitsPerChannel = BitsPerSample,
				BytesPerPacket = 2,
				BytesPerFrame = 2,
				Reserved = 0
			};

			audioQueue = new InputAudioQueue (audioFormat);
			audioQueue.InputCompleted += QueueInputCompleted;

			var bufferByteSize = bufferSize * audioFormat.BytesPerPacket;

			for (var index = 0; index < 3; index++)
			{
				audioQueue.AllocateBufferWithPacketDescriptors (bufferByteSize, bufferSize, out IntPtr bufferPtr);
				audioQueue.EnqueueBuffer (bufferPtr, bufferByteSize, null);
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

				//copy data from the audio queue to a byte buffer
				var buffer = (AudioQueueBuffer) System.Runtime.InteropServices.Marshal.PtrToStructure (e.IntPtrBuffer, typeof (AudioQueueBuffer));
				var audioBytes = new byte [buffer.AudioDataByteSize];
				System.Runtime.InteropServices.Marshal.Copy (buffer.AudioData, audioBytes, 0, (int) buffer.AudioDataByteSize);

				//broadcast the audio data to any listeners
				OnBroadcast?.Invoke (this, audioBytes);

				//check for null/active again, because the auto stop logic may stop the audio queue from within this handler!
				if (Active)
				{
					var status = audioQueue.EnqueueBuffer (e.IntPtrBuffer, bufferSize, e.PacketDescriptions);

					if (status != AudioQueueStatus.Ok)
					{
						System.Diagnostics.Debug.WriteLine ("AudioStream.QueueInputCompleted() :: audioQueue.EnqueueBuffer returned non-Ok status :: {0}", status);
						OnException?.Invoke (this, new Exception ($"audioQueue.EnqueueBuffer returned non-Ok status :: {status}"));
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ("AudioStream.QueueInputCompleted() :: Error: {0}", ex);
			}
		}
	}
}