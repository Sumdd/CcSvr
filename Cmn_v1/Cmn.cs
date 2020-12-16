using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cmn_v1
{
    public class Cmn
    {
        /// <summary>
        /// 生成唯一ID
        /// </summary>
        public static string UniqueID
        {
            get
            {
                return DateTime.Now.ToString("yyyyMMddHHmmssffffff_") + Guid.NewGuid();
            }
        }
        /// <summary>
        /// Marshal.StringToHGlobalAnsi简写得到句柄
        /// </summary>
        /// <returns></returns>
        public static IntPtr Sti(string message)
        {
            return Marshal.StringToHGlobalAnsi(message);
        }
        /// <summary>
        /// Marshal.PtrToStringAnsi简写得到字符串
        /// </summary>
        /// <param name="intptr"></param>
        /// <returns></returns>
        public static string Its(IntPtr intptr)
        {
            return Marshal.PtrToStringAnsi(intptr);
        }
        /// <summary>
        /// 格式路径
        /// </summary>
        /// <param name="_path">路径</param>
        /// <param name="_replace">替换符,默认"\"</param>
        /// <returns></returns>
        public static string PathFmt(string _path, string _replace = "\\")
        {
            return new Regex("[\\\\//]+").Replace(_path, _replace);
        }
        /// <summary>
        /// 忽略大小写比较
        /// </summary>
        /// <param name="a">a字符串,是否有b出现</param>
        /// <param name="b">b字符串</param>
        /// <returns></returns>
        public static bool IgnoreEquals(string a, string b, StringComparison c = StringComparison.OrdinalIgnoreCase)
        {
            return string.Equals(a, b, c);
        }
        /// <summary>
        /// 时间差秒
        /// </summary>
        /// <param name="m_pDateTime1">被减数</param>
        /// <param name="m_pDateTime2">减数</param>
        /// <returns></returns>
        public static int m_fUnsignedSeconds(DateTime m_pDateTime1, DateTime m_pDateTime2)
        {
            try
            {
                double m_dTotalSeconds = m_pDateTime1.Subtract(m_pDateTime2).TotalSeconds;
                int m_uTotalSeconds = (int)m_dTotalSeconds;
                if (m_uTotalSeconds < 0)
                    return 0;
                return m_uTotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        public static int m_fUnsignedSeconds(string _m_pDateTime1, DateTime m_pDateTime2)
        {
            try
            {
                DateTime m_pDateTime1 = DateTime.Now;
                DateTime.TryParse(_m_pDateTime1, out m_pDateTime1);
                double m_dTotalSeconds = m_pDateTime1.Subtract(m_pDateTime2).TotalSeconds;
                int m_uTotalSeconds = (int)m_dTotalSeconds;
                if (m_uTotalSeconds < 0)
                    return 0;
                return m_uTotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        public static int m_fUnsignedSeconds(DateTime m_pDateTime1, string _m_pDateTime2)
        {
            try
            {
                DateTime m_pDateTime2 = DateTime.Now;
                DateTime.TryParse(_m_pDateTime2, out m_pDateTime2);
                double m_dTotalSeconds = m_pDateTime1.Subtract(m_pDateTime2).TotalSeconds;
                int m_uTotalSeconds = (int)m_dTotalSeconds;
                if (m_uTotalSeconds < 0)
                    return 0;
                return m_uTotalSeconds;
            }
            catch
            {
                return 0;
            }
        }
        public static int m_fUnsignedSeconds(string _m_pDateTime1, string _m_pDateTime2)
        {
            try
            {
                DateTime m_pDateTime1 = DateTime.Now;
                DateTime.TryParse(_m_pDateTime1, out m_pDateTime1);
                DateTime m_pDateTime2 = DateTime.Now;
                DateTime.TryParse(_m_pDateTime2, out m_pDateTime2);
                double m_dTotalSeconds = m_pDateTime1.Subtract(m_pDateTime2).TotalSeconds;
                int m_uTotalSeconds = (int)m_dTotalSeconds;
                if (m_uTotalSeconds < 0)
                    return 0;
                return m_uTotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 标准时间字符串
        /// </summary>
        /// <param name="m_pDateTime"></param>
        /// <returns></returns>
        public static string m_fDateTimeString(DateTime? m_pDateTime = null)
        {
            if (m_pDateTime == null)
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return Convert.ToDateTime(m_pDateTime).ToString("yyyy-MM-dd HH:mm:ss");
        }
        /// <summary>
        /// 现在的年月日
        /// </summary>
        /// <param name="m_pDateTime"></param>
        /// <returns></returns>
        public static string m_fDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }
        /// <summary>
        /// 比较是否同年月日
        /// </summary>
        /// <param name="m_pDateTime"></param>
        /// <returns></returns>
        public static bool m_fEqualsDate(DateTime m_pDateTime)
        {
            DateTime m_dtNow = DateTime.Now;
            if (m_dtNow.Year != m_pDateTime.Year)
                return false;
            if (m_dtNow.Month != m_pDateTime.Month)
                return false;
            if (m_dtNow.Day != m_pDateTime.Day)
                return false;
            return true;
        }
        /// <summary>
        /// 比较时间是否非今日
        /// </summary>
        /// <param name="m_pDateTime"></param>
        /// <returns></returns>
        public static bool m_fLessDate(DateTime _m_pDateTime)
        {
            DateTime m_dtNow = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 00:00:00"));
            DateTime m_pDateTime = Convert.ToDateTime(_m_pDateTime.ToString("yyyy-MM-dd 00:00:00"));
            return DateTime.Compare(m_pDateTime, m_dtNow) < 0;
        }
        /// <summary>
        /// 移除空白
        /// </summary>
        /// <param name="m_sString"></param>
        /// <returns></returns>
        public static string m_fRemoveSpace(string m_sString)
        {
            try
            {
                return m_sString.Replace(" ", "");
            }
            catch
            {
                return m_sString;
            }
        }

        /// <summary>
        /// 获取程序运行根
        /// </summary>
        private static string _m_fMPath = null;
        public static string m_fMPath
        {
            get
            {
                try
                {
                    if (_m_fMPath == null)
                        _m_fMPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).TrimEnd(new char[] { '/', '\\' });
                }
                catch
                {

                }
                return _m_fMPath;
            }
        }

        ///委托
        public delegate string m_dGetCPU();
        public static m_dGetCPU m_dfGetCPU;

        ///委托
        public delegate void m_dJSON();
        public static m_dJSON m_dfJSON;
    }
}
