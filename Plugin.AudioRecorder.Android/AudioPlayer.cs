using Android.Media;
using System;

namespace Plugin.AudioRecorder
{
	public partial class AudioPlayer
	{
		private MediaPlayer mediaPlayer;

		public AudioPlayer ()
		{
		}

		public void Play (string pathToAudioFile)
		{
			if (mediaPlayer != null)
			{
				mediaPlayer.Completion -= MediaPlayer_Completion;
				mediaPlayer.Stop ();
			}

			if (pathToAudioFile != null)
			{
				if (mediaPlayer == null)
				{
					mediaPlayer = new MediaPlayer ();

					mediaPlayer.Prepared += (sender, args) =>
					{
						mediaPlayer.Start ();
						mediaPlayer.Completion += MediaPlayer_Completion;
					};
				}

				mediaPlayer.Reset ();
				//_mediaPlayer.SetVolume (1.0f, 1.0f);

				mediaPlayer.SetDataSource (pathToAudioFile);
				mediaPlayer.PrepareAsync ();
			}
		}

		void MediaPlayer_Completion (object sender, EventArgs e)
		{
			FinishedPlaying?.Invoke (this, EventArgs.Empty);
		}

		public void Pause ()
		{
			mediaPlayer?.Pause ();
		}

		public void Play ()
		{
			mediaPlayer?.Start ();
		}
	}
}
