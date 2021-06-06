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

namespace CenoFsSharp {
    public class ChannelFunc {
        private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void m_fDoCall(OutboundSocket _Socket) {
            try {
                string uuid = _Socket?.ChannelData?.UUID;
                string m_sALegPhoneNumberStr = _Socket?.ChannelData?.GetHeader("Channel-Caller-ID-Number");
                string m_sALegIPStr = _Socket?.ChannelData?.GetHeader("variable_sip_received_ip");
                string m_sBLegPhoneNumberStr = _Socket?.ChannelData?.GetHeader("Channel-Destination-Number");
                string m_sBLegIPStr = _Socket?.ChannelData?.GetHeader("variable_sip_req_host");
                string profile = _Socket?.ChannelData?.GetHeader("variable_recovery_profile_name");

                //移除
                {
                    ///|[Uu][Nn][Kk][Nn][Oo][Ww][Nn],去掉
                    Regex m_rIsMatchRegexCaller = new Regex("^[0-9*#\\+]{3,}$");
                    ///兼容IMS
                    Regex m_rIsMatchRegexCallee = new Regex("^[0-9*#\\+@.a-z]{3,}$");
                    if (!m_rIsMatchRegexCaller.IsMatch(m_sALegPhoneNumberStr) || !m_rIsMatchRegexCallee.IsMatch(m_sBLegPhoneNumberStr))
                    {
                        Log.Instance.Debug($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} a-leg-number:{m_sALegPhoneNumberStr};a-leg-ip:{m_sALegIPStr}]");
                        Log.Instance.Debug($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} b-leg-number:{m_sBLegPhoneNumberStr};b-leg-ip:{m_sBLegIPStr}]");
                        Log.Instance.Debug($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} hangup]");

                        #region ***直接毙掉
                        if (_Socket != null && _Socket.IsConnected)
                        {
                            _Socket.Hangup(uuid, HangupCause.CallRejected).ContinueWith(task =>
                            {
                                try
                                {
                                    if (task != null && task.IsCanceled) Log.Instance.Debug($"[CenoFsSharp][ChannelFunc][m_fDoCall][{uuid} Hangup cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Debug($"[CenoFsSharp][ChannelFunc][m_fDoCall][{uuid} Hangup error:{ex.Message}]");
                                }
                            });
                        }

                        if (_Socket != null && _Socket.IsConnected)
                        {
                            _Socket.Exit().ContinueWith(task =>
                            {
                                try
                                {
                                    if (task != null && task.IsCanceled) Log.Instance.Debug($"[CenoFsSharp][ChannelFunc][m_fDoCall][{uuid} Exit cancel]");
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Debug($"[CenoFsSharp][ChannelFunc][m_fDoCall][{uuid} Exit error:{ex.Message}]");
                                }
                            });
                        }
                        _Socket?.Dispose();
                        #endregion
                        return;
                    }
                }

                ///<![CDATA[
                /// 后续：
                /// UA不论,基本一样的逻辑即可
                /// ]]>

                switch (profile) {
                    case "internal":
                        Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} a-leg-number:{m_sALegPhoneNumberStr};a-leg-ip:{m_sALegIPStr}]");
                        Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} b-leg-number:{m_sBLegPhoneNumberStr};b-leg-ip:{m_sBLegIPStr}]");
                        CenoFsSharp.m_fDialClass.m_fDial(_Socket);
                        //_call_do(_Socket);
                        break;
                    case "external":
                        Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} a-leg-number:{m_sALegPhoneNumberStr};a-leg-ip:{m_sALegIPStr}]");
                        Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} b-leg-number:{m_sBLegPhoneNumberStr};b-leg-ip:{m_sBLegIPStr}]");
                        CenoFsSharp.m_fCallClass.m_fCall(_Socket);
                        //_call_do(_Socket);
                        break;
                    default:
                        Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} a-leg-number:{m_sALegPhoneNumberStr};a-leg-ip:{m_sALegIPStr}]");
                        Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][m_fDoCall][{profile}][{uuid} b-leg-number:{m_sBLegPhoneNumberStr};b-leg-ip:{m_sBLegIPStr}]");

                        if (m_sALegPhoneNumberStr.Length == 4 || profile.IndexOf("internal", StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            CenoFsSharp.m_fDialClass.m_fDial(_Socket);
                        }
                        else
                        {
                            CenoFsSharp.m_fCallClass.m_fCall(_Socket);
                        }
                        //_call_do(_Socket);
                        break;
                }
            } catch(Exception ex) {

                try
                {
                    Log.Instance.Error($"[CenoFsSharp][ChannelFunc][channel_connected][Exception][{_Socket?.ChannelData?.UUID},主叫号码:{_Socket?.ChannelData?.GetHeader("Channel-Caller-ID-Number")},主叫IP:{_Socket?.ChannelData?.GetHeader("variable_sip_received_ip")},被叫号码:{_Socket?.ChannelData?.GetHeader("Channel-Destination-Number")},被叫IP:{_Socket?.ChannelData?.GetHeader("variable_sip_req_host")},来电错误:{ex.Message}]");
                    Log.Instance.Debug(ex);
                }
                catch (Exception _ex)
                {
                    Log.Instance.Error($"[CenoFsSharp][ChannelFunc][channel_connected][Exception][Exception][{_ex.Message}]");
                }
            }
        }

        #region 弃用
        [Obsolete("请使用_call_do")]
        private async static void channel_dial_process(OutboundSocket _Socket) {

            Log.Instance.Success($"[CenoFsSharp][ChannelFunc][channel_dial_process][internal]");

            var Destination_Number = _Socket.ChannelData.GetHeader("Channel-Destination-Number");
            var Callee_Chinfo = call_factory.channel_list.FirstOrDefault(x => x.channel_number == Destination_Number);

            if(Callee_Chinfo == null) {

                var UUID = _Socket.ChannelData.Headers["Channel-Call-UUID"];

                //没有找到,发送挂断消息
                await _Socket.Hangup(UUID, HangupCause.UserNotRegistered);

                _Ilog.Fatal($"{Destination_Number}用户未注册,已挂断");

                return;
            }

            Callee_Chinfo.channel_call_uuid = _Socket.ChannelData.UUID;
            Callee_Chinfo.channel_name = _Socket.ChannelData.GetHeader("Channel-Channel-Name");
            Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
            Callee_Chinfo.channel_caller_number = new StringBuilder(Destination_Number);
            try {
                Callee_Chinfo.channel_call_record_info = CH_CALL_RECORD.Instance(Callee_Chinfo);
                KeyValuePair<string, string> dial_number_info = PhoneNumOperate.get_out_dial_number(Destination_Number);
                Callee_Chinfo.channel_call_record_info.C_PhoneNum = new StringBuilder(dial_number_info.Key);
                Callee_Chinfo.channel_call_record_info.PhoneAddress = dial_number_info.Value;
                call_calltype_model CallTypeModel = call_calltype.GetModel("DIAL");
                if(CallTypeModel == null)
                    throw new Exception("get call type error");
                Callee_Chinfo.channel_call_record_info.CallType = CallTypeModel.ID;

                Callee_Chinfo.channel_call_record_info.Call_Date = DateTime.Now.Date.ToString();
                Callee_Chinfo.channel_call_record_info.C_StartTime = GlobalParam.GetNowDateTime;
            } catch(Exception ex) {
                _Ilog.Error(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " initial call record error:" + ex.Message);
            }

            await _Socket.Linger();
            _Ilog.Info(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " wait response after send linger commands to fs");

            var recordingResult = await ChRecording.Recording(Callee_Chinfo.channel_call_uuid, _Socket, Callee_Chinfo);
            if(recordingResult.Success) {
                _Ilog.Info(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " start recording to file " + Callee_Chinfo.channel_call_record_info.RecFile);
            } else {
                _Ilog.Error(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " error recording to file " + Callee_Chinfo.channel_call_record_info.RecFile);
            }

            _Ilog.Info(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " send early media:" + ParamLib.DialingAudioFile);
            {
                try {
                    await _Socket.SetChannelVariable(_Socket.ChannelData.UUID, "instant_ringback", true);
                    await _Socket.SetChannelVariable(_Socket.ChannelData.UUID, "transfer_ringback", ParamLib.DialingAudioFile);
                } catch(Exception ex) {

                }
            }

            //bridge b-leg
            {
                BridgeOptions bridgeOp = new BridgeOptions();
                var Caller_Agent_Info = call_factory.agent_list.FirstOrDefault(x => x.ChInfo == Callee_Chinfo);
                if(Caller_Agent_Info != null) {
                    bridgeOp.CallerIdNumber = Caller_Agent_Info.AgentNum;
                    bridgeOp.CallerIdName = Caller_Agent_Info.LoginName;
                }
                if(string.IsNullOrEmpty(Callee_Chinfo.channel_switch_tactics.Dial_Switch_Adapter.LinkChUid)) {
                    _Ilog.Error(Callee_Chinfo.channel_call_uuid + " " + Callee_Chinfo.channel_name + " can't find switchtactics,fs send hangup to client");
                    channel_hangup_process(Callee_Chinfo);
                    return;
                }

                var dial_gateway_info = call_gateway.GetModel_UniqueID(Callee_Chinfo.channel_switch_tactics.Dial_Switch_Adapter.LinkChUid);

                _Ilog.Info($"{Callee_Chinfo.channel_call_uuid} {Callee_Chinfo.channel_name} send bridge commands(sofia/gateway/{dial_gateway_info.gw_name}/{Destination_Number}) to fs");
                //BridgeResult _bridgeResult = await _Socket.Bridge(Callee_Chinfo.channel_call_uuid, $"sofia/gateway/{dial_gateway_info.gw_name}/{Destination_Number}", bridgeOp);
                //BridgeResult _bridgeResult = await _Socket.Bridge(Callee_Chinfo.channel_call_uuid, $"sofia/external/sip:{Destination_Number}@192.168.0.20:5060", bridgeOp);
                BridgeResult _bridgeResult = await _Socket.Bridge(Callee_Chinfo.channel_call_uuid, $"sofia/external/sip:{Destination_Number}@{dial_gateway_info.gw_name}", bridgeOp);
                if(_bridgeResult.Success) {
                    Callee_Chinfo.channel_call_other_uuid = _bridgeResult.ChannelData.OtherLegUUID;
                    Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_UN_ANSWER;
                    Callee_Chinfo.channel_call_record_info.C_AnswerTime = GlobalParam.GetNowDateTime;
                } else {
                    _Ilog.Error($"{Callee_Chinfo.channel_call_uuid} {Callee_Chinfo.channel_name} bridge failed:{_bridgeResult.ResponseText}");
                    await _Socket.Hangup(_Socket.ChannelData.UUID);
                    await _Socket.Exit();
                }
            }
        }
        #endregion

        #region 弃用
        [Obsolete("请使用_call_do")]
        private async static void channel_call_process(OutboundSocket _Socket) {

            #region 沿用

            //这里不先做那么多判断了,直接呼叫,存储就行了
            var Destination_Number = _Socket.ChannelData.GetHeader("Channel-Destination-Number");
            var UUID = _Socket.ChannelData.UUID;//.Headers["Channel-Call-UUID"];
            var agent = call_factory.agent_list.FirstOrDefault(x => x.AgentNum == Destination_Number);
            if(agent == null) {
                //没有找到,发送挂断消息
                await _Socket.Hangup(UUID, HangupCause.UserNotRegistered);
                _Ilog.Fatal($"{Destination_Number}用户未注册,已挂断");
                return;
            }

            var nCh = agent.ChInfo.nCh;

            var Callee_Chinfo = call_factory.channel_list[agent.ChInfo.nCh];

            if(Callee_Chinfo == null) {
                await _Socket.Hangup(UUID, HangupCause.UserNotRegistered);
                _Ilog.Fatal($"{Destination_Number}用户未注册,已挂断");
                return;
            }

            #region 实例
            //这里查找网关,应该是为了可以回拨
            call_record_model entity = new call_record_model();
            entity.UniqueID = Guid.NewGuid().ToString();
            entity.CallType = 3;
            entity.ChannelID = Callee_Chinfo.channel_id;
            entity.LinkChannelID = -1;
            entity.LocalNum = call_factory.agent_list[nCh].AgentNum;
            entity.T_PhoneNum = _Socket.ChannelData.Headers["Channel-Caller-ID-Number"];
            entity.C_PhoneNum = _Socket.ChannelData.Headers["Channel-Caller-ID-Number"];
            entity.PhoneAddress = "????";
            entity.DtmfNum = "";
            entity.PhoneTypeID = -1;
            entity.PhoneListID = -1;
            entity.PriceTypeID = -1;
            entity.CallPrice = -1;
            entity.AgentID = agent.AgentID;
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
            entity.CallResultID = 18;
            entity.CallForwordFlag = -1;
            entity.CallForwordChannelID = "-1";
            entity.SerOp_ID = -1;
            entity.SerOp_DTMF = "";
            entity.SerOp_LeaveRec = "";
            entity.Detail = "";
            entity.Remark = "";
            #endregion

            //谁挂断
            var WhoHung = String.Empty;

            /* 先直接Insert,后续全部变为更新 */
            call_record.Insert(entity);

            //这里需要设计一个ivr,但是这里不是很会写
            if(Destination_Number == "1000" && false) {
                //没有找到,发送挂断消息
                await _Socket.Hangup(UUID, HangupCause.UserNotRegistered);
                _Ilog.Fatal($"请直接拨打分机号");
                return;
            }

            if(Callee_Chinfo == null) {
                //没有找到,发送挂断消息
                await _Socket.Hangup(UUID, HangupCause.UserNotRegistered);
                _Ilog.Fatal($"{Destination_Number}用户未注册,已挂断");
                return;
            }

            if(Callee_Chinfo.channel_socket._Socket == null) {
                await _Socket.Hangup(UUID, HangupCause.UserNotRegistered);
                _Ilog.Fatal($"{Destination_Number}用户未连接服务器,已挂断");
                return;
            }

            Callee_Chinfo.channel_call_uuid = _Socket.ChannelData.UUID;
            Callee_Chinfo.channel_name = _Socket.ChannelData.GetHeader("Channel-Channel-Name");
            Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
            Callee_Chinfo.channel_caller_number = new StringBuilder(Destination_Number);
            try {
                Callee_Chinfo.channel_call_record_info = CH_CALL_RECORD.Instance(Callee_Chinfo);
                KeyValuePair<string, string> dial_number_info = PhoneNumOperate.get_out_dial_number(Destination_Number);
                Callee_Chinfo.channel_call_record_info.C_PhoneNum = new StringBuilder(dial_number_info.Key);
                Callee_Chinfo.channel_call_record_info.PhoneAddress = dial_number_info.Value;
                call_calltype_model CallTypeModel = call_calltype.GetModel("DIAL");
                if(CallTypeModel == null)
                    throw new Exception("get call type error");
                Callee_Chinfo.channel_call_record_info.CallType = CallTypeModel.ID;

                Callee_Chinfo.channel_call_record_info.Call_Date = DateTime.Now.Date.ToString();
                Callee_Chinfo.channel_call_record_info.C_StartTime = GlobalParam.GetNowDateTime;
            } catch(Exception ex) {
                _Ilog.Error(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " initial call record error:" + ex.Message);
            }

            await _Socket.Linger();
            _Ilog.Info(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " wait response after send linger commands to fs");

            //通道录音

            await ChRecording.Recording(Callee_Chinfo.channel_call_uuid, _Socket, Callee_Chinfo);
            entity.RecordFile = Callee_Chinfo.channel_call_record_info.RecFile;

            _Ilog.Info(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " start recording to file " + Callee_Chinfo.channel_call_record_info.RecFile);

            _Ilog.Info(_Socket.ChannelData.UUID + " " + Callee_Chinfo.channel_name + " send early media:" + ParamLib.DialingAudioFile);
            {
                try {
                    await _Socket.SetChannelVariable(_Socket.ChannelData.UUID, "instant_ringback", true);
                    await _Socket.SetChannelVariable(_Socket.ChannelData.UUID, "transfer_ringback", ParamLib.DialingAudioFile);
                } catch(Exception ex) {

                }
            }

            //bridge b-leg
            {
                BridgeOptions bridgeOp = new BridgeOptions();
                var Caller_Agent_Info = call_factory.agent_list.FirstOrDefault(x => x.ChInfo == Callee_Chinfo);
                if(Caller_Agent_Info != null) {
                    bridgeOp.CallerIdNumber = entity.T_PhoneNum;
                    bridgeOp.CallerIdName = _Socket.ChannelData.Headers["Channel-Caller-ID-Name"];
                }
                bridgeOp.TimeoutSeconds = 20;
                if(string.IsNullOrEmpty(Callee_Chinfo.channel_switch_tactics.Dial_Switch_Adapter.LinkChUid)) {
                    _Ilog.Error(Callee_Chinfo.channel_call_uuid + " " + Callee_Chinfo.channel_name + " can't find switchtactics,fs send hangup to client");
                    channel_hangup_process(Callee_Chinfo);
                    return;
                }

                //var dial_gateway_info = call_gateway.GetModel_UniqueID(Callee_Chinfo.channel_switch_tactics.Dial_Switch_Adapter.LinkChUid);

                // _Ilog.Info($"{Callee_Chinfo.channel_call_uuid} {Callee_Chinfo.channel_name} send bridge commands(sofia/gateway/{dial_gateway_info.gw_name}/{Destination_Number}) to fs");
                //BridgeResult _bridgeResult = await _Socket.Bridge(Callee_Chinfo.channel_call_uuid, $"sofia/gateway/{dial_gateway_info.gw_name}/{Destination_Number}", bridgeOp);
                BridgeResult _bridgeResult = await _Socket.Bridge(Callee_Chinfo.channel_call_uuid, $"user/{Callee_Chinfo.channel_number}", bridgeOp);

                if(_bridgeResult.Success) {
                    entity.C_AnswerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                    call_record.Update(entity.UniqueID,
                        new object[]
                        {
                            "C_WaitTime",entity.C_WaitTime,
                            "C_AnswerTime",entity.C_AnswerTime
                        });

                    //发送socket给前端
                    SendMsgToClient(SendCommonStr("FSLDHM", new string[] {
                        entity.T_PhoneNum+","+
                        Path.GetFileNameWithoutExtension(Callee_Chinfo.channel_call_record_info.RecFile) +"," +
                        Callee_Chinfo.channel_call_record_info.RecFile,
                    }), Callee_Chinfo.channel_socket._Socket);

                    Callee_Chinfo.channel_call_other_uuid = _bridgeResult.ChannelData.OtherLegUUID;
                    Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_UN_ANSWER;
                    Callee_Chinfo.channel_call_record_info.C_AnswerTime = GlobalParam.GetNowDateTime;

                    _Socket.OnHangup(UUID,
                        e => {
                            if(string.IsNullOrWhiteSpace(WhoHung))
                                WhoHung = "A";

                            _Ilog.Info("主叫方通道挂断事件");

                            if(WhoHung == "B") {

                                entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                entity.CallResultID = 15;
                                call_record.Update(entity.UniqueID,
                                    new object[] {
                                        "C_EndTime", entity.C_EndTime,
                                        "C_SpeakTime",entity.C_SpeakTime,
                                        "CallResultID", entity.CallResultID
                                    });

                                _Socket.Exit();
                            }

                        });

                    _Socket.OnHangup(_bridgeResult.BridgeUUID,
                        e => {

                            if(string.IsNullOrWhiteSpace(WhoHung))
                                WhoHung = "B";

                            _Ilog.Info("通道挂断事件");

                            _Socket.Hangup(UUID);

                            if(WhoHung == "A") {

                                entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                entity.CallResultID = 14;
                                call_record.Update(entity.UniqueID,
                                   new object[] {
                                       "C_EndTime", entity.C_EndTime,
                                       "C_SpeakTime",entity.C_SpeakTime,
                                       "CallResultID", entity.CallResultID
                                   });

                                _Socket.Exit();
                            }
                        });
                } else {

                    var ResponseText = _bridgeResult.ResponseText;

                    if(string.Equals(_bridgeResult.ResponseText, "NoAnswer", StringComparison.OrdinalIgnoreCase))
                        entity.CallType = 4;

                    entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                    entity.CallResultID = 17;

                    call_record.Update(entity.UniqueID,
                        new object[] {
                            "CallType",entity.CallType,
                            "C_WaitTime",entity.C_WaitTime,
                            "C_EndTime", entity.C_EndTime,
                            "CallResultID",entity.CallResultID
                        });

                    _Ilog.Error($"bridge failed:{ResponseText}");
                    await _Socket.Hangup(_Socket.ChannelData.UUID);
                    await _Socket.Exit();
                }

                call_record.Update(entity.UniqueID,
                    new object[] {
                        "RecordFile", entity.RecordFile,
                        "C_WaitTime",entity.C_WaitTime,
                        "C_AnswerTime",entity.C_AnswerTime
                    });
            }

            return;
            #endregion

            #region 注释

            //var Destination_Number = _Socket.ChannelData.GetHeader("Channel-Destination-Number");
            //var Source_Number = _Socket.ChannelData.GetHeader("Channel-Caller-ID-Number");
            //var gateway_info = call_factory.gateway_list.FirstOrDefault(x => x.gateway_user_name == Destination_Number);
            //if(gateway_info == null) {
            //    ///unknown call
            //    _Ilog.Debug(string.Format($"{_Socket.ChannelData.UUID} catch an unknown call request(caller-number={Destination_Number}) from other side (caller-number={Source_Number}) "));
            //    _Ilog.Warn(string.Format("{2} we get an unknown call request(caller-number={1}) from other side (caller-number={0}) ", _Socket.ChannelData.GetHeader("Channel-Caller-ID-Number"), _Socket.ChannelData.GetHeader("Channel-Destination-Number"), _Socket.ChannelData.UUID));
            //    _Ilog.Warn(_Socket.ChannelData.UUID + " send hangup command to fs");
            //    await _Socket.Hangup(_Socket.ChannelData.UUID);
            //    await _Socket.Exit();
            //    return;
            //}

            //gateway_info.gateway_call_uuid = _Socket.ChannelData.UUID;
            //gateway_info.gateway_call_name = _Socket.ChannelData.GetHeader("Channel-Channel-Name");
            ////gateway_info.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
            //gateway_info.gateway_caller_number = new StringBuilder(Destination_Number);


            //if(true || string.IsNullOrEmpty(gateway_info.gateway_switch_tactics.Call_Switch_Adapter.LinkChUid)) {
            //    _Ilog.Error(_Socket.ChannelData.UUID + " " + gateway_info.gateway_name + " can't find switchtactics,fs send hangup to client");
            //    await _Socket.Hangup(_Socket.ChannelData.Headers["Channel-Call-UUID"], HangupCause.UserNotRegistered);
            //    return;
            //}
            //var call_channel_info = call_channel.GetModelByUid(gateway_info.gateway_switch_tactics.Call_Switch_Adapter.LinkChUid);
            //var Caller_Chinfo = call_factory.channel_list.FirstOrDefault(x => x.channel_uniqueid == call_channel_info.UniqueID);

            //Caller_Chinfo.channel_call_other_uuid = _Socket.ChannelData.UUID;
            //Caller_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
            //Caller_Chinfo.channel_caller_number = new StringBuilder(Destination_Number);
            //try {
            //    Caller_Chinfo.channel_call_record_info = CH_CALL_RECORD.Instance(Caller_Chinfo);
            //    KeyValuePair<string, string> dial_number_info = PhoneNumOperate.get_out_dial_number(Source_Number);
            //    Caller_Chinfo.channel_call_record_info.C_PhoneNum = new StringBuilder(dial_number_info.Key);
            //    Caller_Chinfo.channel_call_record_info.PhoneAddress = dial_number_info.Value;
            //    call_calltype_model CallTypeModel = call_calltype.GetModel("CALL");
            //    if(CallTypeModel == null)
            //        throw new Exception("get call type error");
            //    Caller_Chinfo.channel_call_record_info.CallType = CallTypeModel.ID;

            //    Caller_Chinfo.channel_call_record_info.Call_Date = DateTime.Now.Date.ToString();
            //    Caller_Chinfo.channel_call_record_info.C_StartTime = GlobalParam.GetNowDateTime;
            //} catch(Exception ex) {
            //    _Ilog.Error(_Socket.ChannelData.UUID + " " + Caller_Chinfo.channel_name + " initial call record error:" + ex.Message);
            //}

            //RecordResult RecRst = await ChRecording.Recording(Caller_Chinfo.channel_call_other_uuid, _Socket, Caller_Chinfo);
            //if(!RecRst.Success) {
            //}

            //_Ilog.Info(_Socket.ChannelData.UUID + " " + Caller_Chinfo.channel_name + " wait response after send answer commands to fs");
            //await _Socket.ExecuteApplication(_Socket.ChannelData.UUID, "answer", null, true, false);

            //_Ilog.Info(_Socket.ChannelData.UUID + " " + Caller_Chinfo.channel_name + " wait response after send linger commands to fs");
            //await _Socket.Linger();

            //_Ilog.Info(Caller_Chinfo.channel_name + " send bridge commands to fs");
            //BridgeOptions bridgeOp = new BridgeOptions();
            //bridgeOp.CallerIdNumber = Source_Number;
            //bridgeOp.IgnoreEarlyMedia = true;
            //BridgeResult _bridgeResult = await _Socket.Bridge(Caller_Chinfo.channel_call_other_uuid, "user/" + Caller_Chinfo.channel_number, bridgeOp);
            //if(!_bridgeResult.Success) {
            //    _Ilog.Info(string.Format("{0} {1} bridge failed: {2},channel start hung up.", _Socket.ChannelData.UUID, Caller_Chinfo.channel_name, _bridgeResult.ResponseText));
            //    await _Socket.Hangup(_Socket.ChannelData.UUID, HangupCause.UserNotRegistered);
            //    await _Socket.Exit();
            //} else {

            //}

            #endregion
        }
        #endregion

        #region WebSocket来电
        private async static void _call_do(OutboundSocket _Socket) {

            #region 沿用

            #region 注释
            if (false)
            {
                Log.Instance.Success($"[Start][{_Socket.ChannelData.Headers.Count}]");
                foreach (KeyValuePair<string, string> item in _Socket.ChannelData.Headers)
                {
                    Log.Instance.Success($"[{item.Key}] = [{item.Value}]");
                }
                Log.Instance.Success($"[End]");
            }
            #endregion

            /*
             * 修正,获取真实被叫
             * 即:如果出现多注册的情况,一定要找到真实被叫
             */

            var UUID = _Socket.ChannelData.UUID;//.Headers["Channel-Call-UUID"];
            var Destination_Number = _Socket.ChannelData.GetHeader("Channel-Destination-Number");
            Log.Instance.Success($"[CenoFsSharp][ChannelFunc][_call_do][首次获取被叫{Destination_Number}]");
            if (Destination_Number.Contains('+'))
            {
                Destination_Number = _Socket.ChannelData.GetHeader("variable_sip_to_user");
                Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][_call_do][再次获取被叫{Destination_Number}]");
                if (string.IsNullOrWhiteSpace(Destination_Number))
                {
                    await _Socket.Hangup(UUID, HangupCause.NoRouteDestination)
                             .ContinueWith(task => {
                                 try {
                                     if(task.IsCanceled) {
                                         Log.Instance.Fail($"[{UUID}] canceled when hangup");
                                     }
                                 } catch(Exception ex) {
                                     Log.Instance.Error($"[{UUID}] canceled when hangup,error:{ex.Message}");
                                 }
                             });
                    return;
                }
            }

            var _ = _Socket.ChannelData.Headers["Channel-Caller-ID-Number"];
            var io = string.Empty;
            AGENT_INFO agent = null;
            var type = string.Empty;

            #region 多号码
            /*
             * 此处加入多号码逻辑
             * 引入号码逻辑,也就是应该是呼入谁上
             */

            if (Destination_Number.StartsWith("*"))
            {
                type = Special.Star;
                io = "内线来电";
                Destination_Number = new Regex("[^(0-9*#)]+").Replace(Destination_Number, "");
                _ = $"*{_.Replace("*", "")}";

                /*
                 * 去掉其他逻辑
                 * 内呼只能呼叫分机号
                 */

                agent = call_factory.agent_list.FirstOrDefault(x => x.ChInfo.channel_number == Destination_Number.Substring(1));
            }
            else
            {
                if (!Call_ParamUtil.IsMultiPhone)
                {
                    type = Special.Zero;
                    io = "外线来电";
                    agent = call_factory.agent_list.FirstOrDefault(x => x.AgentNum == Destination_Number);
                }
                else
                {
                    string m_stNumberStr = string.Empty;
                    int m_uLimitId = -1;
                    int m_uAgentID = m_fDialLimit.m_fGetAgentID(Destination_Number, out m_stNumberStr, true, string.Empty, out m_uLimitId);
                    agent = call_factory.agent_list.FirstOrDefault(x => x.AgentID == m_uAgentID);
                }
            }

            if(agent == null) {
                await _Socket.Hangup(UUID, HangupCause.UserNotRegistered)
                             .ContinueWith(task => {
                                 try {
                                     if(task.IsCanceled) {
                                         Log.Instance.Fail($"[{UUID}] canceled when hangup");
                                     }
                                 } catch(Exception ex) {
                                     Log.Instance.Error($"[{UUID}] canceled when hangup,error:{ex.Message}");
                                 }
                             });
                Log.Instance.Error($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number}无此用户]");
                return;
            }
            #endregion

            var nCh = agent.ChInfo.nCh;
            var Callee_Chinfo = call_factory.channel_list[agent.ChInfo.nCh];

            call_record_model entity = new call_record_model();
            entity.UniqueID = Guid.NewGuid().ToString();
            entity.CallType = 3;
            entity.ChannelID = Callee_Chinfo.channel_id;
            entity.LinkChannelID = -1;
            entity.LocalNum = type == Special.Star ? Destination_Number.TrimStart('*') : Destination_Number;

            #region 修正

            /*
             * 移除该处
             * 将判断规则置于每条线中
             */

            if (false)
            {
                if (Call_ParamUtil.DialDealMethod == "has")
                {
                    entity.T_PhoneNum = _;
                }
                else if (Call_ParamUtil.DialDealMethod == "no")
                {
                    entity.T_PhoneNum = type + _;
                }
                else
                {
                    entity.T_PhoneNum = _;
                }
            }
            #endregion

            /*
             * 这里需要修正,因为有可能网关规则本身就不一样
             * 也就是是否加零需要我们来判断
             * 这里需要调整以及规整一下
             * 调整一:只有内呼需要加*
             * 调整二:外呼不需要加0,而是在拨打的时候自动判断,因为有可能线采用的规则不一致,电话卡打电话无需0
             */

            List<string> m_lStrings = m_cPhone.m_fGetPhoneNumberMemo(_);
            entity.T_PhoneNum = (type == Special.Star ? m_lStrings[1] : m_lStrings[0]);
            entity.C_PhoneNum = m_lStrings[1];
            entity.PhoneAddress = m_lStrings[3];
            switch (type)
            {
                case Special.Star:
                    entity.CallType = 7;
                    break;
                default:
                    entity.CallType = 3;
                    break;
            }

            entity.DtmfNum = "";
            entity.PhoneTypeID = -1;
            entity.PhoneListID = -1;
            entity.PriceTypeID = -1;
            entity.CallPrice = -1;
            entity.AgentID = agent.AgentID;
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
            var WhoHung = String.Empty;
            var link = false;
            var __timeout_seconds = Call_ParamUtil.__timeout_seconds;
            try {
                if(Callee_Chinfo == null) {
                    await _Socket.Hangup(UUID, HangupCause.UserNotRegistered)
                                 .ContinueWith(task => {
                                     try {
                                         if(task.IsCanceled) {
                                             Log.Instance.Fail($"[{UUID}] canceled when hangup");
                                         }
                                     } catch(Exception ex) {
                                         Log.Instance.Error($"[{UUID}] canceled when hangup,error:{ex.Message}");
                                     }
                                 });
                    if(type == "*") {
                        entity.CallType = 8;
                        entity.CallResultID = 44;
                    } else {
                        entity.CallType = 4;
                        entity.CallResultID = 17;
                    }
                    call_record.Insert(entity);
                    Log.Instance.Error($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number}通道未知]");
                    return;
                }
                if(Callee_Chinfo.channel_websocket == null) {
                    await _Socket.Hangup(UUID, HangupCause.UserNotRegistered)
                                 .ContinueWith(task => {
                                     try {
                                         if(task.IsCanceled) {
                                             Log.Instance.Fail($"[{UUID}] canceled when hangup");
                                         }
                                     } catch(Exception ex) {
                                         Log.Instance.Error($"[{UUID}] canceled when hangup,error:{ex.Message}");
                                     }
                                 });
                    if(type == "*") {
                        entity.CallType = 8;
                        entity.CallResultID = 44;
                    } else {
                        entity.CallType = 4;
                        entity.CallResultID = 17;
                    }
                    call_record.Insert(entity);
                    Log.Instance.Error($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number}未连接服务器]");
                    return;
                }

                if(Callee_Chinfo.channel_call_status != APP_USER_STATUS.FS_USER_IDLE) {
                    if(!string.IsNullOrWhiteSpace(Callee_Chinfo.channel_call_uuid)) {
                        var uuid_exists = await _Socket.SendApi($"uuid_exists {Callee_Chinfo.channel_call_uuid}")
                                                       .ContinueWith(task => {
                                                           try {
                                                               if(task.IsCanceled) {
                                                                   Log.Instance.Fail($"[{UUID}] canceled when uuid_exists");
                                                                   return null as ApiResponse;
                                                               } else {
                                                                   return task.Result;
                                                               }
                                                           } catch(Exception ex) {
                                                               Log.Instance.Error($"[{UUID}] canceled when uuid_exists,error:{ex.Message}");
                                                               return null as ApiResponse;
                                                           }
                                                       });
                        if(uuid_exists != null && uuid_exists.Success) {
                            if(uuid_exists.BodyText.StartsWith("true")) {
                                await _Socket.Hangup(UUID, HangupCause.UserBusy)
                                             .ContinueWith(task => {
                                                 try {
                                                     if(task.IsCanceled) {
                                                         Log.Instance.Fail($"[{UUID}] canceled when hangup");
                                                     }
                                                 } catch(Exception ex) {
                                                     Log.Instance.Error($"[{UUID}] canceled when hangup,error:{ex.Message}");
                                                 }
                                             });
                                if(type == "*") {
                                    entity.CallType = 8;
                                    entity.CallResultID = 45;
                                } else {
                                    entity.CallType = 4;
                                    entity.CallResultID = 18;
                                }
                                call_record.Insert(entity);
                                Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number}正忙,无法接通,请稍后再拨]");
                                return;
                            }
                        } else {
                            Log.Instance.Success($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number}非正忙,正在接通中...]");
                        }
                    }
                }

                call_record.Insert(entity);

                //忙的时候不能修改通道UUID,否则无法挂断电话
                Callee_Chinfo.channel_call_uuid = _Socket.ChannelData.UUID;
                Callee_Chinfo.channel_name = _Socket.ChannelData.GetHeader("Channel-Channel-Name");
                Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
                Callee_Chinfo.channel_caller_number = new StringBuilder(Destination_Number);
                try {
                    Callee_Chinfo.channel_call_record_info = CH_CALL_RECORD.Instance(Callee_Chinfo);
                    KeyValuePair<string, string> dial_number_info = PhoneNumOperate.get_out_dial_number(Destination_Number);
                    Callee_Chinfo.channel_call_record_info.C_PhoneNum = new StringBuilder(dial_number_info.Key);
                    Callee_Chinfo.channel_call_record_info.PhoneAddress = dial_number_info.Value;
                    Callee_Chinfo.channel_call_record_info.CallType = 3;
                    Callee_Chinfo.channel_call_record_info.Call_Date = DateTime.Now.Date.ToString();
                    Callee_Chinfo.channel_call_record_info.C_StartTime = GlobalParam.GetNowDateTime;
                    Log.Instance.Success($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},设置录音缓存成功]");
                } catch(Exception ex) {
                    Log.Instance.Error($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},设置录音缓存错误:{ex.Message}]");
                }

                await _Socket.Linger()
                             .ContinueWith(task => {
                                 try {
                                     if(task.IsCanceled) {
                                         Log.Instance.Fail($"[{UUID}] canceled when linger");
                                     }
                                 } catch(Exception ex) {
                                     Log.Instance.Error($"[{UUID}] canceled when linger,error:{ex.Message}");
                                 }
                             });
                Log.Instance.Success($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},设置套接字断开后逗留]");

                Log.Instance.Success($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},发送早期媒体:{ParamLib.DialingAudioFile}]");

                await _Socket.SetChannelVariable(_Socket.ChannelData.UUID, "instant_ringback", true)
                             .ContinueWith(task => {
                                 try {
                                     if(task.IsCanceled) {
                                         Log.Instance.Fail($"[{UUID}] canceled when set instant_ringback");
                                     }
                                 } catch(Exception ex) {
                                     Log.Instance.Error($"[{UUID}] canceled when set instant_ringback,error:{ex.Message}");
                                 }
                             });
                await _Socket.SetChannelVariable(_Socket.ChannelData.UUID, "transfer_ringback", ParamLib.DialingAudioFile)
                             .ContinueWith(task => {
                                 try {
                                     if(task.IsCanceled) {
                                         Log.Instance.Fail($"[{UUID}] canceled when set transfer_ringback");
                                     }
                                 } catch(Exception ex) {
                                     Log.Instance.Error($"[{UUID}] canceled when set transfer_ringback,error:{ex.Message}");
                                 }
                             });

                #region 主叫挂断
                _Socket.OnHangup(UUID, e =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(WhoHung))
                        {
                            WhoHung = "A";
                            Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},主叫挂断:{e.Headers[HeaderNames.HangupCause]}]");
                            if (link)
                            {
                                entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                if (entity.C_SpeakTime < 0)
                                    entity.C_SpeakTime = 0;
                                entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                if (type == "*")
                                {
                                    entity.CallType = 7;
                                    entity.CallResultID = 42;
                                }
                                else
                                {
                                    entity.CallType = 3;
                                    entity.CallResultID = 15;
                                }
                            }
                            else
                            {
                                entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                                if (entity.C_WaitTime < 0)
                                    entity.C_WaitTime = 0;
                                if (type == "*")
                                {
                                    entity.CallType = 8;
                                    entity.CallResultID = 43;
                                }
                                else
                                {
                                    entity.CallType = 4;
                                    entity.CallResultID = 16;
                                }
                            }

                            call_record.Update(entity.UniqueID,
                                new object[] {
                                    "CallType",entity.CallType,
                                    "C_EndTime", entity.C_EndTime,
                                    "CallResultID", entity.CallResultID,
                                    "C_SpeakTime", entity.C_SpeakTime,
                                    "C_WaitTime",entity.C_WaitTime
                                });
                        }
                        if (WhoHung == "B")
                        {
                            if (_Socket != null && _Socket.IsConnected)
                            {
                                _Socket.Exit().ContinueWith(task =>
                                {
                                    try
                                    {
                                        if (task.IsCanceled)
                                        {
                                            Log.Instance.Fail($"[{UUID}] canceled when exit");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Instance.Error($"[{UUID}] canceled when exit,error:{ex.Message}");
                                    }
                                });
                            }
                            Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                            Callee_Chinfo.channel_call_uuid = null;
                            Callee_Chinfo.channel_call_other_uuid = null;
                            Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},最后主叫退出]");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Instance.Error($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},监听主叫挂断,错误:{ex.Message}]");
                    }
                });
                #endregion

                Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_RINGING;
                Callee_Chinfo.channel_call_other_uuid = Guid.NewGuid().ToString();

                BridgeOptions bridgeOp = new BridgeOptions();
                var Caller_Agent_Info = call_factory.agent_list.FirstOrDefault(x => x.ChInfo == Callee_Chinfo);
                if(Caller_Agent_Info != null) {
                    bridgeOp.CallerIdNumber = entity.T_PhoneNum;
                    bridgeOp.CallerIdName = _Socket.ChannelData.GetHeader("Channel-Caller-ID-Name");
                    bridgeOp.UUID = Callee_Chinfo.channel_call_other_uuid;
                }
                bridgeOp.TimeoutSeconds = __timeout_seconds;

                BridgeResult _bridgeResult = await _Socket.Bridge(Callee_Chinfo.channel_call_uuid, $"user/{Callee_Chinfo.channel_number}", bridgeOp)
                                                          .ContinueWith(task => {
                                                              try {
                                                                  if(task.IsCanceled) {
                                                                      Log.Instance.Fail($"[{UUID}] canceled when bridge");
                                                                      return null as BridgeResult;
                                                                  } else {
                                                                      return task.Result;
                                                                  }
                                                              } catch(Exception ex) {
                                                                  Log.Instance.Error($"[{UUID}] canceled when bridge,error:{ex.Message}");
                                                                  return null as BridgeResult;
                                                              }
                                                          });

                if(_bridgeResult != null && _bridgeResult.Success) {
                    link = true;
                    Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_TALKING;

                    await ChRecording.Recording(Callee_Chinfo.channel_call_uuid, _Socket, Callee_Chinfo)
                                     .ContinueWith(task => {
                                         try {
                                             if(task.IsCanceled) {
                                                 Log.Instance.Fail($"[{UUID}] canceled when recording");
                                             }
                                         } catch(Exception ex) {
                                             Log.Instance.Error($"[{UUID}] canceled when recording,error:{ex.Message}");
                                         }
                                     });
                    entity.RecordFile = Callee_Chinfo.channel_call_record_info.RecFile;
                    entity.recordName = Path.GetFileNameWithoutExtension(entity.RecordFile);
                    Log.Instance.Success($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},增加字段recordName:{entity.recordName}]");
                    Log.Instance.Success($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},录音路径:{entity.RecordFile}]");

                    entity.C_AnswerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                    if(entity.C_WaitTime < 0)
                        entity.C_WaitTime = 0;
                    call_record.Update(entity.UniqueID,
                        new object[] {
                            "C_WaitTime", entity.C_WaitTime,
                            "C_AnswerTime", entity.C_AnswerTime
                        });

                    ///<![CDATA[
                    /// 是否发送录音时加上IP地址
                    /// 后续加上
                    /// ]]>

                    Callee_Chinfo.channel_websocket.Send(SendCommonStr("FSLDHM", new string[] { entity.T_PhoneNum + "," + entity.recordName + "," + Callee_Chinfo.channel_call_record_info.RecFile }));
                    Callee_Chinfo.channel_call_record_info.C_AnswerTime = GlobalParam.GetNowDateTime;

                    #region 被叫挂断
                    _Socket.OnHangup(_bridgeResult.BridgeUUID, e =>
                     {
                         try
                         {
                             if (string.IsNullOrWhiteSpace(WhoHung))
                             {
                                 WhoHung = "B";
                                 Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},被叫挂断:{e.Headers[HeaderNames.HangupCause]}]");
                                 entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                 entity.C_SpeakTime = (int)DateTime.Now.Subtract(Convert.ToDateTime(entity.C_AnswerTime)).TotalSeconds;
                                 if (entity.C_SpeakTime < 0)
                                     entity.C_SpeakTime = 0;
                                 if (type == "*")
                                 {
                                     entity.CallResultID = 41;
                                 }
                                 else
                                 {
                                     entity.CallResultID = 14;
                                 }
                                 call_record.Update(entity.UniqueID,
                                   new object[] {
                                        "C_EndTime", entity.C_EndTime,
                                        "C_SpeakTime",entity.C_SpeakTime,
                                        "CallResultID", entity.CallResultID
                                      });
                                 _Socket.Hangup(UUID, HangupCause.NormalClearing).ContinueWith(task =>
                                 {
                                     try
                                     {
                                         if (task.IsCanceled)
                                         {
                                             Log.Instance.Fail($"[{UUID}] canceled when hangup");
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         Log.Instance.Error($"[{UUID}] canceled when hangup,error:{ex.Message}");
                                     }
                                 });
                             }
                             if (WhoHung == "A")
                             {
                                 if (_Socket != null && _Socket.IsConnected)
                                 {
                                     _Socket.Exit().ContinueWith(task =>
                                     {
                                         try
                                         {
                                             if (task.IsCanceled)
                                             {
                                                 Log.Instance.Fail($"[{UUID}] canceled when exit");
                                             }
                                         }
                                         catch (Exception ex)
                                         {
                                             Log.Instance.Error($"[{UUID}] canceled when exit,error:{ex.Message}");
                                         }
                                     });
                                 }
                                 Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                                 Callee_Chinfo.channel_call_uuid = null;
                                 Callee_Chinfo.channel_call_other_uuid = null;
                                 Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},最后被叫退出]");
                             }
                         }
                         catch (Exception ex)
                         {
                             Log.Instance.Warn($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},监听被叫挂断,错误:{ex.Message}]");
                         }
                     });
                    #endregion

                }
                else {

                    var ResponseText = _bridgeResult.ResponseText;

                    #region 判断电话结果
                    if(type == "*") {
                        entity.CallType = 8;
                        if(Callee_Chinfo.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP) {
                            if(Cmn.IgnoreEquals(ResponseText, "NOANSWER"))
                                entity.CallResultID = 43;
                            else if(Cmn.IgnoreEquals(ResponseText, "BUSY"))
                                entity.CallResultID = 45;
                            else if(Cmn.IgnoreEquals(ResponseText, "INVALIDARGS"))
                                entity.CallResultID = 50;
                            else if(Cmn.IgnoreEquals(ResponseText, "USER_NOT_REGISTERED"))
                                entity.CallResultID = 43;
                            else if(Cmn.IgnoreEquals(ResponseText, "SUBSCRIBER_ABSENT"))
                                entity.CallResultID = 43;
                            else
                                entity.CallResultID = 43;
                        } else {
                            if(Cmn.IgnoreEquals(ResponseText, "NOANSWER"))
                                entity.CallResultID = 43;
                            else if(Cmn.IgnoreEquals(ResponseText, "BUSY"))
                                entity.CallResultID = 45;
                            else if(Cmn.IgnoreEquals(ResponseText, "INVALIDARGS"))
                                entity.CallResultID = 52;
                            else if(Cmn.IgnoreEquals(ResponseText, "USER_NOT_REGISTERED"))
                                entity.CallResultID = 43;
                            else if(Cmn.IgnoreEquals(ResponseText, "SUBSCRIBER_ABSENT"))
                                entity.CallResultID = 43;
                            else
                                entity.CallResultID = 43;
                        }
                    } else {
                        entity.CallType = 4;
                        if(Callee_Chinfo.channel_call_status == APP_USER_STATUS.FS_USER_BHANGUP) {
                            if(Cmn.IgnoreEquals(ResponseText, "NOANSWER"))
                                entity.CallResultID = 16;
                            else if(Cmn.IgnoreEquals(ResponseText, "BUSY"))
                                entity.CallResultID = 18;
                            else if(Cmn.IgnoreEquals(ResponseText, "INVALIDARGS"))
                                entity.CallResultID = 49;
                            else if(Cmn.IgnoreEquals(ResponseText, "USER_NOT_REGISTERED"))
                                entity.CallResultID = 16;
                            else if(Cmn.IgnoreEquals(ResponseText, "SUBSCRIBER_ABSENT"))
                                entity.CallResultID = 16;
                            else
                                entity.CallResultID = 16;
                        } else {
                            if(Cmn.IgnoreEquals(ResponseText, "NOANSWER"))
                                entity.CallResultID = 16;
                            else if(Cmn.IgnoreEquals(ResponseText, "BUSY"))
                                entity.CallResultID = 18;
                            else if(Cmn.IgnoreEquals(ResponseText, "INVALIDARGS"))
                                entity.CallResultID = 51;
                            else if(Cmn.IgnoreEquals(ResponseText, "USER_NOT_REGISTERED"))
                                entity.CallResultID = 16;
                            else if(Cmn.IgnoreEquals(ResponseText, "SUBSCRIBER_ABSENT"))
                                entity.CallResultID = 16;
                            else
                                entity.CallResultID = 16;
                        }
                    }
                    #endregion

                    entity.C_EndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    entity.C_WaitTime = (int)DateTime.Now.Subtract(Now).TotalSeconds;
                    if(entity.C_WaitTime < 0)
                        entity.C_WaitTime = 0;

                    call_record.Update(entity.UniqueID,
                        new object[] {
                            "CallType",entity.CallType,
                            "C_WaitTime",entity.C_WaitTime,
                            "C_EndTime", entity.C_EndTime,
                            "CallResultID",entity.CallResultID
                        });

                    Log.Instance.Fail($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},桥接失败:{ResponseText}]");
                    await _Socket.Hangup(UUID)
                                 .ContinueWith(task => {
                                     try {
                                         if(task.IsCanceled) {
                                             Log.Instance.Fail($"[{UUID}] canceled when hangup");
                                         }
                                     } catch(Exception ex) {
                                         Log.Instance.Error($"[{UUID}] canceled when hangup,error:{ex.Message}");
                                     }
                                 });

                    if (_Socket != null && _Socket.IsConnected)
                    {
                        await _Socket.Exit().ContinueWith(task =>
                        {
                            try
                            {
                                if (task.IsCanceled)
                                {
                                    Log.Instance.Fail($"[{UUID}] canceled when exit");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[{UUID}] canceled when exit,error:{ex.Message}");
                            }
                        });
                    }
                    Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                    Callee_Chinfo.channel_call_uuid = null;
                    Callee_Chinfo.channel_call_other_uuid = null;
                }

                call_record.Update(entity.UniqueID,
                    new object[] {
                        "RecordFile", entity.RecordFile,
                        "recordName",entity.recordName,
                        "C_WaitTime",entity.C_WaitTime,
                        "C_AnswerTime",entity.C_AnswerTime
                    });
            } catch(Exception ex) {
                call_record.Update(entity.UniqueID,
                    new object[] {
                        "Remark",ex.Message
                    });
                Callee_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
                Callee_Chinfo.channel_call_uuid = null;
                Callee_Chinfo.channel_call_other_uuid = null;
                Log.Instance.Error($"[CenoFsSharp][ChannelFunc][_call_do][{io}][{UUID},主叫号码:{_},被叫号码:{Destination_Number},处理{io}错误:{ex.Message}]");

                if (_Socket != null && _Socket.IsConnected)
                {
                    await _Socket.Exit().ContinueWith(task =>
                    {
                        try
                        {
                            if (task.IsCanceled)
                            {
                                Log.Instance.Fail($"[{UUID}] canceled when exit");
                            }
                        }
                        catch (Exception tex)
                        {
                            Log.Instance.Error($"[{UUID}] canceled when exit,error:{tex.Message}");
                        }
                    });
                }
            }
            return;
            #endregion

            #region 注释

            //var Destination_Number = _Socket.ChannelData.GetHeader("Channel-Destination-Number");
            //var Source_Number = _Socket.ChannelData.GetHeader("Channel-Caller-ID-Number");
            //var gateway_info = call_factory.gateway_list.FirstOrDefault(x => x.gateway_user_name == Destination_Number);
            //if(gateway_info == null) {
            //    ///unknown call
            //    _Ilog.Debug(string.Format($"{_Socket.ChannelData.UUID} catch an unknown call request(caller-number={Destination_Number}) from other side (caller-number={Source_Number}) "));
            //    _Ilog.Warn(string.Format("{2} we get an unknown call request(caller-number={1}) from other side (caller-number={0}) ", _Socket.ChannelData.GetHeader("Channel-Caller-ID-Number"), _Socket.ChannelData.GetHeader("Channel-Destination-Number"), _Socket.ChannelData.UUID));
            //    _Ilog.Warn(_Socket.ChannelData.UUID + " send hangup command to fs");
            //    await _Socket.Hangup(_Socket.ChannelData.UUID);
            //    await _Socket.Exit();
            //    return;
            //}

            //gateway_info.gateway_call_uuid = _Socket.ChannelData.UUID;
            //gateway_info.gateway_call_name = _Socket.ChannelData.GetHeader("Channel-Channel-Name");
            ////gateway_info.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
            //gateway_info.gateway_caller_number = new StringBuilder(Destination_Number);


            //if(true || string.IsNullOrEmpty(gateway_info.gateway_switch_tactics.Call_Switch_Adapter.LinkChUid)) {
            //    _Ilog.Error(_Socket.ChannelData.UUID + " " + gateway_info.gateway_name + " can't find switchtactics,fs send hangup to client");
            //    await _Socket.Hangup(_Socket.ChannelData.Headers["Channel-Call-UUID"], HangupCause.UserNotRegistered);
            //    return;
            //}
            //var call_channel_info = call_channel.GetModelByUid(gateway_info.gateway_switch_tactics.Call_Switch_Adapter.LinkChUid);
            //var Caller_Chinfo = call_factory.channel_list.FirstOrDefault(x => x.channel_uniqueid == call_channel_info.UniqueID);

            //Caller_Chinfo.channel_call_other_uuid = _Socket.ChannelData.UUID;
            //Caller_Chinfo.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
            //Caller_Chinfo.channel_caller_number = new StringBuilder(Destination_Number);
            //try {
            //    Caller_Chinfo.channel_call_record_info = CH_CALL_RECORD.Instance(Caller_Chinfo);
            //    KeyValuePair<string, string> dial_number_info = PhoneNumOperate.get_out_dial_number(Source_Number);
            //    Caller_Chinfo.channel_call_record_info.C_PhoneNum = new StringBuilder(dial_number_info.Key);
            //    Caller_Chinfo.channel_call_record_info.PhoneAddress = dial_number_info.Value;
            //    call_calltype_model CallTypeModel = call_calltype.GetModel("CALL");
            //    if(CallTypeModel == null)
            //        throw new Exception("get call type error");
            //    Caller_Chinfo.channel_call_record_info.CallType = CallTypeModel.ID;

            //    Caller_Chinfo.channel_call_record_info.Call_Date = DateTime.Now.Date.ToString();
            //    Caller_Chinfo.channel_call_record_info.C_StartTime = GlobalParam.GetNowDateTime;
            //} catch(Exception ex) {
            //    _Ilog.Error(_Socket.ChannelData.UUID + " " + Caller_Chinfo.channel_name + " initial call record error:" + ex.Message);
            //}

            //RecordResult RecRst = await ChRecording.Recording(Caller_Chinfo.channel_call_other_uuid, _Socket, Caller_Chinfo);
            //if(!RecRst.Success) {
            //}

            //_Ilog.Info(_Socket.ChannelData.UUID + " " + Caller_Chinfo.channel_name + " wait response after send answer commands to fs");
            //await _Socket.ExecuteApplication(_Socket.ChannelData.UUID, "answer", null, true, false);

            //_Ilog.Info(_Socket.ChannelData.UUID + " " + Caller_Chinfo.channel_name + " wait response after send linger commands to fs");
            //await _Socket.Linger();

            //_Ilog.Info(Caller_Chinfo.channel_name + " send bridge commands to fs");
            //BridgeOptions bridgeOp = new BridgeOptions();
            //bridgeOp.CallerIdNumber = Source_Number;
            //bridgeOp.IgnoreEarlyMedia = true;
            //BridgeResult _bridgeResult = await _Socket.Bridge(Caller_Chinfo.channel_call_other_uuid, "user/" + Caller_Chinfo.channel_number, bridgeOp);
            //if(!_bridgeResult.Success) {
            //    _Ilog.Info(string.Format("{0} {1} bridge failed: {2},channel start hung up.", _Socket.ChannelData.UUID, Caller_Chinfo.channel_name, _bridgeResult.ResponseText));
            //    await _Socket.Hangup(_Socket.ChannelData.UUID, HangupCause.UserNotRegistered);
            //    await _Socket.Exit();
            //} else {

            //}

            #endregion
        }
        #endregion

        #region 弃用
        [Obsolete("已弃用")]
        private static void channel_hangup_process(ChannelInfo Ch_Info) {
            _Ilog.Debug("");
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

        /// <summary>
        /// 通用的方法,直接传入S_NO即可
        /// </summary>
        /// <param name="S_NO"></param>
        /// <param name="S_Values">参数需一一对应上</param>
        /// <returns></returns>
        public static string SendCommonStr(string S_NO, string[] S_Values) {
            StringBuilder CommandStr = new StringBuilder();
            CommandStr.Append(call_socketcommand_util.GetStartStr(S_NO) + call_socketcommand_util.GetS_NameByS_NO(S_NO));
            CommandStr.Append("{");
            call_socketcommand_model[] _Model = call_socketcommand_util.GetModelNodeByHeadInfo(S_NO);
            if(_Model == null)
                return CommandStr.Append("}" + call_socketcommand_util.GetEndStr(S_NO)).ToString();

            for(int i = 0; i < _Model.Length; i++) {
                CommandStr.Append(_Model[i].S_Name + $":{S_Values[i]};");
            }
            return CommandStr.Append("}" + call_socketcommand_util.GetEndStr(S_NO)).ToString();
        }
        #endregion
    }
}
