using AVFoundation;
using Foundation;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	public partial class AudioRecorderService
	{
		NSString currentAVAudioSessionCategory;

		static AVAudioSessionCategory? requestedAVAudioSessionCategory;

		/// <summary>
		/// If <see cref="RequestAVAudioSessionCategory"/> is used to request an AVAudioSession category, this Action will also be run to configure the <see cref="AVAudioSession"/> before recording audio.
		/// </summary>
		public static Action<AVAudioSession> OnPrepareAudioSession;

		/// <summary>
		/// If <see cref="RequestAVAudioSessionCategory"/> is used to request an AVAudioSession category, this Action will also be run to reset or re-configure the <see cref="AVAudioSession"/> after audio recording is complete.
		/// </summary>
		public static Action<AVAudioSession> OnResetAudioSession;

		partial void Init () { }

		/// <summary>
		/// Call this method in your iOS project if you'd like the <see cref="AudioRecorderService"/> to attempt to set the shared <see cref="AVAudioSession"/> 
		/// category to the requested <paramref name="category"/> before recording audio and return it to its previous value after recording is complete.  
		/// The default category used will be <see cref="AVAudioSessionCategory.PlayAndRecord"/>.  Note that some categories do not support recording.
		/// </summary>
		public static void RequestAVAudioSessionCategory (AVAudioSessionCategory category = AVAudioSessionCategory.PlayAndRecord)
		{
			requestedAVAudioSessionCategory = category;
		}

		Task<string> GetDefaultFilePath ()
		{
			return Task.FromResult (Path.Combine (Path.GetTempPath (), DefaultFileName));
		}

		void OnRecordingStarting ()
		{
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
		}

		void OnRecordingStopped ()
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
		}
	}
}
