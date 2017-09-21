using Plugin.AudioRecorder;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
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
		MediaPlayer audioPlayer;

		public MainPage()
        {
            this.InitializeComponent();

            recorder = new AudioRecorderService
            {
                StopRecordingOnSilence = false,
                StopRecordingAfterTimeout = false
            };

			//alternative event-based API can be used here in lieu of the returned recordTask used below
			//recorder.AudioInputReceived += Recorder_AudioInputReceived;

			audioPlayer = new MediaPlayer ();
			audioPlayer.MediaEnded += AudioPlayer_MediaEnded;
		}

		private async void Recorder_AudioInputReceived(object sender, string audioFile)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                recordBtn.Icon = new SymbolIcon(Symbol.Microphone);
                recordBtn.Label = "Record";
            });
        }


        private async void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!recorder.IsRecording)
            {
				playBtn.IsEnabled = false;
				recorder.StopRecordingOnSilence = checkTimeout.IsChecked.Value;

				//the returned Task here will complete once recording is finished
                var recordTask = await recorder.StartRecording();

                recordBtn.Icon = new SymbolIcon(Symbol.Stop);
                recordBtn.Label = "Stop";

				var audioFile = await recordTask;

				//audioFile will contain the path to the recorded audio file

				recordBtn.Icon = new SymbolIcon (Symbol.Microphone);
				recordBtn.Label = "Record";
				playBtn.IsEnabled = !string.IsNullOrEmpty (audioFile);
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


		async Task PlayRecordedAudio (CoreDispatcher UiDispatcher)
		{
			playBtn.IsEnabled = false;
			recordBtn.IsEnabled = false;

			StorageFolder temporaryFolder = ApplicationData.Current.TemporaryFolder;
			var fileName = recorder.GetAudioFilePath ();

			//await UiDispatcher.RunAsync (CoreDispatcherPriority.Normal, async () =>
			//{
				var storageFile = await StorageFile.GetFileFromPathAsync (fileName);

				//IRandomAccessStream stream = await storageFile.OpenAsync (FileAccessMode.Read);
				audioPlayer.Source = MediaSource.CreateFromStorageFile (storageFile);
				audioPlayer.Play ();
			//});
		}


		private async void AudioPlayer_MediaEnded (MediaPlayer sender, object args)
		{
			await Dispatcher.RunAsync (CoreDispatcherPriority.Normal, () =>
			{
				playBtn.IsEnabled = true;
				recordBtn.IsEnabled = true;
			});
		}
	}
}