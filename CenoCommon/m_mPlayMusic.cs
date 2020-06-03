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
    }
}
