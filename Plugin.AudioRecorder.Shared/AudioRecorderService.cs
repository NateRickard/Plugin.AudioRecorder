using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	/// <summary>
	/// A service that records audio on the device's microphone input.
	/// </summary>
	public partial class AudioRecorderService
	{
		const string DefaultFileName = "ARS_recording.wav";
		const float NearZero = .00000000001F;

		WaveRecorder recorder;

		IAudioStream audioStream;

		bool audioDetected;
		DateTime? silenceTime;
		DateTime? startTime;
		TaskCompletionSource<string> recordTask;
		FileStream fileStream;

		/// <summary>
		/// Gets the details of the underlying audio stream.
		/// </summary>
		/// <remarks>Accessible once <see cref="StartRecording"/> has been called.</remarks>
		public AudioStreamDetails AudioStreamDetails { get; private set; }

		/// <summary>
		/// Gets/sets the desired file path. If null it will be set automatically
		/// to a temporary file.
		/// </summary>
		public string FilePath { get; set; }

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
		/// <remarks>Defaults to .15.  Value should be between 0 and 1.</remarks>
		public float SilenceThreshold { get; set; } = .15f;

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
		/// <param name="recordStream"><c>null</c> (default) Optional stream to write audio data to, if null, a file will be created.</param>
		/// <param name="writeHeaders"><c>false</c> (default) Set to true, to write WAV headers after recording. Note that stream should be seekable.</param>
		/// <returns>A <see cref="Task"/> that will complete when recording is finished.  
		/// The task result will be the path to the recorded audio file, or null if no audio was recorded.</returns>
		public async Task<Task<string>> StartRecording (Stream recordStream = null, bool writeHeaders = false)
		{
			if (recordStream == null)
			{
				if (FilePath == null)
				{
					FilePath = await GetDefaultFilePath ();
				}
				fileStream = new FileStream (FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
				recordStream = fileStream;
				writeHeaders = true;
			}

			ResetAudioDetection ();
			OnRecordingStarting ();

			InitializeStream (PreferredSampleRate);

			await recorder.StartRecorder (audioStream, recordStream, writeHeaders);

			AudioStreamDetails = new AudioStreamDetails
			{
				ChannelCount = audioStream.ChannelCount,
				SampleRate = audioStream.SampleRate,
				BitsPerSample = audioStream.BitsPerSample
			};

			startTime = DateTime.Now;
			recordTask = new TaskCompletionSource<string> ();

			Debug.WriteLine ("AudioRecorderService.StartRecording() complete.  Audio is being recorded.");

			return recordTask.Task;
		}

		/// <summary>
		/// Gets a new <see cref="Stream"/> to the recording audio file in readonly mode.
		/// </summary>
		/// <returns>A <see cref="Stream"/> object that can be used to read the audio file from the beginning.</returns>
		public Stream GetAudioFileStream ()
		{
			//return a new stream to the same audio file, in Read mode
			return new FileStream (FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

			if (level < NearZero && !audioDetected) // discard any initial 0s so we don't jump the gun on timing out
			{
				Debug.WriteLine ("level == {0} && !audioDetected", level);
				return;
			}

			if (level > SilenceThreshold) // did we find a signal?
			{
				audioDetected = true;
				silenceTime = null;

				Debug.WriteLine ("AudioStream_OnBroadcast :: {0} :: level > SilenceThreshold :: bytes: {1}; level: {2}", DateTime.Now, bytes.Length, level);
			}
			else // no audio detected
			{
				// see if we've detected 'near' silence for more than <audioTimeout>
				if (StopRecordingOnSilence && silenceTime.HasValue)
				{
					var currentTime = DateTime.Now;

					if (currentTime.Subtract (silenceTime.Value).TotalMilliseconds > AudioSilenceTimeout.TotalMilliseconds)
					{
						Timeout ($"AudioStream_OnBroadcast :: {currentTime} :: AudioSilenceTimeout exceeded, stopping recording :: Near-silence detected at: {silenceTime}");
						return;
					}
				}
				else
				{
					silenceTime = DateTime.Now;

					Debug.WriteLine ("AudioStream_OnBroadcast :: {0} :: Near-silence detected :: bytes: {1}; level: {2}", silenceTime, bytes.Length, level);
				}
			}

			if (StopRecordingAfterTimeout && DateTime.Now - startTime > TotalAudioTimeout)
			{
				Timeout ("AudioStream_OnBroadcast(): TotalAudioTimeout exceeded, stopping recording");
			}
		}

		void Timeout (string reason)
		{
			Debug.WriteLine (reason);
			audioStream.OnBroadcast -= AudioStream_OnBroadcast; // need this to be immediate or we can try to stop more than once
			
			// since we're in the middle of handling a broadcast event when an audio timeout occurs, we need to break the StopRecording call on another thread
			//	Otherwise, Bad. Things. Happen.
			_ = Task.Run (() => StopRecording ());
		}

		/// <summary>
		/// Stops recording audio.
		/// </summary>
		/// <param name="continueProcessing"><c>true</c> (default) to finish recording and raise the <see cref="AudioInputReceived"/> event. 
		/// Use <c>false</c> here to stop recording but do nothing further (from an error state, etc.).</param>
		public async Task StopRecording (bool continueProcessing = true)
		{
			audioStream.Flush (); // allow the stream to send any remaining data
			audioStream.OnBroadcast -= AudioStream_OnBroadcast;

			try
			{
				await audioStream.Stop ();
				// WaveRecorder will be stopped as result of stream stopping
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in StopRecording: {0}", ex);
			}

			fileStream?.Dispose();
			OnRecordingStopped ();

			var returnedFilePath = GetAudioFilePath ();
			// complete the recording Task for anything waiting on this
			recordTask.TrySetResult (returnedFilePath);

			if (continueProcessing)
			{
				Debug.WriteLine ($"AudioRecorderService.StopRecording(): Recording stopped, raising AudioInputReceived event; audioDetected == {audioDetected}; filePath == {returnedFilePath}");

				AudioInputReceived?.Invoke (this, returnedFilePath);
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
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error: {0}", ex);
			}
		}

		/// <summary>
		/// Gets the full filepath to the recorded audio file.
		/// </summary>
		/// <returns>The full filepath to the recorded audio file, or null if no audio was detected during the last record.</returns>
		public string GetAudioFilePath ()
		{
			return audioDetected ? FilePath : null;
		}
	}
}
