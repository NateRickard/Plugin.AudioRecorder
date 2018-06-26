using Android.Content;
using Android.Media;
using System.IO;
using System;
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
						System.Diagnostics.Debug.WriteLine ($"Setting PreferredSampleRate to {sampleRate} as reported by AudioManager.PropertyOutputSampleRate");
						PreferredSampleRate = sampleRate;
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine ("Error using AudioManager to get AudioManager.PropertyOutputSampleRate: {0}", ex);
					System.Diagnostics.Debug.WriteLine ("PreferredSampleRate will remain at the default");
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
