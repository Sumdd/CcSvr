using Core_v1;
using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB.Basic;
using Model_v1;

namespace WebSocket_v1 {
    /// <summary>
    /// 服务端内部WebSocket通讯
    /// </summary>
    public sealed class InWebSocketMain {

        private static WebSocketServer m_WebSocketServer = null;
        private static string          m_Uri             = null;
        private static string          m_LocalUri        = string.Empty;
        private const string           m_Prefix          = "[internal WebSocket]client send data to server,";
        ///<![CDATA[
        /// 由于传参问题这里写成委托
        /// ]]>
        public delegate m_mWebSocketJson m_dDialTask(string m_sSendMessage, string m_sUse, string m_sUUID);
        public static m_dDialTask m_fDialTask;
        /// <summary>
        /// 重新加载Redis
        /// </summary>
        /// <param name="m_bMain"></param>
        public delegate void m_dLoadShare(bool m_bLoad);
        public static m_dLoadShare m_fLoadShare;
        /// <summary>
        /// 开启内部WebSocket
        /// </summary>
        public static void Start() {
            try {
                m_Uri = Call_ParamUtil.GetParamValueByName("InWebSocket");
                string[] m_UriSplit = m_Uri.Split(':');
                InWebSocketMain.m_LocalUri = $"ws://0.0.0.0:{m_UriSplit[m_UriSplit.Length - 1]}";
                InWebSocketMain.m_WebSocketServer = new WebSocketServer(InWebSocketMain.m_LocalUri);
                InWebSocketMain.m_WebSocketServer.RestartAfterListenError = true;
                FleckLog.LogAction = (LogLevel logLevel, string msg, Exception ex) => {
                    if (logLevel == LogLevel.Warn)
                    {
                        Log.Instance.Warn($"[WebSocket_v1][InSocketMain][Start][FleckLog.LogAction][{msg}]");
                        if (ex != null)
                        {
                            Log.Instance.Error($"[WebSocket_v1][InSocketMain][Start][FleckLog.LogAction][{ex?.Message}]");
                            Log.Instance.Error($"[WebSocket_v1][InSocketMain][Start][FleckLog.LogAction][{ex?.StackTrace}]");
                        }
                    }
                    else if (logLevel == LogLevel.Error)
                    {
                        Log.Instance.Error($"[WebSocket_v1][InSocketMain][Start][FleckLog.LogAction][{msg}]");
                        if (ex != null)
                        {
                            Log.Instance.Error($"[WebSocket_v1][InSocketMain][Start][FleckLog.LogAction][{ex?.Message}]");
                            Log.Instance.Error($"[WebSocket_v1][InSocketMain][Start][FleckLog.LogAction][{ex?.StackTrace}]");
                        }
                    }
                };
                InWebSocketMain.m_WebSocketServer.Start(socket => {
                    socket.OnOpen = () => {
                        Log.Instance.Success($"[WebSocket_v1][InSocketMain][Start][m_WebSocketServer][OnOpen][login,guest,{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                    };
                    socket.OnClose = () => {
                        Log.Instance.Success($"[WebSocket_v1][InSocketMain][Start][m_WebSocketServer][OnClose][{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                        InWebSocketDo.OnClose(socket);
                    };
                    socket.OnMessage = message => {

                        Log.Instance.Debug("From Client WebSocket Start");
                        Log.Instance.Debug(message);
                        Log.Instance.Debug("From Client WebSocket End");

                        InWebSocketDo.MainStep(socket, message);
                    };
                    socket.OnError = (Exception ex) => {
                        Log.Instance.Fail($"[WebSocket_v1][InSocketMain][Start][m_WebSocketServer][OnError][{ex.Message}][{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                        InWebSocketMain.WebSocketClose(socket);
                    };
                });
                Log.Instance.Success($"[WebSocket_v1][InSocketMain][Start][m_WebSocketServer][start internal WebSocket,{InWebSocketMain.m_LocalUri}]");
            } catch(WebSocketException ex) {
                Log.Instance.Error($"[WebSocket_v1][InSocketMain][Start][WebSocketException][{ex.Message}]");
            } catch(ArgumentNullException ex) {
                Log.Instance.Error($"[WebSocket_v1][InSocketMain][Start][ArgumentNullException][{ex.Message}]");
            } catch(Exception ex) {
                Log.Instance.Error($"[WebSocket_v1][InSocketMain][Start][Exception][{ex.Message}]");
            }
        }
        /// <summary>
        /// 服务端Socket实例
        /// </summary>
        public static WebSocketServer WebSocketServer {
            get {
                return InWebSocketMain.m_WebSocketServer;
            }
        }
        /// <summary>
        /// 日志前缀
        /// </summary>
        public static string Prefix {
            get {
                return InWebSocketMain.m_Prefix;
            }
        }
        /// <summary>
        /// 停止内部WebSocket
        /// </summary>
        public static void Stop() {
            try {
                if(InWebSocketMain.m_WebSocketServer == null)
                    return;
                InWebSocketMain.m_WebSocketServer.Dispose();
                InWebSocketMain.m_WebSocketServer = null;
                Log.Instance.Success($"[WebSocket_v1][InSocketMain][Stop][m_WebSocketServer][stop internal WebSocket,{InWebSocketMain.m_LocalUri}]");
            } catch(Exception ex) {
                Log.Instance.Error($"[WebSocket_v1][InSocketMain][Stop][Exception][{ex.Message}]");
            }
        }
        /// <summary>
        /// 停止客户端连接过来的WebSocket
        /// </summary>
        public static void WebSocketClose(IWebSocketConnection socket)
        {
            try
            {
                if (socket != null)
                    socket?.Close();
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][WebSocketClose][Exception][{ex.Message}]");
            }
        }
    }
}
