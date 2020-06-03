using Core_v1;
using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocket_v1 {
    /// <summary>
    /// 服务端外部Socket通讯,将其整理成api,外部可以进行调用
    /// </summary>
    public sealed class OutWebSocketMain {

        private static List<IWebSocketConnection> m_IWebSocketConnections = null;
        private static WebSocketServer            m_WebSocketServer       = null;
        private const string                      m_Prefix                = "[外部WebSocket]客户端向服务端发送数据后,";

        public static void Start() {
            try {
                m_IWebSocketConnections = new List<IWebSocketConnection>();
                m_WebSocketServer = new WebSocketServer("ws://192.168.0.220:9461");
                m_WebSocketServer.Start(socket => {
                    socket.OnOpen = () => {
                        m_IWebSocketConnections.Add(socket);
                    };
                    socket.OnClose = () => {
                        m_IWebSocketConnections.Remove(socket);
                    };
                    socket.OnMessage = message => {
                        OutWebSocketDo.MainStep(socket, message);
                    };
                });
            } catch(WebSocketException ex) {
                Log.Instance.Error($"[OutWebSocketMain][Start][WebSocketException][{ex.Message}]");
            } catch(ArgumentNullException ex) {
                Log.Instance.Error($"[OutWebSocketMain][Start][ArgumentNullException][{ex.Message}]");
            } catch(Exception ex) {
                Log.Instance.Error($"[OutWebSocketMain][Start][Exception][{ex.Message}]");
            }
        }

        /* 目前这个退出不成熟 */
        public static void Stop() {
            try {
                /* 如果循环发送退出,期间会移除socket,也就是会出现集合改变的问题 */
                foreach(IWebSocketConnection _IWebSocketConnection in m_IWebSocketConnections) {
                    _IWebSocketConnection.Send("exit");
                }
            } catch(Exception ex) {
                Log.Instance.Error($"[OutWebSocketMain][Stop][Exception][{ex.Message}]");
            }
            m_WebSocketServer.Dispose();
        }

        /// <summary>
        /// 日志前缀
        /// </summary>
        public static string Prefix {
            get {
                return m_Prefix;
            }
        }
    }
}
