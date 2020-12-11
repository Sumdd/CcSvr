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
        private static void _bhzt_do(IWebSocketConnection socket, SocketInfo dataStack)
        {
            ///扩展,防止出现通道繁忙
            Hashtable m_pHashtable = dataStack.Content;
            string[] m_aSocketCmdArray = call_socketcommand_util.GetParamByHeadName("BHZT");
            string m_sKey = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[0]);
            string m_sUa = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[1]);
            switch (m_sKey)
            {
                case "AHang":
                case "BHang":
                case "XHang":
                    {
                        #region ***强断
                        ///坐席
                        int m_uAgentID = Convert.ToInt32(m_sUa);
                        AGENT_INFO m_mAgent = call_factory.agent_list.Find(x => x.AgentID == m_uAgentID);
                        if (m_mAgent == null)
                        {
                            Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][_bhzt_do][{m_uAgentID} no ua]");
                            break;
                        }
                        ChannelInfo m_mChannel = m_mAgent.ChInfo;
                        if (m_mChannel == null)
                        {
                            Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][_bhzt_do][{m_uAgentID} no channel]");
                            break;
                        }
                        ///根据来源修正状态
                        if (m_sKey == "AHang") m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_AHANGUP;
                        else if (m_sKey == "BHang") m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_BHANGUP;
                        Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][_bhzt_do][{m_uAgentID} {m_sKey}]");
                        if (m_sKey == "XHang")
                        {
                            string m_sUUID = m_mChannel.channel_call_uuid_after;
                            if (string.IsNullOrWhiteSpace(m_sUUID)) m_sUUID = m_mChannel.channel_call_uuid;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_uuid_after = null;
                            SocketMain.m_fKill(m_uAgentID, m_sUUID);
                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_other_uuid = null;
                        }
                        else
                        {
                            string m_sUUID = m_mChannel.channel_call_uuid;
                            m_mChannel.channel_call_uuid = null;
                            SocketMain.m_fKill(m_uAgentID, m_sUUID);
                            if (string.IsNullOrWhiteSpace(m_sUUID)) m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        }
                        #endregion
                    }
                    break;
            }
        }
        #endregion

        #region ***缓存重载激活
        private static object m_pUpdUaLock = new object();
        private static bool m_bUpdUaDoing = false;
        public static void _zdwh_do(SocketInfo dataStack)
        {
            string m_sType = SocketMain.GetBody(dataStack, M_WebSocket._zdwh, 0);
            switch (m_sType)
            {
                case "ReloadInlimit_2":
                    {
                        Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> channel][reload inlimit_2,开发中...]");
                        ///DB.Basic.m_cInrule.m_fInit();
                    }
                    break;
                case "ReloadInrule":
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> channel][reload inrule]");
                        DB.Basic.m_cInrule.m_fInit();
                    }
                    break;
                case "ReloadWbList":
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> channel][reload wblist]");
                        DB.Basic.m_cWblist.m_fInit();
                    }
                    break;
                case "ReloadRoute":
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> channel][reload route]");
                        DB.Basic.m_cRoute.m_fInit();
                    }
                    break;
                case "UpdUa":
                    {
                        Log.Instance.Success($"[WebSocket_v1][InWebSocketDo][_zdwh_do -> channel][ua update]");
                        //单线更新
                        lock (InWebSocketDo.m_pUpdUaLock)
                        {
                            try
                            {
                                if (m_bUpdUaDoing)
                                {
                                    Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][UpdUa][doing]");
                                    return;
                                }

                                m_bUpdUaDoing = true;

                                #region ***先更新通道信息
                                string m_sSQL1 = $" AND ID NOT IN ('{string.Join("','", call_factory.channel_list.Select(x => x.channel_id))}') ";
                                List<call_channel_model> m_lChannel = new List<call_channel_model>(call_channel.GetList(m_sSQL1));
                                if (m_lChannel?.Count > 0)
                                {
                                    int nCh = call_factory.channel_list.Count;
                                    m_lChannel.ForEach(x =>
                                    {
                                        call_factory.channel_list.Add(new ChannelInfo()
                                        {
                                            nCh = nCh++,
                                            channel_id = x.ID,
                                            channel_uniqueid = x.UniqueID,
                                            channel_type = x.ChType,
                                            channel_number = x.ChNum,
                                            channel_call_uuid = string.Empty,
                                            channel_call_type = new CALLTYPE(),
                                            channel_caller_number = new StringBuilder(),
                                            channel_callee_number = new StringBuilder(),
                                            channel_call_dtmf = null,

                                            ///<![CDATA[
                                            /// 减压
                                            /// ]]>
                                            //channel_account_info = call_factory.fs_account_list.FirstOrDefault(x => x.user == _model.ChNum),
                                            channel_call_status = APP_USER_STATUS.FS_USER_IDLE,

                                            ///<![CDATA[
                                            /// 减压
                                            /// ]]>
                                            //channel_call_record_info = CH_CALL_RECORD.Instance()

                                            ///<![CDATA[
                                            /// IsRegister 可选值
                                            /// 1.1注册
                                            /// 2.0不注册,一开始暂定的IP话机模式
                                            /// 3.-1不注册,IP话机Web模式,可使用网页进行拨打
                                            /// ]]>
                                            IsRegister = x.IsRegister
                                        });
                                    });
                                    Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][UpdChannel][{m_lChannel?.Count}]");
                                }
                                #endregion
                                #region ***再更新Ua
                                string m_sSQL2 = $" AND ID NOT IN ('{string.Join("','", call_factory.agent_list.Select(x => x.AgentID))}') ";
                                List<call_agent_model> m_lAgent = new List<call_agent_model>(call_agent_basic.GetList(1000, m_sSQL2));
                                if (m_lAgent?.Count > 0)
                                {
                                    m_lAgent.ForEach(x =>
                                    {
                                        call_factory.agent_list.Add(new AGENT_INFO()
                                        {
                                            AgentID = x.ID,
                                            AgentUUID = x.UniqueID,
                                            LoginName = x.LoginName,
                                            AgentName = x.AgentName,
                                            LoginPsw = x.LoginPassWord,
                                            LastLoginIp = x.LastLoginIp,

                                            ///<![CDATA[
                                            /// 减压,没必要再到数据库进行查询
                                            /// ]]>
                                            ChInfo = call_factory.channel_list.FirstOrDefault(y => y.channel_id == x.ChannelID),
                                            //call_factory.channel_list.FirstOrDefault(x => x.channel_number == call_channel.GetModel(cam.ChannelID).ChNum),
                                            AgentNum = x.AgentNumber,

                                            ///<![CDATA[
                                            /// 减压,这个没用,有用也没必要这样写
                                            /// ]]>
                                            //RoleName = call_role.GetModel(cam.RoleID).RoleName,
                                            //TeamName = call_team.GetModel(cam.TeamID).TeamName,
                                            LoginState = false
                                        });
                                    });
                                    Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][UpdUa][{m_lAgent?.Count}]");
                                }
                                #endregion
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Warn($"[WebSocket_v1][InWebSocketDo][UpdUa][Exception][{ex.Message}]");
                            }
                            finally
                            {
                                m_bUpdUaDoing = false;
                            }
                        }
                    }
                    break;
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
