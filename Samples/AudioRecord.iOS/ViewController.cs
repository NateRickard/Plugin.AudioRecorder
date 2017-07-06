using AVFoundation;
using Foundation;
using Plugin.AudioRecorder;
using System;
using System.Threading.Tasks;
using UIKit;

namespace AudioRecord.iOS
{
    public partial class ViewController : UIViewController
    {
		AudioRecorderService recorder;
		AVAudioPlayer player;

		public ViewController(IntPtr handle) : base(handle)
        {
        }


        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.

			recorder = new AudioRecorderService
			{
				StopRecordingOnSilence = false,
				StopRecordingAfterTimeout = false
			};

			recorder.AudioInputReceived += Recorder_AudioInputReceived;
		}


		public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
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

					await recorder.StartRecording ();

					RecordButton.SetTitle ("Stop", UIControlState.Normal);
					RecordButton.Enabled = true;
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
				using (var session = AVAudioSession.SharedInstance ())
				{
					session.SetCategory (AVAudioSessionCategory.Ambient);
					session.SetActive (true);

					if (player != null)
					{
						player.Stop ();
						player.Dispose ();
					}

					var uri = new NSUrl (recorder.GetFilename ());
					NSError err;

					player = new AVAudioPlayer (uri, "wav", out err);

					if (err != null) throw new Exception (err.Description);

					player.NumberOfLoops = 0;
					player.Play ();
					//player.FinishedPlaying += Player_FinishedPlaying;
				}
			}
			catch (Exception ex)
			{
				//blow up the app!
				throw ex;
			}
		}
	}
}