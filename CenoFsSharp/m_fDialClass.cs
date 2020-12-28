using CenoSipFactory;
using Cmn_v1;
using Core_v1;
using DB.Basic;
using DB.Model;
using Fleck;
using Model_v1;
using NEventSocket;
using NEventSocket.FreeSwitch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CenoFsSharp
{
    public class m_fDialClass
    {
        public static async void m_fDial(OutboundSocket m_pOutboundSocket)
        {
            string uuid = m_pOutboundSocket.ChannelData.UUID;
            string m_sRealCallerNumberStr = m_pOutboundSocket.ChannelData.GetHeader("Channel-Caller-ID-Number").Replace("+86", "0");//主叫
            //string m_sRealCalleeNumberStr = m_pOutboundSocket.ChannelData.GetHeader("Channel-Destination-Number");//被叫
            string m_sRealCalleeNumberStr = m_pOutboundSocket.ChannelData.GetHeader("variable_sip_to_user").Replace("+86", "0");//被叫
            AGENT_INFO m_mAgent = null;
            ChannelInfo m_mChannel = null;
            bool m_bIsDispose = false;
            //此处183直接接通是为了兼容早期媒体无法透传的问题,此处依然保留即可
            bool m_bUseChannelProgressMedia = true;
            int m_uAgentID = -1;
            //真实号码
            string m_stNumberStr = string.Empty;
            //是否使用了共享号码
            bool m_bShare = false;
            //Caller-Caller-ID-Number
            string m_sUAID = string.Empty;
            //FreeSWITCH-IPv4
            string m_sFreeSWITCHIPv4 = InboundMain.FreesSWITCHIPv4;
            //是否需要获取录音ID
            bool m_bIsQueryRecUUID = Call_ParamUtil.m_bIsQueryRecUUID;
            ///使用桥接App
            string m_sBridgeApp = Call_ParamUtil.m_sBridgeApp;
            bool m_bBridgeApp = false;
            if (!string.IsNullOrWhiteSpace(m_sBridgeApp) && m_sBridgeApp != "N") m_bBridgeApp = true;
            ///是否有自己的180放音
            bool m_b180 = false;
            bool m_b183 = false;

            try
            {
                IDisposable m_eChannel180 = null;
                IDisposable m_eChannel183or200 = null;

                m_pOutboundSocket.Disposed += (a, b) =>
                {
                    m_bIsDispose = true;

                    if (m_mChannel != null)
                    {
                        m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        m_mChannel.channel_call_uuid = null;
                        m_mChannel.channel_call_other_uuid = null;
                    }

                    if (m_eChannel180 != null)
                    {
                        m_eChannel180.Dispose();
                        Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} event 180 dispose]");
                    }

                    if (m_eChannel183or200 != null)
                    {
                        m_eChannel183or200.Dispose();
                        Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} event 183,200 dispose]");
                    }

                    //如果有录音ID,这里如果出现什么情况,都要删除掉
                    if (false && m_bIsQueryRecUUID && m_mAgent != null)
                    {
                        DB.Basic.m_fDialLimit.m_fDelDialUUID(m_mAgent?.LoginName);
                    }

                    Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} outbound socket dispose]");
                };

                m_mAgent = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_number == m_sRealCallerNumberStr);

                #region 无用户信息
                if (m_mAgent == null)
                {
                    string m_sTransfer = m_pOutboundSocket.ChannelData.GetHeader("Channel-Destination-Number");//被叫
                    if (m_sTransfer.StartsWith("*"))
                    {
                        Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} dial -> transfer call]");
                        m_fCallClass.m_fCall(m_pOutboundSocket, m_sTransfer);
                        return;
                    }

                    ///<![CDATA[
                    /// <1>加入内呼逻辑,去掉一个拨号计划
                    /// ]]>
                    if (m_sRealCalleeNumberStr.StartsWith("*"))
                    {
                        Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} dial -> * call]");
                        m_fCallClass.m_fCall(m_pOutboundSocket);
                        return;
                    }

                    ///<![CDATA[
                    /// <2>查找号码表,是否由此号码,如果有,路由到呼入
                    /// 为了稳妥,这里使用仅号码表
                    /// 兼容了某次调试注册文件在5060端口的情况,其余暂无走此分支的情况
                    /// 优化为一次查询即可
                    /// ]]>

                    ///为呼入内转做准备
                    int _m_uLimitId = -1;
                    int _m_uAgentID = m_fDialLimit.m_fGetAgentID(m_sRealCalleeNumberStr, out m_stNumberStr, true, string.Empty, out _m_uLimitId);
                    if (_m_uAgentID > -1)
                    {
                        Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} dial -> call]");
                        m_fCallClass.m_fCall(m_pOutboundSocket, null, 0, null, _m_uAgentID, _m_uLimitId);
                        return;
                    }

                    ///<![CDATA[
                    /// <3>如果呼入的是共享号码,需查询Redis数据库
                    /// ]]>
                    share_number m_pShareNumber = null;
                    switch (Redis2.m_fGetTheCall(m_sRealCalleeNumberStr, uuid, out m_pShareNumber))
                    {
                        case 1:
                            {
                                Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} dial -> share call]");
                                m_fCallClass.m_fCall(m_pOutboundSocket, null, 1, m_pShareNumber);
                                return;
                            }
                        case 2:
                            {
                                Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} dial -> share call busy]");
                                m_fCallClass.m_fCall(m_pOutboundSocket, null, 2, m_pShareNumber);
                                return;
                            }
                        default:
                            break;
                    }

                    Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} can,t find user]");
                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.UserNotRegistered).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup error:{ex.Message}]");
                        }
                    });

                    if (m_bIsDispose) return;
                    if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                    {
                        await m_pOutboundSocket.Exit().ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                m_uAgentID = m_mAgent.AgentID;
                Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} -> {m_uAgentID}]");

                #region ***增加单台验证和时间验证
                int m_uUseStatus = m_cModel.m_uUseStatus;
                if (m_uUseStatus > 0)
                {
                    Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID},error code:{m_uUseStatus}]");

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.ServiceUnavailable).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup error:{ex.Message}]");
                        }
                    });

                    if (m_bIsDispose) return;
                    if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                    {
                        await m_pOutboundSocket.Exit().ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                m_mChannel = m_mAgent.ChInfo;
                #region 无通道信息
                if (m_mChannel == null || m_mChannel?.channel_type != Special.SIP)
                {
                    string m_sMsgStr = $"can,t find channel or not sip channel";
                    Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} {m_sMsgStr}]");

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.WrongMessage).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup error:{ex.Message}]");
                        }
                    });

                    if (m_bIsDispose) return;
                    if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                    {
                        await m_pOutboundSocket.Exit().ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                if (m_bShare) m_sUAID = m_mChannel.channel_number;

                Regex m_rReplaceRegex = new Regex("[^(0-9*#)]+");
                Regex m_rIsMatchRegex = new Regex("^[0-9*#]{3,20}$");
                m_sRealCalleeNumberStr = m_rReplaceRegex.Replace(m_sRealCalleeNumberStr, string.Empty);
                #region 被叫号码有误
                if (!m_rIsMatchRegex.IsMatch(m_sRealCalleeNumberStr))
                {
                    Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} invalid phone]");
                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.InvalidNumberFormat).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup error:{ex.Message}]");
                        }
                    });

                    if (m_bIsDispose) return;
                    if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                    {
                        await m_pOutboundSocket.Exit().ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                #region ***是否需要查询联系人姓名
                if (Call_ParamUtil.m_bUseHomeSearch) m_cEsySQL.m_fSetExpc(m_sRealCallerNumberStr);
                #endregion

                #region ***呼出只判断是否是黑名单,黑名单直接限制呼叫即可,如果更新中则暂时失效即可
                if (!m_cWblist.m_bInitWblist && m_cWblist.m_lWblist?.Count > 0)
                {
                    ///判断所有的黑名单即可
                    foreach (m_mWblist item in m_cWblist.m_lWblist)
                    {
                        ///兼容呼入呼出黑名单
                        if (item.wbtype == 2 && (item.wblimittype & 2) > 0)
                        {
                            if (item.regex.IsMatch(m_sRealCalleeNumberStr))
                            {
                                Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} black list:{m_sRealCalleeNumberStr}]");

                                if (m_bIsDispose) return;
                                await m_pOutboundSocket.Hangup(uuid, HangupCause.SystemShutdown).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup error:{ex.Message}]");
                                    }
                                });

                                if (m_bIsDispose) return;
                                if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                                {
                                    await m_pOutboundSocket.Exit().ContinueWith(task =>
                                    {
                                        try
                                        {
                                            if (m_bIsDispose) return;
                                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit cancel]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit error:{ex.Message}]");
                                        }
                                    });
                                }

                                if (m_bIsDispose) return;
                                m_pOutboundSocket?.Dispose();

                                return;
                            }
                        }
                    }
                }
                #endregion

                string m_sEndPointStrB = string.Empty;

                bool m_bInboundTest = Call_ParamUtil.InboundTest;
                List<string> m_lStrings = m_cPhone.m_fGetPhoneNumberMemo(m_sRealCalleeNumberStr);//解析被叫
                bool m_bStar = m_lStrings[2] == Special.Star;

                m_mDialLimit _m_mDialLimit = null;
                string m_sCalleeRemove0000Prefix = string.Empty;
                call_record_model m_mRecord = new call_record_model(m_mAgent.ChInfo.channel_id);
                m_mRecord.AgentID = m_uAgentID;
                m_mRecord.UAID = m_sUAID;
                m_mRecord.fromagentname = m_mAgent.AgentName;
                m_mRecord.fromloginname = m_mAgent.LoginName;

                if (m_bStar)
                {
                    m_mRecord.CallType = 6;
                    m_mRecord.LocalNum = m_mAgent.ChInfo.channel_number;
                    m_mRecord.PhoneAddress = "内呼";
                    m_mRecord.T_PhoneNum = $"{m_lStrings[1]}";
                    m_mRecord.C_PhoneNum = m_mRecord.T_PhoneNum;

                    ///默认终点表达式
                    m_sEndPointStrB = $"user/{m_lStrings[0]}";

                    #region ***兼容内呼规则逻辑
                    if (true)
                    {
                        ///分割即可
                        string m_sContinue = "IN未进行";
                        string[] m_lBf = m_lStrings[0].Split('*');
                        if (m_lBf.Length == 2)
                        {
                            if (m_lBf[0].Length > 0 && m_lBf[1].Length > 0)
                            {
                                ///对比内呼规则
                                if (!m_cInrule.m_bInitInrule && m_cInrule.m_lInrule != null && m_cInrule.m_lInrule.Count > 0)
                                {
                                    m_mInrule _m_mInrule = m_cInrule.m_lInrule.Where(x => x.inrulesuffix == m_lBf[0] && !x.type).FirstOrDefault();
                                    if (_m_mInrule != null)
                                    {
                                        if (m_cInrule.m_pInrule != null)
                                        {
                                            ///根据内呼规则拼接终点表达式
                                            m_sEndPointStrB = $"sofia/{_m_mInrule.inruleua}/sip:{m_lStrings[0]}@{_m_mInrule.inruleip}:{_m_mInrule.inruleport}";
                                            m_mRecord.LocalNum = $"{m_cInrule.m_pInrule.inrulesuffix}*{m_mRecord.LocalNum}";
                                            m_sContinue = null;
                                        }
                                        else
                                        {
                                            m_sContinue = "IN本机规则";
                                        }
                                    }
                                    else
                                    {
                                        ///无内呼规则
                                        m_sContinue = "IN内呼规则";
                                    }
                                }
                                else
                                {
                                    ///无内呼规则
                                    m_sContinue = "IN内呼规则";
                                }
                            }
                            else if (m_lBf[0].Length == 0 && m_lBf[1].Length > 0)
                            {
                                ///查找便捷电话薄
                                if (!m_cInrule.m_bInitInrule && m_cInrule.m_lInrule != null && m_cInrule.m_lInrule.Count > 0)
                                {
                                    m_mInrule _m_mInrule = m_cInrule.m_lInrule.Where(x => x.inrulebookfkey == m_lBf[1] && x.type).FirstOrDefault();
                                    if (_m_mInrule != null)
                                    {
                                        if (m_cInrule.m_pInrule != null)
                                        {
                                            ///转换成短号形式
                                            m_mRecord.T_PhoneNum = $"{_m_mInrule.inrulesuffix}*{_m_mInrule.inrulebooktkey}";
                                            m_mRecord.C_PhoneNum = m_mRecord.T_PhoneNum;
                                            ///根据内呼规则拼接终点表达式
                                            m_sEndPointStrB = $"sofia/{_m_mInrule.inruleua}/sip:{m_mRecord.T_PhoneNum}@{_m_mInrule.inruleip}:{_m_mInrule.inruleport}";
                                            m_mRecord.LocalNum = $"{m_cInrule.m_pInrule.inrulesuffix}*{m_mRecord.LocalNum}";
                                            m_sContinue = null;
                                        }
                                        else
                                        {
                                            m_sContinue = "IN内呼规则";
                                        }
                                    }
                                    else
                                    {
                                        ///无内呼规则
                                        m_sContinue = "IN内呼规则";
                                    }
                                }
                                else
                                {
                                    ///无内呼规则
                                    m_sContinue = "IN内呼规则";
                                }
                            }
                            else
                            {
                                ///拆分后的数据有误,无法继续处理
                                m_sContinue = "IN无效拆分";
                            }
                        }
                        else
                        {
                            ///拆分时有误,无法继续处理
                            m_sContinue = "IN拆分错误";
                        }

                        ///兼容可以直接*4位分机号本机内呼
                        if (m_mRecord.T_PhoneNum.Length != 5 && m_sContinue != null)
                        {
                            Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} inrule callee:{m_mRecord.T_PhoneNum},way:{m_sContinue}]");

                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Hangup(uuid, HangupCause.NoRouteDestination).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup error:{ex.Message}]");
                                }
                            });

                            if (m_bIsDispose) return;
                            if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                            {
                                await m_pOutboundSocket.Exit().ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit error:{ex.Message}]");
                                    }
                                });
                            }

                            if (m_bIsDispose) return;
                            m_pOutboundSocket?.Dispose();

                            return;
                        }
                    }
                    #endregion
                }
                else
                {
                    m_mRecord.CallType = 1;
                    if (m_bInboundTest)
                    {
                        m_mRecord.LocalNum = m_mAgent.ChInfo.channel_number;
                        m_mRecord.PhoneAddress = m_lStrings[3];
                        m_mRecord.T_PhoneNum = $"{m_lStrings[0]}";
                        m_mRecord.C_PhoneNum = $"{m_lStrings[1]}";
                        m_sEndPointStrB = $"user/{m_lStrings[0]}";
                    }
                    else
                    {
                        _m_mDialLimit = m_fDialLimit.m_fGetDialLimitObject(m_sRealCalleeNumberStr, m_uAgentID);
                        if (_m_mDialLimit != null && !string.IsNullOrWhiteSpace(_m_mDialLimit.m_sNumberStr))
                        {
                            #region 网关有误
                            if (string.IsNullOrWhiteSpace(_m_mDialLimit.m_sGatewayNameStr))
                            {
                                Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} gateway fail]");
                                if (m_bIsDispose) return;
                                await m_pOutboundSocket.Hangup(uuid, HangupCause.WrongMessage).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup error:{ex.Message}]");
                                    }
                                });

                                if (m_bIsDispose) return;
                                if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                                {
                                    await m_pOutboundSocket.Exit().ContinueWith(task =>
                                    {
                                        try
                                        {
                                            if (m_bIsDispose) return;
                                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit cancel]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit error:{ex.Message}]");
                                        }
                                    });
                                }

                                if (m_bIsDispose) return;
                                m_pOutboundSocket?.Dispose();

                                return;
                            }
                            #endregion

                            m_mRecord.LocalNum = _m_mDialLimit.m_sNumberStr;
                            m_mRecord.PhoneAddress = m_lStrings[3];
                            m_mRecord.T_PhoneNum = $"{m_lStrings[0]}";
                            m_mRecord.C_PhoneNum = $"{m_lStrings[1]}";
                            Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} dialcount:{_m_mDialLimit.m_uDialCount}]");

                            ///<![CDATA[
                            /// 空也做强制加拨处理
                            /// ]]>

                            ///强制加拨前缀只使用外地加拨即可
                            if (_m_mDialLimit.m_sAreaCodeStr == "0000" || string.IsNullOrWhiteSpace(_m_mDialLimit.m_sAreaCodeStr))
                            {
                                if (!string.IsNullOrWhiteSpace(_m_mDialLimit.m_sDialPrefixStr))
                                {
                                    //强制加前缀
                                    Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} add prefix:{_m_mDialLimit.m_sDialPrefixStr}]");
                                }
                                m_mRecord.T_PhoneNum = $"{_m_mDialLimit.m_sDialPrefixStr}{m_lStrings[0]}";
                                m_sCalleeRemove0000Prefix = m_lStrings[0];
                            }
                            else
                            {
                                switch (m_lStrings[5])
                                {
                                    case Special.Mobile:
                                        if (!m_sRealCalleeNumberStr.Contains('*') && !m_sRealCalleeNumberStr.Contains('#'))
                                        {
                                            if (_m_mDialLimit.m_bZflag)
                                            {

                                                ///<![CDATA[
                                                /// 当被叫号码未找到归属地时,不加拨前缀
                                                /// ]]>

                                                if (!string.IsNullOrWhiteSpace(m_lStrings[4]) && _m_mDialLimit.m_sAreaCodeStr != m_lStrings[4])
                                                {
                                                    m_mRecord.T_PhoneNum = $"{_m_mDialLimit.m_sDialPrefixStr}{m_lStrings[0]}";
                                                }
                                                else
                                                {
                                                    if (string.IsNullOrWhiteSpace(m_lStrings[4]))
                                                    {
                                                        ///如果查询得到的区号为空,使用原号码,方便主动加拨前缀等问题
                                                        Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} no area code,use:{m_lStrings[1]}]");
                                                        m_mRecord.T_PhoneNum = $"{m_lStrings[1]}";
                                                    }
                                                    else
                                                    {
                                                        m_mRecord.T_PhoneNum = $"{_m_mDialLimit.m_sDialLocalPrefixStr}{m_lStrings[0]}";
                                                    }
                                                }
                                                //原号码
                                                m_sCalleeRemove0000Prefix = $"{m_lStrings[0]}";
                                            }
                                        }
                                        break;
                                    case Special.Telephone:
                                        ///仅处理400,800电话
                                        string m_sTs0 = m_lStrings[1].TrimStart('0');
                                        if (m_sTs0.StartsWith("400") || m_sTs0.StartsWith("800"))
                                        {
                                            Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} 400 phone,{m_lStrings[1]}]");
                                            m_mRecord.T_PhoneNum = $"{m_lStrings[1]}";
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            if (_m_mDialLimit.m_bGatewayType) m_sEndPointStrB = $"sofia/gateway/{_m_mDialLimit.m_sGatewayNameStr}/{m_mRecord.T_PhoneNum}";
                            else m_sEndPointStrB = $"sofia/{_m_mDialLimit.m_sGatewayType}/sip:{m_mRecord.T_PhoneNum}@{_m_mDialLimit.m_sGatewayNameStr}";
                        }
                        else
                        {
                            #region 拨号限制
                            Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} no phone number]");
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Hangup(uuid, HangupCause.WrongMessage).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup error:{ex.Message}]");
                                }
                            });

                            if (m_bIsDispose) return;
                            if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                            {
                                await m_pOutboundSocket.Exit().ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit error:{ex.Message}]");
                                    }
                                });
                            }

                            if (m_bIsDispose) return;
                            m_pOutboundSocket?.Dispose();

                            return;
                            #endregion
                        }
                    }
                }

                m_mRecord.ChannelID = m_mChannel.channel_id;

                #region 繁忙
                if (
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_IDLE &&
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_AHANGUP &&
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_BHANGUP
                    )
                {
                    string m_sMsgStr = $"busy";
                    Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} {m_sMsgStr}]");

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.UserBusy).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup error:{ex.Message}]");
                        }
                    });

                    if (m_bIsDispose) return;
                    if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                    {
                        await m_pOutboundSocket.Exit().ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
                m_mChannel.channel_call_uuid = uuid;
                string m_sApplicationStr = Call_ParamUtil._application;
                int m_uTimeoutSeconds = Call_ParamUtil.__timeout_seconds;
                bool m_bIgnoreEarlyMedia = Call_ParamUtil.__ignore_early_media;
                string m_sExtensionStr = Call_ParamUtil._rec_t;
                string m_sWhoHangUpStr = string.Empty;
                bool m_bIsLinked = false;
                DateTime m_dtStartTimeNow = DateTime.Now;
                string m_sStartTimeNowString = Cmn.m_fDateTimeString(m_dtStartTimeNow);
                //真实号码赋值
                if (!m_bStar) m_stNumberStr = _m_mDialLimit.m_stNumberStr;
                //录音中真实号码赋值
                m_mRecord.tnumber = m_stNumberStr;

                if (m_bIsDispose) return;
                await m_pOutboundSocket.Linger().ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Linger cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Linger error:{ex.Message}]");
                    }
                });

                bool Status183or200 = false;
                bool Channel200 = false;
                string m_sAnswer = Call_ParamUtil.m_sCaseAnswer;

                if (m_bIsDispose) return;
                m_pOutboundSocket.OnHangup(uuid, async ax =>
                {
                    if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                    {
                        m_sWhoHangUpStr = "A";
                        Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} a-leg hangup]");

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
                            m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                            m_mRecord.CallResultID = m_bStar ? 37 : 10;
                        }

                        //去掉无法加拨的前缀
                        if (!string.IsNullOrWhiteSpace(m_sCalleeRemove0000Prefix))
                        {
                            m_mRecord.T_PhoneNum = m_sCalleeRemove0000Prefix;
                            m_mRecord.C_PhoneNum = m_sCalleeRemove0000Prefix;
                        }
                        call_record.Insert(m_mRecord);
                        Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} insert record]");

                        if (!m_bStar)
                        {
                            m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, m_mRecord.C_SpeakTime);
                            Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
                        }

                        m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        m_mChannel.channel_call_uuid = null;
                        m_mChannel.channel_call_other_uuid = null;

                        if (m_bIsDispose) return;
                        if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                        {
                            await m_pOutboundSocket.Exit().ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit error:{ex.Message}]");
                                }
                            });
                        }

                        if (m_bIsDispose) return;
                        m_pOutboundSocket?.Dispose();
                    }
                });

                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_RINGING;
                string bridgeUUID = Guid.NewGuid().ToString();
                m_mChannel.channel_call_other_uuid = bridgeUUID;

                #region ***增加183接通问题,暂时保留183吧,虽然没用了
                if (m_bUseChannelProgressMedia)
                {
                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.SubscribeEvents(EventName.ChannelProgress, EventName.ChannelProgressMedia, EventName.ChannelAnswer).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} event 180,183,200 cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} event 180,183,200 error:{ex.Message}]");
                        }
                    });

                    ///如果最后又给了180,自己放音
                    if (m_bIsDispose) return;
                    m_eChannel180 = m_pOutboundSocket.ChannelEvents.Where(x => x.UUID == bridgeUUID && (x.EventName == EventName.ChannelProgress)).Take(1).Subscribe(async x =>
                    {
                        if (m_b183)
                        {
                            ///又接受了180,需自己放音
                            m_b180 = true;

                            #region ***还是兼容不了移动无早期媒体问题
                            if (false)
                            {
                                ///先接通
                                if (!Channel200)
                                {
                                    if (m_bIsDispose) return;
                                    await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                                    {
                                        try
                                        {
                                            if (m_bIsDispose) return;
                                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} {m_sAnswer} cancel]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} {m_sAnswer} error:{ex.Message}]");
                                        }
                                    });
                                }

                                ///还是循环播放吧
                                for (int i = 0; i < 1; i++)
                                {
                                    if (!Channel200)
                                    {
                                        if (m_bIsDispose) return;
                                        await m_pOutboundSocket.Play(uuid, CenoCommon.m_mPlay.m_mBgMusic).ContinueWith(task =>
                                        {
                                            try
                                            {
                                                if (m_bIsDispose) return;
                                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Play a-leg bg music cancel]");
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Play a-leg bg music error:{ex.Message}]");
                                            }
                                        });
                                    }
                                }
                            }
                            #endregion
                        }
                    });

                    //183,200消息合并处理,为得到真实的拨打时间
                    m_eChannel183or200 = m_pOutboundSocket.ChannelEvents.Where(x => x.UUID == bridgeUUID && (x.EventName == EventName.ChannelProgressMedia || x.EventName == EventName.ChannelAnswer)).Take(2).Subscribe(async x =>
                    {
                        DateTime m_dtNow = DateTime.Now;

                        ///已接受183
                        if (x.EventName == EventName.ChannelProgressMedia) m_b183 = true;

                        ///IMS先发送一段媒体,播放给b-leg
                        if (x.EventName == EventName.ChannelProgressMedia && m_bBridgeApp)
                        {
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Play(bridgeUUID, CenoCommon.m_mPlay.m_mNullMusic).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Play b-leg null music cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Play b-leg null music error:{ex.Message}]");
                                }
                            });
                        }

                        //200处理
                        if (x.EventName == EventName.ChannelAnswer && !Channel200)
                        {
                            //接通,解决200问题
                            Channel200 = true;

                            #region ***还是兼容不了移动无早期媒体问题
                            if (false)
                            {
                                if (m_b180)
                                {
                                    if (m_bIsDispose) return;
                                    await m_pOutboundSocket.SendApi($"uuid_break {uuid} all").ContinueWith(task =>
                                    {
                                        try
                                        {
                                            if (m_bIsDispose) return;
                                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} uuid_break a-leg bg music cancel]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} uuid_break a-leg bg music error:{ex.Message}]");
                                        }
                                    });
                                }
                            }
                            #endregion

                            //计算接通时间和等待时间
                            string m_dtAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtNow);
                            m_mRecord.C_AnswerTime = m_dtAnswerTimeNowString;
                            m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtNow, m_dtStartTimeNow);
                        }

                        if (Status183or200) return;
                        Status183or200 = true;

                        #region ***ws或wss,由于183早期媒体无法透传,直接接通播放回铃
                        string m_sProtocol = m_pOutboundSocket.ChannelData.GetHeader("variable_sip_via_protocol");
                        if (m_sProtocol.StartsWith("ws", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} uuid_answer {uuid}]");
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.SendApi($"uuid_answer {uuid}").ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} uuid_answer cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} uuid_answer error:{ex.Message}]");
                                }
                            });
                        }
                        #endregion
                    });
                }
                #endregion

                string m_sCallerStarPrefix = m_bStar ? $"*{m_mRecord.LocalNum}" : m_mRecord.LocalNum;

                if (m_bIsDispose) return;
                BridgeResult m_pBridgeResult = await m_pOutboundSocket.Bridge(uuid, m_sEndPointStrB, new BridgeOptions()
                {
                    UUID = bridgeUUID,
                    CallerIdNumber = m_sCallerStarPrefix,
                    CallerIdName = m_sCallerStarPrefix,
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
                            Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Bridge cancel]");
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
                        Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Bridge error:{ex.Message}]");
                        return null;
                    }
                });

                if (m_pBridgeResult == null || (m_pBridgeResult != null && !m_pBridgeResult.Success))
                {
                    string m_sBridgeResultStr = m_pBridgeResult?.ResponseText;
                    if (string.IsNullOrWhiteSpace(m_sBridgeResultStr)) m_sBridgeResultStr = null;

                    string m_sMsgStr = $"Bridge fail:{m_sBridgeResultStr ?? "unknow"}";

                    if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                    {
                        m_sWhoHangUpStr = "B";
                        Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} {m_sMsgStr}]");

                        DateTime m_dtEndTimeNow = DateTime.Now;
                        string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                        m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_mRecord.C_StartTime);
                        m_mRecord.C_EndTime = m_sEndTimeNowString;

                        #region 判断电话结果
                        if (string.IsNullOrWhiteSpace(m_sBridgeResultStr))
                        {
                            m_mRecord.CallResultID = m_bStar ? 34 : 7;
                        }
                        else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "NOANSWER"))
                        {
                            m_mRecord.CallResultID = m_bStar ? 34 : 7;
                        }
                        else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "BUSY"))
                        {
                            m_mRecord.CallResultID = m_bStar ? 35 : 8;
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
                        }
                        else
                        {
                            m_mRecord.CallResultID = m_bStar ? 34 : 7;
                        }
                        #endregion

                        m_mRecord.Remark = m_sMsgStr;

                        //去掉无法加拨的前缀
                        if (!string.IsNullOrWhiteSpace(m_sCalleeRemove0000Prefix))
                        {
                            m_mRecord.T_PhoneNum = m_sCalleeRemove0000Prefix;
                            m_mRecord.C_PhoneNum = m_sCalleeRemove0000Prefix;
                        }
                        call_record.Insert(m_mRecord);
                        Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} insert record]");

                        if (!m_bStar)
                        {
                            m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, 0);
                            Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
                        }

                        m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        m_mChannel.channel_call_uuid = null;
                        m_mChannel.channel_call_other_uuid = null;

                        if (m_bIsDispose) return;
                        if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                        {
                            await m_pOutboundSocket.Hangup(uuid, HangupCause.OriginatorCancel).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup error:{ex.Message}]");
                                }
                            });
                        }

                        if (m_bIsDispose) return;
                        if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                        {
                            await m_pOutboundSocket.Exit().ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
                                }
                            });
                        }

                        if (m_bIsDispose) return;
                        m_pOutboundSocket?.Dispose();
                    }
                }
                else
                {
                    m_bIsLinked = true;
                    m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_TALKING;

                    #region ***计算接通时间和等待时间
                    DateTime m_dtAnswerTimeNow = DateTime.Now;
                    //如果比200快,这里应该不可能,而且如果接通,一定会有200消息
                    if (false && !Channel200)
                    {
                        string m_sAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtAnswerTimeNow);
                        m_mRecord.C_AnswerTime = m_sAnswerTimeNowString;
                        m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtAnswerTimeNow, m_dtStartTimeNow);
                    }
                    #endregion

                    //录音中移除强制加拨的前缀
                    string m_sCalleeNumberStr = m_mRecord.T_PhoneNum;
                    if (!string.IsNullOrWhiteSpace(m_sCalleeRemove0000Prefix)) m_sCalleeNumberStr = m_sCalleeRemove0000Prefix;

                    string m_dtString = string.Empty;
                    string m_sQueryUUID = string.Empty;
                    if (m_bIsQueryRecUUID) m_sQueryUUID = DB.Basic.m_fDialLimit.m_fGetDialUUID(m_mAgent.LoginName, out m_dtString);
                    string m_sRecSub = string.Empty;

                    //录音中来电主叫或去掉被叫的真实号码获取
                    string m_sRectNumberStr = (!string.IsNullOrWhiteSpace(m_stNumberStr) ? m_stNumberStr : m_mRecord.LocalNum);
                    if (!m_bIsQueryRecUUID || string.IsNullOrWhiteSpace(m_sQueryUUID) || string.IsNullOrWhiteSpace(m_dtString))
                        m_sRecSub = $"{{0}}\\{m_dtAnswerTimeNow.ToString("yyyy")}\\{m_dtStartTimeNow.ToString("yyyyMM")}\\{m_dtAnswerTimeNow.ToString("yyyyMMdd")}\\Rec_{m_dtAnswerTimeNow.ToString("yyyyMMddHHmmss")}_{m_sRectNumberStr.Replace("*", "X")}_{(m_bStar ? "N" : "")}Q_{m_sCalleeNumberStr.Replace("*", "X")}{m_sExtensionStr}";
                    else
                        m_sRecSub = $"{{0}}\\{m_dtString.Substring(0, 4)}\\{m_dtString.Substring(0, 6)}\\{m_dtString.Substring(0, 8)}\\{m_sQueryUUID}{m_sExtensionStr}";

                    string m_sRecordingFile = string.Format(m_sRecSub, ParamLib.RecordFilePath);
                    string m_sRecordingFolder = Path.GetDirectoryName(m_sRecordingFile);
                    if (!Directory.Exists(m_sRecordingFolder)) Directory.CreateDirectory(m_sRecordingFolder);
                    string m_sRecordingID = Path.GetFileNameWithoutExtension(m_sRecordingFile);

                    #region 录音参数设置
                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'").ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} RECORD_ARTIST cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} RECORD_ARTIST error:{ex.Message}]");
                        }
                    });
                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} RECORD_MIN_SEC cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} RECORD_MIN_SEC error:{ex.Message}]");
                        }
                    });
                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_STEREO", "true").ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} RECORD_STEREO cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} RECORD_STEREO error:{ex.Message}]");
                        }
                    });
                    #endregion

                    //录音
                    {

                        ///修正录音路径,防止出错
                        string _m_sRecordingFile = Cmn_v1.Cmn.PathFmt(m_sRecordingFile, "/");

                        if (m_bIsDispose) return;
                        var m_pRecordingResult = await m_pOutboundSocket.SendApi(string.Format("uuid_record {0} start {1}", uuid, _m_sRecordingFile)).ContinueWith((task) =>
                        {
                            try
                            {
                                if (m_bIsDispose) return null;
                                if (task.IsCanceled)
                                {
                                    Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} record cancel]");
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
                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} record error:{ex.Message}]");
                                return null;
                            }
                        });
                        if (m_pRecordingResult != null && m_pRecordingResult.Success)
                        {
                            m_mRecord.RecordFile = m_sRecordingFile;
                            m_mRecord.recordName = m_sRecordingID;

                            //最原始的客户端的发送录音的方式
                            m_fSend(m_uAgentID, m_mChannel.channel_websocket, m_sRecordingID, m_sRecordingFile);

                            #region ***追加录音发送方式
                            try
                            {
                                //由于WebSocket已经连接了,目前可能不再需要此方式进行了,不过也先保留
                                string m_sProtocol = m_pOutboundSocket.ChannelData.GetHeader("variable_sip_via_protocol");
                                if (m_sProtocol.StartsWith("ws", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    //使用W WebSocket发送录音ID
                                    if (m_mChannel.channel_websocket_W != null)
                                    {
                                        WebWebSocketModel m_mWebWebSocketModel = new WebWebSocketModel();
                                        m_mWebWebSocketModel.type = WebWebSocketType.RecID;
                                        m_mWebWebSocketModel.data = new
                                        {
                                            id = Guid.NewGuid().ToString(),
                                            RecID = m_sRecordingID
                                        };
                                        m_fDialClass.m_fSendObject(m_mChannel.channel_websocket_W, m_mWebWebSocketModel, m_uAgentID, m_sRecordingID);
                                    }

                                    bool m_bIsGoOn = true;
                                    if (m_bIsGoOn)
                                    {
                                        string m_sTo = m_pOutboundSocket.ChannelData.GetHeader("Channel-Presence-ID");
                                        Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fSend][{m_uAgentID} record ID:{m_sRecordingID},chat to {m_sTo}]");
                                        //发送录音
                                        await m_pOutboundSocket.SendApi($"chat sip|{m_sTo}|{m_sTo}|{Model_v1.m_mServerToPModel.TextPrefix}{m_sRecordingID}").ContinueWith(task =>
                                        {
                                            try
                                            {
                                                if (m_bIsDispose) return;
                                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} chat cancel]");
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} chat error:{ex.Message}]");
                                            }
                                        });
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} add RecID method error:{ex.Message}]");
                            }
                            #endregion
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} record fail]");
                        }
                    }

                    #region 备份录音
                    string m_sBackupRecords = Call_ParamUtil.m_sBackupRecords;
                    if (!string.IsNullOrWhiteSpace(m_sBackupRecords))
                    {
                        string m_sBackUpRecordingFile = string.Format(m_sRecSub, m_sBackupRecords);
                        string m_sBackupRecordingFolder = Path.GetDirectoryName(m_sBackUpRecordingFile);
                        if (!Directory.Exists(m_sBackupRecordingFolder)) Directory.CreateDirectory(m_sBackupRecordingFolder);

                        ///修正备份录音路径,防止出错
                        string _m_sBackUpRecordingFile = Cmn_v1.Cmn.PathFmt(m_sBackUpRecordingFile, "/");

                        if (m_bIsDispose) return;
                        var m_pRecordingResult = await m_pOutboundSocket.SendApi(string.Format("uuid_record {0} start {1}", uuid, _m_sBackUpRecordingFile)).ContinueWith((task) =>
                        {
                            try
                            {
                                if (m_bIsDispose) return null;
                                if (task.IsCanceled)
                                {
                                    Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} record cancel]");
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
                                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} record error:{ex.Message}]");
                                return null;
                            }
                        });
                        if (m_pRecordingResult != null && m_pRecordingResult.Success)
                        {
                            Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} back up record success:{m_sBackUpRecordingFile}]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} back up record fail]");
                        }
                    }
                    #endregion

                    //被叫挂断
                    if (m_bIsDispose) return;
                    m_pOutboundSocket.OnHangup(bridgeUUID, async bx =>
                    {
                        if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                        {
                            m_sWhoHangUpStr = "B";
                            Log.Instance.Warn($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} b-leg hangup]");

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

                            //去掉无法加拨的前缀
                            if (!string.IsNullOrWhiteSpace(m_sCalleeRemove0000Prefix))
                            {
                                m_mRecord.T_PhoneNum = m_sCalleeRemove0000Prefix;
                                m_mRecord.C_PhoneNum = m_sCalleeRemove0000Prefix;
                            }
                            call_record.Insert(m_mRecord);
                            Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} insert record]");

                            if (!m_bStar)
                            {
                                m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, m_mRecord.C_SpeakTime);
                                Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
                            }

                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_other_uuid = null;

                            if (m_bIsDispose) return;
                            if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                            {
                                await m_pOutboundSocket.Hangup(uuid, HangupCause.NormalClearing).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Hangup error:{ex.Message}]");
                                    }
                                });
                            }

                            if (m_bIsDispose) return;
                            if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                            {
                                await m_pOutboundSocket.Exit().ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{m_uAgentID} Exit error:{ex.Message}]");
                                    }
                                });
                            }

                            if (m_bIsDispose) return;
                            m_pOutboundSocket?.Dispose();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][Exception][{uuid} unfinished error:{ex.Message}]");

                if (m_mChannel != null)
                {
                    m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                    m_mChannel.channel_call_uuid = null;
                    m_mChannel.channel_call_other_uuid = null;
                }

                if (m_bIsDispose) return;
                if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                {
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.NormalClearing).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup cancel]");
                        }
                        catch (Exception eex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Hangup error:{eex.Message}]");
                        }
                    });
                }

                if (m_bIsDispose) return;
                if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                {
                    await m_pOutboundSocket.Exit().ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit cancel]");
                        }
                        catch (Exception eex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fDial][{uuid} Exit error:{eex.Message}]");
                        }
                    });
                }

                if (m_bIsDispose) return;
                m_pOutboundSocket?.Dispose();
            }
        }

        private static void m_fSend(int m_uAgentID, IWebSocketConnection m_pSocket, string m_sRecordingID, string m_sRecordingFile)
        {
            try
            {
                if (m_pSocket != null)
                {
                    m_pSocket.Send(call_socketcommand_util.SendCommonStr("FSLY", m_sRecordingID, m_sRecordingFile));
                    Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fSend][{m_uAgentID} record ID:{m_sRecordingID} send]");
                }
                else
                    Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fSend][{m_uAgentID} record ID:{m_sRecordingID} send fail:no WebSocket]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fSend][Exception][{ex.Message}]");
            }
        }

        private static void m_fSendObject(IWebSocketConnection m_pWebSocket, object m_oObject, int m_uAgentID, string m_sRecordingID)
        {
            try
            {
                if (m_pWebSocket != null)
                {
                    m_pWebSocket.Send(Newtonsoft.Json.JsonConvert.SerializeObject(m_oObject));
                    Log.Instance.Success($"[CenoFsSharp][m_fDialClass][m_fSendObject][{m_uAgentID} record ID:{m_sRecordingID} send]");
                }
                else
                    Log.Instance.Fail($"[CenoFsSharp][m_fDialClass][m_fSendObject][{m_uAgentID} record ID:{m_sRecordingID} send fail:no WebSocket]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][m_fDialClass][m_fSendObject][Exception][{ex.Message}]");
            }
        }
    }
}
