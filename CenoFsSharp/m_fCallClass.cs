using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using NEventSocket;
using NEventSocket.FreeSwitch;
using log4net;
using DB.Basic;
using DB.Model;
using CenoCommon;
using CenoSipFactory;
using System.Net.Sockets;
using System.IO;
using Core_v1;
using Cmn_v1;
using System.Text.RegularExpressions;
using Model_v1;
using System.Threading.Tasks;
using Fleck;
using System.Data;

namespace CenoFsSharp
{
    public class m_fCallClass
    {
        public delegate void m_dBusySendMsg(IWebSocketConnection m_pWebSocket, string m_sMsg);
        public static m_dBusySendMsg m_fBusySendMsg;

        public static async void m_fCall(OutboundSocket m_pOutboundSocket, string m_sTransfer = null, int m_uShare = 0, share_number m_pShareNumber = null, int _m_mAgentID = -1, int _m_uLimitId = -1)
        {
            string uuid = m_pOutboundSocket.ChannelData.UUID;
            string m_sRealCallerNumberStr = m_pOutboundSocket.ChannelData.GetHeader("Channel-Caller-ID-Number")?.Replace("gw+", "")?.Replace("+86", "0");//主叫

            #region ***处理被叫号码,主要防止gateway、ims情况等,如有其它情况再做兼容
            Regex m_rReplaceRegex = new Regex("[^(0-9*#)]+");
            ///被叫
            string _m_sRealCalleeNumberStr = m_pOutboundSocket.ChannelData.GetHeader("Channel-Destination-Number")?.Replace("gw+", "")?.Replace("+86", "0");

            ///兼容86而不是+86开头,如果包含ims,这里需要如此处理
            if (_m_sRealCalleeNumberStr.StartsWith("86") && _m_sRealCalleeNumberStr.Contains("ims"))
            {
                _m_sRealCalleeNumberStr = $"+{_m_sRealCalleeNumberStr}".Replace("+86", "0");
            }

            string m_sRealCalleeNumberStr = m_rReplaceRegex.Replace(_m_sRealCalleeNumberStr, string.Empty);
            #endregion

            ChannelInfo m_mChannel = null;
            bool m_bIsDispose = false;
            int m_uPlayLoops = Call_ParamUtil.m_uCasePlayLoops;
            string m_sAnswer = Call_ParamUtil.m_sCaseAnswer;
            bool m_bWeb = false;
            int m_uAgentID = -1;
            string m_sLoginName = string.Empty;
            Model_v1.AddRecByRec m_pAddRecByRec = null;
            string m_sFreeSWITCHIPv4 = InboundMain.FreesSWITCHIPv4;
            //真实号码
            string m_stNumberStr = string.Empty;
            ///呼入所属拨号限制ID
            int m_uLimitId = -1;
            //呼入回铃
            string m_sCallMusic = Call_ParamUtil.m_sCallMusic;

            try
            {
                m_pOutboundSocket.Disposed += (a, b) =>
                {
                    m_bIsDispose = true;
                    Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} outbound socket dispose]");
                };

                #region 增加IVR逻辑
                bool m_bTransfer = false;
                if (!string.IsNullOrWhiteSpace(m_sTransfer))
                {
                    m_bTransfer = true;
                    Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} callee:{m_sRealCalleeNumberStr} -> transfer callee:{m_sTransfer}]");
                }
                #endregion

                Regex m_rIsMatchRegex = new Regex("^[0-9*#]{3,20}$");
                #region 号码有误
                if (!m_rIsMatchRegex.IsMatch(m_sRealCalleeNumberStr))
                {
                    Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} invalid phone,play invalid music]");

                    #region 播放提示音
                    if (m_uPlayLoops > 0)
                    {
                        //应答播放声音
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                            }
                        });

                        for (int i = 0; i < m_uPlayLoops; i++)
                        {
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Play(uuid, m_mPlay.m_mInvalidMusic).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play invalid music cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play invalid music error:{ex.Message}]");
                                }
                            });
                        }
                    }
                    #endregion

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.InvalidNumberFormat).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Hangup error:{ex.Message}]");
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
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                #region ***呼出只判断是否是黑名单,黑名单直接限制呼叫即可,如果更新中则暂时失效即可
                if (!m_cWblist.m_bInitWblist && m_cWblist.m_lWblist?.Count > 0)
                {
                    ///判断所有的黑名单即可
                    foreach (m_mWblist item in m_cWblist.m_lWblist)
                    {
                        ///兼容呼入呼出黑名单
                        if (item.wbtype == 2 && (item.wblimittype & 1) > 0)
                        {
                            if (item.regex.IsMatch(m_sRealCallerNumberStr))
                            {
                                Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} black list:{m_sRealCallerNumberStr}]");

                                #region 播放提示音
                                if (m_uPlayLoops > 0)
                                {
                                    //应答播放声音
                                    if (m_bIsDispose) return;
                                    await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                                    {
                                        try
                                        {
                                            if (m_bIsDispose) return;
                                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} cancel]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                                        }
                                    });

                                    for (int i = 0; i < m_uPlayLoops; i++)
                                    {
                                        if (m_bIsDispose) return;
                                        await m_pOutboundSocket.Play(uuid, m_mPlay.m_mNoAnswerMusic).ContinueWith(task =>
                                        {
                                            try
                                            {
                                                if (m_bIsDispose) return;
                                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play invalid music cancel]");
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play invalid music error:{ex.Message}]");
                                            }
                                        });
                                    }
                                }
                                #endregion

                                if (m_bIsDispose) return;
                                await m_pOutboundSocket.Hangup(uuid, HangupCause.SystemShutdown).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Hangup error:{ex.Message}]");
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
                                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Exit cancel]");
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Exit error:{ex.Message}]");
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

                #region ***是否需要查询联系人姓名
                if (Call_ParamUtil.m_bUseHomeSearch) m_cEsySQL.m_fSetExpc(m_sRealCallerNumberStr);
                #endregion

                string m_sEndPointStrB = string.Empty;
                AGENT_INFO m_mAgent = null;
                bool m_bInboundTest = Call_ParamUtil.InboundTest;
                List<string> m_lStrings = m_cPhone.m_fGetPhoneNumberMemo(m_sRealCallerNumberStr);//解析主叫
                bool m_bStar = m_lStrings[2] == Special.Star;
                string m_sCalleeNumberStr = m_sRealCalleeNumberStr;
                bool m_bShareReject = false;
                ///是否使用了内呼规则,存储原被叫
                string m_sLocalNum = null;

                ///本呼叫中心呼入坐席
                AGENT_INFO m_mTheAgent = null;
                int m_qInCall = 0;//0不处理

                if (m_bTransfer)
                {
                    m_mAgent = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_number == m_sTransfer.Substring(1));
                }
                else
                {
                    if (m_bStar)
                    {
                        string[] m_lBf = m_sRealCalleeNumberStr.Split('*');
                        if (m_lBf.Length > 1)
                        {
                            m_sLocalNum = m_sCalleeNumberStr;
                            m_sCalleeNumberStr = m_lBf[1];
                        }
                        m_mAgent = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_number == m_sCalleeNumberStr);
                    }
                    else
                    {
                        if (m_bInboundTest)
                        {
                            m_mAgent = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_number == m_sCalleeNumberStr);
                        }
                        else
                        {
                            #region ***此处加入共享号码逻辑
                            if (m_uShare == 0)
                            {
                                ///先查出需要转接的号码,增加主叫号码的带入
                                if (_m_mAgentID == -1)
                                {
                                    _m_mAgentID = m_fDialLimit.m_fGetAgentID(m_sCalleeNumberStr, out m_stNumberStr, false, m_sRealCallerNumberStr, out m_uLimitId);
                                }
                                ///存储拨号限制ID,为呼叫内转做准备
                                else if (_m_uLimitId != -1)
                                {
                                    m_uLimitId = _m_uLimitId;
                                }
                                m_mAgent = call_factory.agent_list.FirstOrDefault(x => x.AgentID == _m_mAgentID);
                            }
                            ///<![CDATA[
                            /// <1>路由Redis查询共享号码
                            ///]]>
                            if (m_mAgent == null && m_uShare == 0)
                            {
                                ///共享、申请式暂时不增加路由规则
                                m_uShare = Redis2.m_fGetTheCall(m_sRealCalleeNumberStr, uuid, out m_pShareNumber);
                            }
                            ///<![CDATA[
                            /// <2>是共享号码时参数条件适配
                            ///]]>
                            if (m_mAgent == null && m_uShare > 0)
                            {
                                switch (m_uShare)
                                {
                                    case 1:
                                    case 2:
                                    case 3:
                                        {
                                            m_pAddRecByRec = m_fDialLimit.m_fGetAgentByRecord(m_sCalleeNumberStr, m_sRealCallerNumberStr, m_sFreeSWITCHIPv4);
                                            if (m_pAddRecByRec == null) m_bShareReject = true;

                                            ///反查出呼入坐席
                                            if (m_pAddRecByRec.m_sEndPointStr.Contains("user/"))
                                            {
                                                m_qInCall = 1;
                                                m_mTheAgent = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_number == m_pAddRecByRec.UAID);
                                                if (m_mTheAgent == null) m_qInCall = -1;
                                            }

                                            else if (string.IsNullOrWhiteSpace(m_pAddRecByRec.m_sEndPointStr)) m_bShareReject = true;
                                            else if (m_pShareNumber == null) m_bShareReject = true;
                                            Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} share {m_uShare} b-leg-endpoint get,{m_bShareReject}]");
                                            break;
                                        }
                                    case 11:
                                    case 12:
                                    case 13:
                                    case 14:
                                        {
                                            if (m_pShareNumber == null) m_bShareReject = true;
                                            else
                                            {
                                                ///申请式,后续逻辑不一样
                                                m_pAddRecByRec = new AddRecByRec();
                                                ///无需再次查询,直接赋值即可,因为上文已查出
                                                m_pAddRecByRec.UAID = m_pShareNumber.fs_num;
                                                if (m_sFreeSWITCHIPv4.Equals(m_pShareNumber.fs_ip))
                                                {
                                                    m_qInCall = 1;
                                                    m_pAddRecByRec.m_sEndPointStr = $"user/{m_pShareNumber.fs_num}";
                                                    ///反查出呼入坐席
                                                    m_mTheAgent = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_number == m_pShareNumber.fs_num);
                                                    if (m_mTheAgent == null) m_qInCall = -1;
                                                }
                                                else
                                                    m_pAddRecByRec.m_sEndPointStr = $"sofia/external/sip:*{m_pShareNumber.fs_num}@{m_pShareNumber.fs_ip}:5080";
                                                m_pAddRecByRec.m_sFreeSWITCHIPv4 = m_pShareNumber.fs_ip;
                                                m_pAddRecByRec.m_uAgentID = m_pShareNumber.agentID;
                                                m_pAddRecByRec.m_uChannelID = m_pShareNumber.channelID;
                                                m_pAddRecByRec.m_uFromAgentID = m_pShareNumber.agentID;
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            #endregion
                        }
                    }
                }

                #region ***无用户信息、共享号码呼入等,若条件不足时挂断
                bool m_bHasShareEndPointStr = !string.IsNullOrWhiteSpace(m_pAddRecByRec?.m_sEndPointStr);
                if (
                    //非共享,但未找到对应用户
                    (m_mAgent == null && m_uShare == 0) ||
                    //共享,但不满足继续条件
                    (m_uShare > 0 && m_bShareReject)
                    )
                {
                    Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} can,t find user,play no user music]");

                    if (m_uShare == 1)
                    {
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                    }

                    #region 播放提示音
                    if (m_uPlayLoops > 0)
                    {
                        //应答播放声音
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                            }
                        });

                        for (int i = 0; i < m_uPlayLoops; i++)
                        {
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Play(uuid, m_mPlay.m_mNoUserMusic).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play no user music cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play no user music error:{ex.Message}]");
                                }
                            });
                        }
                    }
                    #endregion

                    #region ***呼入,这里直接杀死,不再挂断,防止Freeswitch内存泄漏
                    if (false)
                    {
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.Hangup(uuid, HangupCause.UserNotRegistered).ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Hangup cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Hangup error:{ex.Message}]");
                            }
                        });
                    }
                    #endregion

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.SendApi($"uuid_kill {uuid}").ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} uuid_kill cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} uuid_kill error:{ex.Message}]");
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
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                if (m_bStar) m_sEndPointStrB = $"user/{m_sCalleeNumberStr}";
                else
                {
                    ///<![CDATA[
                    /// <3>设置UA终点
                    /// ]]>
                    if (m_uShare > 0) m_sEndPointStrB = m_pAddRecByRec?.m_sEndPointStr;
                    else m_sEndPointStrB = $"user/{m_mAgent.ChInfo.channel_number}";
                }

                #region ***进入共享号码逻辑
                if (m_uShare > 0)
                {
                    ///<![CDATA[
                    /// <4>加入共享号码来电逻辑
                    /// ]]>
                    m_fCallClass.m_fShareCall(m_pOutboundSocket, m_sCalleeNumberStr, m_bStar, m_lStrings, m_sRealCallerNumberStr, m_sEndPointStrB, m_uShare, m_pAddRecByRec, m_pShareNumber, m_mTheAgent, m_qInCall, m_uPlayLoops, m_sAnswer, m_sCallMusic);
                    return;
                }
                #endregion

                m_sLoginName = m_mAgent.LoginName;
                m_uAgentID = m_mAgent.AgentID;
                Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} -> {m_uAgentID}]");
                call_record_model m_mRecord = new call_record_model(m_mAgent.ChInfo.channel_id);
                m_mRecord.AgentID = m_uAgentID;
                m_mRecord.LocalNum = m_sCalleeNumberStr;
                //录音中真实号码赋值
                m_mRecord.tnumber = m_stNumberStr;

                if (m_bTransfer)
                {
                    m_bStar = false;
                    m_mRecord.PhoneAddress = $"{m_lStrings[3]} IVR";
                    m_mRecord.T_PhoneNum = m_sRealCallerNumberStr;
                    m_mRecord.C_PhoneNum = m_sRealCallerNumberStr;
                }
                else
                {
                    if (m_bStar)
                    {
                        m_mRecord.PhoneAddress = "内呼";

                        ///如果走了内呼规则,特殊处理对方号码
                        if (m_sLocalNum != null)
                        {
                            m_mRecord.LocalNum = m_sLocalNum;
                            m_mRecord.T_PhoneNum = m_sRealCallerNumberStr;
                        }
                        else
                            m_mRecord.T_PhoneNum = $"{Special.Star}{m_sRealCallerNumberStr}";

                        m_mRecord.C_PhoneNum = m_mRecord.T_PhoneNum;
                    }
                    else
                    {
                        m_mRecord.PhoneAddress = m_lStrings[3];
                        m_mRecord.T_PhoneNum = m_sRealCallerNumberStr;
                        m_mRecord.C_PhoneNum = m_sRealCallerNumberStr;
                    }
                }

                #region ***呼叫内转逻辑
                ///得到是否有呼叫内转的线路
                m_mInlimit_2 _m_mInlimit_2 = null;
                ///是否内转
                bool m_bInlimit = true;
                if (!m_cInlimit_2.m_bInitInlimit_2 && m_cInlimit_2.m_lInlimit_2 != null && m_cInlimit_2.m_lInlimit_2.Count > 0)
                {
                    ///时间、星期的判断
                    DateTime m_pDateTime = DateTime.Now;
                    DayOfWeek m_sWeekday = m_pDateTime.DayOfWeek;
                    int m_uWeekday = (int)m_sWeekday;
                    if (m_uWeekday == 0) m_uWeekday = 7;
                    m_uWeekday = m_uWeekday - 1;
                    int m_uDay = (int)Math.Pow(2, m_uWeekday);

                    if (m_uLimitId != -1)
                    {
                        ///再加一个星期条件
                        _m_mInlimit_2 = m_cInlimit_2.m_lInlimit_2.Where(x => x.inlimit_2id == m_uLimitId && ((x.inlimit_2whatday & m_uDay) > 0))?.FirstOrDefault();
                    }
                    ///假如通过ID未找到内转信息,如未配置,则找一下该坐席的其它内转信息
                    if (_m_mInlimit_2 == null)
                    {
                        Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} no inlimit_2 by:{m_uLimitId},then by ua:{m_mAgent.AgentID}]");
                        ///增加一个条件,可直接查询内转网关,随机一个没有写入
                        _m_mInlimit_2 = m_cInlimit_2.m_lInlimit_2.Where(x => (x.useuser == m_mAgent.AgentID || !x.type) && ((x.inlimit_2whatday & m_uDay) > 0))?.FirstOrDefault();
                    }
                    ///得到内转信息,配置内转表达式
                    if (_m_mInlimit_2 != null)
                    {

                        ///查看类型,如果为内转线路,则优先级最高
                        ///其次为内转网关,这里内转网关如果需要自动加拨前缀就不能做了,需要自己通过网关的规则变换来实现
                        ///最后坐席自身设定

                        string inlimit_2starttime = _m_mInlimit_2.inlimit_2starttime;
                        string inlimit_2endtime = _m_mInlimit_2.inlimit_2endtime;
                        string inlimit_2number = _m_mInlimit_2.inlimit_2number;
                        if (!_m_mInlimit_2.type)
                        {
                            ///如果该坐席开启了内转,且符合星期
                            if (m_mAgent.isinlimit_2 && (m_mAgent.inlimit_2whatday & m_uDay) > 0)
                            {
                                inlimit_2starttime = m_mAgent.inlimit_2starttime;
                                inlimit_2endtime = m_mAgent.inlimit_2endtime;

                                ///这里还想兼容网关的时间设定,但是优先级问题需要考虑一下
                                ///先去掉,界面也暂时不能操作

                                inlimit_2number = m_mAgent.inlimit_2number;
                            }
                            else
                            {
                                ///如果路由到内转网关但坐席没有设置内转,跳过
                                m_bInlimit = false;
                            }
                        }
                        if (string.IsNullOrWhiteSpace(inlimit_2number)) m_bInlimit = false;

                        ///如果符合内转规则
                        if (m_bInlimit)
                        {
                            m_bInlimit = false;

                            DateTime m_dtStart = Convert.ToDateTime(m_pDateTime.ToString($"yyyy-MM-dd {inlimit_2starttime}"));
                            DateTime m_dtEnd = Convert.ToDateTime(m_pDateTime.ToString($"yyyy-MM-dd {inlimit_2endtime}"));
                            int m_uBs = DateTime.Compare(m_dtStart, m_dtEnd);
                            ///如果相等,代表全天
                            if (m_uBs == 0) m_bInlimit = true;
                            else
                            {
                                if (m_uBs > 0) m_dtEnd = m_dtEnd.AddDays(1);
                                ///判断时间
                                if (DateTime.Compare(m_dtStart, m_pDateTime) <= 0 && DateTime.Compare(m_dtEnd, m_pDateTime) > 0)
                                    m_bInlimit = true;
                                else Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} not time]");
                            }
                        }
                        ///如果符合内转规则
                        if (m_bInlimit)
                        {
                            if (_m_mInlimit_2.m_bGatewayType)
                                m_sEndPointStrB = $"sofia/gateway/{_m_mInlimit_2.m_sGatewayNameStr}/{inlimit_2number}";
                            else
                                m_sEndPointStrB = $"sofia/{_m_mInlimit_2.m_sGatewayType}/sip:{inlimit_2number}@{_m_mInlimit_2.m_sGatewayNameStr}";

                            ///打印呼叫转移日志
                            Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} b-leg-endpoint:{m_sEndPointStrB}]");
                            ///归属地增加内转标记
                            m_mRecord.PhoneAddress = $"{m_mRecord.PhoneAddress} 转移:{inlimit_2number}";
                        }
                    }
                    else Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} no inlimit_2]");
                }
                #endregion

                #region ***测试南昌Unknown
                if (true)
                {
                    if (
                        Cmn.IgnoreEquals(m_mRecord.T_PhoneNum, "Unknown") ||
                        Cmn.IgnoreEquals(m_mRecord.C_PhoneNum, "Unknown")
                        )
                    {
                        try
                        {
                            string m_sPrint = string.Empty;
                            foreach (KeyValuePair<string, string> item in m_pOutboundSocket.ChannelData.Headers)
                            {
                                m_sPrint += $" -> {item.Key}:{item.Value};\r\n";
                            }
                            Log.Instance.Debug($"{m_sPrint}");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][Exception][{uuid} print error:{ex.Message}]");
                        }
                    }
                }
                #endregion

                m_mChannel = m_mAgent.ChInfo;

                #region 1.非内转2.无通道信息
                if (!m_bInlimit && (m_mChannel == null || m_mChannel?.channel_type != Special.SIP))
                {
                    string m_sMsgStr = $"can,t find channel or not sip channel";
                    Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} {m_sMsgStr},play link error music]");

                    #region 播放提示音
                    if (m_uPlayLoops > 0)
                    {
                        //应答播放声音
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                            }
                        });

                        for (int i = 0; i < m_uPlayLoops; i++)
                        {
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Play(uuid, m_mPlay.m_mNoChannelMusic).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play link error music cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play link error music error:{ex.Message}]");
                                }
                            });
                        }
                    }
                    #endregion

                    m_mRecord.CallType = m_bStar ? 8 : 4;
                    m_mRecord.CallResultID = m_bStar ? 45 : 18;
                    m_mRecord.C_EndTime = Cmn.m_fDateTimeString();
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_mRecord.C_EndTime, m_mRecord.C_StartTime);
                    m_mRecord.Uhandler = 0;
                    m_mRecord.Remark = m_sMsgStr;

                    call_record.Insert(m_mRecord);
                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} insert record]");

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.WrongMessage).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup error:{ex.Message}]");
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
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                ///解除PC注册模式的限制,但是用客户端的人怎么办?可能需要加一个字段来开启和关闭轮询API弹屏
                ///暂时不做处理,直接跳过注册模式
                m_bWeb = true;///(m_mChannel.IsRegister == -1 || m_mChannel.IsRegister == 0);

                m_mRecord.ChannelID = m_mChannel.channel_id;

                #region 未连接(1.非内转2.IP话机可无WebSocket)
                if (!m_bInlimit && m_mChannel.IsRegister == 1 && m_mChannel.channel_websocket == null)
                {
                    string m_sMsgStr = $"user no connect";
                    Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} {m_sMsgStr}]");

                    #region 播放提示音
                    if (m_uPlayLoops > 0)
                    {
                        //应答播放声音
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                            }
                        });

                        for (int i = 0; i < m_uPlayLoops; i++)
                        {
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Play(uuid, m_mPlay.m_mNotConnectedMusic).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play link error music cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play link error music error:{ex.Message}]");
                                }
                            });
                        }
                    }
                    #endregion

                    m_mRecord.CallType = m_bStar ? 8 : 4;
                    m_mRecord.CallResultID = m_bStar ? 45 : 18;
                    m_mRecord.C_EndTime = Cmn.m_fDateTimeString();
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_mRecord.C_EndTime, m_mRecord.C_StartTime);
                    m_mRecord.Uhandler = 0;
                    m_mRecord.Remark = m_sMsgStr;

                    call_record.Insert(m_mRecord);
                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} insert record]");

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.UserNotRegistered).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup error:{ex.Message}]");
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
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                #region 繁忙
                if (
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_IDLE &&
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_AHANGUP &&
                    m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_BHANGUP
                    )
                {
                    string m_sMsgStr = $"busy";
                    Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} {m_sMsgStr},play busy music]");

                    #region 播放提示音
                    string m_sPlayMusic = m_mPlay.m_mBusyMusic;
                    if (m_uPlayLoops > 0)
                    {
                        //应答播放声音
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                            }
                        });

                        for (int i = 0; i < m_uPlayLoops; i++)
                        {
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Play(uuid, m_sPlayMusic).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play busy music cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play busy music error:{ex.Message}]");
                                }
                            });
                        }
                    }
                    #endregion

                    ///发送一个来电警告弹屏,并直接刷新一下未接来电
                    #region ***忙来电弹屏,暂时去掉
                    if (false)
                    {
                        if (m_mChannel.IsRegister == 1)
                        {
                            m_fCallClass.m_fBusySendMsg?.Invoke(m_mChannel.channel_websocket, $"{m_mRecord.T_PhoneNum},{m_mRecord.PhoneAddress},{m_mRecord.LocalNum}");
                        }
                    }
                    #endregion

                    m_mRecord.CallType = m_bStar ? 8 : 4;
                    m_mRecord.CallResultID = m_bStar ? 45 : 18;
                    m_mRecord.C_EndTime = Cmn.m_fDateTimeString();
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_mRecord.C_EndTime, m_mRecord.C_StartTime);
                    m_mRecord.Uhandler = 0;
                    m_mRecord.Remark = m_sMsgStr;

                    call_record.Insert(m_mRecord);
                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} insert record]");

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.UserBusy).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup error:{ex.Message}]");
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
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                #region ***设置183或者200,后续无需再设置
                if (m_sAnswer == "uuid_pre_answer")
                {
                    if (!string.IsNullOrWhiteSpace(m_sCallMusic))
                    {
                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} set ringback]");
                        //修正无法解析变量的问题,这里先写成该固定参数即可
                        string m_sData = $"ringback={m_sCallMusic}";
                        //设置183铃声
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.ExecuteApplication(uuid, "set", m_sData).ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} set ringback cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} set ringback error:{ex.Message}]");
                            }
                        });
                    }

                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_sAnswer} {uuid}]");
                    //早期应答
                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                        }
                    });
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

                if (m_bIsDispose) return;
                await m_pOutboundSocket.Linger().ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Linger cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Linger error:{ex.Message}]");
                    }
                });

                if (m_bIsDispose) return;
                m_pOutboundSocket.OnHangup(uuid, async ax =>
                {
                    if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                    {
                        m_sWhoHangUpStr = "A";
                        Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} a-leg hangup]");

                        DateTime m_dtEndTimeNow = DateTime.Now;
                        string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                        m_mRecord.C_EndTime = m_sEndTimeNowString;

                        if (m_bIsLinked)
                        {
                            m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_mRecord.C_AnswerTime);
                            m_mRecord.CallType = m_bStar ? 7 : 3;
                            m_mRecord.CallResultID = m_bStar ? 42 : 15;
                            m_mRecord.Uhandler = 1;
                        }
                        else
                        {
                            m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                            m_mRecord.CallType = m_bStar ? 8 : 4;

                            if (m_mChannel.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP)
                            {
                                m_mRecord.CallResultID = m_bStar ? 50 : 49;
                            }
                            else
                            {
                                m_mRecord.CallResultID = m_bStar ? 52 : 51;
                            }

                            m_mRecord.Uhandler = 0;
                        }

                        call_record.Insert(m_mRecord);
                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} insert record]");

                        //注册含义-1:IP话机Web调用
                        if (m_bWeb)
                            m_fCallClass.m_fIpEndCall(m_uAgentID, m_sLoginName);

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
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit error:{ex.Message}]");
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

                #region 录音参数设置
                if (m_bIsDispose) return;
                await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'").ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} RECORD_ARTIST cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} RECORD_ARTIST error:{ex.Message}]");
                    }
                });
                if (m_bIsDispose) return;
                await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} RECORD_MIN_SEC cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} RECORD_MIN_SEC error:{ex.Message}]");
                    }
                });
                if (m_bIsDispose) return;
                await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_STEREO", "true").ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} RECORD_STEREO cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} RECORD_STEREO error:{ex.Message}]");
                    }
                });
                #endregion

                #region 开始录音
                DateTime m_dtNow = DateTime.Now;
                //录音中来电主叫或去掉被叫的真实号码获取
                string m_sRectNumberStr = (!string.IsNullOrWhiteSpace(m_stNumberStr) ? m_stNumberStr : m_mRecord.LocalNum);
                string m_sRecSub = $"{{0}}\\{m_dtNow.ToString("yyyy")}\\{m_dtStartTimeNow.ToString("yyyyMM")}\\{m_dtNow.ToString("yyyyMMdd")}\\Rec_{m_dtNow.ToString("yyyyMMddHHmmss")}_{m_sRectNumberStr.Replace("*", "X")}_{(m_bStar ? "N" : "")}L_{m_mRecord.T_PhoneNum.Replace("*", "X")}{m_sExtensionStr}";
                string m_sRecordingFile = string.Format(m_sRecSub, ParamLib.RecordFilePath);
                string m_sRecordingFolder = Path.GetDirectoryName(m_sRecordingFile);
                if (!Directory.Exists(m_sRecordingFolder)) Directory.CreateDirectory(m_sRecordingFolder);
                string m_sRecordingID = Path.GetFileNameWithoutExtension(m_sRecordingFile);
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
                                Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} record cancel]");
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
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} record error:{ex.Message}]");
                            return null;
                        }
                    });
                    if (m_pRecordingResult != null && m_pRecordingResult.Success)
                    {
                        m_mRecord.RecordFile = m_sRecordingFile;
                        m_mRecord.recordName = m_sRecordingID;

                        ///内呼不弹屏
                        if (m_mChannel?.channel_websocket != null)
                        {
                            if (!m_bStar)
                            {
                                Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} record ID:{m_sRecordingID} send]");
                                m_fSendRecordingID(m_mChannel, m_sRealCallerNumberStr, m_sRecordingID, m_sRecordingFile);
                            }
                            else
                            {
                                Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} record ID:{m_sRecordingID} not send:*]");
                            }
                        }

                        #region ***追加录音发送方式
                        try
                        {
                            string m_sProtocol = m_pOutboundSocket.ChannelData.GetHeader("variable_sip_via_protocol");
                            if (m_sProtocol.StartsWith("ws", StringComparison.InvariantCultureIgnoreCase))
                            {
                                //使用W WebSocket发送来电内容
                                if (m_mChannel.channel_websocket_W != null)
                                {
                                    WebWebSocketModel m_mWebWebSocketModel = new WebWebSocketModel();
                                    m_mWebWebSocketModel.type = WebWebSocketType.Call;
                                    m_mWebWebSocketModel.data = new
                                    {
                                        id = Guid.NewGuid().ToString(),
                                        number = m_sRealCallerNumberStr,
                                        RecID = m_sRecordingID
                                    };
                                    m_fCallClass.m_fSendObject(m_mChannel.channel_websocket_W, m_mWebWebSocketModel, m_uAgentID, m_sRecordingID);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} add RecID method error:{ex.Message}]");
                        }
                        #endregion
                    }
                    else
                    {
                        m_sRecordingID = string.Empty;
                        Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} record fail]");
                    }
                }
                #endregion

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
                                Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} record cancel]");
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
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} record error:{ex.Message}]");
                            return null;
                        }
                    });
                    if (m_pRecordingResult != null && m_pRecordingResult.Success)
                    {
                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} back up record success:{m_sBackUpRecordingFile}]");
                    }
                    else
                    {
                        Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} back up record fail]");
                    }
                }
                #endregion

                //注册含义-1:IP话机Web调用
                if (m_bWeb)
                    m_fCallClass.m_fIpCall(m_uAgentID, m_sLoginName, m_mRecord.T_PhoneNum, Cmn.m_fDateTimeString(m_dtNow), m_sRecordingID);

                ///To拼接至主叫名称:0不开、1全开、2注册开
                string m_sCallerIdName = m_mRecord.T_PhoneNum;
                if (Call_ParamUtil.m_uAppendTo == 1 || (Call_ParamUtil.m_uAppendTo == 2 && m_mChannel.IsRegister == 1))
                {
                    m_sCallerIdName = $"{m_mRecord.T_PhoneNum}To{(!string.IsNullOrWhiteSpace(m_mRecord.tnumber) ? m_mRecord.tnumber : m_mRecord.LocalNum)}";
                }

                if (m_bIsDispose) return;
                BridgeResult m_pBridgeResult = await m_pOutboundSocket.Bridge(uuid, m_sEndPointStrB, new BridgeOptions()
                {
                    UUID = bridgeUUID,
                    CallerIdNumber = m_mRecord.T_PhoneNum,
                    CallerIdName = m_sCallerIdName,
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
                            Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Bridge cancel]");
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
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Bridge error:{ex.Message}]");
                        return null;
                    }
                });

                if (m_pBridgeResult == null || (m_pBridgeResult != null && !m_pBridgeResult.Success))
                {
                    string m_sBridgeResultStr = m_pBridgeResult?.ResponseText;
                    if (string.IsNullOrWhiteSpace(m_sBridgeResultStr)) m_sBridgeResultStr = null;

                    string m_sMsgStr = $"Bridge fail:{m_sBridgeResultStr ?? "unknow"}";
                    Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} {m_sMsgStr},play prompt music]");

                    DateTime m_dtEndTimeNow = DateTime.Now;
                    string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_mRecord.C_StartTime);
                    m_mRecord.C_EndTime = m_sEndTimeNowString;
                    m_mRecord.CallType = m_bStar ? 8 : 4;

                    #region 判断电话结果
                    string m_sPlayMusic = string.Empty;
                    if (string.IsNullOrWhiteSpace(m_sBridgeResultStr))
                    {
                        m_mRecord.CallResultID = m_bStar ? 43 : 16;
                        m_sPlayMusic = m_mPlay.m_mNoAnswerMusic;
                    }
                    else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "NOANSWER"))
                    {
                        m_mRecord.CallResultID = m_bStar ? 43 : 16;
                        m_sPlayMusic = m_mPlay.m_mNoAnswerMusic;
                    }
                    else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "BUSY"))
                    {
                        m_mRecord.CallResultID = m_bStar ? 45 : 18;
                        m_sPlayMusic = m_mPlay.m_mBusyMusic;
                    }
                    else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "INVALIDARGS"))
                    {
                        if (m_mChannel.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP)
                        {
                            m_mRecord.CallResultID = m_bStar ? 50 : 49;
                        }
                        else
                        {
                            m_mRecord.CallResultID = m_bStar ? 52 : 51;
                            m_sPlayMusic = m_mPlay.m_mUnavailableMusic;
                        }
                    }
                    else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "USER_NOT_REGISTERED"))
                    {
                        m_mRecord.CallResultID = m_bStar ? 43 : 16;
                        m_sPlayMusic = m_mPlay.m_mNoRegisteredMusic;
                    }
                    else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "SUBSCRIBER_ABSENT"))
                    {
                        m_mRecord.CallResultID = m_bStar ? 43 : 16;
                    }
                    else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "NO_USER_RESPONSE"))
                    {
                        m_mRecord.CallResultID = m_bStar ? 50 : 49;
                        m_sPlayMusic = m_mPlay.m_mUnavailableMusic;
                    }
                    else
                    {
                        m_mRecord.CallResultID = m_bStar ? 43 : 16;
                        m_sPlayMusic = m_mPlay.m_mNoAnswerMusic;

                    }
                    #endregion

                    #region 播放提示音
                    if (!string.IsNullOrWhiteSpace(m_sPlayMusic))
                    {
                        #region ***应答尝试放音,目前只有这种方式可以
                        if (m_uPlayLoops > 0)
                        {
                            if (m_sAnswer == "uuid_answer")
                            {
                                if (m_bIsDispose) return;
                                await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                                    }
                                });
                            }

                            for (int i = 0; i < m_uPlayLoops; i++)
                            {
                                if (m_bIsDispose) return;
                                await m_pOutboundSocket.Play(uuid, m_sPlayMusic).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play prompt music cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Play prompt music error:{ex.Message}]");
                                    }
                                });
                            }
                        }
                        #endregion
                    }
                    #endregion

                    if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                    {
                        m_sWhoHangUpStr = "B";

                        m_mRecord.Uhandler = 0;
                        m_mRecord.Remark = m_sMsgStr;

                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} insert record]");
                        call_record.Insert(m_mRecord);

                        //注册含义-1:IP话机Web调用
                        if (m_bWeb)
                            m_fCallClass.m_fIpEndCall(m_uAgentID, m_sLoginName);

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
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup error:{ex.Message}]");
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
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} exit error:{ex.Message}]");
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
                    DateTime m_dtAnswerTimeNow = DateTime.Now;
                    string m_sAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtAnswerTimeNow);
                    m_mRecord.C_AnswerTime = m_sAnswerTimeNowString;
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtAnswerTimeNow, m_dtStartTimeNow);

                    //被叫挂断
                    if (m_bIsDispose) return;
                    m_pOutboundSocket.OnHangup(bridgeUUID, async bx =>
                    {
                        if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                        {
                            m_sWhoHangUpStr = "B";
                            Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} b-leg hangup]");

                            DateTime m_dtEndTimeNow = DateTime.Now;
                            string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                            m_mRecord.C_EndTime = m_sEndTimeNowString;
                            m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtAnswerTimeNow);
                            m_mRecord.CallType = m_bStar ? 7 : 3;
                            m_mRecord.CallResultID = m_bStar ? 41 : 14;

                            call_record.Insert(m_mRecord);
                            Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} insert record]");

                            //注册含义-1:IP话机Web调用
                            if (m_bWeb)
                                m_fCallClass.m_fIpEndCall(m_uAgentID, m_sLoginName);

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
                                        if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Hangup error:{ex.Message}]");
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
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{m_uAgentID} Exit error:{ex.Message}]");
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
                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][Exception][{uuid} unfinished error:{ex.Message}]");
                Log.Instance.Debug(ex);

                if (m_mChannel != null)
                {
                    m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                    m_mChannel.channel_call_uuid = null;
                    m_mChannel.channel_call_other_uuid = null;
                }

                //注册含义-1:IP话机Web调用
                if (m_bWeb)
                    m_fCallClass.m_fIpEndCall(m_uAgentID, m_sLoginName);

                if (m_bIsDispose) return;
                if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                {
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.NormalClearing).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Hangup cancel]");
                        }
                        catch (Exception eex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Hangup error:{eex.Message}]");
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
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Exit cancel]");
                        }
                        catch (Exception eex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fCall][{uuid} Exit error:{eex.Message}]");
                        }
                    });
                }

                if (m_bIsDispose) return;
                m_pOutboundSocket?.Dispose();
            }
        }

        private static void m_fSendRecordingID(ChannelInfo m_mChannel, string m_sPhoneNumberStr, string m_sRecordingID, string m_sRecordingFile = "")
        {
            try
            {
                string m_sMsgStr = call_socketcommand_util.SendCommonStr("FSLDHM", $"{m_sPhoneNumberStr},{m_sRecordingID},{m_sRecordingFile}");
                m_mChannel.channel_websocket.Send(m_sMsgStr);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fSendRecordingID][Exception][{ex.Message}]");
            }
        }

        private static void m_fSendObject(IWebSocketConnection m_pWebSocket, object m_oObject, int m_uAgentID, string m_sRecordingID)
        {
            try
            {
                if (m_pWebSocket != null)
                {
                    m_pWebSocket.Send(Newtonsoft.Json.JsonConvert.SerializeObject(m_oObject));
                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fSendObject][{m_uAgentID} record ID:{m_sRecordingID} send]");
                }
                else
                    Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fSendObject][{m_uAgentID} record ID:{m_sRecordingID} send fail:no WebSocket]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fSendObject][Exception][{ex.Message}]");
            }
        }

        private static void m_fIpCall(int m_uAgentID, string m_sLoginName, string m_sCaller, string m_sCallTime, string m_sRecID)
        {
            try
            {
                string m_sSQL = $@"
INSERT INTO `call_skip` ( `loginname`, `caller`, `calltime`, `recordid`, `isdel` )
VALUES
	( '{m_sLoginName}', '{m_sCaller}', '{m_sCallTime}', '{m_sRecID}', 0 ) 
	ON DUPLICATE KEY UPDATE `call_skip`.`caller` = '{m_sCaller}',
	`call_skip`.`calltime` = '{m_sCallTime}',
	`call_skip`.`recordid` = '{m_sRecID}',
	`call_skip`.`isdel` = 0;
";
                if (MySQL_Method.ExecuteNonQuery(m_sSQL) > 0)
                {
                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fIpCall][{m_uAgentID} insert or update call skip success]");
                    return;
                }
                Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fIpCall][{m_uAgentID} insert or update call skip 0 rows]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fIpCall][Exception][{ex.Message}]");
            }
        }

        private static void m_fIpEndCall(int m_uAgentID, string m_sLoginName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(m_sLoginName))
                    return;

                string m_sSQL = $@"
UPDATE `call_skip` 
SET `call_skip`.`isdel` = 1 
WHERE
	`call_skip`.`loginname` = '{m_sLoginName}' 
	AND `call_skip`.`isdel` = 0;
";
                if (MySQL_Method.ExecuteNonQuery(m_sSQL) > 0)
                {
                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fIpEndCall][{m_uAgentID} delete call skip success]");
                    return;
                }
                Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fIpEndCall][{m_uAgentID} delete call skip 0 rows]");
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fIpEndCall][Exception][{ex.Message}]");
            }
        }

        ///<![CDATA[
        /// <5>共享号码呼入处理方法
        ///]]>
        private static async void m_fShareCall(OutboundSocket m_pOutboundSocket, string m_sCalleeNumberStr, bool m_bStar, List<string> m_lStrings, string m_sRealCallerNumberStr, string m_sEndPointStrB, int m_uShare, Model_v1.AddRecByRec m_pAddRecByRec, Model_v1.share_number m_pShareNumber, AGENT_INFO m_mTheAgent, int m_qInCall, int m_uPlayLoops, string m_sAnswer, string m_sCallMusic)
        {
            #region ***是否内呼,内呼改变一下该通道的状态,挂断时杀死对应通道
            int m_uTheAgentID = -1;
            ChannelInfo m_mTheChannel = null;
            if (m_mTheAgent != null)
            {
                m_uTheAgentID = m_mTheAgent.AgentID;
                m_mTheChannel = m_mTheAgent.ChInfo;
            }
            #endregion

            string uuid = m_pOutboundSocket.ChannelData.UUID;
            bool m_bIsDispose = false;
            dial_area m_pDialArea = null;
            int m_uAgentID = -1;
            string m_sLogUUID = uuid;
            //是否执行了Redis锁删除
            bool m_bDeleteRedisLock = false;
            //真实号码
            string m_stNumberStr = string.Empty;

            try
            {
                string m_sFreeSWITCHIPv4 = m_pAddRecByRec?.m_sFreeSWITCHIPv4;
                m_sLogUUID = $"{m_sFreeSWITCHIPv4} {(m_uAgentID == -1 ? uuid : m_uAgentID.ToString())}";

                m_pOutboundSocket.Disposed += (a, b) =>
                {
                    m_bIsDispose = true;
                    Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} outbound socket dispose]");

                    if (!m_bDeleteRedisLock)
                    {
                        m_bDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                    }

                    ///兼容一下通道强断
                    if (m_qInCall == 1)
                    {
                        if (m_mTheChannel != null && m_mTheChannel.channel_call_status != APP_USER_STATUS.FS_USER_IDLE)
                        {
                            Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} reset when dispose]");
                            m_mTheChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mTheChannel.channel_call_uuid = null;
                            m_mTheChannel.channel_call_uuid_after = null;
                            m_mTheChannel.channel_call_other_uuid = null;
                        }
                    }
                };

                ///兼容一下录音即可,不要生成重复录音即可
                if (m_uShare > 10)
                {
                    m_pDialArea = new dial_area();
                    m_pDialArea.id = -1;
                    m_pDialArea.aname = string.Empty;
                    m_pDialArea.aip = m_sFreeSWITCHIPv4;
                    m_pDialArea.aport = 3306;
                    m_pDialArea.adb = "cmcp10";
                    m_pDialArea.auid = "root";
                    m_pDialArea.apwd = "123";
                    m_pDialArea.amain = 2;
                    m_pDialArea.astate = 2;
                }
                else
                    m_pDialArea = Redis2.m_fGetDialAreaByIPv4(m_pAddRecByRec?.m_sFreeSWITCHIPv4);

                //通话记录中通道ID
                int m_uChannelID = m_pAddRecByRec?.m_uChannelID == null ? -1 : m_pAddRecByRec.m_uChannelID;
                //通话记录中坐席ID
                m_uAgentID = m_pAddRecByRec?.m_uAgentID == null ? -1 : m_pAddRecByRec.m_uAgentID;
                //-1处理
                if (m_uAgentID == -1) m_uAgentID = m_pAddRecByRec?.m_uFromAgentID == null ? -1 : m_pAddRecByRec.m_uFromAgentID;
                //查询坐席姓名及坐席登录名
                string m_sFromAgentName = null;
                string m_sFromLoginName = null;
                string m_sConnStr = MySQLDBConnectionString.m_fConnStr(m_pDialArea);
                DB.Basic.call_record.m_fGetShareFromName(m_uAgentID, m_sConnStr, out m_sFromAgentName, out m_sFromLoginName);
                call_record_model m_mRecord = new call_record_model(m_uChannelID);
                m_mRecord.AgentID = m_uAgentID;
                m_mRecord.FreeSWITCHIPv4 = m_sFreeSWITCHIPv4;
                m_mRecord.UAID = m_pAddRecByRec?.UAID;
                m_mRecord.fromagentname = m_sFromAgentName;
                m_mRecord.fromloginname = m_sFromLoginName;
                m_mRecord.LocalNum = m_sCalleeNumberStr;

                ///非专线类别判断及赋值
                if (m_uShare == 1) m_mRecord.isshare = 1;
                if (m_uShare == 11) m_mRecord.isshare = 2;

                if (m_bStar)
                {
                    m_mRecord.PhoneAddress = "内呼";
                    m_mRecord.T_PhoneNum = $"{Special.Star}{m_sRealCallerNumberStr}";
                    m_mRecord.C_PhoneNum = m_mRecord.T_PhoneNum;
                }
                else
                {
                    m_mRecord.PhoneAddress = m_lStrings[3];
                    m_mRecord.T_PhoneNum = m_sRealCallerNumberStr;
                    m_mRecord.C_PhoneNum = m_sRealCallerNumberStr;
                    //真实号码赋值
                    m_stNumberStr = m_pShareNumber.tnumber;
                    //录音中真实号码赋值
                    m_mRecord.tnumber = m_stNumberStr;
                }

                #region ***无需接通直接挂断即可
                ///<![CDATA[
                /// <6>如果进入此,共享号码已经进行了呼入锁定,使用完成后需要解锁
                /// 共享号码未锁定成功,此时直接添加来电记录并挂断即可
                /// 后续部分也不判断是否找到内转ID,因为要做本机任何线路的来电通道的状态变更
                /// ]]>
                if (m_pAddRecByRec.inlimit_2id == -1 &&///必须未找到呼叫内转信息,前置
                    ((m_uShare > 1 && m_uShare <= 10) || (m_uShare > 11 && m_uShare <= 20)
                    || m_qInCall == -1//反查不到坐席ID
                    || (m_qInCall == 1 && (m_mTheChannel == null || m_mTheChannel?.channel_type != Special.SIP)))//坐席通道信息有误
                    )
                //if (m_uShare > 1 || m_uShare > 11)
                {
                    string m_sMsgStr = string.Empty;
                    #region ***错误信息提示分支,暂时不放音,因为概率很小
                    if (m_qInCall == -1)
                    {
                        m_sMsgStr = "miss a leg info";
                        Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID},{m_sMsgStr}]");
                    }
                    else
                    {
                        if ((m_qInCall == 1 && (m_mTheChannel == null || m_mTheChannel?.channel_type != Special.SIP)))
                        {
                            m_sMsgStr = $"miss channel or not sip channel:{m_mTheChannel?.channel_type}";
                            Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID},{m_sMsgStr}]");
                        }
                        else
                        {
                            m_sMsgStr = $"lock share number fail:{m_uShare}";
                            Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID},{m_sMsgStr}]");
                        }
                    }
                    #endregion

                    m_mRecord.CallType = m_bStar ? 8 : 4;
                    m_mRecord.CallResultID = m_bStar ? 45 : 18;
                    m_mRecord.C_EndTime = Cmn.m_fDateTimeString();
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_mRecord.C_EndTime, m_mRecord.C_StartTime);
                    m_mRecord.Uhandler = 0;
                    m_mRecord.Remark = m_sMsgStr;

                    if (!m_bDeleteRedisLock)
                    {
                        m_bDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                    }

                    call_record.Insert(m_mRecord, true, m_pDialArea, true);
                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} insert record]");

                    #region ***呼入,这里直接杀死,不再挂断,防止Freeswitch内存泄漏
                    if (false)
                    {
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.Hangup(uuid, HangupCause.UserNotRegistered).ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Hangup cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Hangup error:{ex.Message}]");
                            }
                        });
                    }
                    #endregion

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.SendApi($"uuid_kill {uuid}").ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} uuid_kill cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} uuid_kill error:{ex.Message}]");
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
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                #region ***呼叫内转逻辑
                ///得到是否有呼叫内转的线路
                m_mInlimit_2 _m_mInlimit_2 = null;
                ///是否内转
                bool m_bInlimit = true;
                ///如果找到坐席,就要执行内转操作
                if (m_mTheAgent != null && !m_cInlimit_2.m_bInitInlimit_2 && m_cInlimit_2.m_lInlimit_2 != null && m_cInlimit_2.m_lInlimit_2.Count > 0)
                {
                    ///时间、星期的判断
                    DateTime m_pDateTime = DateTime.Now;
                    DayOfWeek m_sWeekday = m_pDateTime.DayOfWeek;
                    int m_uWeekday = (int)m_sWeekday;
                    if (m_uWeekday == 0) m_uWeekday = 7;
                    m_uWeekday = m_uWeekday - 1;
                    int m_uDay = (int)Math.Pow(2, m_uWeekday);

                    if (m_pAddRecByRec.inlimit_2id != -1)
                    {
                        _m_mInlimit_2 = m_cInlimit_2.m_lInlimit_2.Where(x => x.inlimit_2id == m_pAddRecByRec.inlimit_2id && ((x.inlimit_2whatday & m_uDay) > 0))?.FirstOrDefault();
                    }
                    ///假如通过ID未找到内转信息,如未配置,则找一下该坐席的其它内转信息
                    if (_m_mInlimit_2 == null)
                    {
                        Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} no inlimit_2 by:{m_pAddRecByRec.inlimit_2id},then by ua:{m_pAddRecByRec.m_uAgentID}]");
                        ///增加一个条件,可直接查询内转网关,随机一个没有写入
                        _m_mInlimit_2 = m_cInlimit_2.m_lInlimit_2.Where(x => (x.useuser == m_pAddRecByRec.m_uAgentID || !x.type) && ((x.inlimit_2whatday & m_uDay) > 0))?.FirstOrDefault();
                    }
                    ///得到内转信息,配置内转表达式
                    if (_m_mInlimit_2 != null)
                    {

                        ///查看类型,如果为内转线路,则优先级最高
                        ///其次为内转网关,这里内转网关如果需要自动加拨前缀就不能做了,需要自己通过网关的规则变换来实现
                        ///最后坐席自身设定

                        string inlimit_2starttime = _m_mInlimit_2.inlimit_2starttime;
                        string inlimit_2endtime = _m_mInlimit_2.inlimit_2endtime;
                        string inlimit_2number = _m_mInlimit_2.inlimit_2number;
                        if (!_m_mInlimit_2.type)
                        {
                            ///如果该坐席开启了内转,且符合星期
                            if (m_mTheAgent.isinlimit_2 && (m_mTheAgent.inlimit_2whatday & m_uDay) > 0)
                            {
                                inlimit_2starttime = m_mTheAgent.inlimit_2starttime;
                                inlimit_2endtime = m_mTheAgent.inlimit_2endtime;

                                ///这里还想兼容网关的时间设定,但是优先级问题需要考虑一下
                                ///先去掉,界面也暂时不能操作

                                inlimit_2number = m_mTheAgent.inlimit_2number;
                            }
                            else
                            {
                                ///如果路由到内转网关但坐席没有设置内转,跳过
                                m_bInlimit = false;
                            }
                        }
                        if (string.IsNullOrWhiteSpace(inlimit_2number)) m_bInlimit = false;

                        ///如果符合内转规则
                        if (m_bInlimit)
                        {
                            m_bInlimit = false;

                            DateTime m_dtStart = Convert.ToDateTime(m_pDateTime.ToString($"yyyy-MM-dd {_m_mInlimit_2.inlimit_2starttime}"));
                            DateTime m_dtEnd = Convert.ToDateTime(m_pDateTime.ToString($"yyyy-MM-dd {_m_mInlimit_2.inlimit_2endtime}"));
                            int m_uBs = DateTime.Compare(m_dtStart, m_dtEnd);
                            ///如果相等,代表全天
                            if (m_uBs == 0) m_bInlimit = true;
                            else
                            {
                                if (m_uBs > 0) m_dtEnd = m_dtEnd.AddDays(1);
                                ///判断时间
                                if (DateTime.Compare(m_dtStart, m_pDateTime) <= 0 && DateTime.Compare(m_dtEnd, m_pDateTime) > 0)
                                    m_bInlimit = true;
                                else Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} not time]");
                            }
                        }
                        ///如果符合内转规则
                        if (m_bInlimit)
                        {
                            if (_m_mInlimit_2.m_bGatewayType)
                                m_sEndPointStrB = $"sofia/gateway/{_m_mInlimit_2.m_sGatewayNameStr}/{inlimit_2number}";
                            else
                                m_sEndPointStrB = $"sofia/{_m_mInlimit_2.m_sGatewayType}/sip:{inlimit_2number}@{_m_mInlimit_2.m_sGatewayNameStr}";

                            ///打印呼叫转移日志
                            Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} b-leg-endpoint:{m_sEndPointStrB}]");
                            ///归属地增加内转标记
                            m_mRecord.PhoneAddress = $"{m_mRecord.PhoneAddress} 转移:{inlimit_2number}";
                        }
                    }
                    else Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} no inlimit_2]");
                }
                #endregion

                ///记录一下呼入了哪个坐席,方便查错
                Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} -> {m_uTheAgentID}]");

                #region 未连接(IP话机的引入,可以没有WebSocket,越过此处,后续增加一个判定)
                ///如果呼入为本机坐席
                if (m_qInCall == 1 && m_mTheChannel.IsRegister == 1 && m_mTheChannel.channel_websocket == null && m_pAddRecByRec.inlimit_2id == -1)
                {
                    string m_sMsgStr = $"user no connect";
                    Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uTheAgentID} {m_sMsgStr}]");

                    #region 播放提示音
                    if (m_uPlayLoops > 0)
                    {
                        //应答播放声音
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} {m_sAnswer} cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                            }
                        });

                        for (int i = 0; i < m_uPlayLoops; i++)
                        {
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.Play(uuid, m_mPlay.m_mNotConnectedMusic).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} Play link error music cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} Play link error music error:{ex.Message}]");
                                }
                            });
                        }
                    }
                    #endregion

                    m_mRecord.CallType = m_bStar ? 8 : 4;
                    m_mRecord.CallResultID = m_bStar ? 45 : 18;
                    m_mRecord.C_EndTime = Cmn.m_fDateTimeString();
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_mRecord.C_EndTime, m_mRecord.C_StartTime);
                    m_mRecord.Uhandler = 0;
                    m_mRecord.Remark = m_sMsgStr;

                    if (!m_bDeleteRedisLock)
                    {
                        m_bDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                    }

                    call_record.Insert(m_mRecord, true, m_pDialArea, true);
                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uTheAgentID} insert record]");

                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.UserNotRegistered).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uTheAgentID} Hangup cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uTheAgentID} Hangup error:{ex.Message}]");
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
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uTheAgentID} Exit cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uTheAgentID} Exit error:{ex.Message}]");
                            }
                        });
                    }

                    if (m_bIsDispose) return;
                    m_pOutboundSocket?.Dispose();

                    return;
                }
                #endregion

                #region 繁忙
                ///如果呼入为本机坐席
                if (m_qInCall == 1)
                {
                    if (
                        m_mTheChannel.channel_call_status != APP_USER_STATUS.FS_USER_IDLE &&
                        m_mTheChannel.channel_call_status != APP_USER_STATUS.FS_USER_AHANGUP &&
                        m_mTheChannel.channel_call_status != APP_USER_STATUS.FS_USER_BHANGUP
                        )
                    {
                        string m_sMsgStr = $"busy";
                        Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} {m_sMsgStr},play busy music]");

                        #region 播放提示音
                        string m_sPlayMusic = m_mPlay.m_mBusyMusic;
                        if (m_uPlayLoops > 0)
                        {
                            //应答播放声音
                            if (m_bIsDispose) return;
                            await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} {m_sAnswer} cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                                }
                            });

                            for (int i = 0; i < m_uPlayLoops; i++)
                            {
                                if (m_bIsDispose) return;
                                await m_pOutboundSocket.Play(uuid, m_sPlayMusic).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} Play busy music cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} Play busy music error:{ex.Message}]");
                                    }
                                });
                            }
                        }
                        #endregion

                        m_mRecord.CallType = m_bStar ? 8 : 4;
                        m_mRecord.CallResultID = m_bStar ? 45 : 18;
                        m_mRecord.C_EndTime = Cmn.m_fDateTimeString();
                        m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_mRecord.C_EndTime, m_mRecord.C_StartTime);
                        m_mRecord.Uhandler = 0;
                        m_mRecord.Remark = m_sMsgStr;

                        if (!m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                        }

                        call_record.Insert(m_mRecord, true, m_pDialArea, true);
                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} insert record]");

                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.Hangup(uuid, HangupCause.UserBusy).ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} Hangup cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} Hangup error:{ex.Message}]");
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
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} Exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_uAgentID} Exit error:{ex.Message}]");
                                }
                            });
                        }

                        if (m_bIsDispose) return;
                        m_pOutboundSocket?.Dispose();

                        return;
                    }
                }
                #endregion

                #region ***设定状态、ID,兼容强断
                ///如果呼入为本机坐席
                if (m_qInCall == 1)
                {
                    m_mTheChannel.channel_call_uuid_after = uuid;
                    m_mTheChannel.channel_call_uuid = uuid;
                    m_mTheChannel.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
                }
                #endregion

                #region ***设置183或者200,后续无需再设置
                if (m_sAnswer == "uuid_pre_answer")
                {
                    if (!string.IsNullOrWhiteSpace(m_sCallMusic))
                    {
                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} set ringback]");
                        //修正无法解析变量的问题,这里先写成该固定参数即可
                        string m_sData = $"ringback={m_sCallMusic}";
                        //设置183铃声
                        if (m_bIsDispose) return;
                        await m_pOutboundSocket.ExecuteApplication(uuid, "set", m_sData).ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} set ringback cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} set ringback error:{ex.Message}]");
                            }
                        });
                    }

                    Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sAnswer} {uuid}]");
                    //早期应答
                    if (m_bIsDispose) return;
                    await m_pOutboundSocket.SendApi($"{m_sAnswer} {uuid}").ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} {m_sAnswer} cancel]");
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{uuid} {m_sAnswer} error:{ex.Message}]");
                        }
                    });
                }
                #endregion

                string m_sApplicationStr = Call_ParamUtil._application;
                int m_uTimeoutSeconds = Call_ParamUtil.__timeout_seconds;
                bool m_bIgnoreEarlyMedia = Call_ParamUtil.__ignore_early_media;
                string m_sExtensionStr = Call_ParamUtil._rec_t;
                string m_sWhoHangUpStr = string.Empty;
                bool m_bIsLinked = false;
                DateTime m_dtStartTimeNow = DateTime.Now;
                string m_sStartTimeNowString = Cmn.m_fDateTimeString(m_dtStartTimeNow);

                if (m_bIsDispose) return;
                await m_pOutboundSocket.Linger().ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Linger cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Linger error:{ex.Message}]");
                    }
                });

                if (m_bIsDispose) return;
                m_pOutboundSocket.OnHangup(uuid, async ax =>
                {
                    if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                    {
                        m_sWhoHangUpStr = "A";
                        Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} a-leg hangup]");

                        DateTime m_dtEndTimeNow = DateTime.Now;
                        string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                        m_mRecord.C_EndTime = m_sEndTimeNowString;

                        if (m_bIsLinked)
                        {
                            m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_mRecord.C_AnswerTime);
                            m_mRecord.CallType = m_bStar ? 7 : 3;
                            m_mRecord.CallResultID = m_bStar ? 42 : 15;
                            m_mRecord.Uhandler = 1;
                        }
                        else
                        {
                            m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                            m_mRecord.CallType = m_bStar ? 8 : 4;
                            m_mRecord.CallResultID = m_bStar ? 52 : 51;
                            m_mRecord.Uhandler = 0;
                        }

                        if (!m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                        }

                        call_record.Insert(m_mRecord, true, m_pDialArea, true);
                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} insert record]");

                        ///如果呼入为本机坐席
                        if (m_qInCall == 1)
                        {
                            m_mTheChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mTheChannel.channel_call_uuid = null;
                            m_mTheChannel.channel_call_uuid_after = null;
                            m_mTheChannel.channel_call_other_uuid = null;
                        }

                        if (m_bIsDispose) return;
                        if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                        {
                            await m_pOutboundSocket.Exit().ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Exit error:{ex.Message}]");
                                }
                            });
                        }

                        if (m_bIsDispose) return;
                        m_pOutboundSocket?.Dispose();
                    }
                });

                string bridgeUUID = Guid.NewGuid().ToString();
                ///如果呼入为本机坐席
                if (m_qInCall == 1)
                {
                    m_mTheChannel.channel_call_other_uuid = bridgeUUID;
                    m_mTheChannel.channel_call_status = APP_USER_STATUS.FS_USER_RINGING;
                }

                #region 录音参数设置
                if (m_bIsDispose) return;
                await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_ARTIST", "'Opex Hosting Ltd'").ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} RECORD_ARTIST cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} RECORD_ARTIST error:{ex.Message}]");
                    }
                });
                if (m_bIsDispose) return;
                await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_MIN_SEC", 0).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} RECORD_MIN_SEC cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} RECORD_MIN_SEC error:{ex.Message}]");
                    }
                });
                if (m_bIsDispose) return;
                await m_pOutboundSocket.SetChannelVariable(uuid, "RECORD_STEREO", "true").ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} RECORD_STEREO cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} RECORD_STEREO error:{ex.Message}]");
                    }
                });
                #endregion

                #region 开始录音
                DateTime m_dtNow = DateTime.Now;
                //录音中来电主叫或去掉被叫的真实号码获取
                string m_sRectNumberStr = (!string.IsNullOrWhiteSpace(m_stNumberStr) ? m_stNumberStr : m_mRecord.LocalNum);
                string m_sRecSub = $"{{0}}\\{m_dtNow.ToString("yyyy")}\\{m_dtStartTimeNow.ToString("yyyyMM")}\\{m_dtNow.ToString("yyyyMMdd")}\\Rec_{m_dtNow.ToString("yyyyMMddHHmmss")}_{m_sRectNumberStr}_{(m_bStar ? "N" : "")}L_{m_mRecord.T_PhoneNum.Replace("*", "X")}{m_sExtensionStr}";
                string m_sRecordingFile = string.Format(m_sRecSub, ParamLib.RecordFilePath);
                string m_sRecordingFolder = Path.GetDirectoryName(m_sRecordingFile);
                if (!Directory.Exists(m_sRecordingFolder)) Directory.CreateDirectory(m_sRecordingFolder);
                string m_sRecordingID = Path.GetFileNameWithoutExtension(m_sRecordingFile);
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
                                Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} record cancel]");
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
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} record error:{ex.Message}]");
                            return null;
                        }
                    });
                    if (m_pRecordingResult != null && m_pRecordingResult.Success)
                    {
                        m_mRecord.RecordFile = m_sRecordingFile;
                        m_mRecord.recordName = m_sRecordingID;
                    }
                    else
                    {
                        m_sRecordingID = string.Empty;
                        Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} record fail]");
                    }
                }
                #endregion

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
                                Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} record cancel]");
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
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} record error:{ex.Message}]");
                            return null;
                        }
                    });
                    if (m_pRecordingResult != null && m_pRecordingResult.Success)
                    {
                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} back up record success:{m_sBackUpRecordingFile}]");
                    }
                    else
                    {
                        Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} back up record fail]");
                    }
                }
                #endregion

                ///To拼接至主叫名称:0不开、1全开、2注册开
                string m_sCallerIdName = m_mRecord.T_PhoneNum;
                if (Call_ParamUtil.m_uAppendTo == 1 || (Call_ParamUtil.m_uAppendTo == 2 && m_mTheChannel != null && m_mTheChannel.IsRegister == 1))
                {
                    m_sCallerIdName = $"{m_mRecord.T_PhoneNum}To{(!string.IsNullOrWhiteSpace(m_mRecord.tnumber) ? m_mRecord.tnumber : m_mRecord.LocalNum)}";
                }

                if (m_bIsDispose) return;
                BridgeResult m_pBridgeResult = await m_pOutboundSocket.Bridge(uuid, m_sEndPointStrB, new BridgeOptions()
                {
                    UUID = bridgeUUID,
                    CallerIdNumber = m_mRecord.T_PhoneNum,
                    CallerIdName = m_sCallerIdName,
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
                            Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Bridge cancel]");
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
                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Bridge error:{ex.Message}]");
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
                        Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} {m_sMsgStr},play prompt music]");

                        DateTime m_dtEndTimeNow = DateTime.Now;
                        string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                        m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_mRecord.C_StartTime);
                        m_mRecord.C_EndTime = m_sEndTimeNowString;
                        m_mRecord.CallType = m_bStar ? 8 : 4;

                        #region 判断电话结果
                        string m_sPlayMusic = string.Empty;
                        if (string.IsNullOrWhiteSpace(m_sBridgeResultStr))
                        {
                            m_mRecord.CallResultID = m_bStar ? 43 : 16;
                            m_sPlayMusic = m_mPlay.m_mNoAnswerMusic;
                        }
                        else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "NOANSWER"))
                        {
                            m_mRecord.CallResultID = m_bStar ? 43 : 16;
                            m_sPlayMusic = m_mPlay.m_mNoAnswerMusic;
                        }
                        else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "BUSY"))
                        {
                            m_mRecord.CallResultID = m_bStar ? 45 : 18;
                            m_sPlayMusic = m_mPlay.m_mBusyMusic;
                        }
                        else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "INVALIDARGS"))
                        {
                            m_mRecord.CallResultID = m_bStar ? 50 : 49;
                            m_sPlayMusic = m_mPlay.m_mUnavailableMusic;
                        }
                        else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "USER_NOT_REGISTERED"))
                        {
                            m_mRecord.CallResultID = m_bStar ? 43 : 16;
                            m_sPlayMusic = m_mPlay.m_mNoRegisteredMusic;
                        }
                        else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "SUBSCRIBER_ABSENT"))
                        {
                            m_mRecord.CallResultID = m_bStar ? 43 : 16;
                        }
                        else if (Cmn.IgnoreEquals(m_sBridgeResultStr, "NO_USER_RESPONSE"))
                        {
                            m_mRecord.CallResultID = m_bStar ? 50 : 49;
                            m_sPlayMusic = m_mPlay.m_mUnavailableMusic;
                        }
                        else
                        {
                            m_mRecord.CallResultID = m_bStar ? 43 : 16;
                            m_sPlayMusic = m_mPlay.m_mNoAnswerMusic;

                        }
                        #endregion

                        m_mRecord.Uhandler = 0;
                        m_mRecord.Remark = m_sMsgStr;

                        if (!m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                        }

                        Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} insert record]");
                        call_record.Insert(m_mRecord, true, m_pDialArea, true);

                        ///如果呼入为本机坐席
                        if (m_qInCall == 1)
                        {
                            m_mTheChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mTheChannel.channel_call_uuid = null;
                            m_mTheChannel.channel_call_uuid_after = null;
                            m_mTheChannel.channel_call_other_uuid = null;
                        }

                        if (m_bIsDispose) return;
                        if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                        {
                            await m_pOutboundSocket.Hangup(uuid, HangupCause.OriginatorCancel).ContinueWith(task =>
                            {
                                try
                                {
                                    if (m_bIsDispose) return;
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Hangup cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Hangup error:{ex.Message}]");
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
                                    if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} exit error:{ex.Message}]");
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

                    ///如果呼入为本机坐席
                    if (m_qInCall == 1)
                    {
                        m_mTheChannel.channel_call_status = APP_USER_STATUS.FS_USER_TALKING;
                    }
                    DateTime m_dtAnswerTimeNow = DateTime.Now;
                    string m_sAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtAnswerTimeNow);
                    m_mRecord.C_AnswerTime = m_sAnswerTimeNowString;
                    m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtAnswerTimeNow, m_dtStartTimeNow);

                    //被叫挂断
                    if (m_bIsDispose) return;
                    m_pOutboundSocket.OnHangup(bridgeUUID, async bx =>
                    {
                        if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                        {
                            m_sWhoHangUpStr = "B";
                            Log.Instance.Warn($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} b-leg hangup]");

                            DateTime m_dtEndTimeNow = DateTime.Now;
                            string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                            m_mRecord.C_EndTime = m_sEndTimeNowString;
                            m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtAnswerTimeNow);
                            m_mRecord.CallType = m_bStar ? 7 : 3;
                            m_mRecord.CallResultID = m_bStar ? 41 : 14;

                            if (!m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                            }

                            call_record.Insert(m_mRecord, true, m_pDialArea, true, m_uShare > 10);
                            Log.Instance.Success($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} insert record]");

                            ///如果呼入为本机坐席
                            if (m_qInCall == 1)
                            {
                                m_mTheChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                                m_mTheChannel.channel_call_uuid = null;
                                m_mTheChannel.channel_call_uuid_after = null;
                                m_mTheChannel.channel_call_other_uuid = null;
                            }

                            if (m_bIsDispose) return;
                            if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                            {
                                await m_pOutboundSocket.Hangup(uuid, HangupCause.NormalClearing).ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Hangup cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Hangup error:{ex.Message}]");
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
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Exit error:{ex.Message}]");
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
                Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][Exception][{m_sLogUUID} unfinished error:{ex.Message}]");
                Log.Instance.Debug(ex);

                if (!m_bDeleteRedisLock)
                {
                    m_bDeleteRedisLock = true;
                    Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                }

                ///兼容一下通道强断,如果呼入为本机坐席
                if (m_qInCall == 1)
                {
                    if (m_mTheChannel != null && m_mTheChannel.channel_call_status != APP_USER_STATUS.FS_USER_IDLE)
                    {
                        m_mTheChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        m_mTheChannel.channel_call_uuid = null;
                        m_mTheChannel.channel_call_uuid_after = null;
                        m_mTheChannel.channel_call_other_uuid = null;
                    }
                }

                if (m_bIsDispose) return;
                if (m_pOutboundSocket != null && m_pOutboundSocket.IsConnected)
                {
                    await m_pOutboundSocket.Hangup(uuid, HangupCause.NormalClearing).ContinueWith(task =>
                    {
                        try
                        {
                            if (m_bIsDispose) return;
                            if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Hangup cancel]");
                        }
                        catch (Exception eex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Hangup error:{eex.Message}]");
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
                            if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Exit cancel]");
                        }
                        catch (Exception eex)
                        {
                            Log.Instance.Error($"[CenoFsSharp][m_fCallClass][m_fShareCall][{m_sLogUUID} Exit error:{eex.Message}]");
                        }
                    });
                }

                if (m_bIsDispose) return;
                m_pOutboundSocket?.Dispose();
            }
        }
    }
}
