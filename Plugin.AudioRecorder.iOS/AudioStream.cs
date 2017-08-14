using System;
using AudioToolbox;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	internal class AudioStream : IAudioStream
    {
		const int DefaultBufferSize = 640;

		InputAudioQueue audioQueue;

		readonly int bufferSize;

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
		/// Gets the channel count.
		/// </summary>
		/// <value>
		/// The channel count.
		/// </value>
		public int ChannelCount {
			get {
				return 1;
			}
		}


		/// <summary>
		/// Gets bits per sample.
		/// </summary>
		public int BitsPerSample {
			get {
				return 16;
			}
		}


        /// <summary>
        /// Gets a value indicating if the audio stream is active.
        /// </summary>
		public bool Active {
            get {
                return this.audioQueue.IsRunning;
            }
        }


        /// <summary>
        /// Starts the audio stream.
        /// </summary>
		public Task Start()
        {
            try
            {
                if (!Active)
                {
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
			audioQueue.Stop (true);
			OnActiveChanged?.Invoke (this, false);

            return Task.FromResult(true);
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
			init ();
		}


		void init ()
		{
			var audioFormat = new AudioStreamBasicDescription
			{
				SampleRate = this.SampleRate,
				Format = AudioFormatType.LinearPCM,
				FormatFlags = AudioFormatFlags.LinearPCMIsSignedInteger | AudioFormatFlags.LinearPCMIsPacked,
				FramesPerPacket = 1,
				ChannelsPerFrame = 1,
				BitsPerChannel = this.BitsPerSample,
				BytesPerPacket = 2,
				BytesPerFrame = 2,
				Reserved = 0
			};

			audioQueue = new InputAudioQueue (audioFormat);
			audioQueue.InputCompleted += QueueInputCompleted;

			var bufferByteSize = this.bufferSize * audioFormat.BytesPerPacket;

			for (var index = 0; index < 3; index++)
			{
				audioQueue.AllocateBufferWithPacketDescriptors (bufferByteSize, this.bufferSize, out IntPtr bufferPtr);
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
			// return if we aren't actively monitoring audio packets
			if (!this.Active)
			{
				return;
			}

			var buffer = (AudioQueueBuffer)System.Runtime.InteropServices.Marshal.PtrToStructure (e.IntPtrBuffer, typeof (AudioQueueBuffer));

			var send = new byte [buffer.AudioDataByteSize];
			System.Runtime.InteropServices.Marshal.Copy (buffer.AudioData, send, 0, (int)buffer.AudioDataByteSize);

			this.OnBroadcast?.Invoke (this, send);

			var status = audioQueue.EnqueueBuffer (e.IntPtrBuffer, this.bufferSize, e.PacketDescriptions);

			if (status != AudioQueueStatus.Ok)
			{
				OnException?.Invoke (this, new Exception ($"audioQueue.EnqueueBuffer returned non-Ok status :: {status}"));
			}
		}
	}
}