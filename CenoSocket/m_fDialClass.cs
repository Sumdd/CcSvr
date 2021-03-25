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
            ///白名单不受同号码限呼,0非1白名单
            int m_uWhiteList = 0;

            try
            {
                //后续对这里进行减压,不需要每次都进行查询,缓存起来即可
                string[] m_aSocketCmdArray = call_socketcommand_util.GetParamByHeadName("BDDH");
                m_uAgentID = Convert.ToInt32(SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[0]));
                string m_sRealPhoneNumberStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[1]);//真正
                string m_sPhoneNumberStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[2]);//未处理
                string m_sTypeNameStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[3]);
                bool m_bStar = m_sTypeNameStr == Special.Star;

                ///兼容原来的拨号命令,后续也做如此判断
                //归属地
                string m_sPhoneAddressStr = "未知";//默认归属地为未知
                if (m_aSocketCmdArray?.Length > 4) m_sPhoneAddressStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[4]);
                //区号
                string m_sCityCodeStr = string.Empty;//默认区号为空
                if (m_aSocketCmdArray?.Length > 5) m_sCityCodeStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[5]);
                //处理模式
                string m_sDealWithStr = Special.Complete;//默认处理完成
                if (m_aSocketCmdArray?.Length > 6) m_sDealWithStr = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[6]);
                //号码类别
                string m_sNumberType = Special.Common;//默认专线号码
                if (m_aSocketCmdArray?.Length > 7) m_sNumberType = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[7]);
                //类别UUUD
                string m_sTypeUUID = string.Empty;//默认空
                if (m_aSocketCmdArray?.Length > 8) m_sTypeUUID = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[8]);

                #region ***拨号Socket命令自定义数据
                ///自定义分割式数据|&
                string m_sUsrData = string.Empty;
                if (m_aSocketCmdArray?.Length > 9) m_sUsrData = SocketInfo.GetValueByKey(m_pHashtable, m_aSocketCmdArray[9]);
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
                ///是否为32位加密号码
                if (m_lUsrData.Length > 0) m_bQNRegexNumber = m_lUsrData[0] == "1";
                ///归属地详情直接传入
                if (m_lUsrData.Length > 1)
                {
                    string[] m_lPm = m_lUsrData[1].Split('&');
                    if (m_lPm.Length > 0) m_sDt = m_lPm[0];
                    if (m_lPm.Length > 1) m_sCardType = m_lPm[1];
                    if (m_lPm.Length > 2) m_sZipCode = m_lPm[2];
                }
                ///拨号前客户端向服务端发送唯一性ID
                string m_sRecUUID = string.Empty;
                if (m_lUsrData.Length > 2)
                {
                    m_sRecUUID = m_lUsrData[2];
                }
                ///后续追加拓展字段,存储入通话表数据库
                #endregion

                #region ***独立服务号码命令拓展
                string m_sGwName = string.Empty;
                string m_sGwIP = string.Empty;
                if (m_lUsrData.Length > 3)
                {
                    string[] m_lPm = m_lUsrData[3].Split('&');
                    if (m_lPm.Length > 0) m_sGwName = m_lPm[0];
                    if (m_lPm.Length > 1) m_sGwIP = m_lPm[1];

                    if (m_sNumberType == Special.ApiShare)
                    {
                        if (string.IsNullOrWhiteSpace(m_sGwName) || string.IsNullOrWhiteSpace(m_sGwIP))
                        {
                            Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} no number or ip]");
                            m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Api信息缺失"));
                            return;
                        }
                    }
                }
                #endregion

                #region ***客户端是否查询联系人姓名
                bool m_bName = false;
                if (m_lUsrData.Length > 4)
                {
                    ///当开启了查询催收系统获取联系人姓名且客户端未查询时,执行查询命令,减少压力
                    if (Call_ParamUtil.m_bUseHomeSearch && m_lUsrData[4] != "1") m_bName = true;
                }
                else
                {
                    ///如果未告知,只要开启了查询催收系统获取联系人姓名模式即查询
                    if (Call_ParamUtil.m_bUseHomeSearch) m_bName = true;
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

                #region ***增加单台验证和时间验证
                int m_uUseStatus = m_cModel.m_uUseStatus;
                if (m_uUseStatus > 0)
                {
                    Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID},error code:{m_uUseStatus}]");
                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail($"ErrCode{m_uUseStatus}"));
                    return;
                }
                #endregion

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

                    #region ***呼出只判断是否是黑名单,黑名单直接限制呼叫即可,如果更新中则暂时失效即可
                    if (!m_cWblist.m_bInitWblist && m_cWblist.m_lWblist?.Count > 0)
                    {
                        ///判断所有的黑名单即可
                        foreach (m_mWblist item in m_cWblist.m_lWblist)
                        {
                            ///兼容呼入呼出黑名单
                            if (item.wbtype == 2 && (item.wblimittype & 2) > 0)
                            {
                                if (item.regex.IsMatch(m_sDealWithRealPhoneNumberStr))
                                {
                                    Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} black list:{m_sDealWithRealPhoneNumberStr}]");
                                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err黑名单"));
                                    return;
                                }
                            }
                        }

                        ///判断所有的白名单,如果在白名单中,不受同号码限呼的限制
                        foreach (m_mWblist item in m_cWblist.m_lWblist)
                        {
                            ///兼容呼入呼出白名单
                            if (item.wbtype == 1 && (item.wblimittype & 2) > 0)
                            {
                                if (item.regex.IsMatch(m_sDealWithRealPhoneNumberStr))
                                {
                                    Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} white list:{m_sDealWithRealPhoneNumberStr}]");
                                    m_uWhiteList = 1;
                                    break;
                                }
                            }
                        }
                    }
                    #endregion

                    #region ***是否需要查询联系人姓名
                    if (m_bName) m_cEsySQL.m_fSetExpc(m_sDealWithRealPhoneNumberStr);
                    #endregion
                }

                int m_uCh = m_mAgent.ChInfo.nCh;
                m_mChannel = call_factory.channel_list[m_uCh];
                if (m_uCh == -1 || m_mChannel == null || m_mChannel?.channel_type != Special.SIP)
                {
                    Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} miss channel or not sip channel]");
                    m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Err通道有误"));
                    return;
                }

                #region ***判断前缀,支持特殊命令
                if (m_bStar)
                {
                    string m_sDtmfCmdMsg = string.Empty;
                    string[] m_lDfmfCmd = m_sDealWithRealPhoneNumberStr.Trim('*').Split(new string[] { "*" }, StringSplitOptions.RemoveEmptyEntries);
                    string m_sDtmfCmd = m_lDfmfCmd[0];
                    switch (m_sDtmfCmd)
                    {
                        case "70":///取消
                            {
                                if (m_fDialLimit.m_fSetUa2(m_uAgentID, "isinlimit_2", 0))
                                {
                                    m_mAgent.isinlimit_2 = false;
                                    m_sDtmfCmdMsg = "2取消OK";
                                }
                                else m_sDtmfCmdMsg = "2取消Err";
                            }
                            break;
                        case "71":///开启
                            {
                                if (m_lDfmfCmd.Length > 1)
                                {
                                    if (
                                        m_fDialLimit.m_fSetUa2(m_uAgentID, "isinlimit_2", 1) &&
                                        m_fDialLimit.m_fSetUa2(m_uAgentID, "inlimit_2number", m_lDfmfCmd[1])
                                        )
                                    {
                                        m_mAgent.isinlimit_2 = true;
                                        m_mAgent.inlimit_2number = m_lDfmfCmd[1];
                                        m_sDtmfCmdMsg = "2开启OK";
                                    }
                                    else m_sDtmfCmdMsg = "2开启Err";
                                }
                                else
                                {
                                    if (m_fDialLimit.m_fSetUa2(m_uAgentID, "isinlimit_2", 1))
                                    {
                                        m_mAgent.isinlimit_2 = true;
                                        m_sDtmfCmdMsg = "2开启OK";
                                    }
                                    else m_sDtmfCmdMsg = "2开启Err";
                                }
                            }
                            break;
                        case "77":///设定星期
                            {
                                if (m_lDfmfCmd.Length > 1)
                                {
                                    string m_s77 = m_lDfmfCmd[1];
                                    int m_u77 = 0;
                                    for (int i = 0; i < m_s77.Length; i++)
                                    {
                                        char item = m_s77[i];
                                        if (item >= 49 && item <= 55)
                                        {
                                            int val = (int)(Math.Pow(2, item - 48 - 1));
                                            if ((m_u77 & val) <= 0) m_u77 += val;
                                        }
                                    }
                                    if (m_fDialLimit.m_fSetUa2(m_uAgentID, "inlimit_2whatday", m_u77))
                                    {
                                        m_mAgent.inlimit_2whatday = m_u77;
                                        m_sDtmfCmdMsg = "2星期OK";
                                    }
                                    else m_sDtmfCmdMsg = "2星期Err";
                                }
                                else m_sDtmfCmdMsg = "2星期Err";
                            }
                            break;
                        case "78":///开始结束时间
                            {
                                if (m_lDfmfCmd.Length > 1)
                                {
                                    string m_sSE = m_lDfmfCmd[1];
                                    int[] m_lSE = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                                    if (m_sSE.Length == 8)
                                    {
                                        for (int i = 0; i < m_sSE.Length; i++)
                                        {
                                            char item = m_sSE[i];
                                            if (item >= 48 && item <= 57)
                                            {
                                                m_lSE[i] = item - 48;
                                            }
                                        }
                                        int m_uSH = m_lSE[0] * 10 + m_lSE[1];
                                        int m_uSM = m_lSE[2] * 10 + m_lSE[3];
                                        int m_uEH = m_lSE[4] * 10 + m_lSE[5];
                                        int m_uEM = m_lSE[6] * 10 + m_lSE[7];
                                        if (m_uSH >= 0 && m_uSH <= 23 && m_uEH >= 0 && m_uEH <= 23 && m_uSM >= 0 && m_uSM <= 59 && m_uEM >= 0 && m_uEM <= 59)
                                        {
                                            string m_sSHM = $"{m_uSH.ToString().PadLeft(2, '0')}:{m_uSM.ToString().PadLeft(2, '0')}:00";
                                            string m_sEHM = $"{m_uEH.ToString().PadLeft(2, '0')}:{m_uEM.ToString().PadLeft(2, '0')}:00";
                                            if (
                                                m_fDialLimit.m_fSetUa2(m_uAgentID, "inlimit_2starttime", m_sSHM) &&
                                                m_fDialLimit.m_fSetUa2(m_uAgentID, "inlimit_2endtime", m_sEHM)
                                                )
                                            {
                                                m_mAgent.inlimit_2starttime = m_sSHM;
                                                m_mAgent.inlimit_2endtime = m_sEHM;
                                                m_sDtmfCmdMsg = "2时间OK";
                                            }
                                            else m_sDtmfCmdMsg = "2时间Err";
                                        }
                                        else m_sDtmfCmdMsg = "2时间Err";
                                    }
                                    else m_sDtmfCmdMsg = "2时间Err";
                                }
                                else m_sDtmfCmdMsg = "2时间Err";
                            }
                            break;
                    }
                    if (!string.IsNullOrWhiteSpace(m_sDtmfCmdMsg))
                    {
                        Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} set inlimit_2:{m_sDtmfCmdMsg}]");
                        m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail(m_sDtmfCmdMsg));
                        return;
                    }
                }
                #endregion

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

                ///等待强断完成
                if (
                    (m_mChannel.channel_call_status == APP_USER_STATUS.FS_USER_AHANGUP ||
                    m_mChannel.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP)
                    )
                {
                    DateTime m_dtWait = DateTime.Now;
                    while (true)
                    {
                        if (m_mChannel.channel_call_status == APP_USER_STATUS.FS_USER_IDLE)
                            break;
                        if (((TimeSpan)(DateTime.Now - m_dtWait)).TotalSeconds > 3)
                            break;
                    }
                    if (m_mChannel.channel_call_status != APP_USER_STATUS.FS_USER_IDLE)
                    {
                        m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail("Warn请重拨"));
                        return;
                    }
                }

                ///状态拨号前
                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
                m_mChannel.channel_call_uuid_after = uuid;

                call_record_model m_mRecord = new call_record_model(m_mChannel.channel_id);
                m_mRecord.AgentID = m_uAgentID;
                m_mRecord.UAID = m_sUAID;
                m_mRecord.fromagentname = m_mAgent.AgentName;
                m_mRecord.fromloginname = m_mAgent.LoginName;
                m_mDialLimit _m_mDialLimit = null;
                string m_sCalleeNumberStr = m_sDealWithRealPhoneNumberStr;
                string m_sCalleeRemove0000Prefix = string.Empty;
                ///被叫的终点表达式提前
                string m_sEndPointStrB = string.Empty;

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
                    if (m_sNumberType == Special.ApiShare)
                    {
                        m_mRecord.LocalNum = m_sGwName;
                        m_mRecord.CallType = 1;
                        //保证号码真实性,确保可以直接回呼
                        m_mRecord.T_PhoneNum = m_sCalleeNumberStr;
                        m_mRecord.C_PhoneNum = m_sDealWithPhoneNumberStr;
                        m_mRecord.PhoneAddress = m_sPhoneAddressStr;
                    }
                    else
                    {
                        #region ***拨号限制,增加号码池逻辑
                        switch (m_sNumberType)
                        {
                            case Special.Common:
                                {
                                    ///增加白名单、坐席同号码限呼参数的传入
                                    _m_mDialLimit = m_fDialLimit.m_fGetDialLimitObject(m_sDealWithRealPhoneNumberStr, m_uAgentID, m_sTypeUUID, null, m_uWhiteList, m_mAgent.limitthedial, m_mAgent.f99d999);
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
                                        m_mChannel.channel_call_uuid_after = null;
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
                                    m_mChannel.channel_call_uuid_after = null;
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
                                m_mChannel.channel_call_uuid_after = null;
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
                            /// 最终根据昊舜,延申
                            /// 9999：执行原号码加法
                            /// 0000或空或1111：执行真实号码加法
                            /// ]]>

                            ///强制加拨前缀只使用外地加拨即可
                            if (
                                ///兼容原来的逻辑,增加9999做不处理逻辑
                                _m_mDialLimit.m_sAreaCodeStr == "9999" ||
                                _m_mDialLimit.m_sAreaCodeStr == "0000" || _m_mDialLimit.m_sAreaCodeStr == "1111" || string.IsNullOrWhiteSpace(_m_mDialLimit.m_sAreaCodeStr))
                            {
                                if (!string.IsNullOrWhiteSpace(_m_mDialLimit.m_sDialPrefixStr))
                                {
                                    Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} add prefix:{_m_mDialLimit.m_sDialPrefixStr}]");
                                }

                                ///为了可不更新客户端,这里先兼容一下
                                string m_sTs0 = m_sDealWithPhoneNumberStr.TrimStart('0');
                                if (m_sTs0.StartsWith("400") || m_sTs0.StartsWith("800") || _m_mDialLimit.m_sAreaCodeStr == "9999")
                                {
                                    m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialPrefixStr}{m_sDealWithPhoneNumberStr}";
                                    //原号码
                                    m_sCalleeRemove0000Prefix = $"{m_sDealWithPhoneNumberStr}";
                                }
                                else
                                {
                                    m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialPrefixStr}{m_sDealWithRealPhoneNumberStr}";
                                    //处理后的号码
                                    m_sCalleeRemove0000Prefix = $"{m_sDealWithRealPhoneNumberStr}";
                                }
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
                                                    if (string.IsNullOrWhiteSpace(m_sCityCodeStr))
                                                    {
                                                        ///如果查询得到的区号为空,使用原号码,方便主动加拨前缀等问题
                                                        Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} no area code,use:{m_sDealWithPhoneNumberStr}]");
                                                        m_sCalleeNumberStr = $"{m_sDealWithPhoneNumberStr}";
                                                    }
                                                    else
                                                    {
                                                        m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialLocalPrefixStr}{m_sDealWithRealPhoneNumberStr}";
                                                    }
                                                }
                                                //原号码
                                                m_sCalleeRemove0000Prefix = $"{m_sDealWithRealPhoneNumberStr}";
                                            }
                                        }
                                        break;
                                    case Special.Telephone:
                                        ///仅处理400,800电话
                                        string m_sTs0 = m_sDealWithPhoneNumberStr.TrimStart('0');
                                        if (m_sTs0.StartsWith("400") || m_sTs0.StartsWith("800"))
                                        {
                                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} 400 phone,{m_sDealWithPhoneNumberStr}]");
                                            m_sCalleeNumberStr = $"{m_sDealWithPhoneNumberStr}";
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
                            m_mChannel.channel_call_uuid_after = null;
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
                }
                else
                {
                    m_mRecord.LocalNum = m_mAgent.ChInfo.channel_number;
                    m_mRecord.CallType = 6;
                    ///修正需带星号,后续逻辑更改
                    m_mRecord.T_PhoneNum = m_sDealWithPhoneNumberStr;
                    m_mRecord.C_PhoneNum = m_mRecord.T_PhoneNum;
                    m_mRecord.PhoneAddress = m_sPhoneAddressStr;

                    #region ***兼容内呼规则逻辑
                    if (true)
                    {
                        ///分割即可
                        string m_sContinue = "IN未进行";
                        ///这里前方不带星,无需处理
                        List<string> m_lBf = m_sDealWithRealPhoneNumberStr.Split('*').ToList();
                        if (m_lBf.Count() == 1) m_lBf.Insert(0, "");
                        if (m_lBf.Count() == 2)
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
                                            m_sEndPointStrB = $"sofia/{_m_mInrule.inruleua}/sip:{m_sDealWithRealPhoneNumberStr}@{_m_mInrule.inruleip}:{_m_mInrule.inruleport}";
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
                                            ///转换成短号形式,不在保存及推送此外显
                                            string _m_sCallee = $"{_m_mInrule.inrulesuffix}*{_m_mInrule.inrulebooktkey}";
                                            ///根据内呼规则拼接终点表达式
                                            m_sEndPointStrB = $"sofia/{_m_mInrule.inruleua}/sip:{_m_sCallee}@{_m_mInrule.inruleip}:{_m_mInrule.inruleport}";
                                            m_mRecord.LocalNum = $"{m_cInrule.m_pInrule.inrulesuffix}*{m_mRecord.LocalNum}";
                                            m_mRecord.PhoneAddress = $"内呼 {_m_mInrule.inrulename} {_m_mInrule.inrulebookname}";
                                            ///查询本机号码快捷项
                                            m_mInrule _m_mLoaclInrule = m_cInrule.m_lInrule.Where(x => x.inrulemain == 1 && x.type && x.inrulebooktkey == m_mAgent.ChInfo.channel_number).FirstOrDefault();
                                            if (_m_mLoaclInrule != null)
                                            {
                                                m_mRecord.LocalNum = $"*{_m_mLoaclInrule.inrulebookfkey}";
                                            }
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
                        if (m_sDealWithPhoneNumberStr.Length != 5 && m_sContinue != null)
                        {
                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_uuid_after = null;
                            m_mChannel.channel_call_other_uuid = null;
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} inrule callee:{m_sDealWithPhoneNumberStr},way:{m_sContinue}]");
                            m_fSend(m_pSocket, M_WebSocketSend._bhzt_fail(m_sContinue));
                            return;
                        }
                    }
                    #endregion
                }

                bool m_bInboundTest = Call_ParamUtil.InboundTest;
                string m_sEndPointStrA = $"user/{m_mChannel.channel_number}";

                if (m_sNumberType == Special.ApiShare)
                {
                    ///Api出局Ua,已写到文件里
                    m_sEndPointStrB = $"sofia/{Call_ParamUtil.m_sApiUa}/sip:{m_sCalleeNumberStr}@{m_sGwIP}";
                }
                else
                {
                    #region ***原终点表达式
                    if (m_bStar)
                    {
                        if (string.IsNullOrWhiteSpace(m_sEndPointStrB)) m_sEndPointStrB = $"user/{m_sDealWithRealPhoneNumberStr}";
                    }
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
                    #endregion
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
                //真实号码赋值
                if (!m_bStar) m_stNumberStr = _m_mDialLimit?.m_stNumberStr;
                //录音中真实号码赋值
                m_mRecord.tnumber = m_stNumberStr;
                //录音中来电主叫或去掉被叫的真实号码获取
                string m_sRectNumberStr = (!string.IsNullOrWhiteSpace(m_stNumberStr) ? m_stNumberStr : m_mRecord.LocalNum);

                if (m_fDoStatus(m_mChannel, null, 0, m_uAgentID)) return;
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
                    m_mChannel.channel_call_uuid_after = null;
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

                ///强断
                if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
                bool m_bIsDispose = false;
                IDisposable m_eEventChannelCreate = null;
                IDisposable m_eEventChannelPark = null;
                IDisposable m_eChannel200 = null;
                IDisposable m_eEventMessage = null;
                m_sClient.Disposed += (a, b) =>
                {
                    try
                    {
                        m_bIsDispose = true;

                        ///状态回发
                        if (m_mChannel != null)
                        {
                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_other_uuid = null;
                        }

                        Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} socket client dispose]");
                        if (m_eEventChannelPark != null)
                        {
                            m_eEventChannelPark.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelPark dispose]");
                        }
                        if (m_eEventMessage != null)
                        {
                            m_eEventMessage.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event Message dispose]");
                        }

                        if (m_eChannel200 != null)
                        {
                            m_eChannel200.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelAnswer dispose]");
                        }

                        if (m_eEventChannelCreate != null)
                        {
                            m_eEventChannelCreate.Dispose();
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelCreate dispose]");
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

                ///强断
                if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
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
                    ///强断
                    if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
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

                ///强断
                if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
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

                ///强断
                if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
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

                ///强断
                if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
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
                            ///桥接失败录音
                            if (!m_bIsLinked)
                                m_fDialLimit.m_fSetBridgeFialAudio(Call_ParamUtil.m_uBridgeFailAudio, m_sExtensionStr, m_sRectNumberStr, m_bStar, m_mRecord);

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

                            if (!m_bStar && _m_mDialLimit != null)
                            {
                                m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, m_mRecord.C_SpeakTime, m_pDialArea);
                                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
                            }

                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_uuid_after = null;
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
                            ///桥接失败录音
                            m_fDialLimit.m_fSetBridgeFialAudio(Call_ParamUtil.m_uBridgeFailAudio, m_sExtensionStr, m_sRectNumberStr, m_bStar, m_mRecord);

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

                            if (!m_bStar && _m_mDialLimit != null)
                            {
                                m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, 0, m_pDialArea);
                                Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
                            }

                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_uuid_after = null;
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

                        string m_sRecSub = $"{{0}}\\{m_dtAnswerTimeNow.ToString("yyyy")}\\{m_dtStartTimeNow.ToString("yyyyMM")}\\{m_dtAnswerTimeNow.ToString("yyyyMMdd")}\\Rec_{m_dtAnswerTimeNow.ToString("yyyyMMddHHmmss")}_{m_sRectNumberStr.Replace("*", "X")}_{(m_bStar ? "N" : "")}Q_{m_mRecord.T_PhoneNum.Replace("*", "X")}{m_sExtensionStr}";
                        string m_sRecordingFile = string.Format(m_sRecSub, ParamLib.RecordFilePath);
                        string m_sRecordingFolder = Path.GetDirectoryName(m_sRecordingFile);
                        if (!Directory.Exists(m_sRecordingFolder)) Directory.CreateDirectory(m_sRecordingFolder);
                        string m_sRecordingID = Path.GetFileNameWithoutExtension(m_sRecordingFile);

                        //录音
                        {

                            ///修正录音路径,防止出错
                            string _m_sRecordingFile = Cmn_v1.Cmn.PathFmt(m_sRecordingFile, "/");

                            if (m_bIsDispose) return;
                            var m_pRecordingResult = await m_sClient.SendApi(string.Format("uuid_record {0} start {1}", uuid, _m_sRecordingFile)).ContinueWith((task) =>
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

                            ///修正备份录音路径,防止出错
                            string _m_sBackUpRecordingFile = Cmn_v1.Cmn.PathFmt(m_sBackUpRecordingFile, "/");

                            if (m_bIsDispose) return;
                            var m_pRecordingResult = await m_sClient.SendApi(string.Format("uuid_record {0} start {1}", uuid, _m_sBackUpRecordingFile)).ContinueWith((task) =>
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

                                if (!m_bStar && _m_mDialLimit != null)
                                {
                                    m_fDialLimit.m_fSetDialLimit(_m_mDialLimit.m_sNumberStr, m_uAgentID, m_mRecord.C_SpeakTime, m_pDialArea);
                                    Log.Instance.Success($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} update diallimit]");
                                }

                                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                                m_mChannel.channel_call_uuid = null;
                                m_mChannel.channel_call_uuid_after = null;
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
                    ///强断
                    if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
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

                ///强断
                if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
                if (m_bIsDispose) return;
                await m_sClient.SubscribeEvents(EventName.ChannelCreate).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelCreate cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} event ChannelCreate error:{ex.Message}]");
                    }
                });

                ///强断
                if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
                if (m_bIsDispose) return;
                m_eEventChannelCreate = m_sClient.ChannelEvents.Where(x => x.UUID == uuid && (x.EventName == EventName.ChannelCreate)).Take(1).Subscribe(x =>
                {
                    m_mChannel.channel_call_uuid = uuid;
                    ///强断
                    if (m_fDoStatus(m_mChannel, m_sClient, 3, m_uAgentID, false)) return;

                });

                ///强断
                if (m_fDoStatus(m_mChannel, m_sClient, 1, m_uAgentID)) return;
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

                        if (m_mChannel.channel_call_status == APP_USER_STATUS.FS_USER_AHANGUP)
                            m_mRecord.CallResultID = m_bStar ? 55 : 56;
                        else
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
                        m_mChannel.channel_call_uuid_after = null;
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
                m_mChannel.channel_call_uuid_after = null;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m_mChannel"></param>
        /// <param name="m_pClient"></param>
        /// <param name="m_uWay">
        /// 0.不处理
        /// 1.Dispose
        /// 2.Hang
        /// 3.Kill
        /// </param>
        /// <returns></returns>
        private static bool m_fDoStatus(ChannelInfo m_mChannel, InboundSocket m_sClient, int m_uWay, int m_uAgentID, bool m_bReStatus = true)
        {
            if (
                m_mChannel != null &&
                (m_mChannel.channel_call_status == APP_USER_STATUS.FS_USER_AHANGUP ||
                m_mChannel.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP)
                )
            {
                string m_sUUID = m_mChannel.channel_call_uuid;
                m_mChannel.channel_call_uuid = null;
                switch (m_uWay)
                {
                    case 0:
                        break;
                    case 1:
                        {
                            ///强断
                            Log.Instance.Warn($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} hangup]");
                            if (m_sClient != null && m_sClient.IsConnected)
                            {
                                m_sClient.Exit().ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit cancel]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoSocket][m_fDialClass][m_fDial][{m_uAgentID} exit error:{ex.Message}]");
                                    }
                                });
                            }
                        }
                        break;
                    case 3:
                        SocketMain.m_fKill(m_uAgentID, m_sUUID);
                        break;
                }
                if (m_bReStatus)
                {
                    m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                    m_mChannel.channel_call_uuid_after = null;
                    m_mChannel.channel_call_other_uuid = null;
                }
                return true;
            }
            return false;
        }
    }
}
