using Plugin.AudioRecorder;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AudioRecord.UWP
{
	public sealed partial class MainPage : Page
    {
		AudioRecorderService recorder;
		AudioPlayer player;

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

			player = new AudioPlayer ();
			player.FinishedPlaying += Player_FinishedPlaying;
		}

		private async void Recorder_AudioInputReceived (object sender, string audioFile)
		{
			await Dispatcher.RunAsync (CoreDispatcherPriority.Normal, () =>
			{
				recordBtn.Icon = new SymbolIcon (Symbol.Microphone);
				recordBtn.Label = "Record";
			});
		}

		private async void RecordBtn_Click (object sender, RoutedEventArgs e)
		{
			if (!recorder.IsRecording)
			{
				playBtn.IsEnabled = false;
				recorder.StopRecordingOnSilence = checkTimeout.IsChecked.Value;

				//the returned Task here will complete once recording is finished
				var recordTask = await recorder.StartRecording ();

				recordBtn.Icon = new SymbolIcon (Symbol.Stop);
				recordBtn.Label = "Stop";

				var audioFile = await recordTask;

				//audioFile will contain the path to the recorded audio file

				recordBtn.Icon = new SymbolIcon (Symbol.Microphone);
				recordBtn.Label = "Record";
				playBtn.IsEnabled = !string.IsNullOrEmpty (audioFile);
			}
			else
			{
				await recorder.StopRecording ();

				recordBtn.Icon = new SymbolIcon (Symbol.Microphone);
				recordBtn.Label = "Record";
			}
		}

		private void PlayBtn_Click (object sender, RoutedEventArgs e)
		{
			PlayRecordedAudio (Dispatcher);
		}

		void PlayRecordedAudio (CoreDispatcher UiDispatcher)
		{
			try
			{
				var filePath = recorder.GetAudioFilePath ();

				if (filePath != null)
				{
					playBtn.IsEnabled = false;
					recordBtn.IsEnabled = false;

					player.Play (filePath);
				}
			}
			catch (Exception ex)
			{
				//blow up the app!
				throw ex;
			}
		}

		private async void Player_FinishedPlaying (object sender, EventArgs e)
		{
			await Dispatcher.RunAsync (CoreDispatcherPriority.Normal, () =>
			{
				playBtn.IsEnabled = true;
				recordBtn.IsEnabled = true;
			});
		}
	}
}