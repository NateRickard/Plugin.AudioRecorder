using System;
using System.IO;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	public partial class AudioRecorderService
	{
		WaveRecorder recorder;
		AudioStream audioStream;

		bool audioDetected;
		DateTime? silenceTime;
		DateTime? startTime;

		const string RecordingFileName = "recording.wav";

		public int PreferredSampleRate { get; set; } = 44100;

		public TimeSpan AudioSilenceTimeout { get; set; } = TimeSpan.FromSeconds (2);

		public TimeSpan TotalAudioTimeout { get; set; } = TimeSpan.FromSeconds (30);

		public bool StopRecordingOnSilence { get; set; } = true;

		public bool StopRecordingAfterTimeout { get; set; } = true;

		public float SilenceThreshold { get; set; } = .2f;

		public event EventHandler<string> AudioInputReceived;


		public void StartRecording ()
		{
			ResetAudioDetection ();

			InitializeStream (PreferredSampleRate);

			if (!recorder.StartRecorder (audioStream, GetFilename ()))
			{
				throw new Exception ("AudioStream failed to start: busy?");
			}

			startTime = DateTime.Now;

			System.Diagnostics.Debug.WriteLine ("AudioRecorderService.StartRecording() complete.  Audio is being recorded.");
		}


		void ResetAudioDetection ()
		{
			audioDetected = false;
			silenceTime = null;
			startTime = null;
		}


		void AudioStream_OnBroadcast (object sender, byte [] bytes)
		{
			var level = AudioFunctions.CalculateLevel (bytes);

			//System.Diagnostics.Debug.WriteLine ("AudioStream_OnBroadcast :: calculateLevel == {0}", level);

			if (level > SilenceThreshold) //did we find a signal?
			{
				audioDetected = true;
				silenceTime = null;
			}
			else //no audio detected
			{
				//see if we've detected 'near' silence for more than <audioTimeout>
				if (StopRecordingOnSilence && silenceTime.HasValue)
				{
					if (DateTime.Now.Subtract (silenceTime.Value) > AudioSilenceTimeout)
					{
						System.Diagnostics.Debug.WriteLine ("AudioRecorderService.AudioStream_OnBroadcast(): AudioSilenceTimeout exceeded, stopping recording");
						StopRecording ();
						return;
					}
				}
				else
				{
					silenceTime = DateTime.Now;
				}
			}

			if (StopRecordingAfterTimeout && DateTime.Now - startTime > TotalAudioTimeout)
			{
				System.Diagnostics.Debug.WriteLine ("AudioRecorderService.AudioStream_OnBroadcast(): TotalAudioTimeout exceeded, stopping recording");
				StopRecording ();
			}
		}


		public void StopRecording (bool continueProcessing = true)
		{
			audioStream.OnBroadcast -= AudioStream_OnBroadcast;

			Task.Run (() =>
			{
				try
				{
					recorder.StopRecorder ();
					audioStream.Stop ();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine (ex.Message);
				}

				if (continueProcessing)
				{
					System.Diagnostics.Debug.WriteLine ("AudioRecorderService.StopRecording(): Recording stopped, raising AudioInputReceived event");

					AudioInputReceived?.Invoke (this, audioDetected ? GetFilename () : null);
				}
			});
		}

		void InitializeStream (int sampleRate)
		{
			try
			{
				if (audioStream != null)
				{
					audioStream.OnBroadcast -= AudioStream_OnBroadcast;
				}
				else
				{
					audioStream = new AudioStream (sampleRate);
				}

				if (StopRecordingOnSilence)
				{
					audioStream.OnBroadcast += AudioStream_OnBroadcast;
				}

				if (recorder == null)
				{
					recorder = new WaveRecorder ();
				}

				System.Diagnostics.Debug.WriteLine ("AudioRecorderService.InitializeStream() complete.  Audio stream is initialized.");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ("Error: {0}", ex);
			}
		}


		string GetFilename ()
		{
			return Path.Combine (Path.GetTempPath (), RecordingFileName);
		}
	}
}