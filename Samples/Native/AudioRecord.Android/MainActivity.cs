using Android.App;
using Android.Widget;
using Android.OS;
using Plugin.AudioRecorder;
using System.Threading.Tasks;
using System;
using Android.Media;

namespace AudioRecord.Droid
{
	[Activity (Label = "AudioRecord.Android", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		AudioRecorderService recorder;
		MediaPlayer player;

		Button recordButton;
		Button playButton;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			recordButton = FindViewById<Button> (Resource.Id.recordButton);
			playButton = FindViewById<Button> (Resource.Id.playButton);

			recordButton.Click += RecordButton_Click;
			playButton.Click += PlayButton_Click;
		}


		protected override void OnStart ()
		{
			base.OnStart ();

			recorder = new AudioRecorderService
			{
				StopRecordingOnSilence = false,
				StopRecordingAfterTimeout = false
			};

			//alternative event-based API can be used here in lieu of the returned recordTask used below
			//recorder.AudioInputReceived += Recorder_AudioInputReceived;

			player = new MediaPlayer ();
			player.Completion += Player_Completion;

			recordButton.Enabled = true;
		}


		async void RecordButton_Click (object sender, EventArgs e)
		{
			await RecordAudio ();
		}


		async Task RecordAudio ()
		{
			try
			{
				if (!recorder.IsRecording)
				{
					var checkTimeout = FindViewById<CheckBox> (Resource.Id.checkBoxTimeout);
					recorder.StopRecordingOnSilence = checkTimeout.Checked;

					recordButton.Enabled = false;
					playButton.Enabled = false;

					//the returned Task here will complete once recording is finished
					var recordTask = await recorder.StartRecording ();

					recordButton.Text = "Stop";
					recordButton.Enabled = true;

					var audioFile = await recordTask;

					//audioFile will contain the path to the recorded audio file

					recordButton.Text = "Record";

					playButton.Enabled = !string.IsNullOrEmpty (audioFile);
				}
				else
				{
					recordButton.Enabled = false;

					await recorder.StopRecording ();

					recordButton.Text = "Record";
					recordButton.Enabled = true;
				}
			}
			catch (Exception ex)
			{
				//blow up the app!
				throw ex;
			}
		}


		void Recorder_AudioInputReceived (object sender, string audioFile)
		{
			RunOnUiThread (() =>
			{
				recordButton.Text = "Record";

				playButton.Enabled = !string.IsNullOrEmpty (audioFile);
			});
		}


		async void PlayButton_Click (object sender, EventArgs e)
		{
			await PlayRecordedAudio ();
		}


		async Task PlayRecordedAudio ()
		{
			try
			{
				recordButton.Enabled = false;
				playButton.Enabled = false;

				var filePath = recorder.GetAudioFilePath ();

				if (filePath != null)
				{
					player.Reset ();
					await player.SetDataSourceAsync (filePath);
					player.Prepare ();
					player.Start ();
				}
			}
			catch (Exception ex)
			{
				//blow up the app!
				throw ex;
			}
		}


		private void Player_Completion (object sender, EventArgs e)
		{
			player.Stop ();

			recordButton.Enabled = true;
			playButton.Enabled = true;
		}
	}
}