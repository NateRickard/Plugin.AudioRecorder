using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace Plugin.AudioRecorder
{
	public partial class AudioRecorderService
	{
		partial void Init () { }

		async Task<string> GetDefaultFilePath ()
		{
			var temporaryFolder = ApplicationData.Current.TemporaryFolder;
			var tempFile = await temporaryFolder.CreateFileAsync (DefaultFileName, CreationCollisionOption.ReplaceExisting);

			return tempFile.Path;
		}

		void OnRecordingStarting ()
		{
		}

		void OnRecordingStopped ()
		{
		}
	}
}
