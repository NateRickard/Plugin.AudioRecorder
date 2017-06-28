using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.AudioRecorder
{
	public class WaveRecorder : IDisposable
	{
		FileStream fileStream;
		StreamWriter streamWriter;
		BinaryWriter writer;
		int byteCount;
		IAudioStream stream;

		public async Task<bool> StartRecorder (IAudioStream stream, string fileName)
		{
			if (stream == null)
			{
				return false;
			}

			try
			{
				//if we're restarting, let's see if we have an existing stream configred that can be stopped
				if (this.stream != null)
				{
					await this.stream.Stop ();
				}

				this.stream = stream;

				fileStream = new FileStream (fileName, FileMode.Create);
				streamWriter = new StreamWriter (fileStream);
				writer = new BinaryWriter (streamWriter.BaseStream, Encoding.UTF8);

				byteCount = 0;
				this.stream.OnBroadcast += OnStreamBroadcast;
				this.stream.OnActiveChanged += StreamActiveChanged;

				if (!this.stream.Active)
				{
					await this.stream.Start ();
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ("Error in WaveRecorder.StartRecorder(): {0}", ex);
				return false;
			}

			return true;
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
			if (stream != null)
			{
				stream.OnBroadcast -= OnStreamBroadcast;
				stream.OnActiveChanged -= StreamActiveChanged;
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

			stream = null;
		}


		public void Dispose ()
		{
			StopRecorder ();
		}


		void WriteHeader ()
		{
			writer.Seek (0, SeekOrigin.Begin);
			// chunk ID
			writer.Write ('R');
			writer.Write ('I');
			writer.Write ('F');
			writer.Write ('F');

			writer.Write (byteCount + 36);
			writer.Write ('W');
			writer.Write ('A');
			writer.Write ('V');
			writer.Write ('E');

			writer.Write ('f');
			writer.Write ('m');
			writer.Write ('t');
			writer.Write (' ');

			writer.Write (16);
			writer.Write ((short)1);

			writer.Write ((short)stream.ChannelCount);
			writer.Write (stream.SampleRate);
			writer.Write (stream.SampleRate * 2);
			writer.Write ((short)2);
			writer.Write ((short)stream.BitsPerSample);
			writer.Write ('d');
			writer.Write ('a');
			writer.Write ('t');
			writer.Write ('a');
			writer.Write (byteCount);
		}
	}
}