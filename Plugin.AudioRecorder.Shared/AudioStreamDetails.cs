namespace Plugin.AudioRecorder
{
	/// <summary>
	/// Represents the details of an <see cref="IAudioStream"/>, including channel count, sample rate, and bits per sample.
	/// </summary>
	public class AudioStreamDetails
	{
		/// <summary>
		/// Gets the sample rate of the underlying audio stream.
		/// </summary>
		public int SampleRate { get; set; }

		/// <summary>
		/// Gets the channel count of the underlying audio stream.
		/// </summary>
		public int ChannelCount { get; set; }

		/// <summary>
		/// Gets the bits per sample of the underlying audio stream.
		/// </summary>
		public int BitsPerSample { get; set; }
	}
}
