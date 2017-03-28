using System;
using Windows.Storage;

namespace Plugin.AudioRecorder
{
    public partial class AudioRecorderService
    {
        async partial void Init()
        {
            StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
            StorageFile tempFile = await temporaryFolder.CreateFileAsync(RecordingFileName, CreationCollisionOption.ReplaceExisting);

            filePath = tempFile.Path;
        }
    }
}