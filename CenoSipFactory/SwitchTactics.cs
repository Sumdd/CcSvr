using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CenoSipFactory
{
	public class channel_switchtactics_info
	{

		public channel_switchtactics_info()
		{

		}

		public static channel_switchtactics_info Instance()
		{
			return new channel_switchtactics_info();
		}

		public SwitchTacticsLib.RINGCNT_PICKUP_ADAPTER RingCntPickupAdapter;

		public SwitchTacticsLib.RECORDING_ADAPTER Call_RecordingAdapter;

		public SwitchTacticsLib.RECORDING_ADAPTER Dial_RecordingAdapter;

		public SwitchTacticsLib.call_switch_adapter Call_Switch_Adapter;

		public SwitchTacticsLib.dial_switch_adapter Dial_Switch_Adapter;
	}

	public class gateway_switchtactics_info
	{

		public gateway_switchtactics_info()
		{

		}

		public static gateway_switchtactics_info Instance()
		{
			return new gateway_switchtactics_info();
		}

		public SwitchTacticsLib.RINGCNT_PICKUP_ADAPTER RingCntPickupAdapter;

		public SwitchTacticsLib.RECORDING_ADAPTER Call_RecordingAdapter;

		public SwitchTacticsLib.RECORDING_ADAPTER Dial_RecordingAdapter;

		public SwitchTacticsLib.call_switch_adapter Call_Switch_Adapter;

		public SwitchTacticsLib.dial_switch_adapter Dial_Switch_Adapter;
	}
}
