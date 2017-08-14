using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	internal class WaveRecorder : IDisposable
	{
		string audioFileName;
		FileStream fileStream;
		StreamWriter streamWriter;
		BinaryWriter writer;
		int byteCount;
		IAudioStream audioStream;


		/// <summary>
		/// Starts recording WAVE format audio from the audio stream.
		/// </summary>
		/// <param name="stream">A <see cref="IAudioStream"/> that provides the audio data.</param>
		/// <param name="fileName">The full path of the file to record audio to.</param>
		/// <returns></returns>
		public async Task StartRecorder (IAudioStream stream, string fileName)
		{
			if (stream == null)
			{
				throw new ArgumentNullException (nameof (stream));
			}

			try
			{
				//if we're restarting, let's see if we have an existing stream configred that can be stopped
				if (audioStream != null)
				{
					await audioStream.Stop ();
				}

				audioFileName = fileName;
				audioStream = stream;

				fileStream = new FileStream (fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
				streamWriter = new StreamWriter (fileStream);
				writer = new BinaryWriter (streamWriter.BaseStream, Encoding.UTF8);

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
				System.Diagnostics.Debug.WriteLine ("Error in WaveRecorder.StartRecorder(): {0}", ex);
				throw;
			}
		}


		/// <summary>
		/// Gets a new <see cref="Stream"/> to the audio file in readonly mode.
		/// </summary>
		/// <returns>A <see cref="Stream"/> object that can be used to read the audio file from the beginning.</returns>
		public Stream GetAudioFileStream ()
		{
			//return a new stream to the same audio file, in Read mode
			return new FileStream (audioFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
				if (streamWriter != null)
				{
					writer.Write (bytes);
					byteCount += bytes.Length;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ("Error in WaveRecorder.OnStreamBroadcast(): {0}", ex.Message);

				StopRecorder ();
			}
		}


		public void StopRecorder ()
		{
			if (audioStream != null)
			{
				audioStream.OnBroadcast -= OnStreamBroadcast;
				audioStream.OnActiveChanged -= StreamActiveChanged;
			}

			if (streamWriter != null)
			{
				if (streamWriter.BaseStream.CanWrite)
				{
					WriteHeader ();
				}

				//fileStream.Dispose();
				streamWriter.Dispose (); //should properly close/dispose the underlying stream as well
				fileStream = null;
				streamWriter = null;
			}

			audioStream = null;
		}


		public void Dispose ()
		{
			StopRecorder ();
		}


		void WriteHeader ()
		{
			writer.Seek (0, SeekOrigin.Begin);

			//chunk ID
			writer.Write ('R');
			writer.Write ('I');
			writer.Write ('F');
			writer.Write ('F');

			writer.Write (byteCount + 36); //36 + subchunk 2 size (data size)

			//format
			writer.Write ('W');
			writer.Write ('A');
			writer.Write ('V');
			writer.Write ('E');

			//subchunk 1 ID
			writer.Write ('f');
			writer.Write ('m');
			writer.Write ('t');
			writer.Write (' ');

			writer.Write (16); //subchunk 1 (fmt) size
			writer.Write ((short)1); //PCM audio format

			writer.Write ((short)audioStream.ChannelCount);
			writer.Write (audioStream.SampleRate);
			writer.Write (audioStream.SampleRate * 2);
			writer.Write ((short)2); //block align
			writer.Write ((short)audioStream.BitsPerSample);

			//subchunk 2 ID
			writer.Write ('d');
			writer.Write ('a');
			writer.Write ('t');
			writer.Write ('a');

			//subchunk 2 (data) size
			writer.Write (byteCount);
		}
	}
}