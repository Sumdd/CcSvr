using CenoSocket;
using Model_v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoSocket {
    /// <summary>
    /// WebSocket发送模型封装一下
    /// </summary>
    public class M_WebSocketSend {

        #region 连接服务器结果
        /// <summary>
        /// 连接服务器结果
        /// </summary>
        /// <param name="args">参数对</param>
        /// <returns></returns>
        public static string _ljfwqjg(params string[] args) {
            return SocketCommand.SendCommonStr("LJFWQJG", args);
        }
        /// <summary>
        /// 连接服务器结果,成功
        /// </summary>
        /// <returns></returns>
        public static string _ljfwqjg_success() {
            return _ljfwqjg("Success", "连接成功");
        }
        /// <summary>
        /// 连接服务器结果,关闭,多点登录
        /// </summary>
        /// <returns></returns>
        public static string _ljfwqjg_more(string ip) {
            return _ljfwqjg("More", $"多点登录{ip}");
        }
        /// <summary>
        /// 连接服务器结果,关闭,服务端退WebSocket退出
        /// </summary>  
        /// <returns></returns>
        public static string _ljfwqjg_exit() {
            return _ljfwqjg("Exit", "服务器退出");
        }
        #endregion

        #region 拨号状态
        /// <summary>
        /// 拨号状态
        /// </summary>
        /// <param name="args">参数对</param>
        /// <returns></returns>
        public static string _bhzt(params string[] args) {
            return SocketCommand.SendCommonStr("BHZT", args);
        }
        /// <summary>
        /// 拨号状态:失败
        /// </summary>
        /// <param name="_reason">原因</param>
        /// <returns></returns>
        public static string _bhzt_fail(string _reason) {
            return _bhzt(M_WebSocket._bhzt_fail, _reason);
        }
        /// <summary>
        /// 拨号状态:摘机
        /// </summary>
        /// <param name="_reason">原因:发起呼叫成功</param>
        /// <returns></returns>
        public static string _bhzt_pick(string _reason = "发起呼叫成功") {
            return _bhzt(M_WebSocket._bhzt_pick, _reason);
        }
        /// <summary>
        /// 拨号状态:挂断
        /// </summary>
        /// <param name="_reason">原因:挂机或对方挂机;直接显示即可</param>
        /// <returns></returns>
        public static string _bhzt_hang(string _reason)
        {
            return _bhzt(M_WebSocket._bhzt_hang, _reason);
        }
        #endregion

        #region 发送录音
        /// <summary>
        /// 发送录音
        /// </summary>
        /// <param name="args">参数对</param>
        /// <returns></returns>
        public static string _fsly(params string[] args) {
            return SocketCommand.SendCommonStr("FSLY", args);
        }
        #endregion
    }
}
