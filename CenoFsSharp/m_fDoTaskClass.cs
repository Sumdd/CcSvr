using CenoSipFactory;
using Core_v1;
using DB.Basic;
using DB.Model;
using log4net;
using NEventSocket;
using NEventSocket.FreeSwitch;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Model_v1;
using System.Reactive.Linq;
using Cmn_v1;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json.Linq;

namespace CenoFsSharp
{
    public class m_fDoTaskClass
    {
        public static async Task m_fDoTask(m_mThread m_mThread)
        {
            //修正CallStatus含义
            //0未拨打过1已拨打过2加入队列中
            //Originate即算作1
            m_mThread _m_mThread = m_mThread;
            bool m_bShare = false;//是否使用了共享号码
            //Caller-Caller-ID-Number
            string m_sUAID = string.Empty;
            //FreeSWITCH-IPv4
            string m_sFreeSWITCHIPv4 = InboundMain.FreesSWITCHIPv4;

            try
            {
                DateTime m_dtStartTimeNow = DateTime.Now;
                string m_sStartTimeNowString = Cmn.m_fDateTimeString(m_dtStartTimeNow);

                AGENT_INFO m_mAgent = call_factory.agent_list[_m_mThread.m_mChannelInfo.nCh];
                _m_mThread.m_mRecord.AgentID = m_mAgent.AgentID;
                if (m_bShare) m_sUAID = _m_mThread.m_mChannelInfo.channel_number;
                _m_mThread.m_mRecord.UAID = m_sUAID;
                _m_mThread.m_mRecord.fromagentname = m_mAgent.AgentName;
                _m_mThread.m_mRecord.fromloginname = m_mAgent.LoginName;
                string m_sRealPhoneNumberStr = _m_mThread._m_mQueueTask.PhoneNum;
                string m_sExtensionStr = Call_ParamUtil._rec_t;
                //真实号码
                string m_stNumberStr = string.Empty;

                //继续处理,防止号码有误
                Regex m_rReplaceRegex = new Regex("[^(0-9*#)]+");
                Regex m_rIsMatchRegex = new Regex("^[0-9*#]{3,20}$");
                string m_sDealWithPhoneNumberStr = m_rReplaceRegex.Replace(m_sRealPhoneNumberStr, string.Empty);
                if (!m_rIsMatchRegex.IsMatch(m_sDealWithPhoneNumberStr))
                {
                    _m_mThread.m_mRecord.CallResultID = 30;
                    _m_mThread.m_mRecord.Remark = "invalid phone";
                    _m_mThread.m_mRecord.C_EndTime = Cmn.m_fDateTimeString();

                    Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} insert record...]");
                    call_record.Insert(_m_mThread.m_mRecord);

                    //号码有误,本次任务不计拨打次数,也不能再次拨打
                    _m_mThread._m_mQueueTask.CallStatus = 1;
                    _m_mThread._m_mQueueTask.status = "0";

                    //号码不正确
                    _m_mThread._m_mQueueTask.result = "12";
                    _m_mThread._m_mQueueTask.callTime = m_sStartTimeNowString;
                    _m_mThread._m_mQueueTask.endTime = m_sStartTimeNowString;
                    Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update auto dial task...]");
                    m_fUpdateQueueTaskResult(_m_mThread._m_mQueueTask, m_sExtensionStr);
                    Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} invalid phone...]");

                    _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                    _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                    _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;

                    return;
                }

                #region ***是否需要查询联系人姓名
                if (Call_ParamUtil.m_bUseHomeSearch) m_cEsySQL.m_fSetExpc(m_sDealWithPhoneNumberStr);
                #endregion

                m_mDialLimit _m_mDialLimit = null;
                List<string> m_lStrings = m_cPhone.m_fGetPhoneNumberMemo(m_sDealWithPhoneNumberStr);
                _m_mThread.m_mRecord.PhoneAddress = m_lStrings[3];
                bool m_bStar = m_lStrings[2] == Special.Star;

                //置换概念为原真号
                string m_sDealWithRealPhoneNumberStr = m_lStrings[0];
                //原真号,以前去强制前缀
                string m_sCalleeNumberStr = m_lStrings[0];
                string m_sCalleeRemove0000Prefix = string.Empty;

                if (!m_bStar)
                {
                    #region 拨号限制
                    _m_mDialLimit = m_fDialLimit.m_fGetDialLimitObject(m_sDealWithRealPhoneNumberStr, m_mAgent.AgentID);
                    if (_m_mDialLimit != null && !string.IsNullOrWhiteSpace(_m_mDialLimit.m_sNumberStr))
                    {
                        _m_mThread.m_mRecord.LocalNum = _m_mDialLimit.m_sNumberStr;
                        _m_mThread._m_mQueueTask.CallNum = _m_mDialLimit.m_sNumberStr;

                        Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} dialcount:{_m_mDialLimit.m_uDialCount}...]");

                        ///<![CDATA[
                        /// 空也做强制加拨处理
                        /// ]]>

                        ///强制加拨前缀只使用外地加拨即可
                        if (_m_mDialLimit.m_sAreaCodeStr == "0000" || string.IsNullOrWhiteSpace(_m_mDialLimit.m_sAreaCodeStr))
                        {
                            if (!string.IsNullOrWhiteSpace(_m_mDialLimit.m_sDialPrefixStr))
                            {
                                //强制加前缀
                                Log.Instance.Debug($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} add prefix:{_m_mDialLimit.m_sDialPrefixStr}...]");
                            }
                            m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialPrefixStr}{m_lStrings[0]}";
                            //原号码
                            m_sCalleeRemove0000Prefix = m_sDealWithRealPhoneNumberStr;
                        }
                        else
                        {
                            switch (m_lStrings[5])
                            {
                                case Special.Mobile:
                                    if (!m_sDealWithRealPhoneNumberStr.Contains('*') && !m_sDealWithRealPhoneNumberStr.Contains('#'))
                                    {
                                        if (_m_mDialLimit.m_bZflag)
                                        {

                                            ///<![CDATA[
                                            /// 当被叫号码未找到归属地时,不加拨前缀
                                            /// ]]>

                                            if (!string.IsNullOrWhiteSpace(m_lStrings[4]) && _m_mDialLimit.m_sAreaCodeStr != m_lStrings[4])
                                            {
                                                m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialPrefixStr}{m_lStrings[0]}";
                                            }
                                            else
                                            {
                                                m_sCalleeNumberStr = $"{_m_mDialLimit.m_sDialLocalPrefixStr}{m_lStrings[0]}";
                                            }
                                            //原号码
                                            m_sCalleeRemove0000Prefix = $"{m_lStrings[0]}";
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        //保证号码真实性,确保可以直接回呼
                        _m_mThread.m_mRecord.T_PhoneNum = string.IsNullOrWhiteSpace(m_sCalleeRemove0000Prefix) ? m_sCalleeNumberStr : m_sCalleeRemove0000Prefix;
                        _m_mThread.m_mRecord.C_PhoneNum = m_sDealWithPhoneNumberStr;

                        #region 网关有误
                        if (string.IsNullOrWhiteSpace(_m_mDialLimit.m_sGatewayNameStr))
                        {
                            _m_mThread.m_mRecord.CallResultID = 30;
                            _m_mThread.m_mRecord.Remark = "miss gateway";
                            _m_mThread.m_mRecord.C_EndTime = Cmn.m_fDateTimeString();

                            Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} insert record...]");
                            call_record.Insert(_m_mThread.m_mRecord);

                            _m_mThread._m_mQueueTask.CallStatus = 0;
                            _m_mThread._m_mQueueTask.status = "1";
                            _m_mThread._m_mQueueTask.result = "1";
                            _m_mThread._m_mQueueTask.callTime = m_sStartTimeNowString;
                            _m_mThread._m_mQueueTask.endTime = m_sStartTimeNowString;
                            Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update auto dial task...]");
                            m_fUpdateQueueTaskResult(_m_mThread._m_mQueueTask, m_sExtensionStr);
                            Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} miss gateway...]");

                            _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                            _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;

                            //网关有误,直接退出线程
                            //_m_mThread.m_bIsExitThread = true;
                            //修改逻辑,只停当天,线路配置有误
                            _m_mThread.m_bTodayUse = false;
                            Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} today stop...]");

                            return;
                        }
                        #endregion
                    }
                    else
                    {
                        //未配置号码
                        _m_mThread.m_mRecord.CallResultID = 30;
                        _m_mThread.m_mRecord.Remark = "miss local number";
                        _m_mThread.m_mRecord.C_EndTime = Cmn.m_fDateTimeString();

                        Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} insert record...]");
                        call_record.Insert(_m_mThread.m_mRecord);

                        //重置自动拨号任务,让其他线程进行拨打
                        _m_mThread._m_mQueueTask.CallStatus = 0;
                        _m_mThread._m_mQueueTask.status = "1";
                        _m_mThread._m_mQueueTask.result = "1";
                        _m_mThread._m_mQueueTask.callTime = m_sStartTimeNowString;
                        _m_mThread._m_mQueueTask.endTime = m_sStartTimeNowString;
                        Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update auto dial task...]");
                        m_fUpdateQueueTaskResult(_m_mThread._m_mQueueTask, m_sExtensionStr);
                        Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} miss local number...]");

                        _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                        _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;

                        //网关有误,直接退出线程
                        //_m_mThread.m_bIsExitThread = true;
                        //修改逻辑,只停当天,今天是否已经打满
                        _m_mThread.m_bTodayUse = false;
                        Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} today stop...]");

                        return;
                    }
                    #endregion
                }
                else
                {
                    _m_mThread.m_mRecord.LocalNum = m_mThread.m_mChannelInfo.channel_number;
                    _m_mThread.m_mRecord.T_PhoneNum = m_sDealWithRealPhoneNumberStr;
                    _m_mThread.m_mRecord.C_PhoneNum = _m_mThread.m_mRecord.T_PhoneNum;

                    _m_mThread._m_mQueueTask.CallNum = m_mThread.m_mChannelInfo.channel_number;
                }

                //真实号码赋值
                if (!m_bStar) m_stNumberStr = _m_mDialLimit.m_stNumberStr;
                //录音中真实号码赋值
                _m_mThread.m_mRecord.tnumber = m_stNumberStr;

                InboundSocket m_sClient = await InboundMain.fs_cli().ContinueWith((task) =>
                {
                    try
                    {
                        if (task.IsCanceled)
                        {
                            Log.Instance.Debug($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} inbound socket fail...]");
                            return null;
                        }
                        else
                        {
                            return task.Result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} inbound socket error:{ex.Message}...]");
                        return null;
                    }
                });
                if (m_sClient == null)
                {
                    _m_mThread._m_mQueueTask.CallCount++;
                    if (_m_mThread._m_mQueueTask.CallCount >= Call_ParamUtil.m_uDialCount)
                    {
                        _m_mThread._m_mQueueTask.CallStatus = 1;
                        _m_mThread._m_mQueueTask.status = "0";
                    }
                    else
                    {
                        _m_mThread._m_mQueueTask.CallStatus = 0;
                        _m_mThread._m_mQueueTask.status = "1";
                    }

                    _m_mThread.m_mRecord.CallResultID = 30;
                    _m_mThread.m_mRecord.Remark = "inbound socket connect fail";
                    _m_mThread.m_mRecord.C_EndTime = Cmn.m_fDateTimeString();

                    Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} insert record...]");
                    call_record.Insert(_m_mThread.m_mRecord);

                    _m_mThread._m_mQueueTask.result = "1";
                    _m_mThread._m_mQueueTask.callTime = m_sStartTimeNowString;
                    _m_mThread._m_mQueueTask.endTime = m_sStartTimeNowString;
                    Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} rollback auto dial task...]");
                    m_fUpdateQueueTaskResult(_m_mThread._m_mQueueTask, m_sExtensionStr);
                    Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} inbound socket connect fail...]");

                    _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                    _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                    _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;
                    return;
                }

                IDisposable m_eChannel183or200 = null;
                //TTS文件
                List<KeyValuePair<string, bool>> m_lPlayRecords = new List<KeyValuePair<string, bool>>();

                bool m_bIsDispose = false;
                m_sClient.Disposed += (a, b) =>
                {
                    try
                    {
                        m_bIsDispose = true;

                        //状态复原
                        _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                        _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;

                        //TTS文件删除
                        foreach (KeyValuePair<string, bool> m_kPlayRecord in m_lPlayRecords)
                        {
                            if (m_kPlayRecord.Value && !string.IsNullOrWhiteSpace(m_kPlayRecord.Key))
                            {
                                if (File.Exists(m_kPlayRecord.Key))
                                {
                                    File.Delete(m_kPlayRecord.Key);
                                    Log.Instance.Debug($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} delete tts files:{m_kPlayRecord.Key}...]");
                                }
                            }
                        }
                        Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} delete tts files...]");

                        Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} inbound socket dispose...]");

                        if (m_eChannel183or200 != null)
                        {
                            m_eChannel183or200.Dispose();
                            Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} event 183,200 dispose]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} inbound socket dispose error:{ex.Message}...]");
                    }
                };

                string m_sEndPointStr = string.Empty;
                string m_sApplicationStr = Call_ParamUtil._application;
                string m_sTTSUrl = Call_ParamUtil.m_sTTSUrl;

                //自动外呼app
                bool m_bApp = !string.IsNullOrWhiteSpace(Call_ParamUtil.m_sDialTaskApp);
                if (m_bApp) m_sApplicationStr = Call_ParamUtil.m_sDialTaskApp;

                int m_uTimeoutSeconds = Call_ParamUtil.__timeout_seconds;
                bool m_bIgnoreEarlyMedia = Call_ParamUtil.__ignore_early_media;
                string m_sWhoHangUpStr = string.Empty;

                if (m_bStar)
                {
                    m_sEndPointStr = $"user/{m_lStrings[0]}";
                }
                else
                {
                    if (_m_mDialLimit.m_bGatewayType)
                        m_sEndPointStr = $"sofia/gateway/{_m_mDialLimit.m_sGatewayNameStr}/{m_sCalleeNumberStr}";
                    else
                        m_sEndPointStr = $"sofia/{_m_mDialLimit.m_sGatewayType}/sip:{m_sCalleeNumberStr}@{_m_mDialLimit.m_sGatewayNameStr}";
                }

                Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} a-leg-endpoint:{m_sEndPointStr},number:{_m_mThread._m_mQueueTask.CallNum}...]");

                _m_mThread._m_mQueueTask.callTime = m_sStartTimeNowString;
                _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_AUTODIAL;

                string uuid = Guid.NewGuid().ToString();

                if (m_bIsDispose) return;
                await m_sClient.SubscribeEvents(EventName.ChannelProgressMedia, EventName.ChannelAnswer).ContinueWith(task =>
                {
                    try
                    {
                        if (m_bIsDispose) return;
                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} event 183,200 cancel]");
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} event 183,200 error:{ex.Message}]");
                    }
                });

                bool Status183or200 = false;
                bool Channel200 = false;

                if (m_bIsDispose) return;
                m_eChannel183or200 = m_sClient.ChannelEvents.Where(x => x.UUID == uuid && (x.EventName == EventName.ChannelProgressMedia || x.EventName == EventName.ChannelAnswer)).Take(2).Subscribe(async x =>
                {
                    DateTime m_dtNow = DateTime.Now;
                    //200处理
                    if (x.EventName == EventName.ChannelAnswer && !Channel200)
                    {
                        //接通,解决200问题
                        Channel200 = true;
                        //计算接通时间和等待时间
                        string m_dtAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtNow);
                        _m_mThread.m_mRecord.C_AnswerTime = m_dtAnswerTimeNowString;
                        _m_mThread.m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtNow, m_dtStartTimeNow);
                    }
                    //183或200直接退出即可,是否需要语音识别?由于是网关的问题会直接接通
                    if (Status183or200) return;
                    Status183or200 = true;

                    if (false && m_bApp && m_sApplicationStr != "park")
                    {
                        if (m_bIsDispose) return;
                        await m_sClient.SendApi($"uuid_park {uuid}").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_park cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_park error:{ex.Message}...]");
                            }
                        });
                    }

                    string m_sDialTaskRecPath = Call_ParamUtil.m_sDialTaskRecPath;
                    string m_sRecordingFile = string.Empty;

                    //录音中来电主叫或去掉被叫的真实号码获取
                    string m_sRectNumberStr = (!string.IsNullOrWhiteSpace(m_stNumberStr) ? m_stNumberStr : _m_mThread.m_mRecord.LocalNum);
                    //兼容分离模式(Linux)录音及下载
                    string m_sRecSub = string.Empty;
                    if (!string.IsNullOrWhiteSpace(m_sDialTaskRecPath))
                    {
                        m_sRecSub = $"{{0}}{m_dtNow.ToString("yyyy")}/{m_dtNow.ToString("yyyyMM")}/{m_dtNow.ToString("yyyyMMdd")}/Rec_{m_dtNow.ToString("yyyyMMddHHmmss")}_{m_sRectNumberStr}_Z_{_m_mThread.m_mRecord.T_PhoneNum.Replace("*", "X")}{m_sExtensionStr}";
                        m_sRecordingFile = string.Format(m_sRecSub, m_sDialTaskRecPath);
                    }
                    else
                    {
                        m_sRecSub = $"{{0}}\\{m_dtNow.ToString("yyyy")}\\{m_dtNow.ToString("yyyyMM")}\\{m_dtNow.ToString("yyyyMMdd")}\\Rec_{m_dtNow.ToString("yyyyMMddHHmmss")}_{m_sRectNumberStr}_Z_{_m_mThread.m_mRecord.T_PhoneNum.Replace("*", "X")}{m_sExtensionStr}";
                        m_sRecordingFile = string.Format(m_sRecSub, ParamLib.RecordFilePath);
                    }
                    string m_sRecordingFolder = Path.GetDirectoryName(m_sRecordingFile);
                    if (!Directory.Exists(m_sRecordingFolder)) Directory.CreateDirectory(m_sRecordingFolder);
                    string m_sRecordingID = Path.GetFileNameWithoutExtension(m_sRecordingFile);

                    if (m_bIsDispose) return;
                    var recordingResult = await m_sClient.SendApi(string.Format("uuid_record {0} start {1}", uuid, m_sRecordingFile)).ContinueWith((task) =>
                    {
                        try
                        {
                            if (m_bIsDispose) return null;
                            if (task.IsCanceled)
                            {
                                Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_record cancel...]");
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
                            Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_record error:{ex.Message}...]");
                            return null;
                        }
                    });
                    if (recordingResult != null && recordingResult.Success)
                    {
                        Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_record success:{m_sRecordingID}]");
                        _m_mThread.m_mRecord.RecordFile = m_sRecordingFile;
                        _m_mThread.m_mRecord.recordName = m_sRecordingID;
                        _m_mThread._m_mQueueTask.luyinId = m_sRecordingID;
                    }
                    else
                    {
                        Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_record fail...]");
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
                                    Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_record cancel...]");
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
                                Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_record error:{ex.Message}...]");
                                return null;
                            }
                        });
                        if (m_pRecordingResult != null && m_pRecordingResult.Success)
                        {
                            Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} back up record success:{m_sBackUpRecordingFile}]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} back up record fail...]");
                        }
                    }
                    #endregion
                });

                if (m_bIsDispose) return;
                OriginateResult m_pOriginateResult = await m_sClient.Originate(m_sEndPointStr, new OriginateOptions()
                {
                    UUID = uuid,
                    CallerIdNumber = _m_mThread._m_mQueueTask.CallNum,
                    CallerIdName = _m_mThread._m_mQueueTask.CallNum,
                    HangupAfterBridge = false,
                    TimeoutSeconds = m_uTimeoutSeconds,
                    IgnoreEarlyMedia = m_bIgnoreEarlyMedia

                }, m_sApplicationStr).ContinueWith((task) =>
                {
                    try
                    {
                        if (m_bIsDispose) return null;
                        if (task.IsCanceled)
                        {
                            Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} originate fail...]");
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
                        Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} originate error:{ex.Message}...]");
                        return null;
                    }
                });

                //拨打次数加1
                _m_mThread._m_mQueueTask.CallCount += 1;
                if (m_pOriginateResult != null && !m_pOriginateResult.Success)
                {
                    string m_sOriginateResultStr = m_pOriginateResult?.ResponseText;
                    if (string.IsNullOrWhiteSpace(m_sOriginateResultStr)) m_sOriginateResultStr = m_pOriginateResult?.HangupCause?.ToString();
                    if (string.IsNullOrWhiteSpace(m_sOriginateResultStr)) m_sOriginateResultStr = null;

                    string m_sMsgStr = $"Originate fail:{m_sOriginateResultStr ?? "unknow"}".Replace("\r\n", "").Replace("\r", "").Replace("\n", "");

                    if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                    {
                        m_sWhoHangUpStr = "A";
                        Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} {m_sMsgStr}...]");

                        bool m_bLoop = false;

                        _m_mThread._m_mQueueTask.CallStatus = 1;
                        _m_mThread._m_mQueueTask.result = "1";
                        _m_mThread._m_mQueueTask.status = "0";
                        if (string.IsNullOrWhiteSpace(m_pOriginateResult.ResponseText))
                        {
                            _m_mThread.m_mRecord.CallResultID = 24;
                            _m_mThread._m_mQueueTask.CallStatus = 0;
                            _m_mThread._m_mQueueTask.result = "4";
                            m_bLoop = true;
                        }
                        else if (Cmn.IgnoreEquals(m_pOriginateResult.ResponseText, "NoUserResponse"))
                        {
                            _m_mThread.m_mRecord.CallResultID = 24;
                            _m_mThread._m_mQueueTask.CallStatus = 0;
                            _m_mThread._m_mQueueTask.result = "4";
                            m_bLoop = true;
                        }
                        else if (Cmn.IgnoreEquals(m_pOriginateResult.ResponseText, "NOANSWER"))
                        {
                            _m_mThread.m_mRecord.CallResultID = 24;
                            _m_mThread._m_mQueueTask.CallStatus = 0;
                            _m_mThread._m_mQueueTask.result = "4";
                            m_bLoop = true;
                        }
                        else if (Cmn.IgnoreEquals(m_pOriginateResult.ResponseText, "BUSY"))
                        {
                            _m_mThread.m_mRecord.CallResultID = 27;
                            _m_mThread._m_mQueueTask.CallStatus = 0;
                            _m_mThread._m_mQueueTask.result = "3";
                            m_bLoop = true;
                        }
                        else if (Cmn.IgnoreEquals(m_pOriginateResult.ResponseText, "USER_NOT_REGISTERED")) _m_mThread.m_mRecord.CallResultID = 28;
                        else _m_mThread.m_mRecord.CallResultID = 24;

                        DateTime m_dtEndTimeNow = DateTime.Now;
                        string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                        _m_mThread.m_mRecord.C_EndTime = m_sEndTimeNowString;
                        _m_mThread.m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                        _m_mThread.m_mRecord.Remark = m_sMsgStr;

                        Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} insert record...]");
                        call_record.Insert(_m_mThread.m_mRecord);

                        _m_mThread._m_mQueueTask.endTime = m_sEndTimeNowString;
                        Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update auto dial task...]");
                        m_fUpdateQueueTaskResult(_m_mThread._m_mQueueTask, m_sExtensionStr, m_bLoop);

                        if (!m_bStar)
                        {
                            Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update dial limit...]");
                            m_fDialLimit.m_fSetDialLimit(_m_mThread._m_mQueueTask.CallNum, m_mAgent.AgentID, 0);
                        }

                        _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                        _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;

                        if (m_bIsDispose) return;
                        if (m_sClient != null && m_sClient.IsConnected)
                        {
                            await m_sClient.Exit().ContinueWith(task =>
                            {
                                try
                                {
                                    if (task.IsCanceled) Log.Instance.Warn($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} exit cancel...]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} exit error:{ex.Message}...]");
                                }
                            });
                        }
                    }
                }
                else
                {
                    if (true && m_bApp && m_sApplicationStr != "park")
                    {
                        //停止放音

                        if (m_bIsDispose) return;
                        await m_sClient.SendApi($"uuid_park {uuid}").ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_park cancel]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} uuid_park error:{ex.Message}...]");
                            }
                        });
                    }

                    Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} talking...]");
                    _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_TALKING;
                    _m_mThread.m_mChannelInfo.channel_call_uuid = uuid;

                    #region ***计算接通时间和等待时间
                    DateTime m_dtAnswerTimeNow = DateTime.Now;
                    //如果比200快,这里应该不可能,而且如果接通,一定会有200消息
                    if (false && !Channel200)
                    {
                        string m_dtAnswerTimeNowString = Cmn.m_fDateTimeString(m_dtAnswerTimeNow);
                        _m_mThread.m_mRecord.C_AnswerTime = m_dtAnswerTimeNowString;
                        _m_mThread.m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtAnswerTimeNow, m_dtStartTimeNow);
                    }
                    #endregion

                    if (m_bIsDispose) return;
                    m_sClient.OnHangup(uuid, e =>
                    {
                        if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                        {
                            m_sWhoHangUpStr = "B";
                            DateTime m_dtEndTimeNow = DateTime.Now;
                            string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                            _m_mThread.m_mRecord.C_EndTime = m_sEndTimeNowString;
                            //修正通话时长
                            if (Channel200)
                            {
                                _m_mThread.m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, _m_mThread.m_mRecord.C_AnswerTime);
                                _m_mThread.m_mRecord.CallResultID = 23;
                            }
                            else
                            {
                                _m_mThread.m_mRecord.C_SpeakTime = 0;
                                _m_mThread.m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                                _m_mThread.m_mRecord.CallResultID = 25;
                            }

                            _m_mThread._m_mQueueTask.endTime = m_sEndTimeNowString;
                            _m_mThread._m_mQueueTask.CallStatus = 1;
                            _m_mThread._m_mQueueTask.status = "0";
                            _m_mThread._m_mQueueTask.result = "5";

                            Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} insert record...]");
                            call_record.Insert(_m_mThread.m_mRecord);

                            Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update auto dial task...]");
                            m_fUpdateQueueTaskResult(_m_mThread._m_mQueueTask, m_sExtensionStr);

                            if (!m_bStar)
                            {
                                Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update dial limit...]");
                                m_fDialLimit.m_fSetDialLimit(_m_mThread._m_mQueueTask.CallNum, m_mAgent.AgentID, _m_mThread.m_mRecord.C_SpeakTime);
                            }

                            _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                            _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;

                            if (m_bIsDispose) return;
                            if (m_sClient != null && m_sClient.IsConnected)
                            {
                                if (m_bIsDispose) return;
                                m_sClient.Exit().ContinueWith((task) =>
                                {
                                    try
                                    {
                                        if (task.IsCanceled)
                                        {
                                            Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} exit fail...]");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} exit error:{ex.Message}...]");
                                    }
                                });
                            }
                        }
                    });

                    #region TTS播放
                    ///<![CDATA[
                    /// TTS播放
                    /// ]]>
                    m_lPlayRecords = await m_fPlayRecords(_m_mThread._m_mQueueTask.contentTxt);
                    int m_uPlayLoops = Call_ParamUtil.m_uPlayLoops;
                    for (int i = 0; i < m_uPlayLoops; i++)
                    {
                        foreach (KeyValuePair<string, bool> m_kPlayRecord in m_lPlayRecords)
                        {
                            if (!string.IsNullOrWhiteSpace(m_kPlayRecord.Key))
                            {
                                //是否需要置换路径
                                string m_sPlayRecord = m_kPlayRecord.Key;
                                if (!string.IsNullOrWhiteSpace(m_sTTSUrl))
                                {
                                    m_sPlayRecord = m_sPlayRecord.Replace(ParamLib.Tts_AudioFilePath, m_sTTSUrl);
                                }

                                if (m_bIsDispose) return;
                                await m_sClient.Play(uuid, m_sPlayRecord).ContinueWith((task) =>
                                {
                                    try
                                    {
                                        if (m_bIsDispose) return;
                                        if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} play loop {m_uPlayLoops + 1},{m_kPlayRecord.Key} fail...]");
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} play loop {m_uPlayLoops + 1},{m_kPlayRecord.Key}: error:{ex.Message}...]");
                                    }
                                });
                            }
                        }
                    }
                    #endregion

                    if (string.IsNullOrWhiteSpace(m_sWhoHangUpStr))
                    {
                        m_sWhoHangUpStr = "A";
                        DateTime m_dtEndTimeNow = DateTime.Now;
                        string m_sEndTimeNowString = Cmn.m_fDateTimeString(m_dtEndTimeNow);
                        _m_mThread.m_mRecord.C_EndTime = m_sEndTimeNowString;
                        //修正通话时长
                        if (Channel200)
                        {
                            _m_mThread.m_mRecord.C_SpeakTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, _m_mThread.m_mRecord.C_AnswerTime);
                            _m_mThread.m_mRecord.CallResultID = 22;
                        }
                        else
                        {
                            _m_mThread.m_mRecord.C_SpeakTime = 0;
                            _m_mThread.m_mRecord.C_WaitTime = Cmn.m_fUnsignedSeconds(m_dtEndTimeNow, m_dtStartTimeNow);
                            _m_mThread.m_mRecord.CallResultID = 24;
                        }

                        _m_mThread._m_mQueueTask.endTime = m_sEndTimeNowString;
                        _m_mThread._m_mQueueTask.CallStatus = 1;
                        _m_mThread._m_mQueueTask.status = "0";
                        _m_mThread._m_mQueueTask.result = "2";

                        Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} insert record...]");
                        call_record.Insert(_m_mThread.m_mRecord);

                        Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update auto dial task...]");
                        m_fUpdateQueueTaskResult(_m_mThread._m_mQueueTask, m_sExtensionStr);

                        if (!m_bStar)
                        {
                            Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} update dial limit...]");
                            m_fDialLimit.m_fSetDialLimit(_m_mThread._m_mQueueTask.CallNum, m_mAgent.AgentID, _m_mThread.m_mRecord.C_SpeakTime);
                        }

                        _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                        _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                        _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;

                        if (m_bIsDispose) return;
                        await m_sClient.Hangup(uuid, HangupCause.NormalClearing).ContinueWith((task) =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} play file end hangup fail...]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} play file end hangup error:{ex.Message}...]");
                            }
                        });

                        if (m_bIsDispose) return;
                        await m_sClient.Exit().ContinueWith(task =>
                        {
                            try
                            {
                                if (m_bIsDispose) return;
                                if (task.IsCanceled) Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} exit cancel...]");
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} exit error:{ex.Message}...]");
                                return;
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fDoTask][{_m_mThread.m_tThread.Name} error:{ex.Message} {ex.StackTrace}...]");
                _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                _m_mThread.m_mChannelInfo.channel_call_uuid = null;
                _m_mThread.m_mChannelInfo.channel_call_other_uuid = null;
            }
        }

        public static void m_fUpdateQueueTaskResult(m_mQueueTask _m_mQueueTask, string m_sExtensionStr, bool m_bLoop = false)
        {
            string m_sStatusStr = _m_mQueueTask.status ?? "0";
            if (m_bLoop)
            {
                //拨打3次即可,后续将此拨打数量提至配置文件中
                int m_uDialCount = Call_ParamUtil.m_uDialCount;
                if (_m_mQueueTask.CallCount >= m_uDialCount)
                {
                    m_sStatusStr = "0";
                    _m_mQueueTask.CallStatus = 1;
                }
                else
                {
                    m_sStatusStr = "9";
                }
            }

            #region 无ID处理
            ///<![CDATA[
            /// 无ID列作为新增项
            /// ]]>

            if (_m_mQueueTask.ID == null)
            {
                PhoneAutoCall_model _m_mPhoneAutoCall = new PhoneAutoCall_model();
                _m_mPhoneAutoCall.PhoneNum = _m_mQueueTask.PhoneNum;
                _m_mPhoneAutoCall.pici = _m_mQueueTask.pici;
                try
                {
                    if (string.IsNullOrWhiteSpace(_m_mQueueTask.progressFlag)) _m_mPhoneAutoCall.progressFlag = null;
                    else _m_mPhoneAutoCall.progressFlag = Convert.ToInt32(_m_mQueueTask.progressFlag);
                }
                catch { }
                _m_mPhoneAutoCall.contentTxt = _m_mQueueTask.contentTxt;
                _m_mPhoneAutoCall.status = _m_mQueueTask.status;
                try
                {
                    if (string.IsNullOrWhiteSpace(_m_mQueueTask.addTime)) _m_mPhoneAutoCall.addTime = DateTime.Now;
                    else _m_mPhoneAutoCall.addTime = Convert.ToDateTime(_m_mQueueTask.addTime);
                }
                catch { }
                try
                {
                    if (string.IsNullOrWhiteSpace(_m_mQueueTask.callTime)) _m_mPhoneAutoCall.callTime = null;
                    else _m_mPhoneAutoCall.callTime = Convert.ToDateTime(_m_mQueueTask.callTime);
                }
                catch { }
                try
                {
                    if (string.IsNullOrWhiteSpace(_m_mQueueTask.endTime)) _m_mPhoneAutoCall.endTime = null;
                    else _m_mPhoneAutoCall.endTime = Convert.ToDateTime(_m_mQueueTask.endTime);
                }
                catch { }
                _m_mPhoneAutoCall.result = _m_mQueueTask.result;
                _m_mPhoneAutoCall.IsUpdate = _m_mQueueTask.IsUpdate;
                _m_mPhoneAutoCall.luyinId = _m_mQueueTask.luyinId;
                _m_mPhoneAutoCall.CallNum = _m_mQueueTask.CallNum;
                _m_mPhoneAutoCall.CallStatus = _m_mQueueTask.CallStatus;
                _m_mPhoneAutoCall.CallCount = _m_mQueueTask.CallCount;
                try
                {
                    if (string.IsNullOrWhiteSpace(_m_mQueueTask.source_id)) _m_mPhoneAutoCall.source_id = null;
                    else _m_mPhoneAutoCall.source_id = Convert.ToInt32(_m_mQueueTask.source_id);
                }
                catch { }
                _m_mPhoneAutoCall.ajid = _m_mQueueTask.ajid;
                _m_mPhoneAutoCall.inpici = _m_mQueueTask.inpici;
                _m_mPhoneAutoCall.shfzh18 = _m_mQueueTask.shfzh18;
                _m_mPhoneAutoCall.czy = _m_mQueueTask.czy;
                _m_mPhoneAutoCall.asr_status = _m_mQueueTask.asr_status;
                if (!Call_ParamUtil.m_bIsDialTaskAsr)
                {
                    _m_mPhoneAutoCall.asr_status = 2;
                }

                //不需要上报,但需要保存记录
                if (!Call_ParamUtil.m_bUseDialTaskInterface || string.IsNullOrWhiteSpace(Call_ParamUtil.m_sUpInterface))
                {
                    bool m_bInsert = PhoneAutoCall.Insert(_m_mPhoneAutoCall);
                    if (m_bInsert)
                    {
                        Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][update -> insert auto dial task success]");
                    }
                    else
                    {
                        Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][update -> insert auto dial task fail]");
                    }
                }
                else
                {
                    //如果有语音识别,看看数据是否需要识别还是直接上报

                    bool m_bIsUpNow = false;
                    if (Call_ParamUtil.m_bIsDialTaskAsr)
                    {
                        //开启了语音识别,完成状态,已拨打,结果2或5,直接上报即可
                        if (m_sStatusStr == "0" && _m_mQueueTask.CallStatus == 1 && (_m_mQueueTask.result == "2" || _m_mQueueTask.result == "5"))
                        {
                            m_bIsUpNow = true;
                        }
                    }
                    else
                    {
                        m_bIsUpNow = true;
                    }

                    //如果可以直接上报
                    if (m_bIsUpNow)
                    {
                        #region ***上报
                        new System.Threading.Thread(() =>
                        {
                            bool m_bIsUpInterface = false;
                            try
                            {
                                var args = $"recordId={_m_mPhoneAutoCall.ajid}&resultCode={_m_mPhoneAutoCall.result}&diallingTime={_m_mPhoneAutoCall.callTime?.ToString("yyyy-MM-dd HH:mm:ss")}&phoneticUrl={m_fDoTaskClass.m_fGetHttpRec(_m_mPhoneAutoCall.luyinId, m_sExtensionStr)}";
                                var request = (HttpWebRequest)WebRequest.Create(Call_ParamUtil.m_sUpInterface);
                                request.Method = "POST";
                                request.ContentType = "application/x-www-form-urlencoded";
                                byte[] byteData = Encoding.UTF8.GetBytes(args);
                                int length = byteData.Length;
                                request.ContentLength = length;
                                Stream writer = request.GetRequestStream();
                                writer.Write(byteData, 0, length);
                                writer.Close();
                                var response = (HttpWebResponse)request.GetResponse();
                                var responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
                                if (!string.IsNullOrWhiteSpace(responseString))
                                {
                                    try
                                    {
                                        JObject m_bJObject = JObject.Parse(responseString);
                                        if (Convert.ToBoolean(m_bJObject["success"]))
                                        {
                                            Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][ajid:{_m_mPhoneAutoCall.ajid},luyinId:{_m_mPhoneAutoCall.luyinId} POST success,set already update,go on]");
                                            _m_mPhoneAutoCall.IsUpdate = 1;
                                        }
                                        else
                                        {
                                            Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][ajid:{_m_mPhoneAutoCall.ajid},luyinId:{_m_mPhoneAutoCall.luyinId} POST fail:{m_bJObject["result"]}]");
                                        }
                                        bool m_bInsert = PhoneAutoCall.Insert(_m_mPhoneAutoCall);
                                        if (m_bInsert)
                                        {
                                            Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][update -> insert auto dial task success]");
                                        }
                                        else
                                        {
                                            Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][update -> insert auto dial task fail]");
                                        }
                                        m_bIsUpInterface = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][Exception][{ex.Message}]");
                                    }
                                }
                                else
                                {
                                    Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][ajid:{_m_mPhoneAutoCall.ajid},luyinId:{_m_mPhoneAutoCall.luyinId} POST no response]");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][Exception][ajid:{_m_mPhoneAutoCall.ajid},luyinId:{_m_mPhoneAutoCall.luyinId} POST error:{ex.Message}]");
                            }
                            if (!m_bIsUpInterface)
                            {
                                bool m_bInsert = PhoneAutoCall.Insert(_m_mPhoneAutoCall);
                                if (m_bInsert)
                                {
                                    Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][upinterface fail:update -> insert auto dial task success]");
                                }
                                else
                                {
                                    Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][upinterface fail:update -> insert auto dial task fail]");
                                }
                                m_bIsUpInterface = true;
                            }
                        }).Start();
                        #endregion
                    }
                    else
                    {
                        bool m_bInsert = PhoneAutoCall.Insert(_m_mPhoneAutoCall);
                        if (m_bInsert)
                        {
                            Log.Instance.Success($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][update -> insert -> asr auto dial task success]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fUpdateQueueTaskResult][update -> insert -> asr auto dial task fail]");
                        }
                    }
                }
                return;
            }
            #endregion

            PhoneAutoCall.Update(_m_mQueueTask.ID, new object[]
            {
                "result",_m_mQueueTask.result,
                "CallCount",_m_mQueueTask.CallCount,
                "callTime",_m_mQueueTask.callTime,
                "endTime",_m_mQueueTask.endTime,
                "status",m_sStatusStr,
                "luyinId",_m_mQueueTask.luyinId,
                "CallStatus",_m_mQueueTask.CallStatus,
                "CallNum",_m_mQueueTask.CallNum
            });
        }

        private async static Task<List<KeyValuePair<string, bool>>> m_fPlayRecords(string m_sContent)
        {
            List<KeyValuePair<string, bool>> m_lResultKeyValuePair = new List<KeyValuePair<string, bool>>();
            string m_sFileNamePrefix = $"{DateTime.Now.ToString("yyyyMMddHHmmssffffff")}_{Guid.NewGuid()}"
                .Replace("-", "");
            string[] m_lContentString = m_sContent.Split('|');
            Regex m_pRegex = new Regex("[Rr][Ee][Cc]_[0-9]+");
            int m_uInt = 1;
            string m_sDialTaskTTSProvider = Call_ParamUtil.m_sDialTaskTTSProvider;
            foreach (string m_sContentString in m_lContentString)
            {
                if (string.IsNullOrWhiteSpace(m_sContentString)) continue;
                if (m_pRegex.IsMatch(m_sContentString))
                {
                    string m_sRecPath = tellModelRecord.m_fGetRecPath(m_sContentString.Split('_')[1]);
                    if (!string.IsNullOrWhiteSpace(m_sRecPath)) m_lResultKeyValuePair.Add(new KeyValuePair<string, bool>(m_sRecPath, false));
                }
                else
                {
                    switch (m_sDialTaskTTSProvider)
                    {
                        case "lx":
                            string m_sTtsPath = $"{ParamLib.Tts_AudioFilePath}/{m_sFileNamePrefix}_{m_uInt++}.wav";
                            bb_tts m_pTts = new bb_tts();
                            m_pTts.audio_file = m_sTtsPath;
                            m_pTts.text = m_sContentString;
                            m_pTts.speed = Call_ParamUtil.m_iDialTaskTTSSetting;
                            await m_pTts.run().ContinueWith(task => { });
                            m_lResultKeyValuePair.Add(new KeyValuePair<string, bool>(m_sTtsPath, true));
                            break;
                        default:
                            m_lResultKeyValuePair.Add(new KeyValuePair<string, bool>(new CreateSound().CreSound_bak(m_sContentString, $"{m_sFileNamePrefix}_{m_uInt++}.wav", Call_ParamUtil.m_iDialTaskTTSSetting), true));
                            break;
                    }
                }
            }
            return m_lResultKeyValuePair;
        }

        private static string m_fGetHttpRec(string m_sRecID, string m_sExtensionStr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(m_sExtensionStr))
                {
                    m_sExtensionStr = DB.Basic.Call_ParamUtil._rec_t;
                }
                if (!string.IsNullOrWhiteSpace(m_sExtensionStr))
                {
                    if (m_sRecID.Length >= 12)
                    {
                        string yyyy = m_sRecID.Substring(4, 4);
                        string MM = m_sRecID.Substring(8, 2);
                        string dd = m_sRecID.Substring(10, 2);
                        return $"/{yyyy}/{yyyy}{MM}/{yyyy}{MM}{dd}/{m_sRecID}{m_sExtensionStr}";
                    }
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Instance.Fail($"[CenoFsSharp][m_fDoTaskClass][m_fGetHttpRec][Exception][{ex.Message}]");
                return string.Empty;
            }
        }
    }
}
