using System;
using AudioToolbox;

namespace Plugin.AudioRecorder
{
	public class AudioStream
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


		public bool Start ()
		{
			var success = this.audioQueue.Start () == AudioQueueStatus.Ok;

			if (success)
			{
				OnActiveChanged?.Invoke (this, true);
			}

			return success;
		}


		public void Stop ()
		{
			this.audioQueue.Stop (true);
			OnActiveChanged?.Invoke (this, false);
		}


		public bool Active {
			get {
				return this.audioQueue.IsRunning;
			}
		}


		public AudioStream (int sampleRate, int bufferSize = DefaultBufferSize)
		{
			this.SampleRate = sampleRate;
			this.bufferSize = bufferSize;
			this.Init ();
		}


		void Init ()
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

			IntPtr bufferPtr;

			for (var index = 0; index < 3; index++)
			{
				audioQueue.AllocateBufferWithPacketDescriptors (bufferByteSize, this.bufferSize, out bufferPtr);
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