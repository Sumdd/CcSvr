using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace CenoSipFactory {
    public enum CALLTYPE {

    }

    public enum APP_USER_STATUS {
        USER_IDLE,
        USER_GET_1STDTMF,
        USER_GET_DTMF,
        USER_REQ_USER,
        USER_RING_BACK,
        USER_REQ_TRUNK_COMPUTER,
        USER_REQ_TRUNK_TELEPHONE,
        USER_REQ_ISUP_COMPUTER,
        USER_REQ_ISUP_TELEPHONE,
        USER_DIALOUT,
        USER_WAIT_REMOTE_PICKUP,
        USER_TALKING,
        USER_WAIT_HANGUP,
        USER_F_GET_DTMF,
        USER_F_REQ_USER,
        USER_F_RING_BACK,
        USER_F_TALKING,
        USER_F_OPERATE,
        USER_F_WAIT_TALK,
        USER_LISTEN,
        USER_SERVICES,

        /// <summary>
        /// 空闲
        /// </summary>
        FS_USER_IDLE,
        /// <summary>
        /// 拨号前
        /// </summary>
        FS_USER_BF_DIAL,
        FS_USER_BF_ANSWER,
        /// <summary>
        /// 非空闲
        /// </summary>
        FS_USER_UN_DIAL,
        FS_USER_UN_WAIT_REMOTE_PICKUP,
        FS_USER_UN_ANSWER,
        /// <summary>
        /// 响铃
        /// </summary>
        FS_USER_RINGING,
        /// <summary>
        /// 通话中
        /// </summary>
        FS_USER_TALKING,
        /// <summary>
        /// 自动拨号中
        /// </summary>
        FS_USER_AUTODIAL,
        /// <summary>
        /// 主叫挂断
        /// </summary>
        FS_USER_AHANGUP,
        /// <summary>
        /// 被叫挂断
        /// </summary>
        FS_USER_BHANGUP,
        /// <summary>
        /// 强断
        /// </summary>
        FS_USER_BREAKDOWN
    }

    public enum ToneType {
        /// <summary>
        /// DIALING_TONE
        /// </summary>
        DIALING_TONE = 0,
        /// <summary>
        /// BUSY_TONE
        /// </summary>
        BUSY_TONE = 1,
        /// <summary>
        /// RING_BACK_TONE
        /// </summary>
        RING_BACK_TONE = 2,
        /// <summary>
        /// HANG_TONE
        /// </summary>
        HANG_TONE = 3,
    }



    /// <summary>
    /// definition of the Channel Socket infomation
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ChSocket {
        /// <summary>
        /// 客户端是否连接
        /// </summary>
        public bool IsConnect {
            get; set;
        }

        /// <summary>
        /// 客户端心跳检测
        /// </summary>
        public bool HeartBeatFlag {
            get; set;
        }

        /// <summary>
        /// 客户端连接socket
        /// </summary>
        public Socket _Socket {
            get; set;
        }
    }

}
