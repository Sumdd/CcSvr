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
using DB.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebSocket_v1 {
    internal sealed class InWebSocketDo {
        internal static void MainStep(IWebSocketConnection socket, string message) {
            try {
                if (message != null &&
                    message.Length >= m_mWebSocketJsonPrefix._m_sPrefix.Length &&
                    message.StartsWith(m_mWebSocketJsonPrefix._m_sPrefix))
                {
                    //小金额自动外呼
                    m_cInWebSocketWebApiDo.MainStep(socket, message.Substring(m_mWebSocketJsonPrefix._m_sPrefix.Length));
                    return;
                }

                if (message != null &&
                    message.Length >= m_mWebSocketJsonPrefix._m_sHttpCmd.Length &&
                    message.StartsWith(m_mWebSocketJsonPrefix._m_sHttpCmd))
                {
                    ///<![CDATA[
                    /// 网页快捷命令
                    /// 1.话机拨号.
                    /// 2.拨号后准确的获取到对应的录音,可能在表里加字段.
                    /// 3.录音下载,这个改到HTTP上.
                    /// 4.录音试听,这个改到HTTP上.
                    /// 5.来电弹屏怎么弄,目前只有一个轮询可以使用,其他还真没有其他方法.
                    /// ]]>
                    m_cInWebSocketWebApiDo.MainStep(socket, message.Substring(m_mWebSocketJsonPrefix._m_sHttpCmd.Length));
                    return;
                }

                if (message != null &&
                    message.Length >= m_mWebSocketJsonPrefix._m_sFSCmd.Length &&
                    message.StartsWith(m_mWebSocketJsonPrefix._m_sFSCmd))
                {
                    ///<![CDATA[
                    /// 1.发送消息至freeswitch
                    /// 2.发送消息至服务端处理事宜
                    /// {JSON-FS-CMD}CMD
                    /// ]]>
                    m_cInWebSocketWebApiDo.MainStep(socket, message.Substring(m_mWebSocketJsonPrefix._m_sFSCmd.Length));
                    return;
                }

                if (message.Length <= 0 || !message.Contains("{") || !message.Contains("}")) {
                    Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][MainStep][error message:{message}]");
                    ///socket.Send(M_WebSocketSend._bhzt_fail("拨号CMD有误"));
                    socket.Send(M_WebSocketSend._bhzt_fail("Upd有新版本"));
                    return;
                }

                ArrayList arrayList = SocketMain.CutSocketData(message);
                if(arrayList == null)
                    return;
                foreach(string item in arrayList) {
                    //Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][MainStep][{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}][{message}]");
                    SocketInfo dataStack = new SocketInfo(item);
                    switch(SocketMain.GetHeader(dataStack)) {
                        case M_WebSocket._ljfwq:
                            _ljfwq_do(socket, dataStack);
                            break;
                        case M_WebSocket._bddh:
                            _bddh_do(socket, dataStack);
                            break;
                        case M_WebSocket._bhzt:
                            _bhzt_do(socket, dataStack);
                            break;
                        case M_WebSocket._zdwh:
                            _zdwh_do(dataStack);
                            break;
                        default:
                            Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][MainStep][default][{InWebSocketMain.Prefix} invalid type]");
                            break;
                    }
                }
            } catch(ArgumentNullException ex) {
                Log.Instance.Error($"[WebSocket_v1][InWebSocketDo][MainStep][ArgumentNullException][{ex.Message}][data:{message}]");
            } catch(Exception ex) {
                Log.Instance.Error($"[WebSocket_v1][InWebSocketDo][MainStep][Exception][{ex.Message}][data:{message}]");
            }
        }

        #region 退出时移除
        public static void OnClose(IWebSocketConnection socket)
        {
            try
            {
                var _agentEntity = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_websocket == socket);
                if (_agentEntity == null)
                    throw new Exception($"can't find WebSocket,{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort} user");
                _agentEntity.LoginState = false;
                if (_agentEntity.ChInfo == null)
                    throw new Exception($"can't find WebSocket,{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort} channel");
                if (_agentEntity.ChInfo.channel_websocket != null)
                    _agentEntity.ChInfo.channel_websocket = null;
                Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][OnClose][user {_agentEntity.AgentID},{_agentEntity.AgentName},{_agentEntity.AgentNum},logout WebSocket]");
                call_agent_basic.UpdateAgentLoginState("0", $"{socket.ConnectionInfo.ClientIpAddress}", _agentEntity.AgentID.ToString());
                Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][OnClose][update user {_agentEntity.AgentID},logout IP:{socket.ConnectionInfo.ClientIpAddress},logout state]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][InWebSocketDo][OnClose][Exception][{ex.Message}][client WebSocket connect info:{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
            }
            finally
            {
                InWebSocketMain.WebSocketClose(socket);
            }
        }
        #endregion

        #region 连接服务器方法
        /// <summary>
        /// 连接服务器方法
        /// </summary>
        private async static void _ljfwq_do(IWebSocketConnection socket, SocketInfo dataStack) {
            try {
                string _id = SocketMain.GetBody(dataStack, M_WebSocket._ljfwq, 0);
                string _ip = SocketMain.GetBody(dataStack, M_WebSocket._ljfwq, 1);

                call_agent_basic.UpdateAgentLoginState("1", _ip, _id);
                Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_ljfwq_do][update user {_id},login IP:{_ip},login state]");
                await socket.Send(M_WebSocketSend._ljfwqjg_success());

                var _agentEntity = call_factory.agent_list.FirstOrDefault(x => x.AgentID.ToString() == _id);
                if(_agentEntity == null || _agentEntity.ChInfo.nCh < 0)
                    throw new Exception($"can't find user {_id}");

                int nCh = _agentEntity.ChInfo.nCh;
                _agentEntity.LoginState = true;

                if(_agentEntity.ChInfo.channel_websocket != null) {
                    if(_agentEntity.ChInfo.channel_websocket.IsAvailable) {
                        string m_sIP = _agentEntity.ChInfo.channel_websocket.ConnectionInfo.ClientIpAddress;
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_ljfwq_do][{m_sIP} -> {_ip}]");
                        if (m_sIP != _ip) {
                            await _agentEntity.ChInfo.channel_websocket.Send(M_WebSocketSend._ljfwqjg_more(_ip));
                            Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][_ljfwq_do][get previous WebSocket,send multi point login info]");
                        } else
                            Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_ljfwq_do][get previous WebSocket,but not send multi point login info]");
                    } else
                        Log.Instance.Fail($"[WebSocket_v1][InWebSocketDo][_ljfwq_do][can't get WebSocket,can't send multi point login info]");
                } else {
                    Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_ljfwq_do][no login,point is {_ip}]");
                }
                _agentEntity.ChInfo.channel_websocket = socket;
                _agentEntity.ChInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_ljfwq_do][update user WebSocket,{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort}]");
            } catch(Exception ex) {
                Log.Instance.Error($"[WebSocket_v1][InWebSocketDo][_ljfwq_do][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region 拨号操作
        /// <summary>
        /// 拨号操作
        /// </summary>
        private static void _bddh_do(IWebSocketConnection socket, SocketInfo dataStack) {
            try {
                string _inOrOutbound = Call_ParamUtil.GetParamValueByName("InOrOutbound");
                switch(_inOrOutbound) {
                    case "Outbound":
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_bddh_do][外联模式拨号]");
                        string _id = SocketMain.GetBody(dataStack, M_WebSocket._bddh, 0);
                        string _number = SocketMain.GetBody(dataStack, M_WebSocket._bddh, 1);
                        string _type = SocketMain.GetBody(dataStack, M_WebSocket._bddh, 2);
                        if(_type == ParamLib.DialInRole) {
                            Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_bddh_do][内线拨号规则]");
                            SocketMain._bddh_in(_id, _number, socket);
                        } else if(_type == ParamLib.DialOutRole) {
                            Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_bddh_do][外线拨号规则]");
                            SocketMain._bddh_out(_id, _number, socket);
                        } else {
                            Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_bddh_do][无效拨号规则]");
                        }
                        break;
                    case "Inbound":
                    default:
                        switch(Call_ParamUtil.IEM_Do) {
                            case "2":
                                Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_bddh_do][内联模式拨号,早期媒体不算入通话时间]");
                                socket.Send(M_WebSocketSend._bhzt_fail("_bddh_do_2未投入使用,请联系管理员修改拨号方式"));
                                break;
                            default:
                                Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_bddh_do][inbound dial]");
                                CenoSocket.m_fDialClass.m_fExecuteDial(socket, dataStack.Content);
                                //SocketMain._bddh_do_1(socket, dataStack.Content);
                                break;
                        }
                        break;
                }
            } catch(Exception ex) {
                Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_bddh_do][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region 拨号状态
        private static void _bhzt_do(IWebSocketConnection socket, SocketInfo dataStack) {
            try {
                var _agentEntity = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_websocket == socket);
                if(_agentEntity == null)
                    throw new Exception($"not find WebSocket,{socket.ConnectionInfo.Id},{socket.ConnectionInfo.ClientIpAddress}:{socket.ConnectionInfo.ClientPort} user");
                else {
                    SocketMain._bhzt_do(socket, dataStack.Content);
                }
            } catch(Exception ex) {
                Log.Instance.Error($"[WebSocket_v1][InWebSocketDo][_bhzt_do][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region ***缓存重载激活
        public static void _zdwh_do(SocketInfo dataStack)
        {
            string m_sType = SocketMain.GetBody(dataStack, M_WebSocket._zdwh, 0);
            switch (m_sType)
            {
                case "UpdLoginName":
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> channel][ua update login name]");
                        //查出所有坐席信息
                        List<call_agent_model> m_lAgent = new List<call_agent_model>(call_agent_basic.GetList());
                        call_factory.agent_list.ForEach(x =>
                        {
                            string m_sLoginName = m_lAgent.FirstOrDefault(q => q.ID == x.AgentID)?.LoginName;
                            if (string.IsNullOrWhiteSpace(m_sLoginName) && x.LoginName != m_sLoginName)
                                x.LoginName = m_sLoginName;
                        });
                    }
                    break;
                case "PCR":
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> channel][channel is register]");
                        //查出所有通道PC注册信息
                        List<call_channel_model> m_lChannelList = new List<call_channel_model>(call_channel.GetList());
                        call_factory.channel_list.ForEach(x =>
                        {
                            int? IsRegister = m_lChannelList.FirstOrDefault(q => q.ID == x.channel_id)?.IsRegister;
                            if (IsRegister != null)
                            {
                                int m_iIsRegister = Convert.ToInt32(IsRegister);
                                if (x.IsRegister != m_iIsRegister)
                                    x.IsRegister = m_iIsRegister;
                            }
                        });
                        call_factory.agent_list.ForEach(x =>
                        {
                            int? IsRegister = m_lChannelList.FirstOrDefault(q => q.ID == x.ChInfo?.channel_id)?.IsRegister;
                            if (IsRegister != null)
                            {
                                int m_iIsRegister = Convert.ToInt32(IsRegister);
                                if (x.ChInfo.IsRegister != m_iIsRegister)
                                    x.ChInfo.IsRegister = m_iIsRegister;
                            }
                        });
                    }
                    break;
                case "SHARE":
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> redis][update share]");
                        InWebSocketMain.m_fLoadShare?.Invoke(false);
                    }
                    break;
                case "AREA":
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> redis][update share area]");
                        DB.Basic.m_fDialLimit.m_fGetDialArea();
                        Core_v1.Redis2.m_fSetDialArea();
                    }
                    break;
                default:
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do][auto channel activate]");
                        CenoFsSharp.m_fQueueTask.m_fActivate();
                    }
                    break;
            }
        }
        #endregion
    }
}
