using AVFoundation;
using Foundation;
using System;
using System.Diagnostics;

namespace Plugin.AudioRecorder
{
	public partial class AudioPlayer
	{
		AVAudioPlayer _audioPlayer = null;
		NSString currentAVAudioSessionCategory;

		/// <summary>
		/// Set to <c>true</c> in your iOS project if you'd like the <see cref="AudioPlayer"/> to set the shared <see cref="AVAudioSession"/> 
		/// category to <see cref="AVAudioSession.CategoryPlayAndRecord"/> before playing audio and return it to its previous value after playback is complete.  
		/// <see cref="OnPrepareAudioSession"/> and <see cref="OnResetAudioSession"/> will also be called before and after each playback operation to allow for further session configuration.
		/// </summary>
		public static bool ConfigureAVAudioSession { get; set; }

		/// <summary>
		/// If <see cref="ConfigureAVAudioSession"/> is set to <c>true</c>, this Action will be run to configure the <see cref="AVAudioSession"/> before playing audio.
		/// </summary>
		public static Action<AVAudioSession> OnPrepareAudioSession;

		/// <summary>
		/// If <see cref="ConfigureAVAudioSession"/> is set to <c>true</c>, this Action will be run to reset or re-configure the <see cref="AVAudioSession"/> after audio playback is complete.
		/// </summary>
		public static Action<AVAudioSession> OnResetAudioSession;

		public AudioPlayer ()
		{
		}

		public void Play (string pathToAudioFile)
		{
			// Check if _audioPlayer is currently playing
			if (_audioPlayer != null)
			{
				_audioPlayer.FinishedPlaying -= Player_FinishedPlaying;
				_audioPlayer.Stop ();
			}

			if (ConfigureAVAudioSession)
			{
				// does the current AVAudioSession shared config allow playing?
				//	see: https://developer.apple.com/library/archive/documentation/Audio/Conceptual/AudioSessionProgrammingGuide/AudioSessionCategoriesandModes/AudioSessionCategoriesandModes.html#//apple_ref/doc/uid/TP40007875-CH10
				var audioSession = AVAudioSession.SharedInstance ();

				if (audioSession.Category != AVAudioSession.CategoryPlayback &&
					audioSession.Category != AVAudioSession.CategoryPlayAndRecord &&
					audioSession.Category != AVAudioSession.CategoryMultiRoute)
				{
					// track the current category, as long as we haven't already done this (or else we may capture the CategoryPlayAndRecord we're setting below)
					if (currentAVAudioSessionCategory == null)
					{
						currentAVAudioSessionCategory = audioSession.Category;
					}

					if (!audioSession.SetCategory (AVAudioSession.CategoryPlayAndRecord, out NSError err))
					{
						throw new Exception ($"Detected an AVAudioSession category ({currentAVAudioSessionCategory}) that does not support playback, but received error when attempting to set category to {AVAudioSession.CategoryPlayAndRecord}: {err}");
					}
				}

				// allow for additional audio session config
				OnPrepareAudioSession?.Invoke (audioSession);
			}

			string localUrl = pathToAudioFile;
			_audioPlayer = AVAudioPlayer.FromUrl (NSUrl.FromFilename (localUrl));
			_audioPlayer.FinishedPlaying += Player_FinishedPlaying;
			_audioPlayer.Play ();
		}

		void Player_FinishedPlaying (object sender, AVStatusEventArgs e)
		{
			if (ConfigureAVAudioSession)
			{
				var audioSession = AVAudioSession.SharedInstance ();

				if (currentAVAudioSessionCategory != null)
				{
					if (audioSession.SetCategory (currentAVAudioSessionCategory, out NSError err))
					{
						currentAVAudioSessionCategory = null; //reset this if success, otherwise hang onto it to possibly try again
					}
					else
					{
						// we won't error out here as this likely won't prevent us from stopping properly... but we will log an issue
						Debug.WriteLine ($"Error attempting to set the AVAudioSession category back to {currentAVAudioSessionCategory}");
					}
				}

				// allow for additional audio session reset/config
				OnResetAudioSession?.Invoke (audioSession);
			}

			FinishedPlaying?.Invoke (this, EventArgs.Empty);
		}

		public void Pause ()
		{
			_audioPlayer?.Pause ();
		}

		public void Play ()
		{
			_audioPlayer?.Play ();
		}
	}
}
