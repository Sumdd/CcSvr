using System;
using System.Linq;
using System.Reactive.Linq;

using NEventSocket;
using NEventSocket.FreeSwitch;
using log4net;
using CenoCommon;
using CenoSipFactory;

namespace CenoFsSharp
{
	public class ReceiveEventFunc
	{
		private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void ReceiveMessage(OutboundSocket _Socket, EventMessage e = null)
		{
            return;
			try
			{
				if (_Socket == null)
					throw new ArgumentNullException("_Socket");

				_Ilog.Info("receive an event(name=" + e.EventName + ")");

				switch (e.EventName)
				{
					case EventName.ChannelBridge:
						recive_channel_bridge_event(_Socket, e);
						break;
					case EventName.ChannelHangup:
						recive_channel_hangup_event(_Socket, e);
						break;
					case EventName.ChannelHangupComplete:
						recive_channel_hangup_complete_event(_Socket, e);
						break;
					case EventName.PlaybackStop:
						receive_channel_playbackstop_event(_Socket, e);
						break;
					case EventName.ChannelExecuteComplete:
						receive_channel_excute_complete_event(_Socket, e);
						break;
					case EventName.BackgroundJob:
						receive_channel_backgroudjob_event(_Socket, e);
						break;
					case EventName.RecordStart:
						_Ilog.Info(_Socket.ChannelData.UUID + " RecordStart");
						break;
					case EventName.RecordStop:
						_Ilog.Info(_Socket.ChannelData.UUID + " RecordStop");
						break;
					default:
						Console.WriteLine(e.EventName);
						break;
				}
			}
			catch (Exception ex)
			{
				_Ilog.Error("we catch an exception when processing event:" + ex.Message, ex);
			}
		}

		private static void receive_channel_playbackstop_event(OutboundSocket _Socket, EventMessage e)
		{
			_Ilog.Info(_Socket.ChannelData.UUID + " stop play back");
		}

		private static void receive_channel_backgroudjob_event(OutboundSocket _Socket, EventMessage e)
		{
			_Ilog.Info(_Socket.ChannelData.UUID + " excute backgroudjob");
		}

		private static void recive_channel_bridge_event(OutboundSocket _Socket, EventMessage e)
		{
			_Ilog.Info(_Socket.ChannelData.UUID + " excute Bridge");
		}

		private static void recive_channel_hangup_complete_event(OutboundSocket _Socket, EventMessage e)
		{
			_Ilog.Info(_Socket.ChannelData.UUID + " excute HangupComplete");
		}

		private static void recive_channel_hangup_event(OutboundSocket _Socket, EventMessage e)
		{
			_Ilog.Info(_Socket.ChannelData.UUID + " excute Hangup");
		}


		private static void receive_channel_excute_complete_event(OutboundSocket _Socket, EventMessage e)
		{

			try
			{
				var ChInfo = call_factory.channel_list.FirstOrDefault(x => x.channel_call_uuid == e.Headers["UUID"] || x.channel_call_other_uuid == e.Headers["UUID"]);
				var app_head = e.Headers["Application"];
				_Ilog.Info(e.Headers["Core-UUID"] + " event:" + e.Headers["Application"] + " excute complete");
				switch (app_head = e.Headers["Application"])
				{
					case "playback":
						if (ChInfo == null)
						{
							_Ilog.Info("play dialing audio:" + ParamLib.DialingAudioFile + " complete");
							return;
						}
						_Ilog.Info(ChInfo.channel_name + " play dialing audio:" + ParamLib.DialingAudioFile + " complete");
						switch (ChInfo.channel_call_status)
						{
							case APP_USER_STATUS.FS_USER_BF_DIAL:
								break;
							case APP_USER_STATUS.FS_USER_BF_ANSWER:
								//_Ilog.Info(ChInfo.Ch_Name + " play waiting audio:" + ParamLib.WaitingAudioFile);
								//_Socket.Play(ChInfo.Ch_UUID, ParamLib.WaitingAudioFile);

								break;
							default: break;
						}
						break;
					case "answer":
						var Caller_Chinfo = call_factory.channel_list.FirstOrDefault(x => x.channel_call_uuid == e.Headers["UUID"]);
						if (Caller_Chinfo == null)
							throw new Exception("catch an unknown excutecomplete event(name=answer)");
						break;
					case "bridge":
						if (ChInfo == null)
							return;

						_Ilog.Debug(e.Headers);
						switch (ChInfo.channel_call_status)
						{
							case APP_USER_STATUS.FS_USER_BF_DIAL:
								break;
							case APP_USER_STATUS.FS_USER_UN_ANSWER:
								ChInfo.channel_call_record_info.C_EndTime = GlobalParam.GetNowDateTime;

								if (ChInfo.channel_call_record_info != null)
								{
									CallRecordResult InsertRstawait = CallRecord.InsertCallRecord(ChInfo.channel_call_record_info).Result;
									if (InsertRstawait.Success)
									{
										ChInfo.channel_call_record_info = null;
									}
								}

								ChInfo.channel_call_status = APP_USER_STATUS.FS_USER_IDLE;
								break;
							default: break;
						}
						break;
					case "socket":
						var ss = "ss";
						break;
					case "record":
						break;
					default:
						_Ilog.Info("we catch an undefined completen event(name=" + e.Headers["Application"] + ")");
						break;
				}
			}
			catch (Exception ex)
			{
				_Ilog.Error("we catch an exception when processing ChExcuteComplete", ex);
			}
		}

	}
}
