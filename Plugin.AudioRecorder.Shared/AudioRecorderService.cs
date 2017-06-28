using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Plugin.AudioRecorder
{
	public partial class AudioRecorderService
	{
		WaveRecorder recorder;
		AudioStream audioStream;

		bool audioDetected;
		string filePath;
		DateTime? silenceTime;
		DateTime? startTime;

		const string RecordingFileName = "recording.wav";

		public int PreferredSampleRate { get; set; } = 44100;

		public bool IsRecording => audioStream?.Active ?? false;

		public TimeSpan AudioSilenceTimeout { get; set; } = TimeSpan.FromSeconds (2);

		public TimeSpan TotalAudioTimeout { get; set; } = TimeSpan.FromSeconds (30);

		public bool StopRecordingOnSilence { get; set; } = true;

		public bool StopRecordingAfterTimeout { get; set; } = true;

		public float SilenceThreshold { get; set; } = .2f;

		public event EventHandler<string> AudioInputReceived;


		partial void Init ();


		public AudioRecorderService ()
		{
			Init ();
		}


		public async Task StartRecording ()
		{
			ResetAudioDetection ();

			InitializeStream (PreferredSampleRate);

			if (!(await recorder.StartRecorder (audioStream, GetFilename ())))
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


        private IEnumerable<short> Decode(byte[] byteArray)
        {
            for (var i = 0; i < byteArray.Length - 1; i += 2)
            {
                yield return (BitConverter.ToInt16(byteArray, i));
            }
        }


        async void AudioStream_OnBroadcast (object sender, byte [] bytes)
		{
            //var level = AudioFunctions.CalculateLevel(bytes);//, bigEndian: true);//, bigEndian: true, signed: false);

            var level = Decode(bytes).Select(Math.Abs).Average(x => x);

            //double level = Math.Sqrt(sum / (bytes.Length / 2));

			System.Diagnostics.Debug.WriteLine ("AudioStream_OnBroadcast :: calculateLevel == {0}", level);

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
						await StopRecording ();
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
				await StopRecording ();
			}
		}


		public async Task StopRecording (bool continueProcessing = true)
		{
			audioStream.OnBroadcast -= AudioStream_OnBroadcast;

			try
			{
				recorder.StopRecorder ();
				await audioStream.Stop ();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ("Error in StopRecording: {0}", ex.Message);
			}

			if (continueProcessing)
			{
				System.Diagnostics.Debug.WriteLine ("AudioRecorderService.StopRecording(): Recording stopped, raising AudioInputReceived event");

				AudioInputReceived?.Invoke (this, audioDetected ? GetFilename () : null);
			}
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

				audioStream.OnBroadcast += AudioStream_OnBroadcast;

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


		public string GetFilename ()
		{
			return filePath;
		}
	}
}