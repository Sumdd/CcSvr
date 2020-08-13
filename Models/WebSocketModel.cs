using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model_v1
{
    public class M_WebSocket
    {
        /// <summary>
        /// 连接服务器
        /// </summary>
        public const string _ljfwq = "LJFWQ";
        /// <summary>
        /// 连接服务器结果
        /// </summary>
        public const string _ljfwqjg = "LJFWQJG";
        /// <summary>
        /// 发送录音
        /// </summary>  
        public const string _fsly = "FSLY";
        /// <summary>
        /// 发送来电号码
        /// </summary>
        public const string _fsldhm = "FSLDHM";
        /// <summary>
        /// 拨号状态    
        /// </summary>
        public const string _bhzt = "BHZT";
        #region 拨号状态
        /// <summary>
        /// 拨号状态:失败
        /// </summary>
        public const string _bhzt_fail = "Fail";
        /// <summary>
        /// 拨号状态:摘机
        /// </summary>
        public const string _bhzt_pick = "Pick";
        /// <summary>
        /// 拨号状态:挂断
        /// </summary>
        public const string _bhzt_hang = "Hang";
        /// <summary>
        /// 来电时繁忙
        /// </summary>
        public const string _bhzt_call_busy = "Call_Busy";
        #endregion
        /// <summary>
        /// 拨打电话
        /// </summary>
        public const string _bddh = "BDDH";
        /// <summary>
        /// 自动外呼
        /// </summary>
        public const string _zdwh = "ZDWH";
    }

    public class m_mServerToPModel
    {
        public static string JSONPrefix = "{JSONPrefix}";
        public static string TextPrefix = "{TextPrefix}";
    }

    public class WebWebSocketType
    {
        //代码
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string Push = "Push";
        public const string Push_R = "Push-Reply";
        public const string Dial = "Dial";
        public const string Dial_R = "Dial-Reply";
        public const string RecID = "RecID";
        public const string Call = "Call";
        //区分
        public const string P = "P";
        public const string W = "W";
    }

    public class WebWebSocketModel
    {
        public string type;
        public object data;
    }

    #region 自动拨号
    public class m_mWebSocketJson
    {
        public string m_sUse
        {
            get;
            set;
        }

        public object m_oObject
        {
            get;
            set;
        }
    }

    public class m_mSampleDialTask
    {
        public string m_sUUID
        {
            get;
            set;
        }
        public string m_sCallee
        {
            get;
            set;
        }
        public string m_sContent
        {
            get;
            set;
        }
    }
    #endregion
}
