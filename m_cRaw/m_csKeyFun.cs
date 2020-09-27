using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace m_cRaw
{
    public class m_csKeyFun
    {
        ///到期时间
        public static DateTime m_dtEndTime = new DateTime(2020, 10, 1, 11, 37, 0);
        ///改成服务器CPU标识号
        private const string m_sCPU = "BFEBFBFF000306A9";
        ///获取CPU
        public static string GetCPUSerialNumber()
        {
            string cpuSerialNumber = string.Empty;
            ManagementClass mc = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                cpuSerialNumber = mo["ProcessorId"].ToString();
                break;
            }
            mc.Dispose();
            moc.Dispose();
            return cpuSerialNumber;
        }

        ///判断方法
        public static int m_fCanUse()
        {
            int _m_uUseStatus = 0;
            try
            {
                if (m_sCPU != m_csKeyFun.GetCPUSerialNumber())
                {
                    _m_uUseStatus = _m_uUseStatus | 1;
                }

                if (DateTime.Now > m_dtEndTime)
                {
                    _m_uUseStatus = _m_uUseStatus | 2;
                }
                return _m_uUseStatus;
            }
            catch
            {
                return _m_uUseStatus | 4;
            }
        }
    }
}
