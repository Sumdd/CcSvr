using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core_v1;
using Fleck;
using Model_v1;
using System.Collections;
using CenoSocket;
using DB.Basic;
using CenoSipFactory;
using Outbound_v1;
using CenoFsSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebSocket_v1
{
    internal sealed class WebWebSocketDo
    {
        internal static void MainStep(IWebSocketConnection socket, string message)
        {
            try
            {
                WebWebSocketModel m_mWebWebSocketModel = JsonConvert.DeserializeObject<WebWebSocketModel>(message);
                JObject m_pJObject = JObject.Parse(m_mWebWebSocketModel.data.ToString());
                switch (m_mWebWebSocketModel.type)
                {
                    case WebWebSocketType.Login:
                        {
                            string m_sID = m_pJObject.GetValue("id")?.ToString();
                            string m_sUUID = m_pJObject.GetValue("uuid")?.ToString();
                            string m_sType = m_pJObject.GetValue("type")?.ToString();
                            WebWebSocketDo.m_fLogin(socket, m_sID, m_sUUID, m_sType);
                        }
                        break;
                    case WebWebSocketType.Dial:
                    case WebWebSocketType.Push:
                        {
                            string m_sID = m_pJObject.GetValue("id")?.ToString();
                            string m_sUUID = m_pJObject.GetValue("uuid")?.ToString();
                            string m_sNumber = m_pJObject.GetValue("number")?.ToString();
                            WebWebSocketDo.m_fPushOrDial(socket, m_sID, m_sUUID, m_sNumber, m_mWebWebSocketModel.type);
                        }
                        break;
                    case WebWebSocketType.Dial_R:
                    case WebWebSocketType.Push_R:
                        {
                            string m_sUUID = m_pJObject.GetValue("uuid")?.ToString();
                            WebWebSocketDo.m_fPushOrDialReply(m_sUUID, message);
                        }
                        break;
                    default:
                        {
                            Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][MainStep][default][error type:{m_mWebWebSocketModel.type}]");
                        }
                        break;
                }
            }
            catch (ArgumentNullException ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketDo][MainStep][ArgumentNullException][{ex.Message}][data:{message}]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketDo][MainStep][Exception][{ex.Message}][data:{message}]");
            }
        }

        #region ***登陆
        public static void m_fLogin(IWebSocketConnection m_pWebSocket, string m_sID, string m_sUUID, string m_sType)
        {
            try
            {
                AGENT_INFO m_mAgent = call_factory.agent_list.FirstOrDefault(x => x.AgentUUID == m_sUUID);
                WebWebSocketModel m_mWebWebSocket = new WebWebSocketModel();
                if (m_mAgent == null)
                {
                    m_mWebWebSocket.type = WebWebSocketType.Login;
                    m_mWebWebSocket.data = new
                    {
                        id = m_sID,
                        status = -1,
                        msg = $"无此用户"
                    };
                    WebWebSocketDo.m_fSendObject(m_pWebSocket, m_mWebWebSocket);
                    Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fLogin][not find {m_sUUID}]");
                    return;
                }
                Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][m_fLogin][{m_sUUID} -> {m_mAgent.AgentID}]");
                if (m_mAgent.ChInfo != null)
                {
                    switch (m_sType)
                    {
                        case WebWebSocketType.P:
                            {
                                if (m_mAgent.ChInfo.channel_websocket_P != null)
                                {
                                    m_mWebWebSocket.type = WebWebSocketType.Logout;
                                    m_mWebWebSocket.data = new
                                    {
                                        id = m_sID,
                                        type = WebWebSocketType.P,
                                        status = 0,
                                        msg = $"多点登录IP:{m_pWebSocket.ConnectionInfo.ClientIpAddress}:{m_pWebSocket.ConnectionInfo.ClientPort}"
                                    };
                                    Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fLogin][send multi login info to WebSocket P]");
                                    WebWebSocketDo.m_fSendObject(m_mAgent.ChInfo.channel_websocket_P, m_mWebWebSocket);
                                }
                                m_mAgent.ChInfo.channel_websocket_P = m_pWebSocket;
                                m_mWebWebSocket.data = new
                                {
                                    id = m_sID,
                                    status = 0,
                                    msg = $"登录成功"
                                };
                            }
                            break;
                        case WebWebSocketType.W:
                            {
                                if (m_mAgent.ChInfo.channel_websocket_W != null)
                                {
                                    m_mWebWebSocket.type = WebWebSocketType.Logout;
                                    m_mWebWebSocket.data = new
                                    {
                                        id = m_sID,
                                        type = WebWebSocketType.W,
                                        status = 0,
                                        msg = $"多点登录IP:{m_pWebSocket.ConnectionInfo.ClientIpAddress}:{m_pWebSocket.ConnectionInfo.ClientPort}"
                                    };
                                    Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fLogin][send multi login info to WebSocket W]");
                                    WebWebSocketDo.m_fSendObject(m_mAgent.ChInfo.channel_websocket_W, m_mWebWebSocket);
                                }
                                m_mAgent.ChInfo.channel_websocket_W = m_pWebSocket;
                                m_mWebWebSocket.data = new
                                {
                                    id = m_sID,
                                    status = 0,
                                    RecHTTP = DB.Basic.Call_ParamUtil.m_sDialTaskRecDownLoadHTTP,
                                    RecDefaultExt = DB.Basic.Call_ParamUtil._rec_t,
                                    msg = $"登录成功"
                                };
                            }
                            break;
                        default:
                            {
                                m_mWebWebSocket.type = WebWebSocketType.Login;
                                m_mWebWebSocket.data = new
                                {
                                    id = m_sID,
                                    status = -1,
                                    msg = $"未知登录方类型"
                                };
                                WebWebSocketDo.m_fSendObject(m_pWebSocket, m_mWebWebSocket);
                                Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fLogin][{m_mAgent.AgentID} error login type]");
                            }
                            return;
                    }
                    m_mWebWebSocket.type = WebWebSocketType.Login;
                    WebWebSocketDo.m_fSendObject(m_pWebSocket, m_mWebWebSocket);
                    Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][m_fLogin][{m_mAgent.AgentID} WebSocket {m_sType} login success]");
                    return;
                }
                m_mWebWebSocket.type = WebWebSocketType.Login;
                m_mWebWebSocket.data = new
                {
                    id = m_sID,
                    status = -1,
                    msg = $"未找到对应通道"
                };
                WebWebSocketDo.m_fSendObject(m_pWebSocket, m_mWebWebSocket);
                Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fLogin][{m_mAgent.AgentID} not find channel]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketDo][m_fLogin][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region ***退出
        public static void OnClose(IWebSocketConnection socket)
        {
            try
            {
                AGENT_INFO m_mAgent_P = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_websocket_P == socket);
                if (m_mAgent_P != null)
                {
                    Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][OnClose][client WebSocket connect info:]");
                    Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][OnClose][{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                    Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][OnClose][{m_mAgent_P.AgentID} find in WebSocket P,and remove it]");
                    if (m_mAgent_P.ChInfo != null)
                    {
                        m_mAgent_P.ChInfo.channel_websocket_P = null;
                    }
                    return;
                }
                Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][OnClose][client WebSocket connect info:not find in WebSocket P]");
                AGENT_INFO m_mAgent_W = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_websocket_W == socket);
                if (m_mAgent_W != null)
                {
                    Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][OnClose][client WebSocket connect info:]");
                    Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][OnClose][{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
                    Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][OnClose][{m_mAgent_W.AgentID} find in WebSocket W,and remove it]");
                    if (m_mAgent_W.ChInfo != null)
                    {
                        m_mAgent_W.ChInfo.channel_websocket_W = null;
                    }
                    return;
                }
                Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][OnClose][client WebSocket connect info:not find in WebSocket W]");
                Log.Instance.Fail($"[WebSocket_v1][WebWebSocketDo][OnClose][{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketDo][OnClose][Exception][{ex.Message}][client WebSocket connect info:{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
            }
        }
        #endregion

        #region ***设置或拨号
        public static void m_fPushOrDial(IWebSocketConnection m_pWebSocket, string m_sID, string m_sUUID, string m_sNumber, string m_sType)
        {
            try
            {
                AGENT_INFO m_mAgent = call_factory.agent_list.FirstOrDefault(x => x.AgentUUID == m_sUUID);
                WebWebSocketModel m_mWebWebSocket = new WebWebSocketModel();
                if (m_mAgent == null)
                {
                    m_mWebWebSocket.type = m_sType;
                    m_mWebWebSocket.data = new
                    {
                        id = m_sID,
                        status = -1,
                        msg = $"无此用户"
                    };
                    WebWebSocketDo.m_fSendObject(m_pWebSocket, m_mWebWebSocket);
                    Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDial][not find {m_sUUID}]");
                    return;
                }
                Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDial][{m_sUUID} -> {m_mAgent.AgentID}]");
                if (m_mAgent.ChInfo != null)
                {
                    if (m_mAgent.ChInfo.channel_websocket_P != null)
                    {
                        Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDial][{m_mAgent.AgentID} WebSocket P connect,send {m_sType} message,number:{m_sNumber}]");
                        m_mWebWebSocket.type = m_sType;
                        m_mWebWebSocket.data = new
                        {
                            id = m_sID,
                            number = m_sNumber
                        };
                        WebWebSocketDo.m_fSendObject(m_mAgent.ChInfo.channel_websocket_P, m_mWebWebSocket);
                    }
                    else
                    {
                        m_mWebWebSocket.type = m_sType;
                        m_mWebWebSocket.data = new
                        {
                            id = m_sID,
                            status = -1,
                            msg = $"Web电话未连接"
                        };
                        WebWebSocketDo.m_fSendObject(m_pWebSocket, m_mWebWebSocket);
                        Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDial][{m_mAgent.AgentID} WebSocket P not connect]");
                    }
                    return;
                }
                m_mWebWebSocket.type = m_sType;
                m_mWebWebSocket.data = new
                {
                    id = m_sID,
                    status = -1,
                    msg = $"未找到对应通道"
                };
                WebWebSocketDo.m_fSendObject(m_pWebSocket, m_mWebWebSocket);
                Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDial][{m_mAgent.AgentID} not find channel]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDial][Exception][{ex.Message}]");
            }

        }
        #endregion

        #region ***设置或拨号反馈
        public static void m_fPushOrDialReply(string m_sUUID, string m_sMessage)
        {
            try
            {
                AGENT_INFO m_mAgent = call_factory.agent_list.FirstOrDefault(x => x.AgentUUID == m_sUUID);
                WebWebSocketModel m_mWebWebSocket = new WebWebSocketModel();
                if (m_mAgent == null)
                {
                    Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDialReply][not find {m_sUUID}]");
                    return;
                }
                if (m_mAgent.ChInfo != null)
                {
                    if (m_mAgent.ChInfo.channel_websocket_W != null)
                    {
                        Log.Instance.Success($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDialReply][{m_mAgent.AgentID} WebSocket W,send reply]");
                        WebWebSocketDo.m_fSendString(m_mAgent.ChInfo.channel_websocket_W, m_sMessage);
                    }
                    else
                    {
                        Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDialReply][{m_mAgent.AgentID} WebSocket W not connect]");
                    }
                    return;
                }
                Log.Instance.Warn($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDialReply][{m_mAgent.AgentID} not find channel]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketDo][m_fPushOrDialReply][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region ***发送信息
        private static void m_fSendString(IWebSocketConnection m_pWebSocket, string m_sMessage)
        {
            try
            {
                m_pWebSocket.Send(m_sMessage);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketDo][m_fSendString][Exception][{ex.Message}]");
            }
        }

        private static void m_fSendObject(IWebSocketConnection m_pWebSocket, object m_oMessage)
        {
            try
            {
                m_fSendString(m_pWebSocket, JsonConvert.SerializeObject(m_oMessage));
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][WebWebSocketDo][m_fSendObject][Exception][{ex.Message}]");
            }
        }
        #endregion
    }
}
