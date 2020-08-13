using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections;
using System.Data;
using System.IO;

using CenoCommon;
using CenoSipFactory;
using DB.Basic;
using System.Threading.Tasks;
using log4net;
using NEventSocket;
using NEventSocket.FreeSwitch;
using NEventSocket.Util;
using System.Reactive.Linq;
using DB.Model;
using CenoFsSharp;
using Fleck;
using Core_v1;
using Model_v1;
using System.Text.RegularExpressions;
using Cmn_v1;

namespace CenoSocket {
    public class SocketMain {

        #region 变量

        public static TcpClient _TcpClient;
        public static TcpListener _TcpServer;
        public static Thread serverThread;

        public delegate void ChangeConnectState(int nCh);
        public static event ChangeConnectState _ChangeConnectState;

        private static ILog _Ilog = LogManager.GetCurrentLoggers()[0];
        #endregion

        #region 开始socket服务
        /// <summary>
        ///  start services
        /// </summary>
        public static KeyValuePair<bool, string> ServicesStart() {
            Task.Factory.StartNew(() => {
                ServerStart();
            });

            return new KeyValuePair<bool, string>(true, "");
        }

        /// <summary>
        /// start services
        /// </summary>
        public static void ServerStart() {
            try {
                _Ilog.Info("正在开启Socket");
                var dt = DB.Basic.Call_ServerListUtil.GetCallServerInfo();
                if(dt.Rows.Count == 1) {
                    var ip = dt.Rows[0]["ServerIP"].ToString();
                    _Ilog.Info("IP:" + ip);
                    var port = dt.Rows[0]["ServerPort"].ToString();
                    _Ilog.Info("Port:" + port);
                    IPEndPoint _IpEndPoint = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                    _TcpServer = new TcpListener(_IpEndPoint);
                    _TcpServer.Start();
                    _Ilog.Info($"开启Socket {ip}:{port}");
                    while(true) {
                        try {
                            while(!_TcpServer.Pending()) {
                                Thread.Sleep(1000);
                            }
                            _TcpClient = _TcpServer.AcceptTcpClient();
                            Thread ScoketTh = new Thread(new ParameterizedThreadStart(ReceiveData));
                            ScoketTh.IsBackground = true;
                            ScoketTh.Start(_TcpClient);
                            //ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveData));
                        } catch(Exception ex) {
                            _Ilog.Error("start monitor client service error.", ex);
                        }
                    }
                } else {
                    _Ilog.Fatal("开启Socket错误:多于一个端口!");
                }
            } catch(Exception ex) {
                _Ilog.Error("开启Socket错误:" + ex.Message);
            }
        }
        #endregion

        #region 停止服务

        /// <summary>
        /// stop services
        /// </summary>
        public static void ServicesStop() {
            try {
                if(_TcpClient != null) {
                    _TcpClient.Close();
                    _TcpClient = null;
                }

                if(_TcpServer != null) {
                    _TcpServer.Stop();
                    _TcpServer = null;
                }

                serverThread.Abort();
                serverThread = null;
            } catch(Exception ex) {
                _Ilog.Error("stop socket monitor failed.", ex);
            }
        }

        #endregion

        #region 接受数据
        /// <summary>
        /// accept client connection event,receive socket data.
        /// </summary>
        /// <param name="State"></param>
        private static void ReceiveData(object State) {
            TcpClient ChTcpClient = (TcpClient)State;
            ChTcpClient.LingerState = new LingerOption(false, 0);
            NetworkStream ns = ChTcpClient.GetStream();
            string IpAddress = ChTcpClient.Client.RemoteEndPoint.ToString();
            string AcceptData = "";
            _Ilog.Info("socket connect: " + IpAddress + " has connected");

            while(ChTcpClient != null && ChTcpClient.Connected) {
                try {
                    lock(ChTcpClient) {
                        if(ChTcpClient.Available < 0)
                            continue;
                        int ReadSize = ChTcpClient.Available;
                        byte[] AcceptBuffer = new byte[ReadSize];
                        ns.Read(AcceptBuffer, 0, ReadSize);
                        AcceptData += Encoding.UTF8.GetString(AcceptBuffer, 0, ReadSize);


                        if(string.IsNullOrEmpty(AcceptData))
                            continue;

                        bool RecFlag = false;
                        string EndChar = string.Empty;
                        foreach(string s in ParamLib.SocketEndStr) {
                            if(AcceptData.Contains(s)) {
                                EndChar = s;
                                RecFlag = true;
                                break;
                            }
                        }

                        if(!RecFlag && string.IsNullOrEmpty(EndChar))
                            continue;

                        JudgeInfo(AcceptData, ChTcpClient.Client);

                        AcceptData = AcceptData.Substring(AcceptData.LastIndexOf(EndChar) + EndChar.Length);
                    }
                } catch(Exception ex) {
                    _Ilog.Error("receive data error.", ex);
                }
            }
            try {
                int nCh = call_factory.agent_list.FirstOrDefault<AGENT_INFO>(x => x.LastLoginIp == IpAddress.Split(':')[0]).ChInfo.nCh;
                if(nCh >= 0 && call_factory.channel_list[nCh].channel_socket._Socket != null) {
                    call_factory.channel_list[nCh].channel_socket._Socket.Close(1000);
                    call_factory.channel_list[nCh].channel_socket._Socket = null;
                    call_factory.channel_list[nCh].channel_socket.IsConnect = false;
                    call_factory.channel_list[nCh].channel_socket.HeartBeatFlag = false;
                }
                if(_ChangeConnectState != null)
                    _ChangeConnectState(nCh);
            } catch(Exception ex) {
                _Ilog.Error("channel(ip=" + IpAddress.Split(':')[0] + ") clear socket error.", ex);
            } finally {
                ns.Close();
                ChTcpClient.Close();
                _Ilog.Info(IpAddress.Split(':')[0] + " disconnect connected");
            }
        }
        #endregion

        #region 判断接收的数据类型
        public static string GetHeader(SocketInfo dataStack) {
            return call_socketcommand_util.GetHeadInfo(dataStack.HeadInfo);
        }

        public static string GetBody(SocketInfo dataStack, string name, int index) {
            return SocketInfo.GetValueByKey(dataStack.Content, call_socketcommand_util.GetParamByHeadName(name)[index]);
        }

        /// <summary>
        /// 判断接收的数据类型
        /// </summary>
        /// <param name="data"></param>
        /// <param name="client"></param>
        private static void JudgeInfo(string data, Socket client) {
            try {
                _Ilog.Info("recive socket infomation：" + data);
                if(data.Length <= 0 || !data.Contains("{") || !data.Contains("}"))
                    return;

                ArrayList SocketDatas = CutSocketData(data);

                if(SocketDatas == null)
                    return;

                for(int i = 0; i < SocketDatas.Count; i++) {
                    _Ilog.Info("recive socket infomation：" + SocketDatas[i].ToString());
                    SocketInfo _SocketInfo = new SocketInfo(SocketDatas[i].ToString());
                    _Ilog.Info("socket infomation type：" + _SocketInfo.HeadInfo);
                    switch(call_socketcommand_util.GetHeadInfo(_SocketInfo.HeadInfo)) {
                        //$$CONNECTIONSERVER{UserID:2;IpAddress:192.168.3.4;}%%
                        case "LJFWQ":
                            ConnectInfo(_SocketInfo.Content, client);
                            break;

                        //$DIAL{number:018561591960;type:0;phone:200}%%
                        case "BDDH":
                            DIALInfo(_SocketInfo.Content, client);
                            break;

                        ////
                        //case SocketInfoType.MsgInfo:
                        //    LogWrite.Write("消息类别为：聊天信息", "LAN");
                        //    GetMsgInfo(_SocketInfo.Content, client);
                        //    break;

                        ////
                        //case SocketInfoType.DIAL_F:
                        //    LogWrite.Write("消息类别为：呼叫转接", "LAN");
                        //    DIAL_F(_SocketInfo.Content, client);
                        //    break;

                        ////$DTMF{number:3;username:2030}%
                        //case SocketInfoType.DTMF:
                        //    LogWrite.Write("消息类别为：获取DTMF", "LAN");
                        //    SendDTMF(_SocketInfo.Content, client);
                        //    break;
                        ////
                        //case SocketInfoType.PlayMedia:
                        //    LogWrite.Write("消息类别为：播放录音文件", "LAN");
                        //    GetMediaFile(_SocketInfo.Content, client);
                        //    break;
                        ////
                        //case SocketInfoType.HungUp:
                        //    LogWrite.Write("消息类别为：挂机", "LAN");
                        //    HungUp(_SocketInfo.Content, client);
                        //    break;
                        ////
                        //case SocketInfoType.SocketFlag:
                        //    LogWrite.Write("消息类别为：Socket心跳检测", "LAN");
                        //    SocketFlag(_SocketInfo.Content, client);
                        //    break;

                        ////批次下载录音
                        ////LoadRecordFile{File:192.168.0.70\交通\20141010\张三\Rec_20150115111310_Q_18561591960.wav|交通\20141010\张三\Rec_20150115111310_Q_18561591960.wav;}
                        //case SocketInfoType.LoadRecordFile:
                        //    LogWrite.Write("消息类别为：批量下载录音", "LAN");
                        //    LoadRecordFile(_SocketInfo.Content, client);
                        //    break;

                        ////批量下载录音
                        ////BotchRecordFile{File:http://192.168.1.70/temp/luyin/192.168.1.70_20141010101010.txt;}
                        //case SocketInfoType.BotchRecordFile:
                        //    LogWrite.Write("消息类别为：批量下载录音", "LAN");
                        //    BotchRecordFile(_SocketInfo.Content, client);
                        //    break;
                        default:
                            break;
                    }
                }
            } catch(Exception ex) {
                _Ilog.Error("recive socket data information type error.", ex);
            }
        }

        #endregion

        #region 分割socket数据

        /// <summary>
        /// 分割socket数据
        /// </summary>
        /// <param name="SocketData"></param>
        /// <returns></returns>
        public static ArrayList CutSocketData(string SocketData) {
            try {
                ArrayList SocketDatas = new ArrayList();
                for(int i = 0; i < ParamLib.SocketEndStr.Length; i++) {
                    if(SocketData.Contains(ParamLib.SocketStartStr[i].ToString()) && SocketData.Contains(ParamLib.SocketEndStr[i].ToString())) {
                        string[] data = SocketData.Split(new string[1] { ParamLib.SocketEndStr[i] }, StringSplitOptions.RemoveEmptyEntries);
                        for(int j = 0; j < data.Length; j++) {
                            if(!string.IsNullOrEmpty(data[j]) && data[j].StartsWith(ParamLib.SocketStartStr[i]) && data[j].EndsWith("}")) {
                                SocketDatas.Add(data[j].Replace(ParamLib.SocketStartStr[i], ""));
                            }
                        }
                    }
                }
                return SocketDatas;
            } catch(Exception ex) {
                _Ilog.Error("cut recive socket data (" + SocketData + ") error.", ex);

                return null;
            }
        }

        #endregion

        #region 收到链接时处理信息

        /// <summary>
        /// 收到连接时处理信息
        /// </summary>
        /// <param name="data">连接信息内容</param>
        /// <param name="ipinfo">发送放ip</param>
        [Obsolete("请使用CenoSocket/SocketMain/ConnWebSocket方法")]
        private static void ConnectInfo(Hashtable data, Socket state) {
            try {
                string UserID = SocketInfo.GetValueByKey(data, call_socketcommand_util.GetParamByHeadName("LJFWQ")[0]);
                string IpAddress = SocketInfo.GetValueByKey(data, call_socketcommand_util.GetParamByHeadName("LJFWQ")[1]);
                EndPoint EndPoint = state.RemoteEndPoint;

                if(call_agent_basic.UpdateAgentLoginState("1", IpAddress, UserID) <= 0)
                    _Ilog.Error("update agent login status error.");

                string name = call_factory.agent_list.First<AGENT_INFO>(x => x.AgentID.ToString() == UserID).AgentName;

                _Ilog.Info(name + " has connected");
                SendMsgToClient(SocketCommand.SendConnectStr(), state);

                var _AgentInfo = call_factory.agent_list.FirstOrDefault<AGENT_INFO>(x => x.AgentID == (int.Parse(UserID)));
                if(_AgentInfo == null)
                    throw new ArgumentOutOfRangeException("CallFactory.Agent_Info", "can't find channel connected with agent(userid=)" + UserID);

                int nCh = ((AGENT_INFO)_AgentInfo).ChInfo.nCh;
                if(nCh < 0)
                    throw new ArgumentOutOfRangeException("CallFactory.Agent_Info", "can't find channel connected with agent(userid=)" + UserID);
                try {
                    if(call_factory.channel_list[nCh].channel_socket._Socket != null) {
                        try {
                            if(call_factory.channel_list[nCh].channel_socket._Socket.RemoteEndPoint.ToString().Split(':')[0] != EndPoint.ToString().Split(':')[0])
                                SendMsgToClient(SocketCommand.SystemExit("本次登录地址" + EndPoint.ToString().Split(':')[0] + ";在" + call_factory.channel_list[nCh].channel_socket._Socket.RemoteEndPoint.ToString().Split(':')[0] + "已经登录的帐号被强制退出"), call_factory.channel_list[nCh].channel_socket._Socket);
                        } catch(Exception ex) {
                            _Ilog.Error("find agent login unusual.", ex);
                        } finally {
                            call_factory.channel_list[nCh].channel_socket._Socket.Close(1000);
                            call_factory.channel_list[nCh].channel_socket._Socket = null;
                            call_factory.channel_list[nCh].channel_socket.IsConnect = false;
                            call_factory.channel_list[nCh].channel_socket.HeartBeatFlag = false;
                        }
                    }
                } catch(Exception ex) {
                    _Ilog.Error("delete channel(ch=" + nCh.ToString() + ") before socket error.", ex);
                } finally {
                    call_factory.channel_list[nCh].channel_socket._Socket = state;
                    call_factory.channel_list[nCh].channel_socket.IsConnect = true;
                    call_factory.channel_list[nCh].channel_socket.HeartBeatFlag = true;
                    if(_ChangeConnectState != null)
                        _ChangeConnectState(nCh);
                }
            } catch(Exception ex) {
                _Ilog.Error("deal with connection information received from client error.", ex);
            }
        }

        /// <summary>
        /// 收到连接时处理信息
        /// </summary>
        /// <param name="data">连接信息内容</param>
        /// <param name="ipinfo">发送放ip</param>
        private static void ConnWebSocket(Hashtable data, Socket state) {
            try {
                string UserID = SocketInfo.GetValueByKey(data, call_socketcommand_util.GetParamByHeadName("LJFWQ")[0]);
                string IpAddress = SocketInfo.GetValueByKey(data, call_socketcommand_util.GetParamByHeadName("LJFWQ")[1]);
                EndPoint EndPoint = state.RemoteEndPoint;

                if(call_agent_basic.UpdateAgentLoginState("1", IpAddress, UserID) <= 0)
                    _Ilog.Error("update agent login status error.");

                string name = call_factory.agent_list.First<AGENT_INFO>(x => x.AgentID.ToString() == UserID).AgentName;

                _Ilog.Info(name + " has connected");
                SendMsgToClient(SocketCommand.SendConnectStr(), state);

                var _AgentInfo = call_factory.agent_list.FirstOrDefault<AGENT_INFO>(x => x.AgentID == (int.Parse(UserID)));
                if(_AgentInfo == null)
                    throw new ArgumentOutOfRangeException("CallFactory.Agent_Info", "can't find channel connected with agent(userid=)" + UserID);

                int nCh = ((AGENT_INFO)_AgentInfo).ChInfo.nCh;
                if(nCh < 0)
                    throw new ArgumentOutOfRangeException("CallFactory.Agent_Info", "can't find channel connected with agent(userid=)" + UserID);
                try {
                    if(call_factory.channel_list[nCh].channel_socket._Socket != null) {
                        try {
                            if(call_factory.channel_list[nCh].channel_socket._Socket.RemoteEndPoint.ToString().Split(':')[0] != EndPoint.ToString().Split(':')[0])
                                SendMsgToClient(SocketCommand.SystemExit("本次登录地址" + EndPoint.ToString().Split(':')[0] + ";在" + call_factory.channel_list[nCh].channel_socket._Socket.RemoteEndPoint.ToString().Split(':')[0] + "已经登录的帐号被强制退出"), call_factory.channel_list[nCh].channel_socket._Socket);
                        } catch(Exception ex) {
                            _Ilog.Error("find agent login unusual.", ex);
                        } finally {
                            call_factory.channel_list[nCh].channel_socket._Socket.Close(1000);
                            call_factory.channel_list[nCh].channel_socket._Socket = null;
                            call_factory.channel_list[nCh].channel_socket.IsConnect = false;
                            call_factory.channel_list[nCh].channel_socket.HeartBeatFlag = false;
                        }
                    }
                } catch(Exception ex) {
                    _Ilog.Error("delete channel(ch=" + nCh.ToString() + ") before socket error.", ex);
                } finally {
                    call_factory.channel_list[nCh].channel_socket._Socket = state;
                    call_factory.channel_list[nCh].channel_socket.IsConnect = true;
                    call_factory.channel_list[nCh].channel_socket.HeartBeatFlag = true;
                    if(_ChangeConnectState != null)
                        _ChangeConnectState(nCh);
                }
            } catch(Exception ex) {
                _Ilog.Error("deal with connection information received from client error.", ex);
            }
        }

        #endregion

        #region 处理客户端去点电话
        #region 取消使用
        /// <summary>
        /// 处理客户端发送的去电电话
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ipinfo"></param>
        [Obsolete("请使用DIALInfo2方法")]
        private static async void DIALInfo(Hashtable data, Socket state) {
            try {

                #region 变量
                int AgentID = Convert.ToInt32(SocketInfo.GetValueByKey(data, call_socketcommand_util.GetParamByHeadName("BDDH")[0]));

                /* 置换信息 */
                var agent = call_factory.agent_list.Find(x => x.AgentID == Convert.ToInt32(AgentID));
                if(agent == null) {
                    _Ilog.Fatal("{0}信息不存在".Fmt(AgentID));
                    return;
                }

                string number = SocketInfo.GetValueByKey(data, call_socketcommand_util.GetParamByHeadName("BDDH")[1]);
                string type = SocketInfo.GetValueByKey(data, call_socketcommand_util.GetParamByHeadName("BDDH")[2]);

                int nCh = agent.ChInfo.nCh;

                var channel = call_factory.channel_list[nCh];

                if(string.IsNullOrEmpty(number))
                    return;

                if(nCh == -1)
                    return;

                //通过number反差出用户号码
                var agentA = call_factory.agent_list.Find(x => x.AgentNum == number);
                int nChA = -1;

                var gtw = call_gateway.GetModel_UniqueID(agent.ChInfo.channel_switch_tactics.Dial_Switch_Adapter.LinkChUid);
                if(gtw == null) {
                    _Ilog.Fatal("未找到{0}的对应网关".Fmt(number));
                    return;
                }

                if(type == "0") {
                } else {
                    if(agentA == null) {
                        _Ilog.Fatal("号码为{0}用户未注册".Fmt(number));
                        SendMsgToClient(SocketCommand.SendCommonStr("BHZT", new string[] { "Fail", "未注册" }), state);
                        return;
                    }
                    nChA = agentA.ChInfo.nCh;
                    if(nChA == -1)
                        return;
                }

                /* 如何设计拨号规则 */
                /* 啥也不管了,直接使用FreeSwitch的Api直接拨就完事了
                 * 表设计的过于复杂,暂时用不到,只把重要的内容填写上 */

                #endregion

                call_record_model entity = new call_record_model();
                entity.UniqueID = Guid.NewGuid().ToString();
                entity.CallType = 6;
                entity.ChannelID = channel.channel_id;
                entity.LinkChannelID = -1;
                entity.LocalNum = agent.AgentNum;
                entity.T_PhoneNum = number;
                entity.C_PhoneNum = number;
                entity.PhoneAddress = "????";
                entity.DtmfNum = "";
                entity.PhoneTypeID = -1;
                entity.PhoneListID = -1;
                entity.PriceTypeID = -1;
                entity.CallPrice = -1;
                entity.AgentID = AgentID;
                entity.CusID = -1;
                entity.ContactID = -1;
                entity.RecordFile = "";
                DateTime Now = DateTime.Now;
                entity.C_Date = Now.ToString("yyyy-MM-dd 00:00:00");
                entity.C_StartTime = Now.ToString("yyyy-MM-dd HH:mm:ss");
                entity.C_RingTime = Now.ToString("yyyy-MM-dd HH:mm:ss");
                entity.C_AnswerTime = null;
                entity.C_EndTime = null;
                entity.C_WaitTime = 0;
                entity.C_SpeakTime = 0;
                entity.CallResultID = -1;
                entity.CallForwordFlag = -1;
                entity.CallForwordChannelID = "-1";
                entity.SerOp_ID = -1;
                entity.SerOp_DTMF = "";
                entity.SerOp_LeaveRec = "";
                entity.Detail = "";
                entity.Remark = "";

                /* 先直接Insert,后续全部变为更新 */
                call_record.Insert(entity);

                var WhoHungUp = string.Empty;

                if(type == "*") {
                    #region 内线拨号
                    try {
                        var originate =
                             await M_NEventSocket.client.Originate(
                                    //$"user/{agentA.ChInfo.channel_number}",
                                    $"user/{channel.channel_number}",//改为先呼叫主叫
                                    new OriginateOptions {
                                        CallerIdNumber = agent.AgentNum,
                                        //CallerIdName = agent.AgentNum,
                                        HangupAfterBridge = false,
                                        TimeoutSeconds = 20,
                                    });

                        if(!originate.Success) {

                            entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                            entity.CallResultID = 8;

                            call_record.Update(entity.UniqueID,
                                new object[] {
                                "C_EndTime", entity.C_EndTime,
                                "C_WaitTime", entity.C_WaitTime,
                                "CallResultID", entity.CallResultID
                                });

                            _Ilog.Error("A leg 桥接失败:" + originate.ResponseText);

                            SendMsgToClient(SocketCommand.SendCommonStr("BHZT", new string[] { "Fail", originate.ResponseText }), state);

                            //await client.Exit();

                        } else {

                            var link = false;

                            var uuid = originate.ChannelData.Headers[HeaderNames.CallerUniqueId];

                            var recordingPath = "{0}\\{1}\\{2}\\{3}".Fmt(
                                ParamLib.RecordFilePath,
                                Now.ToString("yyyy"),
                                Now.ToString("yyyyMM"),
                                Now.ToString("yyyyMMdd"));

                            if(!Directory.Exists(recordingPath)) {
                                Directory.CreateDirectory(recordingPath);
                            }

                            var recordingFile = "\\Rec_{0}_{1}_{2}.wav".Fmt(
                                Now.ToString("yyyyMMddHHmmss"),
                                entity.LocalNum,
                                number);

                            M_NEventSocket.client.OnHangup(
                                uuid,
                                e => {

                                    if(string.IsNullOrWhiteSpace(WhoHungUp))
                                        WhoHungUp = "A";

                                    _Ilog.Info("被叫方挂断");

                                    entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    entity.CallResultID = 6;

                                    /* 这里有可能通,也有可能不通 */
                                    if(link) {
                                        entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                        entity.CallResultID = 5;
                                    }

                                    call_record.Update(entity.UniqueID,
                                        new object[] {
                                       "C_EndTime", entity.C_EndTime,
                                       "CallResultID", entity.CallResultID,
                                       "C_SpeakTime", entity.C_SpeakTime });

                                    /* 也就是这里有问题,当挂断的时候需要把程序退出,但是退出之后就会报错
                                     * 先看看会不会被catch出来
                                     * 会被catch,而且导致socket无法进行,这里还是不能添加 */

                                    if(WhoHungUp == "B") {
                                        //client.Exit();
                                        _Ilog.Info("最后被叫退出");
                                    }
                                });

                            var bridgeUUID = Guid.NewGuid().ToString();

                            var bridge =
                              await M_NEventSocket.client.Bridge(
                                  uuid,
                                  $"user/{agentA.ChInfo.channel_number}",//再呼叫被叫
                                                                         //$"user/{channel.channel_number}",
                                  new BridgeOptions() {
                                      UUID = bridgeUUID,
                                      TimeoutSeconds = 20,
                                      //CallerIdName = "B",
                                      CallerIdNumber = agent.AgentNum,
                                      HangupAfterBridge = false,
                                      //IgnoreEarlyMedia = true,
                                      //ContinueOnFail = true,
                                      //RingBack = "tone_stream://${uk-ring};loops=-1",
                                      //ConfirmPrompt = "ivr/8000/ivr-to_accept_press_one.wav",
                                      //ConfirmInvalidPrompt = "ivr/8000/ivr-that_was_an_invalid_entry.wav",
                                      //ConfirmKey = "1234",
                                  });

                            if(!bridge.Success) {

                                entity.CallResultID = 12;
                                entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                call_record.Update(entity.UniqueID,
                                    new object[] {
                                    "CallResultID", entity.CallResultID,
                                    "C_WaitTime", entity.C_WaitTime,
                                    "C_EndTime", entity.C_EndTime });

                                _Ilog.Info("B leg 桥接失败:" + bridge.ResponseText);

                                SendMsgToClient(SocketCommand.SendCommonStr("BHZT", new string[] { "Fail", bridge.ResponseText }), state);

                                await M_NEventSocket.client.Hangup(uuid, HangupCause.CallRejected);

                            } else {

                                /* 发送摘机 */
                                SendMsgToClient(SocketCommand.SendCommonStr("BHZT", new string[] { "Pick", bridge.ResponseText }), state);

                                link = true;

                                entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                entity.C_AnswerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                _Ilog.Info("桥接成功 {0} - {1} 状态： {2}".Fmt(bridge.ChannelData.UUID, bridge.BridgeUUID, bridge.ResponseText));

                                M_NEventSocket.client.OnHangup(bridge.BridgeUUID, async e => {

                                    if(string.IsNullOrWhiteSpace(WhoHungUp))
                                        WhoHungUp = "B";

                                    entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    entity.CallResultID = 1;
                                    entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;

                                    call_record.Update(entity.UniqueID,
                                        new object[] {
                                        "CallResultID", entity.CallResultID,
                                        "C_SpeakTime", entity.C_SpeakTime,
                                        "C_EndTime", entity.C_EndTime });

                                    _Ilog.Info("主叫 {0} 挂断，原因：{1} ".Fmt(e.Headers[HeaderNames.CallerUniqueId], e.Headers[HeaderNames.HangupCause]));

                                    /* 因为所有的异步都执行完毕,应该就生成记录了,所以这里需要执行修改 */
                                    await M_NEventSocket.client.Hangup(uuid, HangupCause.NormalClearing);

                                    if(WhoHungUp == "A") {
                                        //await client.Exit();
                                        _Ilog.Info("最后主叫退出");
                                    }
                                });

                                await M_NEventSocket.client.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'");
                                await M_NEventSocket.client.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0);
                                await M_NEventSocket.client.SetChannelVariable(uuid, "RECORD_STEREO", "true");

                                entity.RecordFile = recordingPath + recordingFile;

                                var recordingResult = await M_NEventSocket.client.SendApi("uuid_record {0} start {1}".Fmt(uuid, entity.RecordFile));

                                if(!recordingResult.Success) {
                                    _Ilog.Fatal("{0}录音失败".Fmt(recordingResult.ErrorMessage));
                                }

                                string path = Path.GetFileNameWithoutExtension(entity.RecordFile);

                                //后期去掉

                                //将录音ID返回(去电方)
                                SendMsgToClient(SocketCommand.SendCommonStr("FSLY", new string[] { path, entity.RecordFile }), state);

                                //将录音ID返回(来电方)
                                //SendMsgToClient(SocketCommand.SendCommonStr("FSLY", new string[] { path, entity.RecordFile }), call_factory.agent_list[nChA].ChInfo.channel_socket._Socket);
                            }
                        }
                    } catch(Exception ex) {
                        _Ilog.Error("单独为esl:原因:" + ex.Message);
                    }
                    #endregion
                } else {
                    #region 外线拨号
                    /* 使用哪个网关,这是个问题 */

                    /* 网关取默认第一个吧 */

                    var dial1 = $"sofia/external/sip:{number}@{gtw.gw_name}";
                    var o_dia12 = $"sofia/gateway/{gtw.gw_name}/{number}";

                    var dial_my = $"user/{channel.channel_number}";

                    try {
                        var originate =
                             await M_NEventSocket.client.Originate(
                                    //dial1,
                                    dial_my,
                                    new OriginateOptions {
                                        CallerIdNumber = agent.AgentNum,
                                        //CallerIdNumber = agent.AgentNum,
                                        //CallerIdName = agent.AgentName,
                                        HangupAfterBridge = false,
                                        TimeoutSeconds = 20,
                                    });

                        if(!originate.Success) {

                            entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                            entity.CallResultID = 8;

                            call_record.Update(entity.UniqueID,
                                new object[] {
                                "C_EndTime", entity.C_EndTime,
                                "C_WaitTime", entity.C_WaitTime,
                                "CallResultID", entity.CallResultID
                                });

                            _Ilog.Error("A leg 桥接失败:" + originate.ResponseText);

                            SendMsgToClient(SocketCommand.SendCommonStr("BHZT", new string[] { "Fail", originate.ResponseText }), state);

                            //await client.Exit();

                        } else {

                            var link = false;

                            var uuid = originate.ChannelData.Headers[HeaderNames.CallerUniqueId];

                            var recordingPath = "{0}\\{1}\\{2}\\{3}".Fmt(
                                ParamLib.RecordFilePath,
                                Now.ToString("yyyy"),
                                Now.ToString("yyyyMM"),
                                Now.ToString("yyyyMMdd"));

                            if(!Directory.Exists(recordingPath)) {
                                Directory.CreateDirectory(recordingPath);
                            }

                            var recordingFile = "\\Rec_{0}_{1}_{2}.wav".Fmt(
                                Now.ToString("yyyyMMddHHmmss"),
                                entity.LocalNum,
                                number);

                            M_NEventSocket.client.OnHangup(
                                uuid,
                                e => {

                                    if(string.IsNullOrWhiteSpace(WhoHungUp))
                                        WhoHungUp = "A";

                                    _Ilog.Info("被叫方挂断");

                                    entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    entity.CallResultID = 6;

                                    /* 这里有可能通,也有可能不通 */
                                    if(link) {
                                        entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                        entity.CallResultID = 5;
                                    }

                                    call_record.Update(entity.UniqueID,
                                        new object[] {
                                       "C_EndTime", entity.C_EndTime,
                                       "CallResultID", entity.CallResultID,
                                       "C_SpeakTime", entity.C_SpeakTime });

                                    /* 也就是这里有问题,当挂断的时候需要把程序退出,但是退出之后就会报错
                                     * 先看看会不会被catch出来
                                     * 会被catch,而且导致socket无法进行,这里还是不能添加 */

                                    if(WhoHungUp == "B") {
                                        //client.Exit();
                                        _Ilog.Info("最后被叫退出");
                                    }
                                });

                            var bridgeUUID = Guid.NewGuid().ToString();

                            var bridge =
                              await M_NEventSocket.client.Bridge(
                                  uuid,
                                  //$"user/{channel.channel_number}",
                                  dial1,
                                  new BridgeOptions() {
                                      UUID = bridgeUUID,
                                      TimeoutSeconds = 20,
                                      //CallerIdName = "B",
                                      CallerIdNumber = agent.AgentNum,
                                      HangupAfterBridge = false,
                                      //IgnoreEarlyMedia = true,
                                      //ContinueOnFail = true,
                                      //RingBack = "tone_stream://${uk-ring};loops=-1",
                                      //ConfirmPrompt = "ivr/8000/ivr-to_accept_press_one.wav",
                                      //ConfirmInvalidPrompt = "ivr/8000/ivr-that_was_an_invalid_entry.wav",
                                      //ConfirmKey = "1234",
                                  });

                            if(!bridge.Success) {

                                entity.CallResultID = 12;
                                entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                call_record.Update(entity.UniqueID,
                                    new object[] {
                                    "CallResultID", entity.CallResultID,
                                    "C_WaitTime", entity.C_WaitTime,
                                    "C_EndTime", entity.C_EndTime });

                                _Ilog.Info("B leg 桥接失败:" + bridge.ResponseText);

                                SendMsgToClient(SocketCommand.SendCommonStr("BHZT", new string[] { "Fail", bridge.ResponseText }), state);

                                await M_NEventSocket.client.Hangup(uuid, HangupCause.CallRejected);

                            } else {

                                /* 发送摘机 */
                                SendMsgToClient(SocketCommand.SendCommonStr("BHZT", new string[] { "Pick", bridge.ResponseText }), state);

                                link = true;

                                entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                entity.C_AnswerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                _Ilog.Info("桥接成功 {0} - {1} 状态： {2}".Fmt(bridge.ChannelData.UUID, bridge.BridgeUUID, bridge.ResponseText));

                                M_NEventSocket.client.OnHangup(bridge.BridgeUUID, async e => {

                                    if(string.IsNullOrWhiteSpace(WhoHungUp))
                                        WhoHungUp = "B";

                                    entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    entity.CallResultID = 1;
                                    entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;

                                    call_record.Update(entity.UniqueID,
                                        new object[] {
                                        "CallResultID", entity.CallResultID,
                                        "C_SpeakTime", entity.C_SpeakTime,
                                        "C_EndTime", entity.C_EndTime });

                                    _Ilog.Info("主叫 {0} 挂断，原因：{1} ".Fmt(e.Headers[HeaderNames.CallerUniqueId], e.Headers[HeaderNames.HangupCause]));

                                    /* 因为所有的异步都执行完毕,应该就生成记录了,所以这里需要执行修改 */
                                    await M_NEventSocket.client.Hangup(uuid, HangupCause.NormalClearing);

                                    if(WhoHungUp == "A") {
                                        //await client.Exit();
                                        _Ilog.Info("最后主叫退出");
                                    }
                                });

                                await M_NEventSocket.client.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'");
                                await M_NEventSocket.client.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0);
                                await M_NEventSocket.client.SetChannelVariable(uuid, "RECORD_STEREO", "true");

                                entity.RecordFile = recordingPath + recordingFile;

                                var recordingResult = await M_NEventSocket.client.SendApi("uuid_record {0} start {1}".Fmt(uuid, entity.RecordFile));

                                if(!recordingResult.Success) {
                                    _Ilog.Fatal("{0}录音失败".Fmt(recordingResult.ErrorMessage));
                                }

                                string path = Path.GetFileNameWithoutExtension(entity.RecordFile);

                                //将录音ID返回(去电方)
                                SendMsgToClient(SocketCommand.SendCommonStr("FSLY", new string[] { path, entity.RecordFile }), state);

                                //将录音ID返回(来电方)
                                //SendMsgToClient(SocketCommand.SendCommonStr("FSLY", new string[] { path, entity.RecordFile }), call_factory.agent_list[nChA].ChInfo.channel_socket._Socket);
                            }
                        }
                    } catch(Exception ex) {
                        _Ilog.Error("esl error:" + ex.Message);
                    }

                    #endregion
                }

                call_record.Update(entity.UniqueID,
                    new object[] {
                        "RecordFile", entity.RecordFile,
                        "C_WaitTime",entity.C_WaitTime,
                        "C_AnswerTime",entity.C_AnswerTime
                    });

            } catch(Exception ex) {

                _Ilog.Error("拨号失败:原因:" + ex.Message);

                SendMsgToClient(SocketCommand.SendCommonStr("BHZT", new string[] { "Fail", ex.Message }), state);
            }
        }
        #endregion

        #region 修正,有早期媒体时,将早期媒体放入录音,通话以早媒开始计算
        [Obsolete("已进行修正,请使用[CenoSocket/m_fDialClassm_fDial]方法")]
        public static async void _bddh_do_1(IWebSocketConnection socket, Hashtable data) {

            var UniqueID = string.Empty;

            try {

                #region 变量
                string[] m_aSocketCmdArray = call_socketcommand_util.GetParamByHeadName("BDDH");
                int AgentID = Convert.ToInt32(SocketInfo.GetValueByKey(data, m_aSocketCmdArray[0]));

                /* 置换信息 */
                var agent = call_factory.agent_list.Find(x => x.AgentID == Convert.ToInt32(AgentID));
                if(agent == null) {
                    Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][{AgentID},主叫未注册]");
                    socket.Send(M_WebSocketSend._bhzt_fail("主叫未注册"));
                    return;
                }

                string number = SocketInfo.GetValueByKey(data, m_aSocketCmdArray[1]);
                string m_sRealPhoneNumber = number;
                string m_sPhoneNumber = SocketInfo.GetValueByKey(data, m_aSocketCmdArray[2]);
                string type = SocketInfo.GetValueByKey(data, m_aSocketCmdArray[3]);
                string m_sPhoneAddressStr = SocketInfo.GetValueByKey(data, m_aSocketCmdArray[4]);
                string m_sCityCodeStr = SocketInfo.GetValueByKey(data, m_aSocketCmdArray[5]);
                string m_sDealWithStr = SocketInfo.GetValueByKey(data, m_aSocketCmdArray[6]);

                /*
                 * 测试可行
                 * 现在这里的拨号的判断需要规整一下
                 * 是否自动加0,一般都是外地,所以去掉的东西太多
                 * 需要保留*#俩个符号
                 */

                number = new Regex("[^(0-9*#)]+").Replace(number, "");
                if (!new Regex("^[0-9*#]{3,20}$").IsMatch(number)) {
                    Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][进行容错处理后电话无效]");
                    socket.Send(M_WebSocketSend._bhzt_fail("无线电话"));
                    return;
                }
                Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][电话号码进行容错处理成功,{number}]");

                int nCh = agent.ChInfo.nCh;

                var channel = call_factory.channel_list[nCh];

                if(nCh == -1 || channel == null) {
                    Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][主叫{agent.AgentNum}通道未知]");
                    socket.Send(M_WebSocketSend._bhzt_fail("主叫通道未知"));
                    return;
                }

                var gtw = call_gateway.GetModel_UniqueID(channel.channel_switch_tactics.Dial_Switch_Adapter.LinkChUid);
                if(gtw == null) {
                    Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][呼叫{number}时未找到对应网关]");
                    socket.Send(M_WebSocketSend._bhzt_fail("未找到对应网关"));
                    return;
                }

                /* 如何设计拨号规则 */
                /* 啥也不管了,直接使用FreeSwitch的Api直接拨就完事了
                 * 表设计的过于复杂,暂时用不到,只把重要的内容填写上 */

                #endregion

                #region 实例
                call_record_model entity = new call_record_model();
                entity.UniqueID = Guid.NewGuid().ToString();
                UniqueID = entity.UniqueID;
                entity.CallType = type == Special.Star ? 6 : 1;
                entity.ChannelID = channel.channel_id;
                entity.LinkChannelID = -1;

                #region 修正
                /*
                 * 也就是内外线的兼容问题
                 * 解决一:重新配置即可
                 * 解决二:客户端添加一个加零外呼
                 */
                if (false)
                {
                    var _number = string.Empty;
                    if (Call_ParamUtil.DialDealMethod == "has")
                    {
                        _number = number;
                        if (type == "0")
                        {
                            _number = number.StartsWith("*") ? type + number : number;
                        }
                    }
                    else if (Call_ParamUtil.DialDealMethod == "no")
                    {
                        _number = number.StartsWith("0") ? number : type + number;
                    }
                    else
                    {
                        _number = number;
                    }
                }
                #endregion

                #region 多号码及呼叫限制逻辑
                /*
                 * 增加换号逻辑,
                 * 也就是说,如果想要达到呼叫限制的目的,一定要网关号码是一一对应的
                 * 所有的统计信息,使用本地的资源
                 */

                string m_sGatewayName = string.Empty;
                int m_uDialCount = 0;
                bool m_bIsGateway = false;
                string m_sDialPrefix = string.Empty;
                string m_sAreaCode = string.Empty;
                string m_sAreaName = string.Empty;
                bool m_bZflag = false;

                if (Call_ParamUtil.IsMultiPhone)
                {
                    Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][主叫号码:{agent.AgentNum},被叫号码:{number},读取多号码及呼叫限制");
                    DataTable dt = m_fDialLimit.m_fGetDialLimit(entity.T_PhoneNum, AgentID);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        entity.LocalNum = dt.Rows[0]["number"].ToString();
                        m_sGatewayName = dt.Rows[0]["gw"].ToString();
                        m_uDialCount = Convert.ToInt32(dt.Rows[0]["dialcount"]);
                        m_bIsGateway = dt.Rows[0]["gwtype"].ToString() == "gateway";
                        m_sDialPrefix = dt.Rows[0]["dialprefix"].ToString();
                        m_sAreaCode = dt.Rows[0]["areacode"].ToString();
                        m_sAreaName = dt.Rows[0]["areaname"].ToString();
                        m_bZflag = Convert.ToInt32(dt.Rows[0]["zflag"]) == 1;

                        if (string.IsNullOrWhiteSpace(entity.LocalNum))
                        {
                            Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][主叫号码:{agent.AgentNum},被叫号码:{number},多号码及呼叫限制成功");
                            socket.Send(M_WebSocketSend._bhzt_fail("呼叫限制"));
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(m_sGatewayName))
                        {
                            Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][主叫号码:{agent.AgentNum},被叫号码:{number},未加载多号码及呼叫限制的对应网关");
                            socket.Send(M_WebSocketSend._bhzt_fail("未配置网关"));
                            return;
                        }
                    }
                    else
                    {
                        Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][主叫号码:{agent.AgentNum},被叫号码:{number},无多号码及呼叫限制数据");
                        socket.Send(M_WebSocketSend._bhzt_fail("呼叫限制"));
                        return;
                    }
                    Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][主叫号码:{agent.AgentNum},被叫号码:{number},多号码及呼叫限制,主叫转换:{entity.LocalNum},呼叫次数:{m_uDialCount}");

                    #region 被叫号码处理
                    /*
                     * 处理一:通过获取到的信息进行号码转换
                     */

                    if (type != Special.Star)
                    {
                        if (m_bZflag)
                        {
                            switch (m_sDealWithStr)
                            {
                                case Special.Telephone:
                                    if (!number.Contains('*') && !number.Contains('#'))
                                    {
                                        if (!string.IsNullOrWhiteSpace(m_sDialPrefix))
                                        {
                                            if (!string.IsNullOrWhiteSpace(m_sAreaCode) && !string.IsNullOrWhiteSpace(m_sCityCodeStr))
                                            {
                                                if (m_sAreaCode != m_sCityCodeStr)
                                                {
                                                    number = $"{m_sDialPrefix}{number}";
                                                }
                                            }
                                            else
                                            {
                                                number = $"{m_sDialPrefix}{number}";
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            number = m_sPhoneNumber;
                        }
                        Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][被叫号码{(m_bZflag ? "自动" : "无须")}转换:{number}]");
                    }
                    #endregion
                }
                else
                {
                    /*
                     * 单号码,直接赋值即可
                     */
                    entity.LocalNum = agent.AgentNum;
                }
                #endregion

                #region 精简
                /*
                 * 精简一
                 * 客户端查询出来的地址直接使用
                 * 注释下方代码
                 */

                //if (false)
                //{
                //    if (type == "*")
                //    {
                //        entity.PhoneAddress = "内呼";
                //    }
                //    else if (type == "0")
                //    {
                //        entity.PhoneAddress = 
                //            SeoQuery.PhoneAddress(number);
                //    }
                //    else
                //    {
                //        entity.PhoneAddress = "????";
                //    }
                //}
                #endregion

                /*
                 * 如果是内呼,保存真实输入的号码
                 * 再次拨打的时候直接取用即可
                 * 外呼的号码则处理至极简
                 */

                entity.T_PhoneNum = (type == Special.Star ? m_sPhoneNumber : number);
                entity.C_PhoneNum = m_sPhoneNumber;
                entity.PhoneAddress = m_sPhoneAddressStr;
                entity.DtmfNum = "";
                entity.PhoneTypeID = -1;
                entity.PhoneListID = -1;
                entity.PriceTypeID = -1;
                entity.CallPrice = -1;
                entity.AgentID = AgentID;
                entity.CusID = -1;
                entity.ContactID = -1;
                entity.RecordFile = "";
                DateTime Now = DateTime.Now;
                entity.C_Date = Now.ToString("yyyy-MM-dd 00:00:00");
                entity.C_StartTime = Now.ToString("yyyy-MM-dd HH:mm:ss");
                entity.C_RingTime = Now.ToString("yyyy-MM-dd HH:mm:ss");
                entity.C_AnswerTime = null;
                entity.C_EndTime = null;
                entity.C_WaitTime = 0;
                entity.C_SpeakTime = 0;
                entity.CallResultID = -1;
                entity.CallForwordFlag = -1;
                entity.CallForwordChannelID = "-1";
                entity.SerOp_ID = -1;
                entity.SerOp_DTMF = "";
                entity.SerOp_LeaveRec = "";
                entity.Detail = "";
                entity.Remark = "";
                #endregion

                /*
                 * 目前想对该逻辑进行修正
                 * 因为这样对数据库的要求有点高
                 * 但是时间原因暂时搁置
                 */
                call_record.Insert(entity);

                var WhoHungUp = string.Empty;
                bool m_bIsSetDialLimit = false;

                var client = await InboundMain.fs_cli();

                Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][链接esl成功]");

                var _application = Call_ParamUtil._application;
                var __ignore_early_media = Call_ParamUtil.__ignore_early_media;
                var __timeout_seconds = Call_ParamUtil.__timeout_seconds;
                var path = string.Empty;

                if(type == Special.Star) {
                    #region 内线拨号
                    try {
                        await client.SubscribeEvents(EventName.ChannelPark)
                                    .ContinueWith(task => {
                                        try {
                                            if(task.IsCanceled) {
                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},订阅通道park事件时取消,{task?.Exception?.Message}]");
                                            }
                                        } catch(Exception ex) {
                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},订阅通道park事件时取消,错误,{ex.Message}]");
                                        }
                                    });

                        var uuid = Guid.NewGuid().ToString();

                        #region 通道Park事件
                        client.ChannelEvents.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelPark).Take(1)
                                .Subscribe(async (s) => {
                                    try
                                    {
                                        var link = false;
                                        Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫成功]");

                                        var recordingPath = "{0}\\{1}\\{2}\\{3}".Fmt(
                                            ParamLib.RecordFilePath,
                                            Now.ToString("yyyy"),
                                            Now.ToString("yyyyMM"),
                                            Now.ToString("yyyyMMdd"));

                                        if (!Directory.Exists(recordingPath))
                                        {
                                            Directory.CreateDirectory(recordingPath);
                                        }

                                        var recordingFile = "\\Rec_{0}_{1}_Q_{2}{3}".Fmt(
                                            Now.ToString("yyyyMMddHHmmss"),
                                            entity.LocalNum,
                                            number,
                                            Call_ParamUtil._rec_t);

                                        #region 主叫挂断事件
                                        client.OnHangup(uuid, e =>
                                        {
                                            try
                                            {
                                                if (string.IsNullOrWhiteSpace(WhoHungUp))
                                                {
                                                    WhoHungUp = "A";
                                                    Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},主叫挂断:{e.Headers[HeaderNames.HangupCause]}]");
                                                    entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                                    if (link)
                                                    {
                                                        entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                                        if (entity.C_SpeakTime < 0)
                                                            entity.C_SpeakTime = 0;
                                                        entity.CallResultID = 31;
                                                    }
                                                    else
                                                    {
                                                        entity.CallResultID = 53;
                                                    }

                                                    call_record.Update(entity.UniqueID,
                                                        new object[] {
                                                        "C_EndTime", entity.C_EndTime,
                                                        "CallResultID", entity.CallResultID,
                                                        "C_SpeakTime", entity.C_SpeakTime
                                                        });
                                                }

                                                if (WhoHungUp == "B")
                                                {
                                                    if (client != null && client.IsConnected)
                                                    {
                                                        client.Exit().ContinueWith(task =>
                                                        {
                                                            try
                                                            {
                                                                if (task.IsCanceled)
                                                                {
                                                                    Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后主叫退出后退出esl时取消:{task?.Exception?.Message}]");
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后主叫退出后退出esl时取消,错误:{ex.Message}]");
                                                            }
                                                        });
                                                        Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后主叫退出]");
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},监听主叫挂断事件,错误:{ex.Message}]");
                                            }
                                        });
                                        #endregion

                                        var bridgeUUID = Guid.NewGuid().ToString();

                                        var bridge =
                                          await client.Bridge(
                                              uuid,
                                              $"user/{m_sRealPhoneNumber}",
                                              new BridgeOptions()
                                              {
                                                  UUID = bridgeUUID,
                                                  TimeoutSeconds = __timeout_seconds,
                                                  //CallerIdName = "B",
                                                  CallerIdNumber = agent.AgentNum,
                                                  HangupAfterBridge = false,
                                                  IgnoreEarlyMedia = __ignore_early_media
                                                  //ContinueOnFail = true,
                                                  //RingBack = "tone_stream://${uk-ring};loops=-1",
                                                  //ConfirmPrompt = "ivr/8000/ivr-to_accept_press_one.wav",
                                                  //ConfirmInvalidPrompt = "ivr/8000/ivr-that_was_an_invalid_entry.wav",
                                                  //ConfirmKey = "1234",
                                              }).ContinueWith(task => {
                                                  try
                                                  {
                                                      if (task.IsCanceled)
                                                      {
                                                          Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接时取消:{task?.Exception?.Message}]");
                                                          return null as BridgeResult;
                                                      }
                                                      else
                                                      {
                                                          return task.Result;
                                                      }
                                                  }
                                                  catch (Exception ex)
                                                  {
                                                      Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接时取消,错误:{ex.Message}]");
                                                      return null as BridgeResult;
                                                  }
                                              });

                                        if (bridge != null && bridge.Success)
                                        {

                                            link = true;
                                            Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接成功:{bridge.ResponseText},发送摘机消息]");
                                            socket.Send(M_WebSocketSend._bhzt_pick(bridge.ResponseText));

                                            entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                            if (entity.C_WaitTime < 0)
                                                entity.C_WaitTime = 0;
                                            entity.C_AnswerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                            #region 被叫挂断事件
                                            client.OnHangup(bridge.BridgeUUID, e =>
                                            {
                                                try
                                                {
                                                    if (string.IsNullOrWhiteSpace(WhoHungUp))
                                                    {
                                                        WhoHungUp = "B";
                                                        Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},被叫挂断:{e.Headers[HeaderNames.HangupCause]}]");
                                                        entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                                        entity.CallResultID = 32;
                                                        entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                                        if (entity.C_SpeakTime < 0)
                                                            entity.C_SpeakTime = 0;

                                                        call_record.Update(entity.UniqueID,
                                                            new object[] {
                                                        "CallResultID", entity.CallResultID,
                                                        "C_SpeakTime", entity.C_SpeakTime,
                                                        "C_EndTime", entity.C_EndTime
                                                            });

                                                        client.Hangup(uuid, HangupCause.NormalClearing).ContinueWith(task =>
                                                        {
                                                            try
                                                            {
                                                                if (task.IsCanceled)
                                                                {
                                                                    Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},被叫退出后发送主叫挂断消息时取消:{task?.Exception?.Message}]");
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},被叫退出后发送主叫挂断消息时取消,错误:{ex.Message}]");
                                                            }
                                                        });
                                                    }

                                                    if (WhoHungUp == "A")
                                                    {
                                                        if (client != null && client.IsConnected)
                                                        {
                                                            client.Exit().ContinueWith(task =>
                                                            {
                                                                try
                                                                {
                                                                    if (task.IsCanceled)
                                                                    {
                                                                        Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后被叫退出后退出esl时取消:{task?.Exception?.Message}]");
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后被叫退出后退出esl时取消,错误:{ex.Message}]");
                                                                }
                                                            });
                                                            Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后被叫退出]");
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},监听被叫挂断事件,错误:{ex.Message}]");
                                                }
                                            });
                                            #endregion

                                            await client.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'")
                                                        .ContinueWith(task => {
                                                            try
                                                            {
                                                                if (task.IsCanceled)
                                                                {
                                                                    Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_ARTIST,取消:{task?.Exception?.Message}]");
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_ARTIST,取消,错误:{ex.Message}]");
                                                            }
                                                        });
                                            await client.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0)
                                                        .ContinueWith(task => {
                                                            try
                                                            {
                                                                if (task.IsCanceled)
                                                                {
                                                                    Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_MIN_SEC,取消:{task?.Exception?.Message}]");
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_MIN_SEC,取消,错误:{ex.Message}]");
                                                            }
                                                        });
                                            await client.SetChannelVariable(uuid, "RECORD_STEREO", "true")
                                                        .ContinueWith(task => {
                                                            try
                                                            {
                                                                if (task.IsCanceled)
                                                                {
                                                                    Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_STEREO,取消:{task?.Exception?.Message}]");
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_STEREO,取消,错误:{ex.Message}]");
                                                            }
                                                        });

                                            entity.RecordFile = recordingPath + recordingFile;

                                            var recordingResult = await client.SendApi("uuid_record {0} start {1}".Fmt(uuid, entity.RecordFile))
                                                                              .ContinueWith(task => {
                                                                                  try
                                                                                  {
                                                                                      if (task.IsCanceled)
                                                                                      {
                                                                                          Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音,取消:{task?.Exception?.Message}]");
                                                                                          return null as ApiResponse;
                                                                                      }
                                                                                      else
                                                                                      {
                                                                                          return task.Result;
                                                                                      }
                                                                                  }
                                                                                  catch (Exception ex)
                                                                                  {
                                                                                      Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音,取消,错误:{ex.Message}]");
                                                                                      return null as ApiResponse;
                                                                                  }
                                                                              });
                                            Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音路径:{entity.RecordFile}]");
                                            if (recordingResult != null && recordingResult.Success)
                                            {
                                                Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音成功]");
                                            }
                                            else
                                            {
                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音失败]");
                                            }
                                            path = Path.GetFileNameWithoutExtension(entity.RecordFile);
                                        }
                                        else
                                        {
                                            entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                            if (entity.C_WaitTime < 0)
                                                entity.C_WaitTime = 0;
                                            entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                            var responseText = string.IsNullOrWhiteSpace(bridge?.ResponseText) ? "桥接失败后发送主叫挂断消息时取消" : bridge?.ResponseText;

                                            #region 判断电话结果
                                            if (WhoHungUp == "A")
                                            {
                                                if (Cmn.IgnoreEquals(responseText, "NOANSWER"))
                                                    entity.CallResultID = 34;
                                                else if (Cmn.IgnoreEquals(responseText, "BUSY"))
                                                    entity.CallResultID = 35;
                                                else if (Cmn.IgnoreEquals(responseText, "INVALIDARGS"))
                                                    entity.CallResultID = 37;
                                                else if (Cmn.IgnoreEquals(responseText, "USER_NOT_REGISTERED"))
                                                    entity.CallResultID = 39;
                                                else if (Cmn.IgnoreEquals(responseText, "SUBSCRIBER_ABSENT"))
                                                    entity.CallResultID = 39;
                                                else
                                                    entity.CallResultID = 34;
                                            }
                                            else
                                            {
                                                if (Cmn.IgnoreEquals(responseText, "NOANSWER"))
                                                    entity.CallResultID = 34;
                                                else if (Cmn.IgnoreEquals(responseText, "BUSY"))
                                                    entity.CallResultID = 35;
                                                else if (Cmn.IgnoreEquals(responseText, "INVALIDARGS"))
                                                    entity.CallResultID = 53;
                                                else if (Cmn.IgnoreEquals(responseText, "USER_NOT_REGISTERED"))
                                                    entity.CallResultID = 39;
                                                else if (Cmn.IgnoreEquals(responseText, "SUBSCRIBER_ABSENT"))
                                                    entity.CallResultID = 39;
                                                else
                                                    entity.CallResultID = 33;
                                            }
                                            #endregion

                                            call_record.Update(entity.UniqueID,
                                                new object[] {
                                                "CallResultID", entity.CallResultID,
                                                "C_WaitTime", entity.C_WaitTime,
                                                "C_EndTime", entity.C_EndTime
                                                });

                                            Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接失败:{responseText},发送桥接失败消息]");
                                            socket.Send(M_WebSocketSend._bhzt_fail(responseText));
                                            await client.Hangup(uuid, HangupCause.CallRejected)
                                                        .ContinueWith(task => {
                                                            try
                                                            {
                                                                if (task.IsCanceled)
                                                                {
                                                                    Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接失败后发送主叫挂断消息时取消:{task?.Exception?.Message}]");
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接失败后发送主叫挂断消息时取消,错误:{ex.Message}]");
                                                            }
                                                        });
                                        }

                                        Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},增加字段recordName:{path}]");

                                        call_record.Update(entity.UniqueID,
                                            new object[] {
                                            "RecordFile", entity.RecordFile,
                                            "recordName",Path.GetFileNameWithoutExtension(entity.RecordFile),
                                            "C_WaitTime",entity.C_WaitTime,
                                            "C_AnswerTime",entity.C_AnswerTime
                                            });
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},订阅park异步事件,错误:{ex.Message}]");
                                    }
                                });
                        #endregion

                        Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫]");

                        #region 桥接
                        var originate =
                             await client.Originate(
                                    //$"user/{agentA.ChInfo.channel_number}",
                                    $"user/{channel.channel_number}",//改为先呼叫主叫
                                    new OriginateOptions {
                                        UUID = uuid,
                                        CallerIdNumber = number,
                                        //CallerIdName = agent.AgentNum,
                                        HangupAfterBridge = false,
                                        TimeoutSeconds = __timeout_seconds,
                                        IgnoreEarlyMedia = __ignore_early_media
                                    }, _application).ContinueWith((task)=> 
                                    {
                                        if (task.IsCanceled)
                                            return null;
                                        else
                                            return task.Result;
                                    });

                        if(originate == null || (originate != null && !originate.Success)) {
                            entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                            if(entity.C_WaitTime < 0)
                                entity.C_WaitTime = 0;
                            entity.CallResultID = 40;

                            call_record.Update(entity.UniqueID,
                                new object[] {
                                "C_EndTime", entity.C_EndTime,
                                "C_WaitTime", entity.C_WaitTime,
                                "CallResultID", entity.CallResultID
                                });

                            Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫失败:{originate.ResponseText},发送呼叫失败消息]");
                            socket.Send(M_WebSocketSend._bhzt_fail(string.IsNullOrWhiteSpace(originate.ResponseText) ? "呼叫失败" : originate.ResponseText));

                            if (client != null && client.IsConnected)
                            {
                                await client.Exit().ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (task.IsCanceled)
                                        {
                                            Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫失败后退出esl时取消]");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫失败后退出esl时取消,错误:{ex.Message}]");
                                    }
                                });
                            }
                        }
                        #endregion
                    }
                    catch (Exception ex) {
                        Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][内线去电][主叫号码:{entity.LocalNum},被叫号码:{number},呼叫时出错:{ex.Message},发送呼叫出错消息]");

                        entity.CallResultID = 40;
                        call_record.Update(entity.UniqueID,
                            new object[] {
                                "CallResultID", entity.CallResultID,
                                "Remark", ex.Message
                            });

                        socket.Send(M_WebSocketSend._bhzt_fail(ex.Message));

                        if(client != null) {
                            client.Dispose();
                        }
                    }
                    #endregion
                } else {
                    #region 外线去电拨号
                    /* 使用哪个网关,这是个问题 */

                    /* 网关取默认第一个吧 */
                    try {
                        await client.SubscribeEvents(EventName.ChannelPark)
                                    .ContinueWith(task => {
                                        try {
                                            if(task.IsCanceled) {
                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},订阅通道park事件时取消,{task?.Exception?.Message}]");
                                            }
                                        } catch(Exception ex) {
                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},订阅通道park事件时取消,错误,{ex.Message}]");
                                        }
                                    });

                        #region 只保留该处,可以内呼测试外呼流程
                        /*
                         * 处理是否在进行测试
                         * 处理是否是多好码
                         * 处理是否是注册型的网关
                         */
                        var dial1 = $"sofia/external/sip:{number}@{gtw.gw_name}";

                        if(Call_ParamUtil.InboundTest) {
                            dial1 = $"user/{number}";
                            Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},内网测试外呼,使用 {dial1} 出局]");
                        } else {
                            if (!Call_ParamUtil.IsMultiPhone) { 
                                var dia12 = $"sofia/gateway/{gtw.gw_name}/{number}";
                                if(gtw.remark == "gt" || gtw.gw_name.Contains("gt")) {
                                    dial1 = dia12;
                                    Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},对接SIP网关,使用 {dial1} 出局]");
                                }
                            }else {
                                if (m_bIsGateway) {
                                    dial1 = $"sofia/gateway/{m_sGatewayName}/{number}";
                                    Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},多号码,使用 {dial1} 出局]");
                                }
                                else {
                                    dial1 = $"sofia/external/sip:{number}@{m_sGatewayName}";
                                    Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},多号码对接SIP网关,使用 {dial1} 出局]");
                                }
                            }
                        }
                        #endregion

                        var dial_my = $"user/{channel.channel_number}";

                        var uuid = Guid.NewGuid().ToString();

                        #region 通道Park事件
                        client.ChannelEvents.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelPark).Take(1)
                            .Subscribe(async (s) => {
                                try
                                {
                                    var link = false;
                                    Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫成功]");

                                    var recordingPath = "{0}\\{1}\\{2}\\{3}".Fmt(
                                        ParamLib.RecordFilePath,
                                        Now.ToString("yyyy"),
                                        Now.ToString("yyyyMM"),
                                        Now.ToString("yyyyMMdd"));

                                    if (!Directory.Exists(recordingPath))
                                    {
                                        Directory.CreateDirectory(recordingPath);
                                    }

                                    var recordingFile = "\\Rec_{0}_{1}_Q_{2}{3}".Fmt(
                                        Now.ToString("yyyyMMddHHmmss"),
                                        entity.LocalNum,
                                        number,
                                        Call_ParamUtil._rec_t);

                                        #region 主叫挂断事件
                                        client.OnHangup(uuid, e =>
                                    {
                                        try
                                        {
                                            if (string.IsNullOrWhiteSpace(WhoHungUp))
                                            {
                                                WhoHungUp = "A";

                                                Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},主叫挂断:{e.Headers[HeaderNames.HangupCause]}]");
                                                entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                                if (link)
                                                {
                                                    entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                                    if (entity.C_SpeakTime < 0)
                                                        entity.C_SpeakTime = 0;
                                                    entity.CallResultID = 1;
                                                }
                                                else
                                                {
                                                    entity.CallResultID = 54;
                                                }

                                                call_record.Update(entity.UniqueID,
                                                    new object[] {
                                                        "C_EndTime", entity.C_EndTime,
                                                        "CallResultID", entity.CallResultID,
                                                        "C_SpeakTime", entity.C_SpeakTime
                                                    });

                                                if (!m_bIsSetDialLimit)
                                                {
                                                    m_bIsSetDialLimit = true;
                                                    Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},主叫挂断,写入多号码及呼叫限制]");
                                                    m_fDialLimit.m_fSetDialLimit(entity.LocalNum, AgentID, entity.C_SpeakTime);
                                                }
                                            }

                                            if (WhoHungUp == "B")
                                            {
                                                if (client != null && client.IsConnected)
                                                {
                                                    client.Exit().ContinueWith(task =>
                                                    {
                                                        try
                                                        {
                                                            if (task.IsCanceled)
                                                            {
                                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后主叫退出后退出esl时取消:{task?.Exception?.Message}]");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后主叫退出后退出esl时取消,错误:{ex.Message}]");
                                                        }
                                                    });
                                                    Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后主叫退出]");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},监听主叫挂断事件,错误:{ex.Message}]");
                                        }
                                    });
                                        #endregion

                                        var bridgeUUID = Guid.NewGuid().ToString();

                                    var bridge =
                                      await client.Bridge(
                                          uuid,
                                          //$"user/{channel.channel_number}",
                                          dial1,
                                          new BridgeOptions()
                                          {
                                              UUID = bridgeUUID,
                                              TimeoutSeconds = __timeout_seconds,
                                                  //CallerIdName = "B",
                                                  CallerIdNumber = agent.AgentNum,
                                              HangupAfterBridge = false,
                                              IgnoreEarlyMedia = __ignore_early_media
                                                  //ContinueOnFail = true,
                                                  //RingBack = "tone_stream://${uk-ring};loops=-1",
                                                  //ConfirmPrompt = "ivr/8000/ivr-to_accept_press_one.wav",
                                                  //ConfirmInvalidPrompt = "ivr/8000/ivr-that_was_an_invalid_entry.wav",
                                                  //ConfirmKey = "1234",
                                              }).ContinueWith(task => {
                                              try
                                              {
                                                  if (task.IsCanceled)
                                                  {
                                                      Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接时取消:{task?.Exception?.Message}]");
                                                      return null as BridgeResult;
                                                  }
                                                  else
                                                  {
                                                      return task.Result;
                                                  }
                                              }
                                              catch (Exception ex)
                                              {
                                                  Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接时取消,错误:{ex.Message}]");
                                                  return null as BridgeResult;
                                              }
                                          });

                                    if (bridge != null && bridge.Success)
                                    {

                                        link = true;
                                        Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接成功,发送摘机消息]");
                                        socket.Send(M_WebSocketSend._bhzt_pick(bridge.ResponseText));

                                        entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                        if (entity.C_WaitTime < 0)
                                            entity.C_WaitTime = 0;
                                        entity.C_AnswerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                            #region 被叫挂断
                                            client.OnHangup(bridge.BridgeUUID, e =>
                                        {
                                            try
                                            {
                                                if (string.IsNullOrWhiteSpace(WhoHungUp))
                                                {
                                                    WhoHungUp = "B";
                                                    Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},被叫挂断:{e.Headers[HeaderNames.HangupCause]}]");
                                                    entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                                    entity.CallResultID = 5;

                                                    entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                                    if (entity.C_SpeakTime < 0)
                                                        entity.C_SpeakTime = 0;

                                                    call_record.Update(entity.UniqueID,
                                                        new object[] {
                                                        "CallResultID", entity.CallResultID,
                                                        "C_SpeakTime", entity.C_SpeakTime,
                                                        "C_EndTime", entity.C_EndTime
                                                        });

                                                    if (!m_bIsSetDialLimit)
                                                    {
                                                        m_bIsSetDialLimit = true;
                                                        Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},被叫挂断,写入多号码及呼叫限制]");
                                                        m_fDialLimit.m_fSetDialLimit(entity.LocalNum, AgentID, entity.C_SpeakTime);
                                                    }

                                                    client.Hangup(uuid, HangupCause.NormalClearing).ContinueWith(task =>
                                                    {
                                                        try
                                                        {
                                                            if (task.IsCanceled)
                                                            {
                                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},被叫退出后发送主叫挂断消息时取消:{task?.Exception?.Message}]");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},被叫退出后发送主叫挂断消息时取消,错误:{ex.Message}]");
                                                        }
                                                    });
                                                }

                                                if (WhoHungUp == "A")
                                                {
                                                    if (client != null && client.IsConnected)
                                                    {
                                                        client.Exit().ContinueWith(task =>
                                                        {
                                                            try
                                                            {
                                                                if (task.IsCanceled)
                                                                {
                                                                    Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后被叫退出后退出esl时取消:{task?.Exception?.Message}]");
                                                                }
                                                            }
                                                            catch (Exception ex)
                                                            {
                                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后被叫退出后退出esl时取消,错误:{ex.Message}]");
                                                            }
                                                        });
                                                        Log.Instance.Warn($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},最后被叫退出]");
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},监听被叫挂断,错误:{ex.Message}]");
                                            }
                                        });
                                            #endregion

                                            await client.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'")
                                                    .ContinueWith(task => {
                                                        try
                                                        {
                                                            if (task.IsCanceled)
                                                            {
                                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_ARTIST,取消:{task?.Exception?.Message}]");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_ARTIST,取消,错误:{ex.Message}]");
                                                        }
                                                    });
                                        await client.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0)
                                                    .ContinueWith(task => {
                                                        try
                                                        {
                                                            if (task.IsCanceled)
                                                            {
                                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_MIN_SEC,取消:{task?.Exception?.Message}]");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_MIN_SEC,取消,错误:{ex.Message}]");
                                                        }
                                                    });
                                        await client.SetChannelVariable(uuid, "RECORD_STEREO", "true")
                                                    .ContinueWith(task => {
                                                        try
                                                        {
                                                            if (task.IsCanceled)
                                                            {
                                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_STEREO,取消:{task?.Exception?.Message}]");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},RECORD_STEREO,取消,错误:{ex.Message}]");
                                                        }
                                                    });

                                        entity.RecordFile = recordingPath + recordingFile;

                                        var recordingResult = await client.SendApi("uuid_record {0} start {1}".Fmt(uuid, entity.RecordFile))
                                                                          .ContinueWith(task => {
                                                                              try
                                                                              {
                                                                                  if (task.IsCanceled)
                                                                                  {
                                                                                      Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音,取消:{task?.Exception?.Message}]");
                                                                                      return null as ApiResponse;
                                                                                  }
                                                                                  else
                                                                                  {
                                                                                      return task.Result;
                                                                                  }
                                                                              }
                                                                              catch (Exception ex)
                                                                              {
                                                                                  Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音,取消,错误:{ex.Message}]");
                                                                                  return null as ApiResponse;
                                                                              }
                                                                          });

                                        Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音路径:{entity.RecordFile}]");
                                        if (recordingResult != null && recordingResult.Success)
                                        {
                                            Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音成功]");
                                        }
                                        else
                                        {
                                            Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},录音失败]");
                                        }
                                        path = Path.GetFileNameWithoutExtension(entity.RecordFile);
                                        Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发送录音ID]");
                                        socket.Send(M_WebSocketSend._fsly(path, entity.RecordFile));
                                    }
                                    else
                                    {

                                        entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                        if (entity.C_WaitTime < 0)
                                            entity.C_WaitTime = 0;
                                        entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                        var responseText = string.IsNullOrWhiteSpace(bridge?.ResponseText) ? "桥接失败后发送主叫挂断消息时取消" : bridge?.ResponseText;

                                            #region 判断电话结果
                                            if (WhoHungUp == "A")
                                        {
                                            if (Cmn.IgnoreEquals(responseText, "NOANSWER"))
                                                entity.CallResultID = 7;
                                            else if (Cmn.IgnoreEquals(responseText, "BUSY"))
                                                entity.CallResultID = 8;
                                            else if (Cmn.IgnoreEquals(responseText, "INVALIDARGS"))
                                                entity.CallResultID = 10;
                                            else if (Cmn.IgnoreEquals(responseText, "USER_NOT_REGISTERED"))
                                                entity.CallResultID = 12;
                                            else
                                                entity.CallResultID = 7;
                                        }
                                        else
                                        {
                                            if (Cmn.IgnoreEquals(responseText, "NOANSWER"))
                                                entity.CallResultID = 7;
                                            else if (Cmn.IgnoreEquals(responseText, "BUSY"))
                                                entity.CallResultID = 8;
                                            else if (Cmn.IgnoreEquals(responseText, "INVALIDARGS"))
                                                entity.CallResultID = 54;
                                            else if (Cmn.IgnoreEquals(responseText, "USER_NOT_REGISTERED"))
                                                entity.CallResultID = 12;
                                            else
                                                entity.CallResultID = 6;
                                        }
                                            #endregion

                                            call_record.Update(entity.UniqueID,
                                            new object[] {
                                                "CallResultID", entity.CallResultID,
                                                "C_WaitTime", entity.C_WaitTime,
                                                "C_EndTime", entity.C_EndTime });

                                        if (!m_bIsSetDialLimit)
                                        {
                                            m_bIsSetDialLimit = true;
                                            Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接失败,写入多号码及呼叫限制]");
                                            m_fDialLimit.m_fSetDialLimit(entity.LocalNum, AgentID, 0);
                                        }

                                        Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接失败:{responseText},发送桥接失败消息]");
                                        socket.Send(M_WebSocketSend._bhzt_fail(responseText));
                                        await client.Hangup(uuid, HangupCause.CallRejected)
                                                    .ContinueWith(task => {
                                                        try
                                                        {
                                                            if (task.IsCanceled)
                                                            {
                                                                Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接失败后发送主叫挂断消息时取消:{task?.Exception?.Message}]");
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},桥接失败后发送主叫挂断消息时取消,错误:{ex.Message}]");
                                                        }
                                                    });
                                    }

                                    Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},增加字段recordName:{path}]");

                                    call_record.Update(entity.UniqueID,
                                        new object[] {
                                            "RecordFile", entity.RecordFile,
                                            "recordName", path,
                                            "C_WaitTime",entity.C_WaitTime,
                                            "C_AnswerTime",entity.C_AnswerTime
                                        });
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},订阅park异步事件,错误:{ex.Message}]");
                                }
                            });
                        #endregion

                        Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫]");

                        #region 桥接
                        var originate =
                             await client.Originate(
                                    //dial1,
                                    dial_my,
                                    new OriginateOptions {
                                        UUID = uuid,
                                        CallerIdNumber = number,
                                        //CallerIdNumber = agent.AgentNum,
                                        //CallerIdName = agent.AgentName,
                                        HangupAfterBridge = false,
                                        TimeoutSeconds = __timeout_seconds,
                                        IgnoreEarlyMedia = __ignore_early_media
                                    }, _application).ContinueWith((task)=> {
                                        if (task.IsCanceled)
                                            return null;
                                        else
                                            return task.Result;
                                    });

                        if(originate == null || (originate != null && !originate.Success)) {

                            entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                            if(entity.C_WaitTime < 0)
                                entity.C_WaitTime = 0;
                            entity.CallResultID = 13;

                            call_record.Update(entity.UniqueID,
                                new object[] {
                                    "C_EndTime", entity.C_EndTime,
                                    "C_WaitTime", entity.C_WaitTime,
                                    "CallResultID", entity.CallResultID
                                });

                            Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫失败,不写入多号码及呼叫限制]");
                            //proc_dial_limit.proc_set_dial_limit(entity.T_PhoneNum, AgentID, 0);

                            Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫失败:{originate.ResponseText},发送呼叫失败消息]");
                            socket.Send(M_WebSocketSend._bhzt_fail(string.IsNullOrWhiteSpace(originate.ResponseText) ? "呼叫失败" : originate.ResponseText));

                            if (client != null && client.IsConnected)
                            {
                                await client.Exit()
                                            .ContinueWith(task =>
                                            {
                                                try
                                                {
                                                    if (task.IsCanceled)
                                                    {
                                                        Log.Instance.Fail($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫失败后退出esl时取消:{task?.Exception?.Message}]");
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},发起呼叫失败后退出esl时取消,错误:{ex.Message}]");
                                                }
                                            });
                            }
                            //if(client != null) {
                            //    client.Dispose();
                            //}
                        }
                        #endregion
                    }
                    catch (Exception ex) {
                        Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][外线去电][主叫号码:{entity.LocalNum},被叫号码:{number},呼叫时出错:{ex.Message},发送呼叫出错消息]");

                        entity.CallResultID = 13;
                        call_record.Update(entity.UniqueID,
                            new object[] {
                                "CallResultID", entity.CallResultID,
                                "Remark", ex.Message
                            });

                        socket.Send(M_WebSocketSend._bhzt_fail(ex.Message));

                        if(client != null) {
                            client.Dispose();
                        }
                    }
                    #endregion
                }
            } catch(Exception ex) {
                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_do][Exception][呼叫时出错:{ex.Message},详细:{ex},发送拨号失败消息]");

                call_record.Update(UniqueID,
                    new object[] {
                        "CallResultID", 13,
                        "Remark", ex.Message
                    });

                socket.Send(M_WebSocketSend._bhzt_fail(ex.Message));
            }
        }
        #endregion
        #endregion

        #region 呼叫转义操作

        /// <summary>
        /// 呼叫转移操作
        /// </summary>
        /// <param name="information"></param>
        /// <param name="state"></param>
        private static void DIAL_F(Hashtable information, Socket state) {
            try {
                string fnum = SocketInfo.GetValueByKey(information, "number");
                string userno = SocketInfo.GetValueByKey(information, "user");

                //string path = "";
                //string lpAppName = "CHINFO";
                //string KeyName = "MaxUserNumCou";
                //path = Application.StartupPath + "\\pbx.ini";
                //StringBuilder userch = new StringBuilder(1000);
                //WindowsAPI.GetPrivateProfileString(lpAppName, KeyName, "", userch, 1000, path);

                //if (fnum.Length.ToString() == userch.ToString())
                //{
                //    int nCh = ChannelInfo.GetStaChByNumber(userno);
                //}
            } catch(Exception ex) {
                _Ilog.Error("processing client sends the dial transfer request error.", ex);
            }
        }

        #endregion

        #region 发送dtmf

        /// <summary>
        /// send dtmf to channel
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="state"></param>
        private static void SendDTMF(Hashtable Data, Socket state) {
            try {
                string dtmf = SocketInfo.GetValueByKey(Data, "number");
                _Ilog.Info("receive dtmf is" + dtmf);

                string usernum = SocketInfo.GetValueByKey(Data, "username");
                _Ilog.Info("receive user number is" + usernum);

                if(string.IsNullOrEmpty(dtmf) || string.IsNullOrEmpty(usernum))
                    throw new ArgumentNullException("user number or dtmf is null");

                var _AgentInfo = call_factory.agent_list.FirstOrDefault<AGENT_INFO>(x => x.AgentNum == usernum);
                if(_AgentInfo == null)
                    throw new ArgumentOutOfRangeException("CallFactory.Agent_Info", "can't find channel connected with agent(usernum=)" + usernum);

                int nCh = ((AGENT_INFO)_AgentInfo).ChInfo.nCh;
                if(nCh < 0)
                    throw new ArgumentOutOfRangeException("CallFactory.Agent_Info", "can't find channel connected with agent(usernum=)" + usernum);

                //----------------板卡模式：发送DTMF字符--------------------
                //int trunkch = CallFactory.Ch_Info[ch].nLinkToCh.Value;
                //LogWrite.Write(typeof(SocketMain), LOGLEVEL.INFO, "get the outside channel is" + trunkch.ToString() + "by user number");

                //if (trunkch < 0)
                //    throw new ArgumentException("outside channel(ch=" + trunkch.ToString() + ") is not available");

                //LogWrite.Write(typeof(SocketMain), LOGLEVEL.INFO, "start send dtmf(dtmf=" + dtmf + ") to channel(ch=" + trunkch.ToString() + ")");
                //if (shpa3api.SsmApi.SsmTxDtmf(trunkch, dtmf) == -1)
                //    LogWrite.Write(typeof(SocketMain), LOGLEVEL.ERROR, "start send dtmf(dtmf=" + dtmf + ") to channel(ch=" + trunkch.ToString() + ")", new Exception(shpa3api.SsmApi.SsmGetLastErrMsgA()));
            } catch(Exception ex) {
                _Ilog.Error("processing client sends the dtmf request error.", ex);
            }
        }

        #endregion

        #region 获取媒体文件

        /// <summary>
        /// 播放录音文件
        /// </summary>
        /// <param name="information"></param>
        /// <param name="state"></param>
        private static void GetMediaFile(Hashtable information, Socket state) {
            //string FilePath = string.Empty;
            //if (fileinfo[0] == "path")
            //{
            //    FilePath = fileinfo[1].ToString();                          //搜索中播放录音  完整路径
            //}
            //else
            //{
            //    StringBuilder sb = new StringBuilder(200);
            //    GolbalData.GetPrivateProfileString("RECORDSET", "RecordPath", "", sb, 200, Application.StartupPath + "\\pbx.ini");
            //    FilePath = sb.ToString() + "\\" + FilePath;
            //    FilePath += ChannelInfo.GetRecordName(fileinfo[1].ToString() + ".wav"); //WEB请求中播放录音   不完整录音（仅录音名）
            //}
            //if (!File.Exists(FilePath))
            //{
            //    string SendData = "FileInfo{info:ErrorInfo_Nothing;}";
            //    byte[] senddata = Encoding.UTF8.GetBytes(SendData);
            //    state.Send(senddata, senddata.Length, 0);
            //    LogWrite.Write("请求播放录音文件为空", "RECORD");
            //    return;
            //}
            //else
            //{
            //    try
            //    {
            //        FileInfo f = new FileInfo(FilePath);
            //        StringBuilder sb = new StringBuilder(50);
            //        GolbalData.GetPrivateProfileString("PATHSET", "InventedPath", @"C:\", sb, 50, Application.StartupPath + "\\pbx.ini");
            //        string RarPath = sb.ToString() + "\\temp\\";
            //        if (!Directory.Exists(RarPath))
            //        {
            //            Directory.CreateDirectory(RarPath);
            //        }
            //        File.Copy(f.FullName, RarPath + f.Name, true);
            //        string SendData = "FileInfo{playmedia:temp/" + f.Name + ";}";
            //        byte[] senddata = Encoding.UTF8.GetBytes(SendData);
            //        state.Send(senddata, senddata.Length, 0);
            //    }
            //    catch (Exception ex)
            //    {
            //        LogWrite.Write("复制文件(完整路径)出错：" + ex.Message, "RECRARD");
            //    }
            //}
        }

        #endregion

        #region 处理日常交流信息

        /// <summary>
        /// 处理日常交流信息
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="state"></param>
        private static void GetMsgInfo(Hashtable Data, Socket state) {
            string ip = state.RemoteEndPoint.ToString();
            string MsgMode = SocketInfo.GetValueByKey(Data, "mode");
            string Msgs = SocketInfo.GetValueByKey(Data, "mode");
            string MsgsAim = SocketInfo.GetValueByKey(Data, "mode");
            try {
                if(MsgsAim == "all")//广播此信息
                {
                    string strDataLine = "MsgInfo{usernum`" + state.RemoteEndPoint.ToString() + "~msg`" + Msgs + "~aim`all}";
                    MsgMode = "广播说：   ";
                } else {
                    if(MsgsAim != "") {
                        string sendmsgs = "MsgInfo{usernum`" + state.RemoteEndPoint.ToString() + "~msg`" + Msgs + "~aim`}";
                        Byte[] sendData = Encoding.UTF8.GetBytes(sendmsgs);

                        ip = call_factory.agent_list.FirstOrDefault<AGENT_INFO>(x => x.AgentNum == MsgsAim).LastLoginIp;
                        IPAddress ipadr = IPAddress.Parse(ip.Split(':')[0]);
                        IPEndPoint endpoint = new IPEndPoint(ipadr, int.Parse(ip.Split(':')[1]));
                    }
                }
            } catch {
                //LogWrite.Write("GetMsgInfo   " + ex.Message, "LAN");
            }



        }

        #endregion

        #region 挂断

        /// <summary>
        /// 处理挂机信息
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="state"></param>
        private static void HungUp(Hashtable Data, Socket state) {
            try {

                //-----------------板卡模式:挂断电话请求----------------
                //string UserID = SocketInfo.GetValueByKey(Data, "User");

                //int nCh = CallFactory.Agent_Info[int.Parse(UserID)].nCh;
                //ArrayList HuCh = new ArrayList();
                //HuCh.Add(nCh);
                //if (CallFactory.Ch_Info[nCh].nLinkToCh >= 0)
                //{
                //    HuCh.Add(CallFactory.Ch_Info[nCh].nLinkToCh.Value);
                //}
                //for (int i = 0; i < HuCh.Count; i++)
                //{
                //    switch (CallFactory.Ch_Info[int.Parse(HuCh[i].ToString())].nChType)
                //    {
                //        case 0:
                //            ChannelProc.AnalogProc.SsmHangUpTrunkCh(int.Parse(HuCh[i].ToString()));
                //            break;
                //        case 2:
                //            ChannelProc.StationProc.SsmHangUpUserCh(int.Parse(HuCh[i].ToString()));
                //            break;
                //        case 11:
                //            ChannelProc.Ss7Proc.SsmHangUpISUP(int.Parse(HuCh[i].ToString()));
                //            break;
                //        case 16:
                //            ChannelProc.SipProc.SsmHangUpSipCh(int.Parse(HuCh[i].ToString()));
                //            break;
                //        default:
                //            break;
                //    }
                //}
            } catch(Exception ex) {
                _Ilog.Error("processing client sends the hangup request error.", ex);
            }
        }

        #endregion

        #region 处理心跳

        /// <summary>
        /// 处理心跳检测
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="state"></param>
        private static void SocketFlag(Hashtable Data, Socket state) {
            Thread.Sleep(1000);
            SocketMain.SendMsgToClient(SocketCommand.SendTestSocketStr(), state);
        }

        #endregion

        #region 批次下载录音

        /// <summary>
        /// 批次下载录音
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="state"></param>
        private static void LoadRecordFile(Hashtable Data, Socket state) {

        }

        #endregion

        #region 批量下载录音

        /// <summary>
        /// 批量下载录音
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="state"></param>
        private static void BotchRecordFile(Hashtable Data, Socket state) {
            try {
                string FileName = SocketInfo.GetValueByKey(Data, "File").Replace("~", ":");

                string RecordFile = string.Empty;
                string RecordFolderName = Directory.GetDirectoryRoot(ParamLib.VirtulPath) + DateTime.Now.ToString("yyyyMMddHHmmss");

                string RecordRarFile = ParamLib.CallIpAddress + "_" + DateTime.Now.ToString("yyyymmddHHmmss") + ".rar";
                string RecordRarPath = ParamLib.VirtulPath + "\\temp";

                if(!Directory.Exists(RecordRarPath))
                    Directory.CreateDirectory(RecordRarPath);

                try {
                    HttpWebRequest _WebRequest = (HttpWebRequest)WebRequest.Create(FileName);
                    _WebRequest.Method = "GET";
                    HttpWebResponse response = (HttpWebResponse)_WebRequest.GetResponse();
                    RecordFile = new StreamReader(response.GetResponseStream(), System.Text.Encoding.Default).ReadToEnd();
                } catch(Exception ex) {
                    SocketMain.SendMsgToClient(SocketCommand.SendBotchRecordLoadProgress("False", "获取文件内容失败"), state);
                    throw ex;
                }

                if(string.IsNullOrEmpty(RecordFile)) {
                    SocketMain.SendMsgToClient(SocketCommand.SendBotchRecordLoadProgress("False", "获取文件内容为空"), state);
                    throw new ArgumentNullException("get file content is empty");
                }
                _Ilog.Info("start find record when fatch load record.");

                SocketMain.SendMsgToClient(SocketCommand.SendBotchRecordLoadProgress("Loading", "开始查找录音"), state);
                try {
                    string[] RecordFileList = RecordFile.Replace("\r\n", "~").Split('~');

                    for(int i = 0; i < RecordFileList.Length; i++) {
                        string[] RecordNameInfo = RecordFileList[i].Split('|');
                        string RecordName = RecordNameInfo[3];
                        string RecordFolderPath = RecordFolderName + "\\" + RecordNameInfo[0] + "\\" + RecordNameInfo[1] + "\\" + RecordNameInfo[2];

                        if(!Directory.Exists(RecordFolderPath))
                            Directory.CreateDirectory(RecordFolderPath);

                        string RecordPath = CommonClass.GetRecordPathByName(RecordName);

                        if(File.Exists(ParamLib.RecordPath + "\\" + RecordPath) && new FileInfo(ParamLib.RecordPath + "\\" + RecordPath).Length < 50 * 1024 * 1024)
                            File.Copy(ParamLib.RecordPath + "\\" + RecordPath, RecordFolderPath + "\\" + RecordName, false);

                        if(File.Exists(ParamLib.RecordPath + "\\" + RecordPath + ".wav") && new FileInfo(ParamLib.RecordPath + "\\" + RecordPath + ".wav").Length < 50 * 1024 * 1024)
                            File.Copy(ParamLib.RecordPath + "\\" + RecordPath + ".wav", RecordFolderPath + "\\" + RecordName + ".wav", false);

                        if(File.Exists(ParamLib.RecordPath + "\\" + RecordPath + ".wma") && new FileInfo(ParamLib.RecordPath + "\\" + RecordPath + ".wma").Length < 50 * 1024 * 1024)
                            File.Copy(ParamLib.RecordPath + "\\" + RecordPath + ".wma", RecordFolderPath + "\\" + RecordName + ".wma", false);
                    }
                } catch(Exception ex) {
                    throw ex;
                }

                _Ilog.Info("start compress record when fatch load record.");
                SocketMain.SendMsgToClient(SocketCommand.SendBotchRecordLoadProgress("Loading", "start compress record"), state);
                //CreateCompressFile.RARsave(RecordFolderName, RecordRarPath, RecordRarFile);
                _Ilog.Info("start delete cache record when fatch load record.");
                Directory.Delete(RecordFolderName, true);
                _Ilog.Info("compress record when fatch load record success.");
                SocketMain.SendMsgToClient(SocketCommand.SendBotchRecordLoadProgress("Complete", RecordRarPath + "\\" + RecordRarFile), state);
            } catch(Exception ex) {
                _Ilog.Error("compress record when fatch load record failed.", ex);
            }
        }
        #endregion

        #region 发送数据到客户端(尽量少使用socket通讯,还是以sip协议为主)
        public static void SendMsgToClient(string MsgInfo, Socket _Socket) {
            try {
                string strDataLine = MsgInfo;
                Byte[] sendData = Encoding.UTF8.GetBytes(strDataLine);

                if(_Socket.Send(sendData, sendData.Length, 0) <= 0)
                    throw new Exception("socket(ip=" + _Socket.RemoteEndPoint.ToString() + ") send message to client error.");
                _Ilog.Info("send message(msg=" + MsgInfo + ") to socket(ip=" + _Socket.RemoteEndPoint.ToString() + ") success.");
            } catch(Exception ex) {
                _Ilog.Error("send message to client use socket error.", ex);
            }
        }

        public static void SendMsgToClient(string MsgInfo, int nCh) {
            try {
                string strDataLine = MsgInfo;
                Byte[] sendData = Encoding.UTF8.GetBytes(strDataLine);

                if(call_factory.channel_list[nCh].channel_socket._Socket == null)
                    throw new ArgumentException("channel(ch=" + nCh.ToString() + ") not connected server");

                if(call_factory.channel_list[nCh].channel_socket._Socket.SendTo(sendData, call_factory.channel_list[nCh].channel_socket._Socket.RemoteEndPoint) == -1)
                    throw new Exception("channel(ch=" + nCh.ToString() + ") send message to client error.");

                _Ilog.Info("send message(msg=" + MsgInfo + ") to client(ch=" + nCh.ToString() + ") success.");
            } catch(Exception ex) {
                _Ilog.Error("send message to client use channel number error.", ex);
            }
        }
        #endregion

        #region 外联模式方法
        #region 内线拨号
        public static async void _bddh_in(string _id, string _number, IWebSocketConnection socket) {
            try {
                var ua = call_factory.agent_list.FirstOrDefault(x => x.AgentID.ToString() == _id);
                if(ua == null)
                    throw new Exception($"用户{_id}未注册");
                var nCh = ua.ChInfo.nCh;
                if(nCh < 0)
                    throw new Exception($"用户{_id},{ua.AgentName},{ua.AgentNum},无通道");
                var originate = await M_NEventSocket.client.Originate($"user/{ua.ChInfo.channel_number}");
                if(originate.Success) {
                    Log.Instance.Success($"[CenoSocket][SocketMain][_bddh_in][内线][主叫号码:{ua.AgentNum},被叫号码:{_number},发起呼叫]");
                    var UUID = originate.ChannelData.UUID;
                    var bridge = await M_NEventSocket.client.Bridge(UUID, $"sofia/gateway/mygtw5060/{_number}");
                    if(bridge.Success) {
                        await socket.Send(M_WebSocketSend._bhzt_pick());
                    } else {
                        await socket.Send(M_WebSocketSend._bhzt_fail(bridge.ResponseText));
                    }
                } else {
                    await socket.Send(M_WebSocketSend._bhzt_fail(originate.ResponseText));
                }
            } catch(Exception ex) {
                Log.Instance.Error($"[CenoSocket][SocketMain][_bddh_in][Exception][{ex.Message}]");
            }
        }
        #endregion
        #region 外线拨号
        public static async void _bddh_out(string _id, string _number, IWebSocketConnection socket) {
            try {
                var originate = await M_NEventSocket.client.Originate("", new OriginateOptions() { });
            } catch(Exception ex) {
                Log.Instance.Error($"[CenoFsSharp][InboundMain][_bddh_out][Exception][{ex.Message}]");
            }
        }
        #endregion
        #endregion

        #region ***杀死UUID,强断
        public static async void m_fKill(int m_uAgentID, string m_sUUID)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(m_sUUID))
                {
                    InboundSocket client = await InboundMain.fs_cli();
                    await client.SendApi($"uuid_kill {m_sUUID}").ContinueWith(task =>
                    {
                        try
                        {
                            if (task.IsCanceled)
                            {
                                Log.Instance.Fail($"[CenoSocket][SocketMain][m_fKill][{m_uAgentID} SendApi uuid_kill cancel]");
                                return;
                            }
                            string m_sMsg = task?.Result?.BodyText;
                            if (!m_sMsg.StartsWith("+OK")) Log.Instance.Fail($"[CenoSocket][SocketMain][m_fKill][{m_uAgentID} SendApi uuid_kill:{m_sMsg}]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoSocket][SocketMain][m_fKill][{m_uAgentID} SendApi uuid_kill cancel:{ex.Message}]");
                            return;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][SocketMain][m_fKill][Exception][{m_uAgentID} SendApi uuid_kill:{ex.Message}]");
            }
        }
        #endregion

        #region ***套接字发送委托
        public static void m_fBusySendMsg(IWebSocketConnection m_pWebSocket, string m_sMsg)
        {
            try
            {
                m_pWebSocket.Send(M_WebSocketSend._bhzt_call_busy(m_sMsg));
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][SocketMain][m_fSendMsg][Exception][{ex.Message}]");
            }
        }
        #endregion
    }
}