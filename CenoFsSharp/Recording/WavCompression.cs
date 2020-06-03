using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WaveLib;
using Yeti.WMFSdk;
using Yeti.MMedia;

namespace CenoFsSharp.Recording
{
	class WavCompression
	{
		public bool _WavToWma(string WavFile)
		{
			bool ConvertResult = true;
			string WmaFile = string.Empty;

			if (!File.Exists(WavFile))
				throw new FileNotFoundException("convert failed because the wavfile is not exists", WavFile);

			if (!File.Exists(Environment.CurrentDirectory + @"\midea\test.wma"))
				throw new FileNotFoundException("convert failed because the wmafile is not exists", Environment.CurrentDirectory + @"\midea\test.wma");

			try
			{
				WmaFile = WavFile.Substring(0, WavFile.Length - 4) + ".wma";
				if (File.Exists(WmaFile))
					WmaFile = WavFile.Substring(0, WavFile.Length - 4) + "_1.wma";
			}
			catch (Exception ex)
			{
				throw new FileLoadException("failed to create new wav file", WmaFile);
			}
			int Read = 0;
			try
			{
				using (WaveStream _WavStream = new WaveStream(WavFile))
				{
					using (FileStream _WmaFileStream = new FileStream(WmaFile, FileMode.Create))
					{
						using (WmaStream _WmaStream = new WmaStream(@"midea\test.wma"))
						{
							using (WmaWriter _WmaWrite = new WmaWriter(_WmaFileStream, _WavStream.Format, _WmaStream.Profile))
							{
								byte[] Buff = new byte[_WmaWrite.OptimalBufferSize];
								Read = _WavStream.Read(Buff, 0, Buff.Length);

								while (Read > 0)
								{
									_WmaWrite.Write(Buff, 0, Read);
									Read = _WavStream.Read(Buff, 0, Buff.Length);
								}
								_WmaWrite.Flush();
								_WmaWrite.Close();
							}
							_WmaStream.Close();
						}
						_WmaFileStream.Close();
					}
					_WavStream.Close();
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			File.SetCreationTime(WmaFile, File.GetCreationTime(WavFile));
			File.Delete(WavFile);
			return ConvertResult;
		}

	}
}
