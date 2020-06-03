using Core_v1;
using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB.Basic;

namespace WebSocket_v1
{
    /// <summary>
    /// 服务端WebWebSocket通讯
    /// </summary>
    public sealed class WebWebSocketMain
    {

        private static WebSocketServer m_WebSocketServer = null;
        private static string m_Uri = null;
        private static string m_LocalUri = string.Empty;
        private const string m_Prefix = "[web WebSocket]client send data to server,";

        /// <summary>
        /// 开启WebWebSocket
        /// </summary>
        public static void Start()
        {
            try
            {
                m_Uri = Call_ParamUtil.GetParamValueByName("WebWebSocket");
                string[] m_UriSplit = m_Uri.Split(':');
                WebWebSocketMain.m_LocalUri = $"ws://0.0.0.0:{m_UriSplit[m_UriSplit.Length - 1]}";
                WebWebSocketMain.m_WebSocketServer = new WebSocketServer(WebWebSocketMain.m_LocalUri);
                FleckLog.LogAction = (LogLevel logLevel, string msg, Exception ex) =>
                {
                    //Log.Instance.Success($"[WebSocket_v1][WebWebSocketMain][Start][FleckLog.LogAction][取消Fleck日志功能]");
                };
                WebWebSocketMain.m_WebSocketServer.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        Log.Instance.Success($"[WebSocket_v1][WebWebSocketMain][Start][m_WebSocketServer][OnOpen][login,guest,{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                    };
                    socket.OnClose = () =>
                    {
                        Log.Instance.Success($"[WebSocket_v1][WebWebSocketMain][Start][m_WebSocketServer][OnClose][{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                        WebWebSocketDo.OnClose(socket);
                    };
                    socket.OnMessage = message =>
                    {
                        WebWebSocketDo.MainStep(socket, message);
                    };
                    socket.OnBinary = e =>
                    {
                        ///处理完整的e
                        ///后续补充完整
                        {

                        }
                    };
                    socket.OnError = (Exception ex) =>
                    {
                        Log.Instance.Fail($"[WebSocket_v1][WebWebSocketMain][Start][m_WebSocketServer][OnError][{ex.Message}][{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                    };
                });
                Log.Instance.Success($"[WebSocket_v1][WebWebSocketMain][Start][m_WebSocketServer][start web WebSocket,{WebWebSocketMain.m_LocalUri}]");
            }
            catch (WebSocketException ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketMain][Start][WebSocketException][{ex.Message}]");
            }
            catch (ArgumentNullException ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketMain][Start][ArgumentNullException][{ex.Message}]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketMain][Start][Exception][{ex.Message}]");
            }
        }
        /// <summary>
        /// 服务端Socket实例
        /// </summary>
        public static WebSocketServer WebSocketServer
        {
            get
            {
                return WebWebSocketMain.m_WebSocketServer;
            }
        }
        /// <summary>
        /// 日志前缀
        /// </summary>
        public static string Prefix
        {
            get
            {
                return WebWebSocketMain.m_Prefix;
            }
        }
        /// <summary>
        /// 停止WebWebSocket
        /// </summary>
        public static void Stop()
        {
            try
            {
                if (WebWebSocketMain.m_WebSocketServer == null)
                    return;
                WebWebSocketMain.m_WebSocketServer.Dispose();
                WebWebSocketMain.m_WebSocketServer = null;
                Log.Instance.Success($"[WebSocket_v1][WebWebSocketMain][Stop][m_WebSocketServer][stop web WebSocket,{WebWebSocketMain.m_LocalUri}]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketMain][Stop][Exception][{ex.Message}]");
            }
        }
    }
}
