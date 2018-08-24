using AVFoundation;
using Plugin.AudioRecorder;
using System;
using System.Threading.Tasks;
using UIKit;

namespace AudioRecord.iOS
{
	public partial class ViewController : UIViewController
    {
        AudioRecorderService recorder;
		AudioPlayer player;

		public ViewController (IntPtr handle) : base (handle)
		{
			// this controls whether the library will attempt to set the shared AVAudioSession category, and then reset it after recording completes
			AudioRecorderService.RequestAVAudioSessionCategory (AVAudioSessionCategory.PlayAndRecord);
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			recorder = new AudioRecorderService
			{
				StopRecordingOnSilence = TimeoutSwitch.On,
				StopRecordingAfterTimeout = false
			};

			player = new AudioPlayer ();
			player.FinishedPlaying += Player_FinishedPlaying;

			//alternative event-based API can be used here in lieu of the returned recordTask used below
			//recorder.AudioInputReceived += Recorder_AudioInputReceived;
		}

		async partial void RecordButton_TouchUpInside (UIButton sender)
		{
			await RecordAudio ();
		}

		async Task RecordAudio ()
		{
			try
			{
				if (!recorder.IsRecording)
				{
					recorder.StopRecordingOnSilence = TimeoutSwitch.On;

					RecordButton.Enabled = false;
					PlayButton.Enabled = false;

					//the returned Task here will complete once recording is finished
					var recordTask = await recorder.StartRecording ();

					RecordButton.SetTitle ("Stop", UIControlState.Normal);
					RecordButton.Enabled = true;

					var audioFile = await recordTask;

					//audioFile will contain the path to the recorded audio file

					RecordButton.SetTitle ("Record", UIControlState.Normal);
					PlayButton.Enabled = !string.IsNullOrEmpty (audioFile);
				}
				else
				{
					RecordButton.Enabled = false;

					await recorder.StopRecording ();

					RecordButton.SetTitle ("Record", UIControlState.Normal);
					RecordButton.Enabled = true;
				}
			}
			catch (Exception ex)
			{
				//blow up the app!
				throw ex;
			}
		}

		private void Recorder_AudioInputReceived (object sender, string audioFile)
		{
			InvokeOnMainThread (() =>
			{
				RecordButton.SetTitle ("Record", UIControlState.Normal);

				PlayButton.Enabled = !string.IsNullOrEmpty (audioFile);
			});
		}

		partial void PlayButton_TouchUpInside (UIButton sender)
		{
			try
			{
				var filePath = recorder.GetAudioFilePath ();

				if (filePath != null)
				{
					PlayButton.Enabled = false;
					RecordButton.Enabled = false;

					player.Play (filePath);
				}
			}
			catch (Exception ex)
			{
				//blow up the app!
				throw ex;
			}
		}

		void Player_FinishedPlaying (object sender, EventArgs e)
		{
			PlayButton.Enabled = true;
			RecordButton.Enabled = true;
		}
	}
}
