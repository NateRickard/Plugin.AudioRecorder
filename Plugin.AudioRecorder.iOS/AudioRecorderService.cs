using AVFoundation;
using Foundation;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	public partial class AudioRecorderService
	{
		NSString currentAVAudioSessionCategory;

		public static bool ConfigureAVAudioSession { get; set; }

		partial void Init () { }

		Task<string> GetDefaultFilePath ()
		{
			return Task.FromResult (Path.Combine (Path.GetTempPath (), DefaultFileName));
		}

		void OnRecordingStarting ()
		{
			if (ConfigureAVAudioSession)
			{
				// does the current AVAudioSession shared config allow recording?
				//	see: https://developer.apple.com/library/archive/documentation/Audio/Conceptual/AudioSessionProgrammingGuide/AudioSessionCategoriesandModes/AudioSessionCategoriesandModes.html#//apple_ref/doc/uid/TP40007875-CH10
				var audioSession = AVAudioSession.SharedInstance ();

				if (audioSession.Category != AVAudioSession.CategoryRecord &&
					audioSession.Category != AVAudioSession.CategoryPlayAndRecord &&
					audioSession.Category != AVAudioSession.CategoryMultiRoute)
				{
					// track the current category, as long as we haven't already done this (or else we may capture the CategoryRecord we're setting below)
					if (currentAVAudioSessionCategory == null)
					{
						currentAVAudioSessionCategory = audioSession.Category;
					}

					if (!audioSession.SetCategory (AVAudioSession.CategoryRecord, out NSError err))
					{
						throw new Exception ($"Detected an AVAudioSession category ({currentAVAudioSessionCategory}) that does not support recording, but received error when attempting to set category to {AVAudioSession.CategoryRecord}: {err}");
					}
				}
			}
		}

		void OnRecordingStopped ()
		{
			if (ConfigureAVAudioSession)
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
						System.Diagnostics.Debug.WriteLine ($"Error attempting to set the AVAudioSession category back to {currentAVAudioSessionCategory}");
					}
				}
			}
		}
	}
}
