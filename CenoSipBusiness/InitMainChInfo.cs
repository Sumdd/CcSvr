using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CenoCommon;
using CenoSipFactory;
using DB.Model;
using DB.Basic;
using CenoSocket;
using log4net;
using CenoFsSharp;
using System.Timers;
using WebSocket_v1;
using Core_v1;
using Outbound_v1;
using System.Threading.Tasks;
using Model_v1;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace CenoSipBusiness {
    public class intilizate_services {
        private static ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //private static Timer AutoDialTaskTimer;
        //private static Timer AutoSeeDialTaskTimer;

        private static Timer m_tSeeUseStatus;
        private static Timer m_tEnqueueTaskTimer;
        private static Timer m_tTaskUpdPhone;

        public static bool IsExit;
        public static bool m_bIsLoadedShare = false;
        private static bool m_bLoadShare = false;

        public static bool InitSysInfo() {

            ///<![CDATA[
            /// 该句没有用
            /// ]]>
            //fs_channellib.intilizate_fs_account_info();

            intilizete_channel();

            ///<![CDATA[
            /// 网关现查现用,这里用不到加载
            /// ]]>
            //intilizate_gateway();

            intilizate_agent();

            ///<![CDATA[
            /// 表结构换了,这里没必要加载了,太慢了
            /// ]]>
            //intilizate_switchtactics.intilizate_channel_switchtactics();

            ///<![CDATA[
            /// 表结构换了,这里没必要加载了,太慢了
            /// ]]>
            //intilizate_switchtactics.intilizate_gateway_switchtactics();

            ///<![CDATA[
            /// 共享号码池
            /// ]]>
            {
                InWebSocketMain.m_fLoadShare += intilizate_services.m_fLoadShare;
                intilizate_services.m_fLoadShare(true);
            }

            ///<![CDATA[
            /// 方法委托
            /// ]]>
            {
                m_fCallClass.m_fBusySendMsg += SocketMain.m_fBusySendMsg;
            }

            ///<![CDATA[
            /// event socket 外连模式
            /// ]]>
            MainServices.StartServices();

            /*
             * 注释掉8041
             */

            //OutboundMain.Start();

            //SocketMain.ServicesStart();

            InWebSocketMain.Start();

            ///<![CDATA[
            /// 追加网页版电话
            /// ]]>
            bool m_bIsUseWebWebSocket = Call_ParamUtil.GetParamValueByName("IsUseWebWebSocket", "0") == "1";
            if (m_bIsUseWebWebSocket)
            {
                WebWebSocketMain.Start();
            }
            else
            {
                Log.Instance.Success($"[CenoSipBusiness][intilizate_services][InitSysInfo][not use WebWebSocket]");
            }

            CenoFsSharp.InboundMain.m_fGetEventSocketMemo();
            CenoSipBusiness.intilizate_services.m_fGetAutoChannelList();
            CenoFsSharp.m_fQueueTask.m_fEnqueueTask(
                CenoFsSharp.m_fQueueTask.m_cDataTableToQueueTask(
                    DB.Basic.PhoneAutoCall.m_fGetUnDequeueTaskDataTable()));
            #region 注释
            //AutoSeeDialTaskTimer = new Timer();
            //AutoSeeDialTaskTimer.AutoReset = false;
            //AutoSeeDialTaskTimer.Interval = 5000;
            //AutoSeeDialTaskTimer.Elapsed += AutoSeeDialTaskTimer_Elapsed;
            //AutoSeeDialTaskTimer.Start();

            /*
             * 重写一下自动拨号
             * 将读取的数据放入队列中
             * 每个通道开启一个线程进行呼叫
             */

            //AutoDialTaskTimer = new Timer();
            //AutoDialTaskTimer.AutoReset = false;
            //AutoDialTaskTimer.Interval = 5000;
            //AutoDialTaskTimer.Elapsed += AutoDialTaskTimer_Elapsed;
            //AutoDialTaskTimer.Start();
            #endregion


            {
                ///<![CDATA[
                /// 支持自动拨号推送
                /// ]]>
                WebSocket_v1.InWebSocketMain.m_fDialTask += m_fPushDialTask;
            }

            m_tEnqueueTaskTimer = new Timer();
            m_tEnqueueTaskTimer.AutoReset = false;
            m_tEnqueueTaskTimer.Interval = 5000;
            m_tEnqueueTaskTimer.Elapsed += (a, b) =>
            {
                m_tEnqueueTaskTimer.Stop();
                try
                {
                    string m_sTimeStr = DateTime.Now.ToString("HH:mm:ss");
                    string m_sStartTimeStr = ParamLib.AutoDIalStartTime.Split(' ')[1];
                    string m_sEndTimeStr = ParamLib.AutoDialEndTime.Split(' ')[1];
                    if (m_sTimeStr.CompareTo(m_sStartTimeStr) == 1 && m_sTimeStr.CompareTo(m_sEndTimeStr) == -1)
                    {
                        m_fQueueTask.m_bIsSleep = false;
                        CenoFsSharp.m_fQueueTask.m_fEnqueueTask();
                    }
                    else
                    {
                        m_fQueueTask.m_bIsSleep = true;
                    }
                }
                catch (Exception ex)
                {
                    m_fQueueTask.m_bIsSleep = true;
                    Log.Instance.Success($"[CenoSipBusiness][intilizate_services][m_tEnqueueTaskTimer][Elapsed][Exception][{ex.Message}]");
                }
                m_tEnqueueTaskTimer.Start();
            };
            m_tEnqueueTaskTimer.Start();
            Log.Instance.Success($"[CenoSipBusiness][intilizate_services][InitSysInfo][start auto dial task]");

            #region ***自动更新号码任务
            m_tTaskUpdPhone = new Timer();
            m_tTaskUpdPhone.AutoReset = false;
            m_tTaskUpdPhone.Interval = 5000;
            m_tTaskUpdPhone.Elapsed += (a, b) =>
            {
                m_tTaskUpdPhone.Stop();
                try
                {
                    int m_uTaskUpdPhoneInterval = Call_ParamUtil.m_uTaskUpdPhoneInterval;
                    string m_sTaskUpdPhoneURL = Call_ParamUtil.m_sTaskUpdPhoneURL;
                    if (m_uTaskUpdPhoneInterval != -1 && !string.IsNullOrWhiteSpace(m_sTaskUpdPhoneURL))
                    {
                        lock (m_cPhone.m_pTaskUpdPhoneLock)
                        {
                            if (m_cPhone.m_lTaskUpdPhone.Count > 0)
                            {
                                string m_sPhoneNumberStr = m_cPhone.m_lTaskUpdPhone[0];
                                m_cPhone.m_lTaskUpdPhone.RemoveAt(0);
                                string m_sResponseJsonStr = Core_v1.m_cHttp.m_fGet(m_sTaskUpdPhoneURL.Replace("@Args", m_sPhoneNumberStr));
                                if (m_sPhoneNumberStr != null)
                                {
                                    JObject m_pJObject = JObject.Parse(m_sPhoneNumberStr);
                                    int status = Convert.ToInt32(m_pJObject["status"]);
                                    string msg = m_pJObject["msg"].ToString();
                                    if (status == 0)
                                    {
                                        ///处理后号码
                                        string tp = m_pJObject["tp"].ToString();
                                        ///原号
                                        string op = m_pJObject["op"].ToString();
                                        ///前缀
                                        string pp = m_pJObject["pp"].ToString();
                                        ///首字符内呼外呼
                                        string way = m_pJObject["way"].ToString();
                                        ///区号
                                        string code = m_pJObject["code"].ToString();
                                        ///归属地
                                        string addr = m_pJObject["addr"].ToString();
                                        ///卡类型
                                        string cardtype = m_pJObject["cardtype"].ToString();
                                        ///邮编
                                        string zipcode = m_pJObject["zipcode"].ToString();
                                        ///是否需要继续处理该号码
                                        string type = m_pJObject["type"].ToString();
                                        DateTime? m_dtUpdTime = null;
                                        ///最后更新时间
                                        string upt = m_pJObject["upt"].ToString();
                                        if (!string.IsNullOrWhiteSpace(upt))
                                        {
                                            try
                                            {
                                                m_dtUpdTime = Convert.ToDateTime(upt);
                                            }
                                            catch { }
                                        }

                                        ///<![CDATA[
                                        /// 执行归属地表的更新语句即可
                                        /// ]]>

                                    }
                                    else
                                    {
                                        Log.Instance.Warn($"[CenoSipBusiness][intilizate_services][m_tTaskUpdPhone][Elapsed][{msg}]");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Success($"[CenoSipBusiness][intilizate_services][m_tTaskUpdPhone][Elapsed][Exception][{ex.Message}]");
                }
                m_tTaskUpdPhone.Start();
            };
            m_tTaskUpdPhone.Start();
            #endregion

            #region ***加载路由
            {
                DB.Basic.m_cRoute.m_fInit();
                ///委托赋值
                DB.Basic.m_fDialLimit.m_fGetChByAgentID += m_fGetChByAgentID;
            }
            #endregion

            #region ***加载黑白名单
            {
                DB.Basic.m_cWblist.m_fInit();
            }
            #endregion

            #region ***加载内呼规则
            {
                DB.Basic.m_cInrule.m_fInit();
            }
            #endregion

            #region ***自动判断是否到期
            {
                try
                {
                    ///加载类库
                    string m_sPath = $"{Cmn_v1.Cmn.m_fMPath}/m_cRaw.dll";
                    Assembly asm = Assembly.LoadFrom(m_sPath);
                    Type type = asm.GetType("m_cRaw.m_csKeyFun");

                    ///将此类中的方法加载至委托,后续
                    Cmn_v1.Cmn.m_dfGetCPU += () =>
                    {
                        string m_sCPU = string.Empty;
                        try
                        {
                            object obj = type.InvokeMember("GetCPUSerialNumber", BindingFlags.InvokeMethod, null, null, new object[] { });
                            m_sCPU = obj?.ToString();
                            if (!string.IsNullOrWhiteSpace(m_sCPU)) m_sCPU = Core_v1.m_cSafe.EncryptString(m_sCPU);
                            Log.Instance.Warn($"cpu:{m_sCPU},status:{Model_v1.m_cModel.m_uUseStatus}");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoSipBusiness][intilizate_services][m_tSeeUseStatus][m_dfGetCPU][Exception][{ex.Message}]");
                            Log.Instance.Error($"[CenoSipBusiness][intilizate_services][m_tSeeUseStatus][m_dfGetCPU][InnerException][{ex?.InnerException?.Message}]");
                        }
                        return m_sCPU;
                    };

                    ///默认为4,故障
                    Model_v1.m_cModel.m_uUseStatus = 4;

                    m_tSeeUseStatus = new Timer();
                    m_tSeeUseStatus.AutoReset = false;
                    m_tSeeUseStatus.Interval = 1000 * 60 * 15;

                    ElapsedEventHandler m_pElapsedEventHandler = (a, b) =>
                    {
                        m_tSeeUseStatus.Stop();
                        try
                        {
                            object obj = type.InvokeMember("m_fCanUse", BindingFlags.InvokeMethod, null, null, new object[] { });
                            Model_v1.m_cModel.m_uUseStatus = Convert.ToInt32(obj);
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoSipBusiness][intilizate_services][m_tSeeUseStatus][Elapsed][Exception][{ex.Message}]");
                            Log.Instance.Error($"[CenoSipBusiness][intilizate_services][m_tSeeUseStatus][Elapsed][InnerException][{ex?.InnerException?.Message}]");
                        }
                        m_tSeeUseStatus.Start();
                    };
                    m_tSeeUseStatus.Elapsed += m_pElapsedEventHandler;
                    m_pElapsedEventHandler(null, null);
                }
                catch (Exception ex)
                {
                    Log.Instance.Error($"[CenoSipBusiness][intilizate_services][InitSysInfo][Exception][{ex.Message}]");
                }
            }
            #endregion

            return true;
        }

        #region 自动外呼起止时间,设定开关变量
        private static void AutoSeeDialTaskTimer_Elapsed(object sender, ElapsedEventArgs e) {
            ((System.Timers.Timer)sender).Stop();
            try {
                if(DateTime.Compare(Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd") + " " + Convert.ToDateTime(ParamLib.AutoDIalStartTime).ToString("HH:mm:ss")), DateTime.Now) < 0) {
                    if(DateTime.Compare(Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd") + " " + Convert.ToDateTime(ParamLib.AutoDialEndTime).ToString("HH:mm:ss")), DateTime.Now) > 0) {
                        ParamLib.IsAutoDial = true;
                    } else {
                        ParamLib.IsAutoDial = false;
                    }
                } else {
                    ParamLib.IsAutoDial = false;
                }
            } catch(Exception ex) {
                _Ilog.Error("检查自动拨号时间失败:" + ex.Message);
                ParamLib.IsAutoDial = false;
            }
            ((System.Timers.Timer)sender).Start();
        }
        #endregion

        private static void intilizete_channel() {
            if(call_factory.channel_list == null)
                call_factory.channel_list = new List<ChannelInfo>();

            List<call_channel_model> ChannelModel = new List<call_channel_model>(call_channel.GetList());
            if(ChannelModel == null || ChannelModel.Count() <= 0) {
                _Ilog.Error("failed to initilizate channel info, not found any data in db");
                return;
            }

            int i = 0;
            foreach(call_channel_model _model in ChannelModel)
                try {
                    call_factory.channel_list.Add(new ChannelInfo() {
                        nCh = i++,
                        channel_id = _model.ID,
                        channel_uniqueid = _model.UniqueID,
                        channel_type = _model.ChType,
                        channel_number = _model.ChNum,
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
                        IsRegister = _model.IsRegister
                    });
                    _Ilog.Info("initilizate channel(" + _model.UniqueID + ") info");
                } catch(Exception ex) {
                    _Ilog.Error("failed to initilizate channel info(id=" + _model.UniqueID + ")", ex);
                    continue;
                }

        }

        private static void intilizate_agent() {
            if(call_factory.agent_list == null)
                call_factory.agent_list = new List<AGENT_INFO>();

            List<call_agent_model> agentList = new List<call_agent_model>(call_agent_basic.GetList());
            if(agentList == null || agentList.Count() <= 0) {
                _Ilog.Error("failed to initilizate agent info, not found any data in db");
                return;
            }
            foreach(call_agent_model cam in agentList)
                try {
                    call_factory.agent_list.Add(new AGENT_INFO() {
                        AgentID = cam.ID,
                        AgentUUID = cam.UniqueID,
                        LoginName = cam.LoginName,
                        AgentName = cam.AgentName,
                        LoginPsw = cam.LoginPassWord,
                        LastLoginIp = cam.LastLoginIp,

                        ///<![CDATA[
                        /// 减压,没必要再到数据库进行查询
                        /// ]]>
                        ChInfo = call_factory.channel_list.FirstOrDefault(x => x.channel_id == cam.ChannelID),
                            //call_factory.channel_list.FirstOrDefault(x => x.channel_number == call_channel.GetModel(cam.ChannelID).ChNum),
                        AgentNum = cam.AgentNumber,

                        ///<![CDATA[
                        /// 减压,这个没用,有用也没必要这样写
                        /// ]]>
                        //RoleName = call_role.GetModel(cam.RoleID).RoleName,
                        //TeamName = call_team.GetModel(cam.TeamID).TeamName,
                        LoginState = false
                    });
                    _Ilog.Info("initilizate agent(" + cam.UniqueID + ") info");

                } catch(Exception ex) {
                    _Ilog.Error("failed to initilizate agent info(id=" + cam.ID + ")", ex);
                    continue;
                }
        }

        private static void intilizate_gateway() {
            if(call_factory.gateway_list == null)
                call_factory.gateway_list = new List<GatewayInfo>();

            List<call_gateway_model> gateway_model_list = new List<call_gateway_model>(call_gateway.GetList());
            if(gateway_model_list == null || gateway_model_list.Count() <= 0) {
                _Ilog.Error("failed to initilizate gateway info, not found any data in db");
                return;
            }
            foreach(call_gateway_model _model in gateway_model_list)
                try {
                    call_factory.gateway_list.Add(new GatewayInfo() {
                        gateway_uniqueid = _model.UniqueID,
                        gateway_user_name = _model.username,
                        gateway_name = _model.gw_name,
                        gateway_call_uuid = string.Empty,
                        gateway_call_type = new CALLTYPE(),
                        gateway_caller_number = new StringBuilder(),
                        gateway_callee_number = new StringBuilder(),
                        gateway_call_dtmf = null,
                        gateway_call_status = APP_USER_STATUS.FS_USER_IDLE,
                        gateway_call_record_info = CH_CALL_RECORD.Instance()
                    });
                    _Ilog.Info("initilizate gateway(" + _model.gw_name + ") info");

                } catch(Exception ex) {
                    _Ilog.Error("failed to initilizate gateway info(id=" + _model.ID + ")", ex);
                    continue;
                }
        }

        /// <summary>
        /// 提取自动外呼通道
        /// </summary>
        private static void m_fGetAutoChannelList()
        {
            //在通道中即可提取自动外呼通道的信息
            //提取完成之后,打电话依然要试用多号码及呼叫限制逻辑
            //就可以走队列了
            List<ChannelInfo> m_lChannelInfoList = call_factory.channel_list.Where(x => x.channel_type == 256).ToList();
            foreach (ChannelInfo m_mChannelInfo in m_lChannelInfoList)
            {
                CenoFsSharp.m_mThread entity = new m_mThread();
                entity.m_eEventWaitHandle = new System.Threading.AutoResetEvent(false);
                entity.m_mChannelInfo = m_mChannelInfo;
                entity.m_bIsStart = true;
                entity.m_tThread = new System.Threading.Thread(() =>
                {
                    CenoFsSharp.m_fQueueTask m_fQueueTaskInstance = new CenoFsSharp.m_fQueueTask();
                    m_fQueueTaskInstance.m_fWork(entity);
                });
                entity.m_tThread.Name = $"{m_mChannelInfo.channel_number}";
                CenoFsSharp.m_fQueueTask.m_lThreadList.Add(entity);
                entity.m_tThread.Start();
            }
        }

        #region 自动外呼推送
        private static m_mWebSocketJson m_fPushDialTask(string m_sSendMessage, string m_sUse, string m_sUUID)
        {
            m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
            _m_mWebSocketJson.m_sUse = m_sUse;
            try
            {
                List<m_mSampleDialTask> m_lSampleDialTask = JsonConvert.DeserializeObject<List<m_mSampleDialTask>>(m_sSendMessage);
                List<m_mQueueTask> m_lQueueTask = new List<m_mQueueTask>();
                string m_sNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string m_sResultMessage = string.Empty;
                Regex m_rReplaceRegex = new Regex("[^(0-9*#)]+");
                Regex m_rIsMatchRegex = new Regex("^[0-9*#]{3,20}$");
                bool m_bHasError = false;
                if (m_lSampleDialTask != null && m_lSampleDialTask.Count > 0)
                {
                    for (int i = 0; i < m_lSampleDialTask.Count; i++)
                    {
                        m_mSampleDialTask _m_mSampleDialTask = m_lSampleDialTask[i];
                        string m_sDealWithCallee = m_rReplaceRegex.Replace(_m_mSampleDialTask.m_sCallee, string.Empty);
                        if (!m_rIsMatchRegex.IsMatch(m_sDealWithCallee))
                        {
                            m_sResultMessage = $"拨号任务{i + 1}:号码有误";
                            m_bHasError = true;
                            break;
                        }
                        m_mQueueTask _m_mQueueTask = new m_mQueueTask();
                        _m_mQueueTask.ID = null;
                        _m_mQueueTask.PhoneNum = m_sDealWithCallee;
                        _m_mQueueTask.pici = null;
                        _m_mQueueTask.progressFlag = null;
                        _m_mQueueTask.contentTxt = _m_mSampleDialTask.m_sContent;
                        _m_mQueueTask.status = "1";
                        _m_mQueueTask.addTime = m_sNow;
                        _m_mQueueTask.callTime = null;
                        _m_mQueueTask.endTime = null;
                        _m_mQueueTask.result = "1";
                        _m_mQueueTask.IsUpdate = 0;
                        _m_mQueueTask.luyinId = null;
                        _m_mQueueTask.CallNum = null;
                        _m_mQueueTask.CallStatus = 0;
                        _m_mQueueTask.CallCount = 0;
                        _m_mQueueTask.source_id = null;
                        _m_mQueueTask.ajid = _m_mSampleDialTask.m_sUUID;
                        _m_mQueueTask.inpici = m_sNow
                            .Replace("-", "")
                            .Replace(" ", "")
                            .Replace(":", "");
                        _m_mQueueTask.shfzh18 = null;
                        _m_mQueueTask.czy = "WebApi";
                        m_lQueueTask.Add(_m_mQueueTask);
                    }
                }
                else
                {
                    m_sResultMessage = "拨号任务为空";
                    m_bHasError = true;
                }
                if (m_bHasError)
                {
                    _m_mWebSocketJson.m_oObject = new
                    {
                        m_sUUID = m_sUUID,
                        m_sStatus = -1,
                        m_sResultMessage = m_sResultMessage
                    };
                    return _m_mWebSocketJson;
                }
                bool m_bBool = CenoFsSharp.m_fQueueTask.m_fEnqueueTask(m_lQueueTask, out m_sResultMessage);
                _m_mWebSocketJson.m_oObject = new
                {
                    m_sUUID = m_sUUID,
                    m_sStatus = 0,
                    m_sResultMessage = m_sResultMessage
                };
                return _m_mWebSocketJson;
            }
            catch (Exception ex)
            {
                _m_mWebSocketJson.m_oObject = new
                {
                    m_sUUID = m_sUUID,
                    m_sStatus = -1,
                    m_sResultMessage = ex.Message
                };
                return _m_mWebSocketJson;
            }
        }
        #endregion

        #region ***根据规则查找接入坐席
        private static void m_fGetChByAgentID(m_mRoute _m_mRoute, int m_uDefAgentID, out int m_uAgentID)
        {
            m_uAgentID = -1;

            if (m_uDefAgentID != -1)
            {
                ///如果坐席已寻到,首先看自己是否空闲
                AGENT_INFO m_uAgent = call_factory.agent_list.Find(
                    x => x.AgentID == m_uDefAgentID &&
                    (x.ChInfo.IsRegister == 1 && x.ChInfo.channel_websocket != null || x.ChInfo.IsRegister != 1) &&
                    (x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_IDLE ||
                    x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_AHANGUP ||
                    x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP));

                ///接给自己
                if (m_uAgent != null)
                {
                    m_uAgentID = m_uDefAgentID;
                    return;
                }
            }

            ///逻辑已经固定,这里进行随机接入空闲坐席即可
            if (_m_mRoute.routeua != null && _m_mRoute.routeua.Count > 0)
            {
                switch (_m_mRoute.ctype)
                {
                    case 1:
                        {
                            AGENT_INFO m_uAgent = call_factory.agent_list.Find(
                                   x => _m_mRoute.routeua.Contains(x.AgentID) &&
                                   (x.ChInfo?.IsRegister == 1 && x.ChInfo?.channel_websocket != null || x.ChInfo?.IsRegister != 1) &&
                                   (x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_IDLE ||
                                   x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_AHANGUP ||
                                   x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP));
                            if (m_uAgent != null)
                                m_uAgentID = m_uAgent.AgentID;
                        }
                        break;
                    case 2:
                        {
                            AGENT_INFO m_uAgent = call_factory.agent_list.FindLast(
                                   x => _m_mRoute.routeua.Contains(x.AgentID) &&
                                   (x.ChInfo?.IsRegister == 1 && x.ChInfo?.channel_websocket != null || x.ChInfo?.IsRegister != 1) &&
                                   (x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_IDLE ||
                                   x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_AHANGUP ||
                                   x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP));
                            if (m_uAgent != null)
                                m_uAgentID = m_uAgent.AgentID;
                        }
                        break;
                    case 3:
                        {
                            Random m_pRandom = new Random();
                            List<AGENT_INFO> m_lAgent = call_factory.agent_list.FindAll(
                                     x => _m_mRoute.routeua.Contains(x.AgentID) &&
                                     (x.ChInfo?.IsRegister == 1 && x.ChInfo?.channel_websocket != null || x.ChInfo?.IsRegister != 1) &&
                                     (x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_IDLE ||
                                     x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_AHANGUP ||
                                     x.ChInfo?.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP));
                            if (m_lAgent != null && m_lAgent.Count > 0)
                            {
                                m_uAgentID = m_lAgent[m_pRandom.Next(m_lAgent.Count)].AgentID;
                            }
                        }
                        break;
                }
            }
        }
        #endregion

        #region ***Redis默认加载
        public static void m_fLoadShare(bool m_bLoad)
        {
            if (intilizate_services.m_bLoadShare)
            {
                Log.Instance.Warn($"[CenoSipBusiness][intilizate_services][m_fLoadShare][loading,please await]");
                return;
            }

            new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                try
                {
                    intilizate_services.m_bLoadShare = true;
                    #region ***异步加载共享号码
                    ///<![CDATA[
                    /// 共享号码逻辑增加
                    /// 0.无,不做任何加载处理
                    /// 1.判断是否是主服务器
                    /// 2.非主服务器简化
                    /// ]]>

                    switch (DB.Basic.Call_ParamUtil.m_uShareNumSetting)
                    {
                        case 1:
                            //读取数据
                            DB.Basic.m_fDialLimit.m_fGetDialArea();
                            DB.Basic.m_fDialLimit.m_fGetShareNumber();
                            //将此redis做为本机共享号码域
                            intilizate_services.m_fLoadRedis(true);
                            //写入redis
                            if (m_bLoad)
                            {
                                //号码是否已加载过
                                if (!intilizate_services.m_bIsLoadedShare)
                                {
                                    intilizate_services.m_bIsLoadedShare = true;
                                    Core_v1.Redis2.m_fSetShareNum();
                                }
                                else Core_v1.Redis2.m_fShareSynchronize();
                            }
                            else Core_v1.Redis2.m_fShareSynchronize();
                            break;
                        case 2:
                            //读取数据
                            DB.Basic.m_fDialLimit.m_fGetDialArea();
                            //号码加载过需删除,并缓存为未加载
                            if (intilizate_services.m_bIsLoadedShare)
                            {
                                intilizate_services.m_bIsLoadedShare = false;
                                //不先做删除,删除逻辑不成熟
                                //Redis2.m_fClearShare();
                                //读取主服务器中的redis做为本机共享号码域
                                intilizate_services.m_fLoadRedis(false);
                            }
                            else
                            {
                                //读取主服务器中的redis做为本机共享号码域
                                intilizate_services.m_fLoadRedis(false);
                            }
                            break;
                        default:
                            //号码加载过需删除,并缓存为未加载
                            if (intilizate_services.m_bIsLoadedShare)
                            {
                                intilizate_services.m_bIsLoadedShare = false;
                                //不先做删除,删除逻辑不成熟
                                //Redis2.m_fClearShare();
                            }
                            Log.Instance.Warn($"[CenoSipBusiness][intilizate_services][m_fLoadShare][default 0,not use share]");
                            break;
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Log.Instance.Warn($"[CenoSipBusiness][intilizate_services][m_fLoadShare][Exception][{ex.Message}]");
                }
                finally
                {
                    intilizate_services.m_bLoadShare = false;
                }

            })).Start();
        }
        private static void m_fLoadRedis(bool m_bMain)
        {
            try
            {
                string m_sRedisConfig = string.Empty;
                if (m_bMain)
                {
                    Redis2.use = Call_ParamUtil.m_bIsHasRedis;
                    m_sRedisConfig = Call_ParamUtil.GetParamValueByName("RedisConfig");
                }
                else
                {
                    string m_sConnStr = MySQLDBConnectionString.m_fConnStr(Redis2.m_EsyMainDialArea);
                    Redis2.use = Call_ParamUtil.GetParamValueByName("IsHasRedis", "0", m_sConnStr) == "1";
                    m_sRedisConfig = Call_ParamUtil.GetParamValueByName("RedisConfig", $"{Redis2.m_EsyMainDialArea.aip}:{Redis2.defaultPort};123456;15", m_sConnStr)?.Replace(Redis2.defaultHost, Redis2.m_EsyMainDialArea.aip);
                }

                if (Redis2.use)
                {
                    string[] m_lRedisConfig = m_sRedisConfig.Split(';');
                    if (m_lRedisConfig.Count() >= 2)
                    {
                        string password = m_lRedisConfig[1];
                        Redis2.password = string.IsNullOrWhiteSpace(password) ? null : password;
                    }
                    if (m_lRedisConfig.Count() >= 3)
                    {
                        int m_uDb = Redis2.defaultDb;
                        int.TryParse(m_lRedisConfig[2], out m_uDb);
                        Redis2.db = m_uDb;
                    }
                    string[] m_lHostPort = m_lRedisConfig[0].Split(':');
                    Redis2.host = m_lHostPort[0];
                    int.TryParse(m_lHostPort[1], out Redis2.port);
                    Log.Instance.Success($"[CenoSipBusiness][intilizate_services][m_fLoadRedis][{Redis2.host}:{Redis2.port}]");
                }

                Redis2.reUse = true;
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSipBusiness][intilizate_services][m_fLoadRedis][Exception][{ex.Message}]");
                Redis2.use = false;
            }
        }
        #endregion
    }
}
