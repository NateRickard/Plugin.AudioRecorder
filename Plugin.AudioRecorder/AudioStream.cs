using System;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	// Dummy implementation - Bait & Switch will be used to force apps to use platform specific dlls.
	internal class AudioStream : IAudioStream
	{
		public int SampleRate => throw new NotImplementedException ();

		public int ChannelCount => throw new NotImplementedException ();

		public int BitsPerSample => throw new NotImplementedException ();

		public bool Active => throw new NotImplementedException ();

		public event EventHandler<byte []> OnBroadcast;
		public event EventHandler<bool> OnActiveChanged;
		public event EventHandler<Exception> OnException;

		public AudioStream (int sampleRate)
		{
		}

		public Task Start ()
		{
			throw new NotImplementedException ();
		}

		public Task Stop ()
		{
			throw new NotImplementedException ();
		}

		public void Flush ()
		{
			throw new NotImplementedException ();
		}
	}
}
