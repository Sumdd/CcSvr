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

        /// <summary>
        /// 移动电话
        /// </summary>
        public const string Mobile = "mobile";
        public const string Telephone = "telephone";
        public const string Complete = "complete";

        public const string Common = "common";
        public const string Share = "share";
        public const string ApiShare = "apiShare";

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

    public class m_cFileCmdType
    {
        /// <summary>
        /// 上传文件的相对目录
        /// </summary>
        public const string _m_sFilePath = "fload";
        /// <summary>
        /// 创建文件
        /// </summary>
        public const string _m_sFileCreate = "FileCreate";
        /// <summary>
        /// 删除文件
        /// </summary>
        public const string _m_sFileDelete = "FileDelete";
    }
}
