using System;
using Windows.Storage;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	public partial class AudioRecorderService
	{
		partial void Init () { }

		async Task<string> GetDefaultFilePath ()
		{
			StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
			StorageFile tempFile = await temporaryFolder.CreateFileAsync(DefaultFileName, CreationCollisionOption.ReplaceExisting);
			return tempFile.Path;
		}
	}
}
