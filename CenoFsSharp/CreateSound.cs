using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SpeechLib;
using System.IO;
using CenoSipFactory;
using DB.Basic;

namespace CenoFsSharp
{
	public class CreateSound
	{
		public enum Sound
		{
			woman,
			man,
		};

		public enum SoundSize
		{
			higher,
			high,
			middle,
			low,
			lower,
		};

		public enum SoundSpeed
		{
			fast,
			middle,
			slow,
		};

		public static string FilePath = ParamLib.Tts_AudioFilePath + @"\";

		public static Sound Voices;               //声音
		public static SoundSize VoiceSize;        //声音大小
		public static SoundSpeed VoiceSpeed;      //语速

		public static SpeechVoiceSpeakFlags SpFlags = SpeechVoiceSpeakFlags.SVSFlagsAsync;
		public static SpVoice Voice = new SpVoice();

		/// <summary>
		/// 生成音频文件
		/// </summary>
		/// <param name="content"></param>
		/// <returns>文件路径</returns>
		public static string CreSound(string content, string FileName)
		{
			SpeechStreamFileMode SpFileMode = SpeechStreamFileMode.SSFMCreateForWrite;
			SpFileStream Sp = new SpFileStream();
			Sp.Format.Type = SpeechAudioFormatType.SAFT8kHz8BitMono;
			if (!Directory.Exists(FilePath))
			{
				Directory.CreateDirectory(FilePath);
			}
			Sp.Open(FilePath + FileName, SpFileMode, false);

            //语速
            if (!string.IsNullOrWhiteSpace(Call_ParamUtil.m_sDialTaskTTSSetting))
            {
                int m_iSpeed = 0;
                int.TryParse(Call_ParamUtil.m_sDialTaskTTSSetting, out m_iSpeed);
                Voice.Rate = m_iSpeed;
            }

			Voice.AudioOutputStream = Sp;
			Voice.Speak(content, SpFlags);
			Voice.WaitUntilDone(-1);
			Sp.Close();

			return FilePath + FileName;
		}

		public string CreSound_bak(string content, string FileName, int? Rate = null)
		{

			try
			{
				lock (Voice)
				{
					SpeechStreamFileMode SpFileMode = SpeechStreamFileMode.SSFMCreateForWrite;
					SpFileStream Sp = new SpFileStream();
					Sp.Format.Type = SpeechAudioFormatType.SAFT8kHz8BitMono;
					if (!Directory.Exists(FilePath))
					{
						Directory.CreateDirectory(FilePath);
					}
					Sp.Open(FilePath + FileName, SpFileMode, false);

                    if (Rate != null)
                    {
                        Voice.Rate = Convert.ToInt32(Rate);
                    }

					Voice.AudioOutputStream = Sp;
					Voice.Speak(content, SpFlags);
					Voice.WaitUntilDone(-1);
					Sp.Close();
				}
			}
			catch
			{
				throw;
			}
			return FilePath + FileName;
		}

		public static string CreSound(string content, string FileName, Sound Voices, SoundSize VoiceSize, SoundSpeed VoiceSpeed)
		{
			GetVoiceToken(Voices);
			GetVoiceSpeed(VoiceSpeed);
			GetVoiceSize(VoiceSize);

			SpeechStreamFileMode SpFileMode = SpeechStreamFileMode.SSFMCreateForWrite;
			SpFileStream Sp = new SpFileStream();
			Sp.Format.Type = SpeechAudioFormatType.SAFT8kHz8BitMono;
			Sp.Open(FilePath + FileName, SpFileMode, false);
			Voice.AudioOutputStream = Sp;
			Voice.Speak(content, SpFlags);
			Voice.WaitUntilDone(-1);
			Sp.Close();

			return FilePath + FileName;
		}

		public static void GetVoiceToken(Sound VoiceToken)
		{
			ISpeechObjectTokens isot = Voice.GetVoices(string.Empty, string.Empty);
			int i;
			if (VoiceToken == Sound.woman)
			{
				i = 0;
				foreach (ISpeechObjectToken sot in isot)
				{
					if (sot.GetDescription(0) == "VW Lily")
					{
						Voice.Voice = isot.Item(i);
					}
					i++;
				}
			}
			else
			{
				i = 0;
				foreach (ISpeechObjectToken sot in isot)
				{
					if (sot.GetDescription(0) == "VW Liang")
					{
						Voice.Voice = isot.Item(i);
					}
					i++;
				}
			}

		}

		public static void GetVoiceSpeed(SoundSpeed VoiceSpeed)
		{
			switch (VoiceSpeed)
			{
				case SoundSpeed.fast:
					Voice.Rate = 5;
					break;
				case SoundSpeed.middle:
					Voice.Rate = 0;
					break;
				case SoundSpeed.slow:
					Voice.Rate = -5;
					break;
				default:
					break;
			}
		}

		public static void GetVoiceSize(SoundSize VoiceSize)
		{
			switch (VoiceSize)
			{
				case SoundSize.high:
					Voice.Volume = 10;
					break;
				case SoundSize.higher:
					Voice.Volume = 8;
					break;
				case SoundSize.middle:
					Voice.Volume = 5;
					break;
				case SoundSize.low:
					Voice.Volume = 2;
					break;
				case SoundSize.lower:
					Voice.Volume = 0;
					break;
				default:
					break;
			}
		}
	}
}
