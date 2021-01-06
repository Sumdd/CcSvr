using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoCommon
{
    public class m_mPlay
    {

        private static string m_fGetRunPathString()
        {
            return System.AppDomain.CurrentDomain.BaseDirectory;
        }
        /// <summary>
        /// 放音路径
        /// </summary>
        private static string m_fPlayMusicPathString
        {
            get
            {
                return $"{m_mPlay.m_fGetRunPathString()}\\audio";
            }
        }

        public static string m_mBusyMusic = $"{m_fPlayMusicPathString}\\m_mBusyMusic.wav";
        public static string m_mInvalidMusic = $"{m_fPlayMusicPathString}\\m_mInvalidMusic.wav";
        public static string m_mNoUserMusic = $"{m_fPlayMusicPathString}\\m_mNoUserMusic.wav";
        public static string m_mNoChannelMusic = $"{m_fPlayMusicPathString}\\m_mNoChannelMusic.wav";
        public static string m_mNoAnswerMusic = $"{m_fPlayMusicPathString}\\m_mNoAnswerMusic.wav";
        public static string m_mNoRegisteredMusic = $"{m_fPlayMusicPathString}\\m_mNoRegisteredMusic.wav";
        public static string m_mNotConnectedMusic = $"{m_fPlayMusicPathString}\\m_mNotConnectedMusic.wav";
        public static string m_mUnavailableMusic = $"{m_fPlayMusicPathString}\\m_mUnavailableMusic.wav";

        ///呼叫转移提示
        public static string _2closeerr = $"{m_fPlayMusicPathString}\\2closeerr.wav";
        public static string _2closeok = $"{m_fPlayMusicPathString}\\2closeok.wav";
        public static string _2openerr = $"{m_fPlayMusicPathString}\\2openerr.wav";
        public static string _2openok = $"{m_fPlayMusicPathString}\\2openok.wav";
        public static string _2timeerr = $"{m_fPlayMusicPathString}\\2timeerr.wav";
        public static string _2timeok = $"{m_fPlayMusicPathString}\\2timeok.wav";
        public static string _2whatdayok = $"{m_fPlayMusicPathString}\\2whatdayok.wav";
        public static string _2whatdayerr = $"{m_fPlayMusicPathString}\\2whatdayerr.wav";

        /// <summary>
        /// 空白音
        /// </summary>
        public static string m_mNullMusic = $"{m_fPlayMusicPathString}\\null.wav";
        /// <summary>
        /// 背景音乐
        /// </summary>
        public static string m_mBgMusic = $"{m_fPlayMusicPathString}\\BgMusic.wav";

        /// <summary>
        /// 嘟嘟嘟声音字节
        /// </summary>
        public static byte[] m_lDuDuDuWav = null;
        public static byte[] m_lDuDuDuMp3 = null;
        public static int m_fDuDuDu()
        {
            int m_uStatus = 0;
            ///Wav
            if (m_lDuDuDuWav == null)
            {
                string _dududu = $"{m_fPlayMusicPathString}\\busy.wav";
                if (System.IO.File.Exists(_dududu))
                {
                    m_lDuDuDuWav = System.IO.File.ReadAllBytes(_dududu);
                    m_uStatus |= 1;
                }
            }
            ///Mp3
            if (m_lDuDuDuMp3 == null)
            {
                string _dududu = $"{m_fPlayMusicPathString}\\busy.mp3";
                if (System.IO.File.Exists(_dududu))
                {
                    m_lDuDuDuMp3 = System.IO.File.ReadAllBytes(_dududu);
                    m_uStatus |= 2;
                }
            }
            return m_uStatus;
        }
    }
}
