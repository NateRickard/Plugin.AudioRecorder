using System.IO;

namespace Plugin.AudioRecorder
{
    public partial class AudioRecorderService
    {
        partial void Init()
        {
            filePath = Path.Combine(Path.GetTempPath(), RecordingFileName);
        }
    }
}