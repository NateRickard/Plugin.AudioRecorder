using Android.Media;
using System;

namespace Plugin.AudioRecorder
{
	public partial class AudioPlayer
	{
		private MediaPlayer _mediaPlayer;

		public AudioPlayer ()
		{
		}

		public void Play (string pathToAudioFile)
		{
			if (_mediaPlayer != null)
			{
				_mediaPlayer.Completion -= MediaPlayer_Completion;
				_mediaPlayer.Stop ();
			}

			if (pathToAudioFile != null)
			{
				if (_mediaPlayer == null)
				{
					_mediaPlayer = new MediaPlayer ();

					_mediaPlayer.Prepared += (sender, args) =>
					{
						_mediaPlayer.Start ();
						_mediaPlayer.Completion += MediaPlayer_Completion;
					};
				}

				_mediaPlayer.Reset ();
				//_mediaPlayer.SetVolume (1.0f, 1.0f);

				_mediaPlayer.SetDataSource (pathToAudioFile);
				_mediaPlayer.PrepareAsync ();
			}
		}

		void MediaPlayer_Completion (object sender, EventArgs e)
		{
			FinishedPlaying?.Invoke (this, EventArgs.Empty);
		}

		public void Pause ()
		{
			_mediaPlayer?.Pause ();
		}

		public void Play ()
		{
			_mediaPlayer?.Start ();
		}
	}
}