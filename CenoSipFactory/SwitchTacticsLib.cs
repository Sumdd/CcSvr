using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CenoSipFactory
{
	public class SwitchTacticsLib
	{
		public struct call_switch_adapter
		{
			public call_switch_tactics CallSwitchGroup;
			public string LinkChUid;
			public int[] LinkGroup;
			public bool BusyTransfer;
			public bool NoAnswerTransfer;
			public bool EnforceTransfer;
			public int? TransferCh;
			public int[] TransferChGroup;
		}

		public struct dial_switch_adapter
		{
			public dial_switch_tacitcs DialSwitchGroup;
			public string LinkChUid;
			public int[] LinkGroup;
			public bool BusyTransfer;
			public bool NoAnswerTransfer;
			public bool EnforceTransfer;
			public int? TransferCh;
			public int[] TransferChGroup;
		}


		public enum call_switch_tactics
		{
			NAVIGATION_IVR,
			AUTO_ANSWER_HANGUP,
			AUTO_ANSWER_TANSFER_AGENT,
			MEMBER_VERIFICATION,
			TRANSFER_OTHER_NUMBER,
			TRANSFER_LINK_AGENT,
			TRANSFER_ASSIGNATION_AGENT,
		}
		public enum dial_switch_tacitcs
		{
			NAVIGATION_IVR,
			AUTO_ANSWER_HANGUP,
			AUTO_ANSWER_TANSFER_AGENT,
			MEMBER_VERIFICATION,
			TRANSFER_OTHER_NUMBER,
			TRANSFER_LINK_AGENT,
			TRANSFER_ASSIGNATION_AGENT,
		}

		public struct RINGCNT_PICKUP_ADAPTER
		{
			public RINGCNTPICKUP _RingCntModel;
			public int RingCnt;
		}

		public enum RINGCNTPICKUP
		{
			PICKUP_USER_PICKUP,
			RIGHTNOW_PICKUP,
			DEFINE_RINGCOUNT,
		}

		public enum RECORDING_ADAPTER
		{
			CALL_GET_CALLERNUMBER,
			CALL_GET_IMCOMEING,
			CALL_CALLER_PICKUP,
			CALL_NEVER,

			DIAL_USER_PICKUP,
			DIAL_RING_BACK,
			DIAL_GET_DIALNUMBER,
			DIAL_NEVER
		}
	}
}
