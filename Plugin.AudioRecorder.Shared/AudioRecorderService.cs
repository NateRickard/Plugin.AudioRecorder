using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	/// <summary>
	/// A service that records audio on the device's microphone input.
	/// </summary>
	public partial class AudioRecorderService
	{
		WaveRecorder recorder;
		AudioStream audioStream;

		bool audioDetected;
		string filePath;
		DateTime? silenceTime;
		DateTime? startTime;

		const string RecordingFileName = "recording.wav";


		/// <summary>
		/// Gets/sets the preferred sample rate to be used during recording.
		/// </summary>
		/// <remarks>This value may be overridden by platform-specific implementations, e.g. the Android AudioManager will be asked for its preferred sample rate and may override any user-set value here.</remarks>
		public int PreferredSampleRate { get; set; } = 44100;


		/// <summary>
		/// Returns a value indicating if the <see cref="AudioRecorderService"/> is currently recording audio
		/// </summary>
		public bool IsRecording => audioStream?.Active ?? false;


		/// <summary>
		/// If <see cref="StopRecordingOnSilence"/> is set to <c>true</c>, this <see cref="TimeSpan"/> indicates the amount of 'silent' time is required before recording is stopped.
		/// </summary>
		/// <remarks>Defaults to 2 seconds.</remarks>
		public TimeSpan AudioSilenceTimeout { get; set; } = TimeSpan.FromSeconds (2);


		/// <summary>
		/// If <see cref="StopRecordingAfterTimeout"/> is set to <c>true</c>, this <see cref="TimeSpan"/> indicates the total amount of time to record audio for before recording is stopped. Defaults to 30 seconds.
		/// </summary>
		/// <seealso cref="StopRecordingAfterTimeout"/>
		public TimeSpan TotalAudioTimeout { get; set; } = TimeSpan.FromSeconds (30);


		/// <summary>
		/// Gets/sets a value indicating if the <see cref="AudioRecorderService"/> should stop recording after silence (low audio signal) is detected.
		/// </summary>
		/// <remarks>Default is `true`</remarks>
		public bool StopRecordingOnSilence { get; set; } = true;


		/// <summary>
		/// Gets/sets a value indicating if the <see cref="AudioRecorderService"/> should stop recording after a certain amount of time.
		/// </summary>
		/// <remarks>Defaults to <c>true</c></remarks>
		/// <seealso cref="TotalAudioTimeout"/>
		public bool StopRecordingAfterTimeout { get; set; } = true;


		/// <summary>
		/// Gets/sets a value indicating the signal threshold that determines silence.  If the recorder is being over or under aggressive when detecting silence, you can alter this value to achieve different results.
		/// </summary>
		/// <remarks>Defaults to .2.  Value should be between 0 and 1.</remarks>
		public float SilenceThreshold { get; set; } = .2f;


		/// <summary>
		/// This event is raised when audio recording is complete and delivers a full filepath to the recorded audio file.
		/// </summary>
		/// <remarks>This event will be raised on a background thread to allow for any further processing needed.  The audio file will be <c>null</c> in the case that no audio was recorded.</remarks>
		public event EventHandler<string> AudioInputReceived;


		partial void Init ();


		/// <summary>
		/// Creates a new instance of the <see cref="AudioRecorderService"/>.
		/// </summary>
		public AudioRecorderService ()
		{
			Init ();
		}


		/// <summary>
		/// Starts recording audio.
		/// </summary>
		public async Task StartRecording ()
		{
			ResetAudioDetection ();

			InitializeStream (PreferredSampleRate);

			await recorder.StartRecorder (audioStream, GetFilename ());

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
            var level = AudioFunctions.CalculateLevel(bytes);//, bigEndian: true);//, bigEndian: true, signed: false);

            //var level = Decode(bytes).Select(Math.Abs).Average(x => x);

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


		/// <summary>
		/// Stops recording audio.
		/// </summary>
		/// <param name="continueProcessing"><c>true</c> (default) to finish recording and raise the <see cref="AudioInputReceived"/> event. 
		/// Use <c>false</c> here to stop recording but do nothing further (from an error state, etc.).</param>
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


		/// <summary>
		/// Gets the full filepath to the recorded audio file.
		/// </summary>
		/// <returns>The full filepath to the recorded audio file.</returns>
		public string GetFilename ()
		{
			return filePath;
		}
	}
}