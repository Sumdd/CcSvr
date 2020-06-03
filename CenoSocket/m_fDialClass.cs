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

namespace CenoSocket
{
    public class m_fDialClass
    {
        public static async void m_fExecuteDial(IWebSocketConnection m_pSocket, Hashtable Hashtable)
        {
            try
            {
                await m_fDial(m_pSocket, Hashtable);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fExecuteDial][logic error:{ex.Message}]");
                m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err严重错误"));
            }
        }
        /// <summary>
        /// 原因一:需要统计有效时长,在原来的基础上不是很好调整
        /// 原因二:需要将一开始的先插入再修改的逻辑调成仅添加,来减小数据库压力
        /// 原因三:想将详细的拨打电话的信息提至前台
        /// 修正四:异步中加TryCatch来加强稳定性
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        public static async Task m_fDial(IWebSocketConnection m_pSocket, Hashtable m_pHashtable)
        {
            int m_uAgentID = -1;
            ChannelInfo m_mChannel = null;
            bool m_bIp = false;//是否为IP话机
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

            try
            {
                //后续对这里进行减压,不需要每次都进行查询,缓存起来即可
                string[] m_aSocketCmdArray = call_socketcommand_util.GetParamByHeadName("BDDH");
                m_uAgentID = Convert.ToInt32(SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[0]));
                string m_sRealPhoneNumberStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[1]);//真正
                string m_sPhoneNumberStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[2]);//未处理
                string m_sTypeNameStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[3]);
                bool m_bStar = m_sTypeNameStr == Special.Star;
                string m_sPhoneAddressStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[4]);
                string m_sCityCodeStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[5]);
                string m_sDealWithStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[6]);
                string m_sNumberType = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[7]);
                string m_sTypeUUID = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[8]);

                #region ***拨号Socket命令自定义数据
                ///自定义分割式数据|&
                string m_sUsrData = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[9]);
                ///是否兼容加密号码
                bool m_bQNRegexNumber = false;
                ///号码归属地时间
                string m_sDt = string.Empty;
                ///卡运营商类型
                string m_sCardType = string.Empty;
                ///邮编
                string m_sZipCode = string.Empty;
                ///分割数据
                string[] m_lUsrData = m_sUsrData.Split('|');
                if (m_lUsrData.Length > 0) m_bQNRegexNumber = m_lUsrData[0] == "1";
                if (m_lUsrData.Length > 1)
                {
                    string[] m_lPm = m_lUsrData[1].Split('&');
                    if (m_lPm.Length > 0) m_sDt = m_lPm[0];
                    if (m_lPm.Length > 1) m_sCardType = m_lPm[1];
                    if (m_lPm.Length > 2) m_sZipCode = m_lPm[2];
                }
                #endregion

                m_bShare = m_sNumberType == Special.Share;

                AGENT_INFO m_mAgent = call_factory.agent_list.Find(x => x.AgentID == m_uAgentID);
                if (m_mAgent == null)
                {
                    Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} miss a leg info]");
                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err账户有误"));
                    return;
                }

                //继续处理,防止号码有误,兼容加密号码
                string m_sDealWithRealPhoneNumberStr = string.Empty;
                string m_sDealWithPhoneNumberStr = string.Empty;
                if (m_bQNRegexNumber)
                {
                    m_sDealWithRealPhoneNumberStr = m_sPhoneNumberStr;
                    m_sDealWithPhoneNumberStr = m_sPhoneNumberStr;
                }
                else
                {
                    Regex m_rReplaceRegex = new Regex("[^(0-9*#)]+");
                    Regex m_rIsMatchRegex = new Regex("^[0-9*#]{3,20}$");
                    m_sDealWithRealPhoneNumberStr = m_rReplaceRegex.Replace(m_sRealPhoneNumberStr, string.Empty);
                    m_sDealWithPhoneNumberStr = m_rReplaceRegex.Replace(m_sPhoneNumberStr, string.Empty);
                    if (!m_rIsMatchRegex.IsMatch(m_sDealWithRealPhoneNumberStr) || !m_rIsMatchRegex.IsMatch(m_sDealWithPhoneNumberStr))
                    {
                        Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} invalid phone]");
                        m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err号码有误"));
                        return;
                    }
                }

                int m_uCh = m_mAgent.ChInfo.nCh;
                m_mChannel = call_factory.channel_list[m_uCh];
                if (m_uCh == -1 || m_mChannel == null || m_mChannel?.channel_type != Special.SIP)
                {
                    Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} miss channel or not sip channel]");
                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err通道有误"));
                    return;
                }

                if (m_bShare) m_sUAID = m_mChannel.channel_number;
                m_bIp = (m_mChannel.IsRegister == 0);
                if (
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_IDLE &&
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_AHANGUP &&
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_BHANGUP
                    )
                {
                    Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} channel busy]");
                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err通道繁忙"));
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

                #region 采取哪种dtmf的发送方式
                string m_sDTMFSendMethod = Call_ParamUtil.m_sDTMFSendMethod;
                bool m_bSubMessage = false;
                if (m_sDTMFSendMethod == Call_ParamUtil.inbound || m_sDTMFSendMethod == Call_ParamUtil.bothSignal)
                {
                    m_bSubMessage = true;
                }
                #endregion

                if (!m_bStar)
                {
                    #region ***拨号限制,增加号码池逻辑
                    switch (m_sNumberType)
                    {
                        case Special.Common:
                            {
                                _m_mDialLimit = m_fDialLimit.m_fGetDialLimitObject(m_sDealWithRealPhoneNumberStr, m_uAgentID, m_sTypeUUID);
                                break;
                            }
                        case Special.Share:
                            {
                                //跳转至号码池逻辑,需要持久化至数据库,录音记录都进行保存
                                string m_sErrMsg = string.Empty;
                                m_pShareNumber = Redis2.m_fGetTheShareNumber(uuid, m_uAgentID, m_sTypeUUID, m_sDealWithRealPhoneNumberStr, DB.Basic.Call_ParamUtil.m_uShareNumSetting, out m_sErrMsg);
                                _m_mDialLimit = m_fDialLimit.m_fGetDialLimitByShare(m_pShareNumber);
                                if (_m_mDialLimit == null)
                                {
                                    Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} get share error]");
                                    if (string.IsNullOrWhiteSpace(m_sErrMsg)) m_sErrMsg = "Err获取号码";
                                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail(m_sErrMsg));

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
                                Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} unknown number type]");
                                m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err号码类别"));

                                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                                m_mChannel.channel_call_uuid = null;
                                m_mChannel.channel_call_other_uuid = null;

                                return;
                            }
                    }

                    if (_m_mDialLimit?.m_sDtmf == Call_ParamUtil.inbound || _m_mDialLimit?.m_sDtmf == Call_ParamUtil.bothSignal)
                    {
                        m_sDTMFSendMethod = _m_mDialLimit.m_sDtmf;
                    }

                    if (_m_mDialLimit != null && !string.IsNullOrWhiteSpace(_m_mDialLimit.m_sNumberStr))
                    {
                        #region 网关有误
                        if (string.IsNullOrWhiteSpace(_m_mDialLimit.m_sGatewayNameStr))
                        {
                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_other_uuid = null;
                            Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} gateway fail]");
                            m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err网关有误"));

                            if (m_bShare && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                            }

                            return;
                        }
                        #endregion

                        m_mRecord.LocalNum = _m_mDialLimit.m_sNumberStr;
                        Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} dialcount:{_m_mDialLimit.m_uDialCount}]");

                        ///<![CDATA[
                        /// 空也做强制加拨处理
                        /// ]]>

                        if (_m_mDialLimit.m_sAreaCodeStr == "0000" || string.IsNullOrWhiteSpace(_m_mDialLimit.m_sAreaCodeStr))
                        {
                            if (!string.IsNullOrWhiteSpace(_m_mDialLimit.m_sDialPrefixStr))
                            {
                                Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} add prefix:{_m_mDialLimit.m_sDialPrefixStr}]");
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
                                        if (!string.IsNullOrWhiteSpace(_m_mDialLimit.m_sDialPrefixStr))
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
                                            }
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
                        Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} no phone number]");
                        m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err拨号限制"));

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
                            //加一个拨号计划
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

                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} a-leg-endpoint:{m_sEndPointStrA},number:{m_mRecord.LocalNum}]");
                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} b-leg-endpoint:{m_sEndPointStrB},number:{m_mRecord.T_PhoneNum}]");

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
                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} socket client error:{ex.Message}]");
                        return null;
                    }
                });

                if (m_sClient == null)
                {
                    m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                    m_mChannel.channel_call_uuid = null;
                    m_mChannel.channel_call_other_uuid = null;
                    Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} miss InboundSocket]");
                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("ErrESL"));

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
                IDisposable m_eEventMessage = null;
                m_sClient.Disposed += (a, b) =>
                {
                    try
                    {
                        m_bIsDispose = true;
                        Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} socket client dispose]");
                        if (m_eEventChannelPark != null)
                        {
                            m_eEventChannelPark.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelPark dispose]");
                        }
                        if (m_eEventMessage != null)
                        {
                            m_eEventChannelPark.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event Message dispose]");
                        }

                        if (m_eChannel200 != null)
                        {
                            m_eChannel200.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelAnswer dispose]");
                        }

                        if (m_bShare && !m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} socket client dispose do error:{ex.Message}]");
                    }
                };

                if (m_bIsDispose) return;
                await m_sClient.SubscribeEvents(EventName.ChannelPark).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelPark cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelPark error:{ex.Message}]");
                    }
                });

                #region 订阅消息
                if (m_bSubMessage)
                {
                    if (m_bIsDispose) return;
                    await m_sClient.SubscribeEvents(EventName.Message).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event Message cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event Message error:{ex.Message}]");
                        }
                    });
                }
                #endregion

                DateTime m_dtStartTimeNow = DateTime.Now;
                string m_sStartTimeNowString = Cmn.m_fDateTimeString(m_dtStartTimeNow);
                m_mRecord.C_Date = m_sStartTimeNowString;
                m_mRecord.C_StartTime = m_sStartTimeNowString;
                string bridgeUUID = Guid.NewGuid().ToString();

                #region ***修改根据200计算通话时长
                bool Channel200 = false;

                if (m_bIsDispose) return;
                await m_sClient.SubscribeEvents(EventName.ChannelAnswer).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event 200 cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event 200 error:{ex.Message}]");
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
                    Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event {x.EventName} success]");

                    if (m_bIp)
                    {
                        /*发送消息*/
                    }

                    //主叫挂断
                    if (m_bIsDispose) return;
                    m_sClient.OnHangup(uuid, ax =>
                    {
                        if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                        {
                            m_sWhoHangUpStr = "A";
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} a-leg hangup]");

                            DateTime m_dtEndTimeNow = DateTime.Now;
                            string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                            m_mRecord.C_EndTime = m_sEndTimeNowString;
                            //追加200消息
                            if (m_bIsLinked && Channel200)
                            {
                                m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_mRecord.C_AnswerTime);
                                m_mRecord.CallResultID = m_bStar ? 31 : 1;

                                if (m_bIp)
                                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_hang("挂机"));
                            }
                            else
                            {
                                m_mRecord.C_SpeakTime = 0;
                                m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                                m_mRecord.CallResultID = m_bStar ? 37 : 10;

                                if (m_bIp)
                                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_hang("呼叫取消"));
                            }

                            dial_area m_pDialArea = null;
                            if (!m_bStar && m_bShare && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, m_mRecord.C_SpeakTime);
                            }

                            Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} insert record]");
                            call_record.Insert(m_mRecord, !m_bStar && m_bShare, m_pDialArea);

                            if (!m_bStar)
                            {
                                m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, m_mRecord.C_SpeakTime, m_pDialArea);
                                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
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
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
                                    }
                                });
                            }
                        }
                    });

                    m_mChannel.channel_call_other_uuid = bridgeUUID;
                    m_mRecord.C_RingTime = Cmn.m_fDateTimeString();

                    //桥接被叫
                    if (m_bIsDispose) return;
                    var m_pBridgeResult = await m_sClient.Bridge(uuid, m_sEndPointStrB, new BridgeOptions()
                    {
                        UUID = bridgeUUID,
                        CallerIdNumber = m_mRecord.LocalNum,
                        CallerIdName = m_mRecord.LocalNum,
                        HangupAfterBridge = false,
                        TimeoutSeconds = m_uTimeoutSeconds,
                        IgnoreEarlyMedia = m_bIgnoreEarlyMedia

                    }).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return null;
                            if (task.IsCanceled)
                            {
                                Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} Bridge cancel]");
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
                            Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} Bridge error:{ex.Message}]");
                            return null;
                        }
                    });

                    //桥接失败
                    if (m_pBridgeResult == null || (m_pBridgeResult != null && !m_pBridgeResult.Success))
                    {
                        string m_sBridgeResultStr = m_pBridgeResult?.ResponseText;
                        if (string.IsNullOrWhiteSpace(m_sBridgeResultStr)) m_sBridgeResultStr = null;

                        string m_sMsgStr = $"Bridge fail:{m_sBridgeResultStr ?? "unknow"}";
                        Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} {m_sMsgStr}]");

                        if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                        {
                            m_sWhoHangUpStr = "A";

                            string m_sSendMsgStr = "Err呼叫失败";

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
                            else if (m_sBridgeResultStr.Contains("592"))
                            {
                                m_mRecord.CallResultID = m_bStar ? 34 : 7;
                                m_sSendMsgStr = "Err黑名单";
                            }
                            else
                            {
                                m_mRecord.CallResultID = m_bStar ? 34 : 7;
                            }
                            #endregion

                            m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail(m_sSendMsgStr));

                            m_mRecord.Remark = m_sMsgStr;

                            dial_area m_pDialArea = null;
                            if (!m_bStar && m_bShare && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, 0);
                            }

                            Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} insert record]");
                            call_record.Insert(m_mRecord, !m_bStar && m_bShare, m_pDialArea);

                            if (!m_bStar)
                            {
                                m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, 0, m_pDialArea);
                                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
                            }

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
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
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

                        if (m_bShare) m_pShareNumber.state = SHARE_NUM_STATUS.TALKING;

                        DateTime m_dtAnswerTimeNow = DateTime.Now;
                        //如果比200快,这里应该不可能,而且如果接通,一定会有200消息
                        if (false && !Channel200)
                        {
                            string m_sAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtAnswerTimeNow);
                            m_mRecord.C_AnswerTime = m_sAnswerTimeNowString;
                            m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtAnswerTimeNow, m_dtStartTimeNow);
                        }

                        Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} Bridge success]");

                        ///修正,桥接之后未必要摘机
                        m_fSend(m_pSocket, M_WebSocketSend._bhzt_pick());

                        m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtAnswerTimeNow, m_dtStartTimeNow);

                        #region 录音参数设置
                        if (m_bIsDispose) return;
                        await m_sClient.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} RECORD_ARTIST cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} RECORD_ARTIST error:{ex.Message}]");
                            }
                        });
                        if (m_bIsDispose) return;
                        await m_sClient.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0).ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} RECORD_MIN_SEC cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} RECORD_MIN_SEC error:{ex.Message}]");
                            }
                        });
                        if (m_bIsDispose) return;
                        await m_sClient.SetChannelVariable(uuid, "RECORD_STEREO", "true").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} RECORD_STEREO cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} RECORD_STEREO error:{ex.Message}]");
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
                                        Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} record cancel]");
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
                                    Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} record error:{ex.Message}]");
                                    return null;
                                }
                            });
                            if (m_pRecordingResult != null && m_pRecordingResult.Success)
                            {
                                m_mRecord.RecordFile = m_sRecordingFile;
                                m_mRecord.recordName = m_sRecordingID;
                                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} record ID:{m_sRecordingID} send]");
                                m_fSend(m_pSocket, M_WebSocketSend._fsly(m_sRecordingID, m_sRecordingFile));
                            }
                            else
                            {
                                Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} record fail]");
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
                                        Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} record cancel]");
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
                                    Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} record error:{ex.Message}]");
                                    return null;
                                }
                            });
                            if (m_pRecordingResult != null && m_pRecordingResult.Success)
                            {
                                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} back up record success:{m_sBackUpRecordingFile}]");
                            }
                            else
                            {
                                Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} back up record fail]");
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
                                Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} b-leg hangup]");

                                string m_sHangupCase = "对方挂机";
                                if (bx != null && !string.IsNullOrWhiteSpace(bx.BodyText) && bx.BodyText.Contains("592")) m_sHangupCase = "Err黑名单";
                                m_fSend(m_pSocket, M_WebSocketSend._bhzt_hang(m_sHangupCase));

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

                                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} insert record]");
                                call_record.Insert(m_mRecord, !m_bStar && m_bShare, m_pDialArea);

                                if (!m_bStar)
                                {
                                    m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, m_mRecord.C_SpeakTime, m_pDialArea);
                                    Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
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
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} Hangup error:{ex.Message}]");
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
                                            if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit cancel]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
                                        }
                                    });
                                }
                            }
                        });
                    }
                });

                #region 根据消息内容以及商定的方式发送dtmf
                if (m_bSubMessage)
                {
                    if (m_bIsDispose) return;
                    m_eEventMessage = m_sClient.ChannelEvents.Where(x => x.UUID == uuid && x.EventName == EventName.Message).Subscribe(async x =>
                    {
                        try
                        {
                            if (x.BodyText != null && x.BodyText.Length > 6 && x.BodyText.Contains("{dtmf}"))
                            {
                                var dtmf = x.BodyText.Substring(6);
                                if (m_bIsLinked && !string.IsNullOrWhiteSpace(bridgeUUID))
                                {
                                    Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} {m_sDTMFSendMethod} send dtmf:{dtmf}]");
                                    string tone_stream = string.Empty;
                                    switch (m_sDTMFSendMethod)
                                    {
                                        case Call_ParamUtil.inbound:
                                            {
                                                switch (dtmf)
                                                {
                                                    case "1":
                                                        tone_stream = "tone_stream://%(50,0,697,1209)";
                                                        break;
                                                    case "2":
                                                        tone_stream = "tone_stream://%(50,0,697,1336)";
                                                        break;
                                                    case "3":
                                                        tone_stream = "tone_stream://%(50,0,697,1477)";
                                                        break;
                                                    case "4":
                                                        tone_stream = "tone_stream://%(50,0,770,1209)";
                                                        break;
                                                    case "5":
                                                        tone_stream = "tone_stream://%(50,0,770,1336)";
                                                        break;
                                                    case "6":
                                                        tone_stream = "tone_stream://%(50,0,770,1477)";
                                                        break;
                                                    case "7":
                                                        tone_stream = "tone_stream://%(50,0,852,1209)";
                                                        break;
                                                    case "8":
                                                        tone_stream = "tone_stream://%(50,0,852,1336)";
                                                        break;
                                                    case "9":
                                                        tone_stream = "tone_stream://%(50,0,852,1477)";
                                                        break;
                                                    case "*":
                                                    case "10":
                                                        tone_stream = "tone_stream://%(50,0,941,1209)";
                                                        break;
                                                    case "0":
                                                        tone_stream = "tone_stream://%(50,0,941,1336)";
                                                        break;
                                                    case "#":
                                                    case "11":
                                                        tone_stream = "tone_stream://%(50,0,941,1477)";
                                                        break;
                                                }
                                                await m_sClient.ExecuteApplication(bridgeUUID, "playback", tone_stream).ContinueWith(task =>
                                                {
                                                    try
                                                    {
                                                        if (m_bIsDispose) return;
                                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} playback cancel]");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} playback error:{ex.Message}]");
                                                    }
                                                });

                                                //为主叫播放,相当于提示音
                                                await m_sClient.ExecuteApplication(uuid, "playback", tone_stream).ContinueWith(task =>
                                                {
                                                    try
                                                    {
                                                        if (m_bIsDispose) return;
                                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} playback cancel]");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} playback error:{ex.Message}]");
                                                    }
                                                });
                                            }
                                            break;
                                        case Call_ParamUtil.bothSignal:
                                            {
                                                await m_sClient.SendApi($"uuid_send_dtmf {bridgeUUID} {dtmf}").ContinueWith(task =>
                                                {
                                                    try
                                                    {
                                                        if (m_bIsDispose) return;
                                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} uuid_send_dtmf cancel]");
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} uuid_send_dtmf error:{ex.Message}]");
                                                    }
                                                });
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} Message error:{ex.Message}]");
                        }
                    });
                }
                #endregion

                //呼叫主叫
                if (m_bIsDispose) return;
                OriginateResult m_pOriginateResult = await m_sClient.Originate(m_sEndPointStrA, new OriginateOptions()
                {
                    UUID = uuid,
                    CallerIdNumber = m_sCalleeNumberStr,
                    CallerIdName = m_sCalleeNumberStr,
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
                            Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} Originate cancel]");
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
                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} Originate error:{ex.Message}]");
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
                        Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} {m_sMsgStr}]");

                        m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err呼叫主叫"));

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

                        Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} insert record]");
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
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
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
                Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} unfinished error:{ex.Message}]");
                m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err未完成"));

                if (m_bShare && !m_bDeleteRedisLock)
                {
                    m_bDeleteRedisLock = true;
                    Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                }
            }
        }

        private static void m_fSend(IWebSocketConnection m_pSocket, string m_sMsgStr)
        {
            try
            {
                m_pSocket.Send(m_sMsgStr);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fSend][Exception][{ex.Message}]");
            }
        }
    }
}
