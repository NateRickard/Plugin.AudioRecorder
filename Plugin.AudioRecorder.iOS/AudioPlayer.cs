using AVFoundation;
using Foundation;
using System;
using System.Diagnostics;

namespace Plugin.AudioRecorder
{
	public partial class AudioPlayer
	{
		AVAudioPlayer audioPlayer = null;
		NSString currentAVAudioSessionCategory;

		static AVAudioSessionCategory? requestedAVAudioSessionCategory;

		/// <summary>
		/// If <see cref="RequestAVAudioSessionCategory"/> is used to request an AVAudioSession category, this Action will also be run to configure the <see cref="AVAudioSession"/> before playing audio.
		/// </summary>
		public static Action<AVAudioSession> OnPrepareAudioSession;

		/// <summary>
		/// If <see cref="RequestAVAudioSessionCategory"/> is used to request an AVAudioSession category, this Action will also be run to reset or re-configure the <see cref="AVAudioSession"/> after audio playback is complete.
		/// </summary>
		public static Action<AVAudioSession> OnResetAudioSession;

		public AudioPlayer ()
		{
		}

		/// <summary>
		/// Call this method in your iOS project if you'd like the <see cref="AudioPlayer"/> to set the shared <see cref="AVAudioSession"/> 
		/// category to the requested <paramref name="category"/> before playing audio and return it to its previous value after playback is complete.  
		/// <see cref="OnPrepareAudioSession"/> and <see cref="OnResetAudioSession"/> will also be called before and after each playback operation to allow for further session configuration.  
		/// Note that some categories do not support playback.
		/// </summary>
		public static void RequestAVAudioSessionCategory (AVAudioSessionCategory category)
		{
			requestedAVAudioSessionCategory = category;
		}

		public void Play (string pathToAudioFile)
		{
			// Check if _audioPlayer is currently playing
			if (audioPlayer != null)
			{
				audioPlayer.FinishedPlaying -= Player_FinishedPlaying;
				audioPlayer.Stop ();
			}

			if (requestedAVAudioSessionCategory.HasValue)
			{
				// If the user has called RequestAVAudioSessionCategory(), let's attempt to set that category for them
				//	see: https://developer.apple.com/library/archive/documentation/Audio/Conceptual/AudioSessionProgrammingGuide/AudioSessionCategoriesandModes/AudioSessionCategoriesandModes.html#//apple_ref/doc/uid/TP40007875-CH10
				var audioSession = AVAudioSession.SharedInstance ();

				if (!audioSession.Category.ToString ().EndsWith (requestedAVAudioSessionCategory.Value.ToString ()))
				{
					// track the current category, as long as we haven't already done this (or else we may capture the category we're setting below)
					if (currentAVAudioSessionCategory == null)
					{
						currentAVAudioSessionCategory = audioSession.Category;
					}

					var err = audioSession.SetCategory (requestedAVAudioSessionCategory.Value);

					if (err != null)
					{
						throw new Exception ($"Current AVAudioSession category is ({currentAVAudioSessionCategory}); Application requested an AVAudioSession category of {requestedAVAudioSessionCategory.Value} but received error when attempting to set it: {err}");
					}
				}

				// allow for additional audio session config
				OnPrepareAudioSession?.Invoke (audioSession);
			}

			string localUrl = pathToAudioFile;
			audioPlayer = AVAudioPlayer.FromUrl (NSUrl.FromFilename (localUrl));
			audioPlayer.FinishedPlaying += Player_FinishedPlaying;
			audioPlayer.Play ();
		}

		void Player_FinishedPlaying (object sender, AVStatusEventArgs e)
		{
			if (currentAVAudioSessionCategory != null)
			{
				var audioSession = AVAudioSession.SharedInstance ();

				if (audioSession.SetCategory (currentAVAudioSessionCategory, out NSError err))
				{
					currentAVAudioSessionCategory = null; //reset this if success, otherwise hang onto it to possibly try again
				}
				else
				{
					// we won't error out here as this likely won't prevent us from stopping properly... but we will log an issue
					Debug.WriteLine ($"Error attempting to set the AVAudioSession category back to {currentAVAudioSessionCategory} :: {err}");
				}

				// allow for additional audio session reset/config
				OnResetAudioSession?.Invoke (audioSession);
			}

			FinishedPlaying?.Invoke (this, EventArgs.Empty);
		}

		public void Pause ()
		{
			audioPlayer?.Pause ();
		}

		public void Play ()
		{
			audioPlayer?.Play ();
		}
	}
}
