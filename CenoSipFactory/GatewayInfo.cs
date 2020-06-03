using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CenoSipFactory
{
	public class GatewayInfo
	{
		public string ngw;
		public string gateway_uniqueid;
		public string gateway_name;
		public string gateway_call_name;
		public string gateway_user_name;

		public string gateway_call_uuid;

		public string gateway_number;
		public string gateway_call_other_uuid;
		public int gateway_type;
		public CALLTYPE gateway_call_type;
		public StringBuilder gateway_caller_number;
		public StringBuilder gateway_callee_number;
		public Dictionary<int, char> gateway_call_dtmf;
		public AccountInfo gateway_account_info;
		public APP_USER_STATUS gateway_call_status;
		public CH_CALL_RECORD gateway_call_record_info;
		public ChSocket gateway_socket;
		public gateway_switchtactics_info gateway_switch_tactics;
	}
}
