using CenoFsSharp;
using CenoSipFactory;
using Cmn_v1;
using Core_v1;
using DB.Basic;
using DB.Model;
using Fleck;
using Model_v1;
using NEventSocket;
using NEventSocket.FreeSwitch;
using NEventSocket.Util;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace CenoSocket
{
    public class m_cIp
    {
        public static async void m_fExecuteDial(IWebSocketConnection m_pWebSocket, string m_sUUID, string m_sLoginName, string m_sPhoneNumber, string m_sCaller, string m_sNumberType, int m_uMustNbr = 0)
        {
            try
            {
                await m_fDial(m_pWebSocket, m_sUUID, m_sLoginName, m_sPhoneNumber, m_sCaller, m_sNumberType, m_uMustNbr);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][m_cIp][m_fExecuteDial][logic error:{ex.Message}]");
            }
        }

        public static async Task m_fDial(IWebSocketConnection m_pWebSocket, string m_sUUID, string m_sLoginName, string m_sPhoneNumber, string m_sCaller, string m_sNumberType, int m_uMustNbr = 0)
        {
            int m_uAgentID = -1;
            ChannelInfo m_mChannel = null;
            share_number m_pShareNumber = null;
            bool m_bShare = false;//是否使用了共享号码
            string uuid = Guid.NewGuid().ToString();
            //Caller-Caller-ID-Number
            string m_sUAID = string.Empty;
            //FreeSWITCH-IPv4
            string m_sFreeSWITCHIPv4 = InboundMain.FreesSWITCHIPv4;
            //是否执行了Redis锁删除
            bool m_bDeleteRedisLock = false;
            //真实号码
            string m_stNumberStr = string.Empty;
            //此处183直接接通是为了兼容早期媒体无法透传的问题,此处依然保留即可
            bool m_bUseChannelProgressMedia = true;
            //IP话机显示归属地参数
            bool m_bIsIpShowWhere = Call_ParamUtil.m_bIsIpShowWhere;
            //IP话机显示归属地
            string _m_sPhoneAddressStr = string.Empty;

            try
            {
                AGENT_INFO m_mAgent = call_factory.agent_list.Find(x => x.LoginName == m_sLoginName);
                if (m_mAgent == null)
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} miss a leg info]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err账户有误");
                    return;
                }
                m_uAgentID = m_mAgent.AgentID;
                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} inbound web dial]");

                //使用共享号码必须填写外显号码
                m_bShare = m_sNumberType == Special.Share;
                if (m_bShare && string.IsNullOrWhiteSpace(m_sCaller))
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} miss share number]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err共享号码");
                    return;
                }

                //继续处理,防止号码有误
                Regex m_rReplaceRegex = new Regex("[^(0-9*#)]+");
                Regex m_rIsMatchRegex = new Regex("^[0-9*#]{3,20}$");
                string m_sDealWithPhoneNumberStr = m_rReplaceRegex.Replace(m_sPhoneNumber, string.Empty);
                if (!m_rIsMatchRegex.IsMatch(m_sDealWithPhoneNumberStr))
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} invalid phone]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err号码有误");
                    return;
                }

                List<string> m_lStrings = m_cPhone.m_fGetPhoneNumberMemo(m_sDealWithPhoneNumberStr);
                string m_sDealWithRealPhoneNumberStr = m_lStrings[0];
                bool m_bStar = m_lStrings[2] == Special.Star;
                string m_sPhoneAddressStr = m_lStrings[3];
                //IP话机显示地址即可,看看效果如何
                if (m_bIsIpShowWhere) _m_sPhoneAddressStr = m_sPhoneAddressStr;
                string m_sCityCodeStr = m_lStrings[4];
                string m_sDealWithStr = m_lStrings[5];

                int m_uCh = m_mAgent.ChInfo.nCh;
                m_mChannel = call_factory.channel_list[m_uCh];

                ///<![CDATA[
                /// IP话机的引入
                /// 注册状态可受范围:
                /// 1.1注册
                /// 2.0不注册,一开始暂定的IP话机模式
                /// 3.-1不注册,IP话机Web模式,可使用网页进行拨打
                /// 如果客户需要同时使用客户端与网页?这里放开0不注册的权限
                /// ]]>

                if (m_uCh == -1 || m_mChannel == null || m_mChannel?.channel_type != Special.SIP || (m_mChannel?.IsRegister != 0 && m_mChannel?.IsRegister != -1))
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} miss channel or not sip channel]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err通道有误");
                    return;
                }

                if (m_bShare) m_sUAID = m_mChannel.channel_number;
                if (
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_IDLE &&
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_AHANGUP &&
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_BHANGUP
                    )
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} channel busy]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err通道繁忙");
                    return;
                }

                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
                call_record_model m_mRecord = new call_record_model(m_mChannel.channel_id);
                m_mRecord.AgentID = m_uAgentID;
                m_mRecord.UAID = m_sUAID;
                m_mRecord.fromagentname = m_mAgent.AgentName;
                m_mRecord.fromloginname = m_mAgent.LoginName;
                m_mDialLimit _m_mDialLimit = null;
                string m_sCalleeNumberStr = m_sDealWithRealPhoneNumberStr;
                string m_sCalleeRemove0000Prefix = string.Empty;

                if (!m_bStar)
                {
                    #region 拨号限制,增加号码池概念
                    switch (m_sNumberType)
                    {
                        case Special.Common:
                            {
                                _m_mDialLimit = m_fDialLimit.m_fGetDialLimitObject(m_sDealWithRealPhoneNumberStr, m_uAgentID, m_sCaller);
                                break;
                            }
                        case Special.Share:
                            {
                                //跳转至号码池逻辑,需要持久化至数据库,录音记录都进行保存
                                string m_sErrMsg = string.Empty;
                                m_pShareNumber = Redis2.m_fGetTheShareNumber(uuid, m_uAgentID, m_sCaller, m_sDealWithRealPhoneNumberStr, DB.Basic.Call_ParamUtil.m_uShareNumSetting, out m_sErrMsg);
                                _m_mDialLimit = m_fDialLimit.m_fGetDialLimitByShare(m_pShareNumber);
                                if (_m_mDialLimit == null)
                                {
                                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} get share error]");
                                    if (string.IsNullOrWhiteSpace(m_sErrMsg)) m_sErrMsg = "Err获取号码";
                                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, $"{m_sErrMsg}");

                                    m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                                    m_mChannel.channel_call_uuid = null;
                                    m_mChannel.channel_call_other_uuid = null;

                                    return;
                                }
                                //如果可以出来,任何需要解锁的地方都要加逻辑
                                m_mRecord.isshare = 1;
                                m_mRecord.FreeSWITCHIPv4 = m_sFreeSWITCHIPv4;
                                break;
                            }
                        default:
                            {
                                Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} unknown number type]");
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err号码类别");

                                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                                m_mChannel.channel_call_uuid = null;
                                m_mChannel.channel_call_other_uuid = null;

                                return;
                            }
                    }

                    if (_m_mDialLimit != null && !string.IsNullOrWhiteSpace(_m_mDialLimit.m_sNumberStr))
                    {
                        #region 网关有误
                        if (string.IsNullOrWhiteSpace(_m_mDialLimit.m_sGatewayNameStr))
                        {
                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_other_uuid = null;
                            Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} gateway fail]");
                            m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err网关有误");

                            if (m_bShare && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                            }

                            return;
                        }
                        #endregion

                        m_mRecord.LocalNum = _m_mDialLimit.m_sNumberStr;
                        Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} dialcount:{_m_mDialLimit.m_uDialCount}]");

                        ///<![CDATA[
                        /// 空也做强制加拨处理
                        /// ]]>

                        ///强制加拨前缀只使用外地加拨即可
                        if (_m_mDialLimit.m_sAreaCodeStr == "0000" || string.IsNullOrWhiteSpace(_m_mDialLimit.m_sAreaCodeStr) || m_uMustNbr == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(_m_mDialLimit.m_sDialPrefixStr))
                            {
                                //强制加前缀
                                Log.Instance.Debug($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} add prefix:{_m_mDialLimit.m_sDialPrefixStr}]");
                            }
                            m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialPrefixStr}{m_sDealWithRealPhoneNumberStr}";
                            //原号码
                            m_sCalleeRemove0000Prefix = $"{m_sDealWithRealPhoneNumberStr}";
                        }
                        else
                        {
                            switch (m_sDealWithStr)
                            {
                                case Special.Mobile:
                                    if (!m_sDealWithRealPhoneNumberStr.Contains('*') && !m_sDealWithRealPhoneNumberStr.Contains('#'))
                                    {
                                        if (_m_mDialLimit.m_bZflag)
                                        {

                                            ///<![CDATA[
                                            /// 当被叫号码未找到归属地时,不加拨前缀
                                            /// ]]>

                                            if (!string.IsNullOrWhiteSpace(m_sCityCodeStr) && _m_mDialLimit.m_sAreaCodeStr != m_sCityCodeStr)
                                            {
                                                m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialPrefixStr}{m_sDealWithRealPhoneNumberStr}";
                                            }
                                            else
                                            {
                                                m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialLocalPrefixStr}{m_sDealWithRealPhoneNumberStr}";
                                            }
                                            //原号码
                                            m_sCalleeRemove0000Prefix = $"{m_sDealWithRealPhoneNumberStr}";
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        m_mRecord.CallType = 1;
                        //保证号码真实性,确保可以直接回呼
                        m_mRecord.T_PhoneNum = string.IsNullOrWhiteSpace(m_sCalleeRemove0000Prefix) ? m_sCalleeNumberStr : m_sCalleeRemove0000Prefix;
                        m_mRecord.C_PhoneNum = m_sDealWithPhoneNumberStr;
                        m_mRecord.PhoneAddress = m_sPhoneAddressStr;
                    }
                    else
                    {
                        m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        m_mChannel.channel_call_uuid = null;
                        m_mChannel.channel_call_other_uuid = null;
                        Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} no phone number]");
                        m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err拨号限制");

                        if (m_bShare && !m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                        }

                        return;
                    }
                    #endregion
                }
                else
                {
                    m_mRecord.LocalNum = m_mAgent.ChInfo.channel_number;
                    m_mRecord.CallType = 6;
                    m_mRecord.T_PhoneNum = m_sCalleeNumberStr;
                    m_mRecord.C_PhoneNum = m_mRecord.T_PhoneNum;
                    m_mRecord.PhoneAddress = m_sPhoneAddressStr;
                }

                bool m_bInboundTest = Call_ParamUtil.InboundTest;
                string m_sEndPointStrA = $"user/{m_mChannel.channel_number}";
                string m_sEndPointStrB = string.Empty;
                if (m_bStar) m_sEndPointStrB = $"user/{m_sDealWithRealPhoneNumberStr}";
                else
                {
                    if (m_bInboundTest) m_sEndPointStrB = $"user/{m_sDealWithRealPhoneNumberStr}";
                    else
                    {
                        if (_m_mDialLimit.m_bGatewayType)
                        {
                            //m_sEndPointStrB = $"sofia/gateway/{_m_mDialLimit.m_sGatewayNameStr}/{m_sCalleeNumberStr}";
                            if (_m_mDialLimit.m_sGatewayNameStr == "haoshunhz")
                            {
                                if (
                                    m_mRecord.T_PhoneNum == "114"
                                    ||
                                    (m_mRecord.T_PhoneNum.StartsWith("0") && m_mRecord.T_PhoneNum.EndsWith("114"))
                                    ||
                                    m_sDealWithStr == Special.Telephone
                                    )
                                {
                                    string m_sPrefixCallee = m_sCalleeNumberStr.StartsWith("3303") ? m_sCalleeNumberStr : $"3303{m_sCalleeNumberStr}";
                                    m_sEndPointStrB = $"sofia/gateway/haoshunhzgh/{m_sPrefixCallee}";
                                }
                                else
                                {
                                    m_sEndPointStrB = $"sofia/gateway/{_m_mDialLimit.m_sGatewayNameStr}/{m_sCalleeNumberStr}";
                                }
                            }
                            else
                            {
                                m_sEndPointStrB = $"sofia/gateway/{_m_mDialLimit.m_sGatewayNameStr}/{m_sCalleeNumberStr}";
                            }
                        }
                        else m_sEndPointStrB = $"sofia/{_m_mDialLimit.m_sGatewayType}/sip:{m_sCalleeNumberStr}@{_m_mDialLimit.m_sGatewayNameStr}";
                    }
                }

                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} a-leg-endpoint:{m_sEndPointStrA},number:{m_mRecord.LocalNum}]");
                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} b-leg-endpoint:{m_sEndPointStrB},number:{m_mRecord.T_PhoneNum}]");

                string m_sApplicationStr = Call_ParamUtil._application;
                int m_uTimeoutSeconds = Call_ParamUtil.__timeout_seconds;
                int m_uALegTimeoutSeconds = Call_ParamUtil.ALegTimeoutSeconds;
                bool m_bIgnoreEarlyMedia = Call_ParamUtil.__ignore_early_media;
                bool m_bIsLinked = false;
                string m_sExtensionStr = Call_ParamUtil._rec_t;
                string m_sWhoHangUpStr = string.Empty;
                m_mChannel.channel_call_uuid = uuid;
                //真实号码赋值
                if (!m_bStar) m_stNumberStr = _m_mDialLimit.m_stNumberStr;
                //录音中真实号码赋值
                m_mRecord.tnumber = m_stNumberStr;

                InboundSocket m_sClient = await InboundMain.fs_cli().ContinueWith(task =>
                {
                    try
                    {
                        if (task.IsCanceled) return null;
                        else return task.Result;
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} socket client error:{ex.Message}]");
                        return null;
                    }
                });

                if (m_sClient == null)
                {
                    m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                    m_mChannel.channel_call_uuid = null;
                    m_mChannel.channel_call_other_uuid = null;
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} miss InboundSocket]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "ErrESL");

                    if (m_bShare && !m_bDeleteRedisLock)
                    {
                        m_bDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                    }

                    return;
                }

                bool m_bIsDispose = false;
                IDisposable m_eEventChannelPark = null;
                IDisposable m_eChannel200 = null;
                m_sClient.Disposed += (a, b) =>
                {
                    try
                    {
                        m_bIsDispose = true;
                        Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} socket client dispose]");
                        if (m_eEventChannelPark != null)
                        {
                            m_eEventChannelPark.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} event ChannelPark dispose]");
                        }

                        if (m_eChannel200 != null)
                        {
                            m_eChannel200.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} event ChannelAnswer dispose]");
                        }

                        if (m_bShare && !m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} socket client dispose do error:{ex.Message}]");
                    }
                };

                if (m_bIsDispose) return;
                await m_sClient.SubscribeEvents(EventName.ChannelPark).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} event ChannelPark cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} event ChannelPark error:{ex.Message}]");
                    }
                });

                DateTime m_dtStartTimeNow = DateTime.Now;
                string m_sStartTimeNowString = Cmn.m_fDateTimeString(m_dtStartTimeNow);
                m_mRecord.C_Date = m_sStartTimeNowString;
                m_mRecord.C_StartTime = m_sStartTimeNowString;
                //将bridgeUUID提前生成
                string bridgeUUID = Guid.NewGuid().ToString();

                #region ***修改根据200计算通话时长
                bool Channel200 = false;

                if (m_bIsDispose) return;
                await m_sClient.SubscribeEvents(EventName.ChannelAnswer).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} event 200 cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} event 200 error:{ex.Message}]");
                    }
                });

                //200消息处理,得到真实的拨打时间
                m_eChannel200 = m_sClient.ChannelEvents.Where(x => x.UUID == bridgeUUID && (x.EventName == EventName.ChannelAnswer)).Take(1).Subscribe(x =>
                {
                    if (Channel200) return;
                    //接通,解决200问题
                    Channel200 = true;

                    DateTime m_dtNow = DateTime.Now;
                    //计算接通时间和等待时间
                    string m_dtAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtNow);
                    m_mRecord.C_AnswerTime = m_dtAnswerTimeNowString;
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtNow, m_dtStartTimeNow);
                });
                #endregion

                //通道停泊
                if (m_bIsDispose) return;
                m_eEventChannelPark = m_sClient.ChannelEvents.Where(x => x.UUID == uuid && x.EventName == EventName.ChannelPark).Take(1).Subscribe(async x =>
                {
                    Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} event {x.EventName} success]");

                    //主叫挂断
                    if (m_bIsDispose) return;
                    m_sClient.OnHangup(uuid, ax =>
                    {
                        if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                        {
                            m_sWhoHangUpStr = "A";
                            Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} a-leg hangup]");

                            DateTime m_dtEndTimeNow = DateTime.Now;
                            string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                            m_mRecord.C_EndTime = m_sEndTimeNowString;
                            //追加200消息
                            if (m_bIsLinked && Channel200)
                            {
                                m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_mRecord.C_AnswerTime);
                                m_mRecord.CallResultID = m_bStar ? 31 : 1;
                            }
                            else
                            {
                                m_mRecord.C_SpeakTime = 0;
                                m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                                m_mRecord.CallResultID = m_bStar ? 37 : 10;

                                //未接通主叫挂断
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "主叫挂断,取消呼叫");
                            }

                            dial_area m_pDialArea = null;
                            if (!m_bStar && m_bShare && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, m_mRecord.C_SpeakTime);
                            }

                            Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} insert record]");
                            call_record.Insert(m_mRecord, !m_bStar && m_bShare, m_pDialArea);

                            if (!m_bStar)
                            {
                                m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, m_mRecord.C_SpeakTime);
                                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} update diallimit]");
                            }

                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_other_uuid = null;

                            if (m_bIsDispose) return;
                            if (m_sClient != null && m_sClient.IsConnected)
                            {
                                m_sClient.Exit().ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
                                    }
                                });
                            }
                        }
                    });

                    m_mChannel.channel_call_other_uuid = bridgeUUID;
                    m_mRecord.C_RingTime = Cmn.m_fDateTimeString();

                    //桥接被叫
                    if (m_bIsDispose) return;
                    BridgeResult m_pBridgeResult = await m_sClient.Bridge(uuid, m_sEndPointStrB, new BridgeOptions()
                    {
                        UUID = bridgeUUID,
                        CallerIdNumber = m_mRecord.LocalNum,
                        CallerIdName = m_mRecord.LocalNum,
                        HangupAfterBridge = false,
                        ContinueOnFail = true,
                        TimeoutSeconds = m_uTimeoutSeconds,
                        IgnoreEarlyMedia = m_bIgnoreEarlyMedia

                    }).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return null;
                            if (task.IsCanceled)
                            {
                                Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Bridge cancel]");
                                return null;
                            }
                            else
                            {
                                if (m_bIsDispose) return null;
                                return task.Result;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Bridge error:{ex.Message}]");
                            return null;
                        }
                    });

                    //桥接失败
                    if (m_pBridgeResult == null || (m_pBridgeResult != null && !m_pBridgeResult.Success))
                    {
                        string m_sBridgeResultStr = m_pBridgeResult?.ResponseText;
                        if (string.IsNullOrWhiteSpace(m_sBridgeResultStr)) m_sBridgeResultStr = null;

                        string m_sMsgStr = $"Bridge fail:{m_sBridgeResultStr ?? "unknow"}";
                        Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} {m_sMsgStr}]");

                        if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                        {
                            m_sWhoHangUpStr = "A";

                            string m_sSendMsgStr = "Err呼叫失败";
                            string m_sWebSendMsgStr = string.Empty;

                            DateTime m_dtEndTimeNow = DateTime.Now;
                            string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                            m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                            m_mRecord.C_EndTime = m_sEndTimeNowString;

                            #region 判断电话结果
                            if (string.IsNullOrWhiteSpace(m_sBridgeResultStr))
                            {
                                m_mRecord.CallResultID = m_bStar ? 34 : 7;
                                m_sSendMsgStr = "无人接听";
                            }
                            else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "NOANSWER"))
                            {
                                m_mRecord.CallResultID = m_bStar ? 34 : 7;
                                m_sSendMsgStr = "无人接听";
                            }
                            else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "BUSY"))
                            {
                                m_mRecord.CallResultID = m_bStar ? 35 : 8;
                                m_sSendMsgStr = "用户忙";
                            }
                            else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "INVALIDARGS"))
                            {
                                m_mRecord.CallResultID = m_bStar ? 37 : 10;
                            }
                            else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "USER_NOT_REGISTERED"))
                            {
                                m_mRecord.CallResultID = m_bStar ? 39 : 12;
                                m_sWebSendMsgStr = "Err被叫未注册";
                            }
                            else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "SUBSCRIBER_ABSENT"))
                            {
                                m_mRecord.CallResultID = m_bStar ? 39 : 12;
                            }
                            else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "NO_USER_RESPONSE"))
                            {
                                m_mRecord.CallResultID = m_bStar ? 53 : 54;
                                m_sSendMsgStr = "拒接";
                            }
                            else
                            {
                                m_mRecord.CallResultID = m_bStar ? 34 : 7;
                            }
                            #endregion

                            //发送呼叫失败具体原因
                            if (string.IsNullOrWhiteSpace(m_sWebSendMsgStr)) m_sWebSendMsgStr = m_sSendMsgStr;
                            m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, $"{m_sWebSendMsgStr}");

                            m_mRecord.Remark = m_sMsgStr;

                            dial_area m_pDialArea = null;
                            if (!m_bStar && m_bShare && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, 0);
                            }

                            Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} insert record]");
                            call_record.Insert(m_mRecord, !m_bStar && m_bShare, m_pDialArea);

                            if (!m_bStar)
                            {
                                m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, 0);
                                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} update diallimit]");
                            }

                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_other_uuid = null;

                            if (m_bIsDispose) return;
                            if (m_sClient != null && m_sClient.IsConnected)
                            {
                                await m_sClient.Hangup(uuid, HangupCause.OriginatorCancel).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Hangup error:{ex.Message}]");
                                    }
                                });
                            }

                            if (m_bIsDispose) return;
                            if (m_sClient != null && m_sClient.IsConnected)
                            {
                                await m_sClient.Exit().ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
                                    }
                                });
                            }
                        }
                    }
                    //桥接成功
                    else
                    {
                        m_bIsLinked = true;
                        m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_TALKING;
                        DateTime m_dtAnswerTimeNow = DateTime.Now;
                        #region ***计算接通时间和等待时间
                        //如果比200快,这里应该不可能,而且如果接通,一定会有200消息
                        if (false && !Channel200)
                        {
                            string m_sAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtAnswerTimeNow);
                            m_mRecord.C_AnswerTime = m_sAnswerTimeNowString;
                            m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtAnswerTimeNow, m_dtStartTimeNow);
                        }
                        #endregion

                        Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Bridge success]");

                        //发送拨号成功
                        //m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, 0, "Success拨号成功");

                        #region 录音参数设置
                        if (m_bIsDispose) return;
                        await m_sClient.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} RECORD_ARTIST cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} RECORD_ARTIST error:{ex.Message}]");
                            }
                        });
                        if (m_bIsDispose) return;
                        await m_sClient.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0).ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} RECORD_MIN_SEC cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} RECORD_MIN_SEC error:{ex.Message}]");
                            }
                        });
                        if (m_bIsDispose) return;
                        await m_sClient.SetChannelVariable(uuid, "RECORD_STEREO", "true").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} RECORD_STEREO cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} RECORD_STEREO error:{ex.Message}]");
                            }
                        });
                        #endregion

                        //录音中来电主叫或去掉被叫的真实号码获取
                        string m_sRectNumberStr = (!string.IsNullOrWhiteSpace(m_stNumberStr) ? m_stNumberStr : m_mRecord.LocalNum);
                        string m_sRecSub = $"{{0}}\\{m_dtAnswerTimeNow.ToString("yyyy")}\\{m_dtStartTimeNow.ToString("yyyyMM")}\\{m_dtAnswerTimeNow.ToString("yyyyMMdd")}\\Rec_{m_dtAnswerTimeNow.ToString("yyyyMMddHHmmss")}_{m_sRectNumberStr}_{(m_bStar ? "N" : "")}Q_{m_mRecord.T_PhoneNum.Replace("*", "X")}{m_sExtensionStr}";
                        string m_sRecordingFile = string.Format(m_sRecSub, ParamLib.RecordFilePath);
                        string m_sRecordingFolder = Path.GetDirectoryName(m_sRecordingFile);
                        if (!Directory.Exists(m_sRecordingFolder)) Directory.CreateDirectory(m_sRecordingFolder);
                        string m_sRecordingID = Path.GetFileNameWithoutExtension(m_sRecordingFile);

                        //录音
                        {
                            if (m_bIsDispose) return;
                            var m_pRecordingResult = await m_sClient.SendApi(string.Format("uuid_record {0} start {1}", uuid, m_sRecordingFile)).ContinueWith((task) =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return null;
                                    if (task.IsCanceled)
                                    {
                                        Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} record cancel]");
                                        return null;
                                    }
                                    else
                                    {
                                        if (m_bIsDispose) return null;
                                        return task.Result;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} record error:{ex.Message}]");
                                    return null;
                                }
                            });
                            if (m_pRecordingResult != null && m_pRecordingResult.Success)
                            {
                                m_mRecord.RecordFile = m_sRecordingFile;
                                m_mRecord.recordName = m_sRecordingID;
                                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} record ID:{m_sRecordingID} send]");

                                //录音成功
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, 0, $"{m_sRecordingID}");
                            }
                            else
                            {
                                Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} record fail]");

                                //录音失败
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, 0, $"");
                            }
                        }

                        #region 备份录音
                        string m_sBackupRecords = Call_ParamUtil.m_sBackupRecords;
                        if (!string.IsNullOrWhiteSpace(m_sBackupRecords))
                        {
                            string m_sBackUpRecordingFile = string.Format(m_sRecSub, m_sBackupRecords);
                            string m_sBackupRecordingFolder = Path.GetDirectoryName(m_sBackUpRecordingFile);
                            if (!Directory.Exists(m_sBackupRecordingFolder)) Directory.CreateDirectory(m_sBackupRecordingFolder);
                            if (m_bIsDispose) return;
                            var m_pRecordingResult = await m_sClient.SendApi(string.Format("uuid_record {0} start {1}", uuid, m_sBackUpRecordingFile)).ContinueWith((task) =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return null;
                                    if (task.IsCanceled)
                                    {
                                        Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} record cancel]");
                                        return null;
                                    }
                                    else
                                    {
                                        if (m_bIsDispose) return null;
                                        return task.Result;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} record error:{ex.Message}]");
                                    return null;
                                }
                            });
                            if (m_pRecordingResult != null && m_pRecordingResult.Success)
                            {
                                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} back up record success:{m_sBackUpRecordingFile}]");
                            }
                            else
                            {
                                Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} back up record fail]");
                            }
                        }
                        #endregion

                        //被叫挂断
                        if (m_bIsDispose) return;
                        m_sClient.OnHangup(bridgeUUID, bx =>
                        {
                            if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                            {
                                m_sWhoHangUpStr = "B";
                                Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} b-leg hangup]");

                                DateTime m_dtEndTimeNow = DateTime.Now;
                                string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                                m_mRecord.C_EndTime = m_sEndTimeNowString;
                                //修正通话时长
                                if (m_bIsLinked && Channel200)
                                {
                                    m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_mRecord.C_AnswerTime);
                                    m_mRecord.CallResultID = m_bStar ? 32 : 5;
                                }
                                else
                                {
                                    m_mRecord.C_SpeakTime = 0;
                                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                                    m_mRecord.CallResultID = m_bStar ? 53 : 54;
                                }

                                dial_area m_pDialArea = null;
                                if (!m_bStar && m_bShare && !m_bDeleteRedisLock)
                                {
                                    m_bDeleteRedisLock = true;
                                    m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, m_mRecord.C_SpeakTime);
                                }

                                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} insert record]");
                                call_record.Insert(m_mRecord, !m_bStar && m_bShare, m_pDialArea);

                                if (!m_bStar)
                                {
                                    m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, m_mRecord.C_SpeakTime);
                                    Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} update diallimit]");
                                }

                                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                                m_mChannel.channel_call_uuid = null;
                                m_mChannel.channel_call_other_uuid = null;

                                if (m_bIsDispose) return;
                                m_sClient.Hangup(uuid, HangupCause.NormalClearing).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Hangup error:{ex.Message}]");
                                    }
                                });

                                if (m_bIsDispose) return;
                                if (m_sClient != null && m_sClient.IsConnected)
                                {
                                    m_sClient.Exit().ContinueWith(task =>
                                    {
                                        try
                                        {
                                            if (m_bIsDispose) return;
                                            if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} exit cancel]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
                                        }
                                    });
                                }
                            }
                        });
                    }
                });

                //呼叫主叫
                string m_sCalleeRemove0000 = string.IsNullOrWhiteSpace(m_sCalleeRemove0000Prefix) ? m_sCalleeNumberStr : m_sCalleeRemove0000Prefix;
                if (m_bIsDispose) return;
                OriginateResult m_pOriginateResult = await m_sClient.Originate(m_sEndPointStrA, new OriginateOptions()
                {
                    UUID = uuid,
                    CallerIdNumber = m_sCalleeRemove0000,
                    //主叫显示对方电话的归属地
                    CallerIdName = string.IsNullOrWhiteSpace(_m_sPhoneAddressStr) ? m_sCalleeRemove0000 : _m_sPhoneAddressStr,
                    HangupAfterBridge = false,
                    TimeoutSeconds = m_uALegTimeoutSeconds,
                    IgnoreEarlyMedia = m_bIgnoreEarlyMedia

                }, m_sApplicationStr).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return null;
                        if (task.IsCanceled)
                        {
                            Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Originate cancel]");
                            return null;
                        }
                        else
                        {
                            if (m_bIsDispose) return null;
                            return task.Result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} Originate error:{ex.Message}]");
                        return null;
                    }
                });

                //呼叫失败
                if (m_pOriginateResult == null || (m_pOriginateResult != null && !m_pOriginateResult.Success))
                {
                    string m_sOriginateResultStr = m_pOriginateResult?.ResponseText?.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
                    if (string.IsNullOrWhiteSpace(m_sOriginateResultStr)) m_sOriginateResultStr = m_pOriginateResult?.HangupCause?.ToString();

                    string m_sMsgStr = $"Originate fail:{m_sOriginateResultStr ?? "unknow"}";

                    if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                    {
                        m_sWhoHangUpStr = "A";
                        Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} {m_sMsgStr}]");

                        //呼叫失败
                        m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err呼叫主叫");

                        DateTime m_dtEndTimeNow = DateTime.Now;
                        string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                        m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                        m_mRecord.C_EndTime = m_sEndTimeNowString;
                        m_mRecord.CallResultID = m_bStar ? 40 : 13;
                        m_mRecord.Remark = m_sMsgStr;

                        dial_area m_pDialArea = null;
                        if (!m_bStar && m_bShare && !m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, m_mRecord.C_SpeakTime);
                        }

                        Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} insert record]");
                        call_record.Insert(m_mRecord);

                        m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        m_mChannel.channel_call_uuid = null;
                        m_mChannel.channel_call_other_uuid = null;

                        if (m_bIsDispose) return;
                        if (m_sClient != null && m_sClient.IsConnected)
                        {
                            await m_sClient.Exit().ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                m_mChannel.channel_call_uuid = null;
                m_mChannel.channel_call_other_uuid = null;
                Log.Instance.Error($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} unfinished error:{ex.Message}]");
                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err未完成");

                if (m_bShare && !m_bDeleteRedisLock)
                {
                    m_bDeleteRedisLock = true;
                    Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                }
            }
        }

        #region ***发送信息方法封装
        private static void m_fSendObject(IWebSocketConnection m_pWebSocket, object m_oObject)
        {
            try
            {
                m_cIp.m_fSendString(m_pWebSocket, JsonConvert.SerializeObject(m_oObject));
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][m_cIp][m_fSendObject][Exception][{ex.Message}]");
            }
        }

        private static void m_fSendString(IWebSocketConnection m_pWebSocket, string m_sString)
        {
            try
            {
                m_pWebSocket.Send(m_sString);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][m_cIp][m_fSendString][Exception][{ex.Message}]");
            }
        }

        private static void m_fIpDialSend(IWebSocketConnection m_pWebSocket, string m_sUUID, int m_sStatus, string m_sResultMessage)
        {
            try
            {
                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                _m_mWebSocketJson.m_sUse = m_mWebSocketJsonCmd._m_sDialTask;
                _m_mWebSocketJson.m_oObject = new
                {
                    m_sUUID = m_sUUID,
                    m_sStatus = m_sStatus,
                    m_sResultMessage = m_sResultMessage
                };
                m_cIp.m_fSendObject(m_pWebSocket, _m_mWebSocketJson);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][m_cIp][m_fIpDialSend][Exception][{ex.Message}]");
            }
        }
        #endregion
    }

    public class m_cIpCmd
    {
        /// <summary>
        /// IP话机拨号
        /// </summary>
        public const string _m_sIpDial = "IpDial";
        /// <summary>
        /// IP话机拨号版本2
        /// </summary>
        public const string _m_sIpDialv2 = "IpDialv2";
        /// <summary>
        /// 获取共享号码
        /// </summary>
        public const string _m_sGetShare = "GetShare";
        /// <summary>
        /// 获取申请式线路
        /// </summary>
        public const string _m_sGetApply = "GetApply";
    }

    public class m_mIpCmd
    {
        public string m_sUse
        {
            get;
            set;
        }

        public object m_oObject
        {
            get;
            set;
        }
    }
}
