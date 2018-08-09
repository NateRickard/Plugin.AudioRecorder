using Android.Content;
using Android.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	public partial class AudioRecorderService
	{
		partial void Init ()
		{
			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.JellyBean)
			{
				try
				{
					//if the below call to AudioManager is blocking and never returning/taking forever, ensure the emulator has proper access to the system mic input
					var audioManager = (AudioManager) Android.App.Application.Context.GetSystemService (Context.AudioService);
					var property = audioManager.GetProperty (AudioManager.PropertyOutputSampleRate);

					if (!string.IsNullOrEmpty (property) && int.TryParse (property, out int sampleRate))
					{
						Debug.WriteLine ($"Setting PreferredSampleRate to {sampleRate} as reported by AudioManager.PropertyOutputSampleRate");
						PreferredSampleRate = sampleRate;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine ("Error using AudioManager to get AudioManager.PropertyOutputSampleRate: {0}", ex);
					Debug.WriteLine ("PreferredSampleRate will remain at the default");
				}
			}
		}

		Task<string> GetDefaultFilePath ()
		{
			return Task.FromResult (Path.Combine (Path.GetTempPath (), DefaultFileName));
		}

		void OnRecordingStarting ()
		{
		}

		void OnRecordingStopped ()
		{
		}
	}
}
