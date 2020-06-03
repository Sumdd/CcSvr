using CenoSipFactory;
using System;

using CenoCommon;
using log4net;
using DataBaseModel;
using DB.Basic;

namespace CenoSipBusiness
{
	public class intilizate_switchtactics
	{
		private static ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /* 通道转换 */
		public static void intilizate_channel_switchtactics()
		{
			if (call_factory.channel_list != null && call_factory.channel_list.Count <= 0)
			{
				_Ilog.Error("channel is not found in db", new ArgumentException("channel_info"));
				return;
			}
			for (int i = 0; i < call_factory.channel_list.Count; i++)
			{
				call_factory.channel_list[i].channel_switch_tactics = new channel_switchtactics_info();

				_Ilog.Info(string.Format("start intilizate channel(ch={0}) switchtactics ", call_factory.channel_list[i].nCh));

				if (call_factory.channel_list[i] == null)
				{
					_Ilog.Error(string.Format("channel(ch={0}) is empty data", call_factory.channel_list[i].nCh));
					continue;
				}
				call_switchtactics_model _model = call_switchtactics.GetModelByChUid(call_factory.channel_list[i].channel_uniqueid);
				if (_model == null)
				{
					_Ilog.Error(string.Format("query channel(ch={0}) switchtactics model error", call_factory.channel_list[i].nCh));
					continue;
				}

				try
				{
					call_factory.channel_list[i].channel_switch_tactics.Dial_Switch_Adapter.LinkChUid = _model.GatewayUid;
				}
				catch (Exception ex)
				{
					_Ilog.Error(string.Format("query channel(ch={0}) switchtactics model error", call_factory.channel_list[i].nCh), ex);
					continue;
				}
			}
		}

        /* 网关转换 */
        /* 这俩我现在都没有搞明白和上面的区别 */
        /* 先进行测试吧 */
		public static void intilizate_gateway_switchtactics()
		{
			if (call_factory.gateway_list != null && call_factory.gateway_list.Count <= 0)
			{
				_Ilog.Error("gateway is not found in db", new ArgumentException("gateway_list"));
				return;
			}
			for (int i = 0; i < call_factory.gateway_list.Count; i++)
			{
                /* 实例 */
				call_factory.gateway_list[i].gateway_switch_tactics = new gateway_switchtactics_info();

				_Ilog.Info(string.Format("start intilizate gateway(ch={0}) switchtactics ", call_factory.gateway_list[i].gateway_name));

				if (call_factory.gateway_list[i] == null)
				{
					_Ilog.Error(string.Format("gateway(ch={0}) is empty data", call_factory.gateway_list[i].gateway_name));
					continue;
				}

                /* 通过网关标识找 */
				call_switchtactics_model _model = call_switchtactics.GetModelByGwUid(call_factory.gateway_list[i].gateway_uniqueid);
				if (_model == null)
				{
					_Ilog.Error(string.Format("query gateway(ch={0}) switchtactics model error", call_factory.gateway_list[i].gateway_name));
					continue;
				}

				try
				{
					call_factory.gateway_list[i].gateway_switch_tactics.Call_Switch_Adapter.LinkChUid = _model.ChannelUid;
				}
				catch (Exception ex)
				{
					_Ilog.Error(string.Format("query gateway(ch={0}) switchtactics model error", call_factory.gateway_list[i].gateway_name), ex);
					continue;
				}
			}
		}

	}
}
