using System;

namespace Plugin.AudioRecorder
{
	internal static class AudioFunctions
	{
		static float MAX_8_BITS_SIGNED = byte.MaxValue;
		static float MAX_8_BITS_UNSIGNED = 0xff;
		static float MAX_16_BITS_SIGNED = short.MaxValue;
		static float MAX_16_BITS_UNSIGNED = 0xffff;


		// Adapted from http://stackoverflow.com/questions/5800649/detect-silence-when-recording
		public static float CalculateLevel (byte [] buffer, int readPoint = 0, int leftOver = 0, bool use16Bit = true, bool signed = true, bool bigEndian = false)
		{
			float level;
			int max = 0;

			//bool signed = (RECORDER_AUDIO_ENCODING == Android.Media.Encoding. AudioFormat. Encoding.PCM_SIGNED);
			//bool bigEndian = false;// (format.isBigEndian());

			if (use16Bit)
			{
				for (int i = readPoint; i < buffer.Length - leftOver; i += 2)
				{
					int value = 0;
					// deal with endianness
					int hiByte = (bigEndian ? buffer [i] : buffer [i + 1]);
					int loByte = (bigEndian ? buffer [i + 1] : buffer [i]);

					if (signed)
					{
						short shortVal = (short)hiByte;
						shortVal = (short)((shortVal << 8) | (byte)loByte);
						value = shortVal;
					}
					else
					{
						value = (hiByte << 8) | loByte;
					}
					max = Math.Max (max, value);
				} // for
			}
			else
			{
				// 8 bit - no endianness issues, just sign
				for (int i = readPoint; i < buffer.Length - leftOver; i++)
				{
					int value = 0;

					if (signed)
					{
						value = buffer [i];
					}
					else
					{
						short shortVal = 0;
						shortVal = (short)(shortVal | buffer [i]);
						value = shortVal;
					}

					max = Math.Max (max, value);
				} // for
			} // 8 bit
			  // express max as float of 0.0 to 1.0 of max value
			  // of 8 or 16 bits (signed or unsigned)
			if (signed)
			{
				if (use16Bit) { level = (float)max / MAX_16_BITS_SIGNED; } else { level = (float)max / MAX_8_BITS_SIGNED; }
			}
			else
			{
				if (use16Bit) { level = (float)max / MAX_16_BITS_UNSIGNED; } else { level = (float)max / MAX_8_BITS_UNSIGNED; }
			}

			//System.Diagnostics.Debug.WriteLine ("LEVEL is {0}", level);

			return level;
		}
	}
}