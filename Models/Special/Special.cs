using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model_v1
{
    public class Special
    {
        public const string External = "external";
        public const string Gateway = "gateway";

        public const string Zero = "0";
        public const string Star = "*";
        public const string Hash = "#";

        public const string Mobile = "mobile";
        public const string Telephone = "telephone";
        public const string Complete = "complete";

        public const string Common = "common";
        public const string Share = "share";

        public const int SIP = 16;
        public const int AUTO = 256;
    }

    public class m_mWebSocketJsonCmd
    {
        /// <summary>
        /// 登陆
        /// </summary>
        public const string _m_sLogin = "Login";
        /// <summary>
        /// 拨号任务
        /// </summary>
        public const string _m_sDialTask = "DialTask";
    }

    public class m_cFSCmdType
    {
        /// <summary>
        /// 发送并执行freeswitch命令
        /// </summary>
        public const string _m_sFSCmd = "FSCmd";
        /// <summary>
        /// 读取gateway文件
        /// </summary>
        public const string _m_sReadGateway = "ReadGateway";
        /// <summary>
        /// 创建gateway文件
        /// </summary>
        public const string _m_sCreateGateway = "CreateGateway";
        /// <summary>
        /// 写入gateway文件
        /// </summary>
        public const string _m_sWriteGateway = "WriteGateway";
        /// <summary>
        /// 删除gateway文件
        /// </summary>
        public const string _m_sDeleteGateway = "DeleteGateway";

    }
}
