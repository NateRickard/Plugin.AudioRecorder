using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	internal class WaveRecorder : IDisposable
	{
		BinaryWriter writer;
		int byteCount;
		IAudioStream audioStream;
		bool writeHeadersToStream;

		/// <summary>
		/// Starts recording WAVE format audio from the audio stream.
		/// </summary>
		/// <param name="stream">A <see cref="IAudioStream"/> that provides the audio data.</param>
		/// <param name="recordStream">The stream the audio will be written to.</param>
		/// <param name="writeHeaders"><c>false</c> (default) Write WAV headers to stream at the end of recording.</param>
		public async Task StartRecorder (IAudioStream stream, Stream recordStream, bool writeHeaders = false)
		{
			if (stream == null)
			{
				throw new ArgumentNullException (nameof (stream));
			}

			if (recordStream == null)
			{
				throw new ArgumentNullException (nameof (recordStream));
			}

			writeHeadersToStream = writeHeaders;

			try
			{
				//if we're restarting, let's see if we have an existing stream configured that can be stopped
				if (audioStream != null)
				{
					await audioStream.Stop ();
				}
				
				audioStream = stream;
				writer = new BinaryWriter (recordStream, Encoding.UTF8, true);

				byteCount = 0;
				audioStream.OnBroadcast += OnStreamBroadcast;
				audioStream.OnActiveChanged += StreamActiveChanged;

				if (!audioStream.Active)
				{
					await audioStream.Start ();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in WaveRecorder.StartRecorder(): {0}", ex.Message);

				StopRecorder ();
				throw;
			}
		}
		
		void StreamActiveChanged (object sender, bool active)
		{
			if (!active)
			{
				StopRecorder ();
			}
		}

		void OnStreamBroadcast (object sender, byte [] bytes)
		{
			try
			{
				if (writer != null)
				{
					writer.Write (bytes);
					byteCount += bytes.Length;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error in WaveRecorder.OnStreamBroadcast(): {0}", ex.Message);

				StopRecorder ();
			}
		}

		/// <summary>
		/// Stops recording WAV audio from the underlying <see cref="IAudioStream"/> and finishes writing the WAV file.
		/// </summary>
		public void StopRecorder ()
		{
			try
			{
				if (audioStream != null)
				{
					audioStream.OnBroadcast -= OnStreamBroadcast;
					audioStream.OnActiveChanged -= StreamActiveChanged;
				}

				if (writer != null)
				{
					if (writeHeadersToStream && writer.BaseStream.CanWrite && writer.BaseStream.CanSeek)
					{
						//now that audio is finished recording, write a WAV/RIFF header at the beginning of the file
						writer.Seek (0, SeekOrigin.Begin);
						AudioFunctions.WriteWavHeader (writer, audioStream.ChannelCount, audioStream.SampleRate, audioStream.BitsPerSample, byteCount);
					}

					writer.Dispose (); //this should properly close/dispose the underlying stream as well
					writer = null;
				}

				audioStream = null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine ("Error during StopRecorder: {0}", ex.Message);
				throw;
			}
		}

		public void Dispose ()
		{
			StopRecorder ();
		}
	}
}
