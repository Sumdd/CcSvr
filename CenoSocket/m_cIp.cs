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
        public static async void m_fExecuteDial(IWebSocketConnection m_pWebSocket, string m_sUUID, string m_sLoginName, string m_sPhoneNumber, string m_sCaller, string m_sNumberType, int m_uMustNbr = 0, int m_uDescMode = 0, int m_uDecryptMode = 0, int m_uPhoneNumberValidMode = 0, share_number m_pXxShareNumber = null, int m_uALegTimeoutSeconds = 0)
        {
            try
            {
                await m_fDial(m_pWebSocket, m_sUUID, m_sLoginName, m_sPhoneNumber, m_sCaller, m_sNumberType, m_uMustNbr, m_uDescMode, m_uDecryptMode, m_uPhoneNumberValidMode, m_pXxShareNumber, m_uALegTimeoutSeconds);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][m_cIp][m_fExecuteDial][logic error:{ex.Message}]");
            }
        }

        public static async Task m_fDial(IWebSocketConnection m_pWebSocket, string m_sUUID, string m_sLoginName, string m_sPhoneNumber, string m_sCaller, string m_sNumberType, int m_uMustNbr = 0, int m_uDescMode = 0, int m_uDecryptMode = 0, int m_uPhoneNumberValidMode = 0, share_number m_pXxShareNumber = null, int m_uALegTimeoutSeconds = 0)
        {
            int m_uAgentID = -1;
            ChannelInfo m_mChannel = null;
            share_number m_pShareNumber = null;
            bool m_bShare = m_sNumberType == Special.Share;//是否使用了共享号码
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
            ///白名单不受同号码限呼,0非1白名单
            int m_uWhiteList = 0;
            //判断是否需要回发,如果为共享线路、且类型为新呼出式续联时需要回发
            bool m_bNeedReset = (m_bShare && m_pXxShareNumber != null);
            //是否执行了续联的Redis锁删除
            bool m_bXxDeleteRedisLock = false;
            //桥接主叫超时时间
            if (m_uALegTimeoutSeconds <= 0) m_uALegTimeoutSeconds = Call_ParamUtil.ALegTimeoutSeconds;

            try
            {
                AGENT_INFO m_mAgent = call_factory.agent_list.Find(x => x.LoginName == m_sLoginName);
                if (m_mAgent == null)
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_sLoginName} miss a leg info]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err账户有误", m_pXxShareNumber);

                    if (m_bNeedReset && !m_bXxDeleteRedisLock)
                    {
                        m_bXxDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                    }

                    return;
                }
                m_uAgentID = m_mAgent.AgentID;
                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} inbound web dial]");

                #region ***增加单台验证和时间验证
                int m_uUseStatus = m_cModel.m_uUseStatus;
                if (m_uUseStatus > 0)
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID},error code:{m_uUseStatus}]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, $"ErrCode{m_uUseStatus}", m_pXxShareNumber);

                    if (m_bNeedReset && !m_bXxDeleteRedisLock)
                    {
                        m_bXxDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                    }

                    return;
                }
                #endregion

                //使用共享号码必须填写外显号码
                if (m_bShare && !m_bNeedReset && string.IsNullOrWhiteSpace(m_sCaller))
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} miss share number]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err共享号码", m_pXxShareNumber);
                    return;
                }

                //兼容被叫号码验证模式
                string m_sDealWithPhoneNumberStr = m_sPhoneNumber;
                switch (m_uPhoneNumberValidMode)
                {
                    case 1:
                        //信修等特殊模式略过验证
                        Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} ignore invalid phone]");
                        break;
                    default:
                        //继续处理,防止号码有误
                        Regex m_rReplaceRegex = new Regex("[^(0-9*#)]+");
                        Regex m_rIsMatchRegex = new Regex("^[0-9*#]{3,20}$");
                        m_sDealWithPhoneNumberStr = m_rReplaceRegex.Replace(m_sPhoneNumber, string.Empty);
                        if (!m_rIsMatchRegex.IsMatch(m_sDealWithPhoneNumberStr))
                        {
                            Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} invalid phone]");
                            m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err号码有误", m_pXxShareNumber);

                            if (m_bNeedReset && !m_bXxDeleteRedisLock)
                            {
                                m_bXxDeleteRedisLock = true;
                                Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                            }

                            return;
                        }
                        break;
                }

                #region ***呼出只判断是否是黑名单,黑名单直接限制呼叫即可,如果更新中则暂时失效即可
                if (!m_bNeedReset && m_uPhoneNumberValidMode == 0 && !m_cWblist.m_bInitWblist && m_cWblist.m_lWblist?.Count > 0)
                {
                    ///判断所有的黑名单即可
                    foreach (m_mWblist item in m_cWblist.m_lWblist)
                    {
                        ///兼容呼入呼出黑名单
                        if (item.wbtype == 2 && (item.wblimittype & 2) > 0)
                        {
                            if (item.regex.IsMatch(m_sDealWithPhoneNumberStr))
                            {
                                Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} black list:{m_sDealWithPhoneNumberStr}]");
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err黑名单", m_pXxShareNumber);
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
                            if (item.regex.IsMatch(m_sDealWithPhoneNumberStr))
                            {
                                Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} white list:{m_sDealWithPhoneNumberStr}]");
                                m_uWhiteList = 1;
                                break;
                            }
                        }
                    }
                }
                #endregion

                #region ***是否需要查询联系人姓名
                if (m_uPhoneNumberValidMode == 0 && Call_ParamUtil.m_bUseHomeSearch) m_cEsySQL.m_fSetExpc(m_sDealWithPhoneNumberStr);
                #endregion

                List<string> m_lStrings = m_cPhone.m_fGetPhoneNumberMemo(m_sDealWithPhoneNumberStr, m_uPhoneNumberValidMode);
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

                if (m_uCh == -1 || m_mChannel == null || m_mChannel?.channel_type != Special.SIP
                    ///接触模式限制,都接受API调用即可
                    ///|| (m_mChannel?.IsRegister != 0 && m_mChannel?.IsRegister != -1)
                    )
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} miss channel or not sip channel]");
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err通道有误", m_pXxShareNumber);

                    if (m_bNeedReset && !m_bXxDeleteRedisLock)
                    {
                        m_bXxDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                    }

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
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err通道繁忙", m_pXxShareNumber);

                    if (m_bNeedReset && !m_bXxDeleteRedisLock)
                    {
                        m_bXxDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                    }

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
                ///被叫的终点表达式提前
                string m_sEndPointStrB = string.Empty;

                if (!m_bStar)
                {
                    #region 拨号限制,增加号码池概念
                    switch (m_sNumberType)
                    {
                        case Special.Common:
                            {
                                ///增加白名单、坐席同号码限呼参数的传入
                                _m_mDialLimit = m_fDialLimit.m_fGetDialLimitObject(m_sDealWithRealPhoneNumberStr, m_uAgentID, m_sCaller, null, m_uWhiteList, m_mAgent.limitthedial, m_mAgent.f99d999);
                                break;
                            }
                        case Special.Share:
                            {
                                switch (m_bNeedReset)
                                {
                                    case true:
                                        {
                                            //号码转换为普通拨号限制的模式来做兼容
                                            _m_mDialLimit = m_fDialLimit.m_fGetDialLimitByShare(m_pXxShareNumber);
                                        }
                                        break;
                                    case false:
                                        {
                                            #region ***普通共享号码
                                            //跳转至号码池逻辑,需要持久化至数据库,录音记录都进行保存
                                            string m_sErrMsg = string.Empty;
                                            m_pShareNumber = Redis2.m_fGetTheShareNumber(uuid, m_uAgentID, m_sCaller, m_sDealWithRealPhoneNumberStr, DB.Basic.Call_ParamUtil.m_uShareNumSetting, out m_sErrMsg);
                                            _m_mDialLimit = m_fDialLimit.m_fGetDialLimitByShare(m_pShareNumber);
                                            if (_m_mDialLimit == null)
                                            {
                                                Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} get share error]");
                                                if (string.IsNullOrWhiteSpace(m_sErrMsg)) m_sErrMsg = "Err获取号码";
                                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, $"{m_sErrMsg}", m_pXxShareNumber);

                                                if (m_bNeedReset && !m_bXxDeleteRedisLock)
                                                {
                                                    m_bXxDeleteRedisLock = true;
                                                    Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                                                }

                                                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                                                m_mChannel.channel_call_uuid = null;
                                                m_mChannel.channel_call_other_uuid = null;

                                                return;
                                            }
                                            //如果可以出来,任何需要解锁的地方都要加逻辑
                                            m_mRecord.isshare = 1;
                                            m_mRecord.FreeSWITCHIPv4 = m_sFreeSWITCHIPv4;
                                            #endregion
                                        }
                                        break;
                                }
                                break;
                            }
                        default:
                            {
                                Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} unknown number type]");
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err号码类别", m_pXxShareNumber);

                                if (m_bNeedReset && !m_bXxDeleteRedisLock)
                                {
                                    m_bXxDeleteRedisLock = true;
                                    Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                                }

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
                            m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err网关有误", m_pXxShareNumber);

                            if (m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                            }

                            if (m_bNeedReset && !m_bXxDeleteRedisLock)
                            {
                                m_bXxDeleteRedisLock = true;
                                Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                            }

                            return;
                        }
                        #endregion

                        m_mRecord.LocalNum = _m_mDialLimit.m_sNumberStr;
                        Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} dialcount:{_m_mDialLimit.m_uDialCount}]");

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
                            _m_mDialLimit.m_sAreaCodeStr == "0000" || _m_mDialLimit.m_sAreaCodeStr == "1111" || string.IsNullOrWhiteSpace(_m_mDialLimit.m_sAreaCodeStr) || m_uMustNbr == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(_m_mDialLimit.m_sDialPrefixStr))
                            {
                                //强制加前缀
                                Log.Instance.Debug($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} add prefix:{_m_mDialLimit.m_sDialPrefixStr}]");
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
                                                    Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} no area code,use:{m_sDealWithPhoneNumberStr}]");
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
                                        Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} 400 phone,{m_sDealWithPhoneNumberStr}]");
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
                        m_mChannel.channel_call_other_uuid = null;
                        Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} no phone number]");
                        m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err拨号限制", m_pXxShareNumber);

                        if (m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                        }

                        if (m_bNeedReset && !m_bXxDeleteRedisLock)
                        {
                            m_bXxDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
                        }

                        return;
                    }
                    #endregion
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
                            ///状态回发
                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_other_uuid = null;

                            Log.Instance.Warn($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} inrule callee:{m_sDealWithPhoneNumberStr},way:{m_sContinue}]");
                            m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, m_sContinue, m_pXxShareNumber);
                            return;
                        }
                    }
                    #endregion
                }

                bool m_bInboundTest = Call_ParamUtil.InboundTest;
                string m_sEndPointStrA = $"user/{m_mChannel.channel_number}";
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
                bool m_bIgnoreEarlyMedia = Call_ParamUtil.__ignore_early_media;
                bool m_bIsLinked = false;
                string m_sExtensionStr = Call_ParamUtil._rec_t;
                string m_sWhoHangUpStr = string.Empty;
                m_mChannel.channel_call_uuid = uuid;
                //真实号码赋值
                if (!m_bStar) m_stNumberStr = _m_mDialLimit.m_stNumberStr;
                //录音中真实号码赋值
                m_mRecord.tnumber = m_stNumberStr;
                //录音中来电主叫或去掉被叫的真实号码获取
                string m_sRectNumberStr = (!string.IsNullOrWhiteSpace(m_stNumberStr) ? m_stNumberStr : m_mRecord.LocalNum);

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
                    m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "ErrESL", m_pXxShareNumber);

                    if (m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                    {
                        m_bDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                    }

                    if (m_bNeedReset && !m_bXxDeleteRedisLock)
                    {
                        m_bXxDeleteRedisLock = true;
                        Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
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

                        ///状态回发
                        if (m_mChannel != null)
                        {
                            m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            m_mChannel.channel_call_uuid = null;
                            m_mChannel.channel_call_other_uuid = null;
                        }

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

                        if (m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                        }

                        if (m_bNeedReset && !m_bXxDeleteRedisLock)
                        {
                            m_bXxDeleteRedisLock = true;
                            Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
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
                            ///桥接失败录音
                            if (!m_bIsLinked)
                                m_fDialLimit.m_fSetBridgeFialAudio(Call_ParamUtil.m_uBridgeFailAudio, m_sExtensionStr, m_sRectNumberStr, m_bStar, m_mRecord);

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
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "主叫挂断,取消呼叫", m_pXxShareNumber, true, "Err主叫挂断取消呼叫");
                            }

                            dial_area m_pDialArea = null;
                            if (!m_bStar && m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, m_mRecord.C_SpeakTime);
                            }

                            if (m_bNeedReset && !m_bXxDeleteRedisLock)
                            {
                                m_bXxDeleteRedisLock = true;
                                //Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, uuid);
                                //编辑
                                m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, m_sUUID, m_pXxShareNumber, m_mRecord.C_SpeakTime);
                            }

                            Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} insert record]");
                            call_record.Insert(m_mRecord, !m_bStar && m_bShare && !m_bNeedReset, m_pDialArea);

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

                    //自动接听参数
                    BridgeOptions CheeVariables = new BridgeOptions()
                    {
                        UUID = bridgeUUID,
                        CallerIdNumber = m_mRecord.LocalNum,
                        CallerIdName = m_mRecord.LocalNum,
                        HangupAfterBridge = false,
                        ContinueOnFail = true,
                        TimeoutSeconds = m_uTimeoutSeconds,
                        IgnoreEarlyMedia = m_bIgnoreEarlyMedia
                    };
                    if (CheeVariables.ChannelVariables.ContainsKey("sip_h_X_ALegAutoAccept")) CheeVariables.ChannelVariables["sip_h_X_ALegAutoAccept"] = "N";

                    //桥接被叫
                    if (m_bIsDispose) return;
                    BridgeResult m_pBridgeResult = await m_sClient.Bridge(uuid, m_sEndPointStrB, CheeVariables).ContinueWith(task =>
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
                            ///桥接失败录音
                            m_fDialLimit.m_fSetBridgeFialAudio(Call_ParamUtil.m_uBridgeFailAudio, m_sExtensionStr, m_sRectNumberStr, m_bStar, m_mRecord);

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
                            m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, $"{m_sWebSendMsgStr}", m_pXxShareNumber);

                            m_mRecord.Remark = m_sMsgStr;

                            dial_area m_pDialArea = null;
                            if (!m_bStar && m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                            {
                                m_bDeleteRedisLock = true;
                                m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, 0);
                            }

                            if (m_bNeedReset && !m_bXxDeleteRedisLock)
                            {
                                m_bXxDeleteRedisLock = true;
                                //Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, uuid);
                                //编辑
                                m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, m_sUUID, m_pXxShareNumber, 0);
                            }

                            Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} insert record]");
                            call_record.Insert(m_mRecord, !m_bStar && m_bShare && !m_bNeedReset, m_pDialArea);

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

                        if (m_bShare && !m_bNeedReset) m_pShareNumber.state = SHARE_NUM_STATUS.TALKING;
                        else if (m_bNeedReset) m_pXxShareNumber.state = SHARE_NUM_STATUS.TALKING;

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

                        string m_sRecSub = $"{{0}}\\{m_dtAnswerTimeNow.ToString("yyyy")}\\{m_dtStartTimeNow.ToString("yyyyMM")}\\{m_dtAnswerTimeNow.ToString("yyyyMMdd")}\\Rec_{Cmn.UniqueID(m_dtAnswerTimeNow)}_{m_sRectNumberStr.Replace("*", "X")}_{(m_bStar ? "N" : "")}Q_{m_mRecord.T_PhoneNum.Replace("*", "X")}{m_sExtensionStr}";
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
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, 0, $"{m_sRecordingID}", m_pXxShareNumber, true);
                            }
                            else
                            {
                                Log.Instance.Fail($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} record fail]");

                                //录音失败
                                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, 0, $"", m_pXxShareNumber, true, "Err拨号成功录音失败");
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
                                if (!m_bStar && m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                                {
                                    m_bDeleteRedisLock = true;
                                    m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, m_mRecord.C_SpeakTime);
                                }

                                if (m_bNeedReset && !m_bXxDeleteRedisLock)
                                {
                                    m_bXxDeleteRedisLock = true;
                                    //Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, uuid);
                                    //编辑
                                    m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, m_sUUID, m_pXxShareNumber, m_mRecord.C_SpeakTime);
                                }

                                Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} insert record]");
                                call_record.Insert(m_mRecord, !m_bStar && m_bShare && !m_bNeedReset, m_pDialArea);

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
                ///加密模式,不适用于客户端即可,客户端只需去掉全号显示
                if (m_mChannel?.IsRegister != 1)
                {
                    switch (m_uDescMode)
                    {
                        case 1:///保留前3后4脱敏中间位
                            m_sCalleeRemove0000 = Cmn_v1.Cmn.m_fSecret(m_sCalleeRemove0000);
                            break;
                        case 2:///本机外显号码
                            m_sCalleeRemove0000 = m_mRecord.LocalNum;
                            break;
                    }
                }

                //自动接听参数
                Dictionary<string, string> CherVariables = new Dictionary<string, string>();
                CherVariables.Add("sip_h_X_ALegAutoAccept", "Y");

                //拨打之前记录uuid
                m_mChannel.channel_call_uuid = uuid;

                //设定180回铃音,使用
                if (Call_ParamUtil.m_uUseRingBack == 1)
                {
                    CherVariables.Add("ringback", Call_ParamUtil.m_sCallMusic);
                }

                if (m_bIsDispose) return;
                OriginateResult m_pOriginateResult = await m_sClient.Originate(m_sEndPointStrA, new OriginateOptions()
                {
                    UUID = uuid,
                    CallerIdNumber = m_sCalleeRemove0000,
                    //主叫显示对方电话的归属地
                    CallerIdName = string.IsNullOrWhiteSpace(_m_sPhoneAddressStr) ? m_sCalleeRemove0000 : _m_sPhoneAddressStr,
                    HangupAfterBridge = false,
                    TimeoutSeconds = m_uALegTimeoutSeconds,
                    IgnoreEarlyMedia = m_bIgnoreEarlyMedia,
                    ChannelVariables = CherVariables

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
                        m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err呼叫主叫", m_pXxShareNumber);

                        DateTime m_dtEndTimeNow = DateTime.Now;
                        string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                        m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                        m_mRecord.C_EndTime = m_sEndTimeNowString;
                        m_mRecord.CallResultID = m_bStar ? 40 : 13;
                        m_mRecord.Remark = m_sMsgStr;

                        dial_area m_pDialArea = null;
                        if (!m_bStar && m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                        {
                            m_bDeleteRedisLock = true;
                            m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, uuid, m_pShareNumber, m_mRecord.C_SpeakTime);
                        }

                        if (m_bNeedReset && !m_bXxDeleteRedisLock)
                        {
                            m_bXxDeleteRedisLock = true;
                            //Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, uuid);
                            //编辑
                            m_pDialArea = Redis2.m_fEditShareNumber(m_uAgentID, m_sUUID, m_pXxShareNumber, m_mRecord.C_SpeakTime);
                        }

                        Log.Instance.Success($"[CenoSocket][m_cIp][m_fDial][{m_uAgentID} insert record]");
                        //修正此处如果为共享号码也肌肉共享记录中
                        call_record.Insert(m_mRecord, !m_bStar && m_bShare && !m_bNeedReset, m_pDialArea);

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
                m_cIp.m_fIpDialSend(m_pWebSocket, m_sUUID, -1, "Err未完成", m_pXxShareNumber);

                if (m_bShare && !m_bNeedReset && !m_bDeleteRedisLock)
                {
                    m_bDeleteRedisLock = true;
                    Redis2.m_fResetShareNumber(m_uAgentID, m_pShareNumber, uuid);
                }

                if (m_bNeedReset && !m_bXxDeleteRedisLock)
                {
                    m_bXxDeleteRedisLock = true;
                    Redis2.m_fResetShareNumber(m_uAgentID, m_pXxShareNumber, m_sUUID);
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

        private static void m_fIpDialSend(IWebSocketConnection m_pWebSocket, string m_sUUID, int m_sStatus, string m_sResultMessage, share_number m_pXxShareNumber
            //是否发送
            , bool m_bSend = true
            //替换结果表达
            , string m_sReResultMessage = null
            )
        {
            try
            {
                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();


                object m_oObject = null;
                if (m_pXxShareNumber != null)
                {
                    //不发送,不需要,有移除逻辑
                    if (!m_bSend) return;

                    //新呼出式续联
                    _m_mWebSocketJson.m_sUse = m_cIpCmd._m_sGetApply2;
                    m_oObject = new
                    {
                        //替换
                        m_sErrMsg = string.IsNullOrWhiteSpace(m_sReResultMessage) ? m_sResultMessage : m_sReResultMessage,
                        m_pShareNumber = m_pXxShareNumber == null ? null : JsonConvert.SerializeObject(m_pXxShareNumber)
                    };
                }
                else
                {
                    _m_mWebSocketJson.m_sUse = m_mWebSocketJsonCmd._m_sDialTask;
                    m_oObject = m_sResultMessage;
                }

                _m_mWebSocketJson.m_oObject = new
                {
                    m_sUUID = m_sUUID,
                    m_sStatus = m_sStatus,
                    m_sResultMessage = m_oObject
                };
                m_cIp.m_fSendObject(m_pWebSocket, _m_mWebSocketJson);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoSocket][m_cIp][m_fIpDialSend][Exception][{ex.Message}]");
            }
        }
        #endregion

        public static void m_fIpKill(string m_sLoginName, ref int m_sStatus, ref string m_sErrMsg)
        {
            try
            {
                int m_uAgentID = -1;
                ChannelInfo m_mChannel = null;

                AGENT_INFO m_mAgent = call_factory.agent_list.Find(x => x.LoginName == m_sLoginName);
                if (m_mAgent == null)
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fIpKill][{m_sLoginName} miss a leg info]");
                    m_sStatus = -1;
                    m_sErrMsg = "Err账户有误";
                    return;
                }

                m_uAgentID = m_mAgent.AgentID;
                int m_uCh = m_mAgent.ChInfo.nCh;
                m_mChannel = call_factory.channel_list[m_uCh];

                if (m_uCh == -1 || m_mChannel == null || m_mChannel?.channel_type != Special.SIP
                    ///接触模式限制,都接受API调用即可
                    ///|| (m_mChannel?.IsRegister != 0 && m_mChannel?.IsRegister != -1)
                    )
                {
                    Log.Instance.Fail($"[CenoSocket][m_cIp][m_fIpKill][{m_uAgentID} miss channel or not sip channel]");
                    m_sStatus = -1;
                    m_sErrMsg = "Err通道有误";
                    return;
                }

                //强断
                SocketMain.m_fKill(m_uAgentID, m_mChannel.channel_call_uuid);
                m_mChannel.channel_call_uuid = null;
                SocketMain.m_fKill(m_uAgentID, m_mChannel.channel_call_other_uuid);
                m_mChannel.channel_call_other_uuid = null;
                m_mChannel.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;

                m_sStatus = 0;
                m_sErrMsg = "OK强断";
            }
            catch (Exception ex)
            {
                m_sStatus = -1;
                m_sErrMsg = $"Err{ex.Message}";
            }
        }
    }

    public class m_cIpCmd
    {
        /// <summary>
        /// IP话机拨号
        /// </summary>
        public const string _m_sIpDial = "IpDial";
        /// <summary>
        /// IP话机拨号版本2,直接在该接口拓展,增加一个强断内容,然后拨号时的录音返回放到主叫提机后,便于接口立即返回结果
        /// <para>提前生成录音Id,然后反查即可,为录音绑定做准备</para>
        /// <para>还要改录音表结构</para>
        /// <para>接口增加话单查询,可自行绑定录音,或者制造假录音放入话单以及生成文件</para>
        /// </summary>
        public const string _m_sIpDialv2 = "IpDialv2";
        /// <summary>
        /// 基于IP话机拨号版本2延申
        /// 5.m_uDescMode,脱敏模式(0,正常;1,保留前3后4脱敏中间位;2.本机外显号码)
        /// 6.m_uDecryptMode,解密模式(0,正常;1,交行解密)
        /// </summary>
        public const string _m_sIpDialv3 = "IpDialv3";
        /// <summary>
        /// 基于IP话机拨号版本3延申
        /// 7.m_uPhoneNumberValidMode,号码验证模式(0,正常;1,略过)
        /// </summary>
        public const string _m_sIpDialv4 = "IpDialv4";
        /// <summary>
        /// 获取共享号码
        /// </summary>
        public const string _m_sGetShare = "GetShare";
        /// <summary>
        /// 获取申请式线路
        /// </summary>
        public const string _m_sGetApply = "GetApply";
        /// <summary>
        /// 获取申请式线路2,新呼出式续联
        /// </summary>
        public const string _m_sGetApply2 = "GetApply2";
        /// <summary>
        /// 强断
        /// </summary>
        public const string _m_sIpKill = "IpKill";
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
