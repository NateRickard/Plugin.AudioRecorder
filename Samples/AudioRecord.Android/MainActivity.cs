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
		SoundPool soundPool;

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


		protected override async void OnStart ()
		{
			base.OnStart ();

			//this can actually take a longggg time in the case that there are issues accessing the AudioManager
			//	in this case the app will essentially be unusable (it's typically an issue accessing the mic, etc., from the emulator)
			//	but at least the UI can/will load by throwing this on a background thread

			await Task.Run (() =>
			 {
				 recorder = new AudioRecorderService
				 {
					 StopRecordingOnSilence = false,
					 StopRecordingAfterTimeout = false
				 };

				 recorder.AudioInputReceived += Recorder_AudioInputReceived;

				 soundPool = new SoundPool (1, Stream.Music, 0);
			 });

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

					await recorder.StartRecording ();

					recordButton.Text = "Stop";
					recordButton.Enabled = true;
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
				var fileName = recorder.GetFilename ();

				var soundId = await soundPool.LoadAsync (fileName, 1);

				var playResult = -1;

				while (playResult <= 0) //so hacky, but... sample app
				{
					await Task.Delay (200);
					playResult = soundPool.Play (soundId, .99f, .99f, 0, 0, 1);
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