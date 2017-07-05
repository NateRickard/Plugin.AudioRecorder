using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace Plugin.AudioRecorder
{
    public class AudioStream : IAudioStream
    {
        readonly uint bufferSize = 1024;
        MediaCapture capture;
        InMemoryRandomAccessStream stream;

        /// <summary>
        /// Occurs when new audio has been streamed.
        /// </summary>
        public event EventHandler<byte[]> OnBroadcast;

        /// <summary>
        /// Occurs when the audio stream active status changes.
        /// </summary>
        public event EventHandler<bool> OnActiveChanged;

        /// <summary>
        /// Occurs when there's an error while capturing audio.
        /// </summary>
        public event EventHandler<Exception> OnException;

        /// <summary>
        /// Gets the sample rate.
        /// </summary>
        /// <value>
        /// The sample rate.
        /// </value>
        public int SampleRate { get; private set; } = 44100;

        /// <summary>
        /// Gets the channel count.
        /// </summary>
        /// <value>
        /// The channel count.
        /// </value>
        public int ChannelCount { get; private set; }

        /// <summary>
        /// Gets bits per sample.
        /// </summary>
        public int BitsPerSample { get; private set; } = 16;


        /// <summary>
        /// Gets a value indicating if the audio stream is active.
        /// </summary>
		public bool Active { get; private set; }


        /// <summary>
		/// Initializes a new instance of the <see cref="AudioStream"/> class.
		/// </summary>
		/// <param name="sampleRate">Sample rate.</param>
		/// <param name="channels">A value representing the number of channels to record.</param>
        public AudioStream(int sampleRate, int channels = 1)
        {
            SampleRate = sampleRate;
            ChannelCount = channels;
        }


        async Task Init()
        {
            await Stop();

            stream = new InMemoryRandomAccessStream();

            try
            {
                var settings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio,
                    AudioProcessing = Windows.Media.AudioProcessing.Raw
                };

                capture = new MediaCapture();
                await capture.InitializeAsync(settings);

                capture.RecordLimitationExceeded += OnRecordLimitationExceeded;
                capture.Failed += OnCaptureFailed;
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// Starts the audio stream.
        /// </summary>
        public async Task Start()
        {
            try
            {
                if (!Active)
                {
                    await Init();
                    
                    var profile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low);
                    profile.Audio = AudioEncodingProperties.CreatePcm((uint)SampleRate, (uint)ChannelCount, (uint)BitsPerSample);
                    

                    await capture.StartRecordToStreamAsync(profile, stream);

                    Active = true;
                    OnActiveChanged?.Invoke(this, true);

                    _ = Task.Run(() => Record());
                }
            }
            catch (Exception ex)
            {
                Active = false;
                System.Diagnostics.Debug.WriteLine("Error in AudioStream.Start(): {0}", ex);
                throw;
            }
        }


        /// <summary>
        /// Stops the audio stream.
        /// </summary>
        public async Task Stop()
        {
            if (capture != null)
            {
                capture.RecordLimitationExceeded -= OnRecordLimitationExceeded;
                capture.Failed -= OnCaptureFailed;
            }

            if (Active)
            {
                Active = false;

                await capture.StopRecordAsync();
                stream?.Dispose();
                capture?.Dispose();

                OnActiveChanged?.Invoke(this, false);
            }
        }


        async void OnRecordLimitationExceeded(MediaCapture sender)
        {
            await Stop();

            throw new Exception("Record Limitation Exceeded ");
        }


        async void OnCaptureFailed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            await Stop();

            throw new Exception(string.Format("OnCaptureFailed error; Code.Message: {0}. {1}", errorEventArgs.Code, errorEventArgs.Message));
        }


        /// <summary>
		/// Record from the microphone and broadcast the buffer.
		/// </summary>
		async Task Record()
        {
            try
            {
                int readFailureCount = 0;

                using (var readStream = stream.CloneStream())
                using (var reader = new DataReader(readStream))
                {
                    reader.InputStreamOptions = InputStreamOptions.Partial;
                    //reader.UnicodeEncoding = UnicodeEncoding.Utf8;

                    while (Active)
                    {
                        try
                        {
                            //not sure if this is even a good idea (likely no), but we'll try to allow a single bad read, and past that shut it down
                            if (readFailureCount > 1)
                            {
                                System.Diagnostics.Debug.WriteLine("AudioStream.Record(): Multiple read failures detected, stopping stream");
                                await Stop();
                                break;
                            }

                            var loadResult = await reader.LoadAsync(bufferSize);

                            //readResult should contain the # bytes read
                            if (loadResult > 0)
                            {
                                byte[] bytes = new byte[loadResult];
                                reader.ReadBytes(bytes);

                                //System.Diagnostics.Debug.WriteLine("AudioStream.Record(): Read {0} bytes, broadcasting {1} bytes", loadResult, bytes.Length);

                                OnBroadcast?.Invoke(this, bytes);
                            }
                            else
                            {
                                //System.Diagnostics.Debug.WriteLine("AudioStream.Record(): Non positive readResult returned: {0}", loadResult);
                            }
                        }
                        catch (Exception ex)
                        {
                            readFailureCount++;

                            System.Diagnostics.Debug.WriteLine("Error in Android AudioStream.Record(): {0}", ex.Message);
                            OnException?.Invoke(this, ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in Android AudioStream.Record(): {0}", ex.Message);
                OnException?.Invoke(this, ex);
            }
        }
    }
}