using Plugin.AudioRecorder;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AudioRecord.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AudioRecorderService recorder;

        public MainPage()
        {
            this.InitializeComponent();

            recorder = new AudioRecorderService
            {
                StopRecordingOnSilence = false,
                StopRecordingAfterTimeout = false
            };

            recorder.AudioInputReceived += Recorder_AudioInputReceived;
        }

        private async void Recorder_AudioInputReceived(object sender, string audioFile)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                recordBtn.Icon = new SymbolIcon(Symbol.Microphone);
                recordBtn.Label = "Record";
            });
        }

        async Task PlayRecordedAudio(CoreDispatcher UiDispatcher)
        {
            MediaElement playback = new MediaElement();
            StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
            var fileName = recorder.GetFilename();

            await UiDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(fileName);

                IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read);
                playback.SetSource(stream, storageFile.FileType);
                playback.Play();
            });
        }


        private async void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!recorder.IsRecording)
            {
                recorder.StopRecordingOnSilence = checkTimeout.IsChecked.Value;

                await recorder.StartRecording();

                recordBtn.Icon = new SymbolIcon(Symbol.Stop);
                recordBtn.Label = "Stop";
            }
            else
            {
                await recorder.StopRecording();

                recordBtn.Icon = new SymbolIcon(Symbol.Microphone);
                recordBtn.Label = "Record";
            }
        }


        private async void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            await PlayRecordedAudio(Dispatcher);
        }
    }
}