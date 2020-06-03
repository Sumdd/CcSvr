using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace CenoSipFactory
{
	public class RegistrationInfo
	{
		public RegistrationInfo(DataTable RegInfo)
		{
			if (RegInfo == null || RegInfo.Rows.Count <= 0)
				return;

			this.Call_ID = RegInfo.Rows[0]["call_id"].ToString();
			this.User = RegInfo.Rows[0]["sip_user"].ToString();
			this.Contact = RegInfo.Rows[0]["contact"].ToString();
			this.Agent = RegInfo.Rows[0]["user_agent"].ToString();
			this.Status = RegInfo.Rows[0]["status"].ToString();
			this.Host = RegInfo.Rows[0]["sip_host"].ToString();
			this.IP = RegInfo.Rows[0]["network_ip"].ToString();
			this.Port = RegInfo.Rows[0]["network_port"].ToString();
			this.Auth_User = RegInfo.Rows[0]["sip_username"].ToString();
			this.Auth_Realm = RegInfo.Rows[0]["sip_realm"].ToString();
			this.MWI_Account = RegInfo.Rows[0]["mwi_user"].ToString() + "@" + RegInfo.Rows[0]["mwi_host"].ToString();
		}



		public string Call_ID { get; set; }
		public string User { get; set; }
		public string Contact { get; set; }
		public string Agent { get; set; }
		public string Status { get; set; }
		public string Host { get; set; }
		public string IP { get; set; }
		public string Port { get; set; }
		public string Auth_User { get; set; }
		public string Auth_Realm { get; set; }
		public string MWI_Account { get; set; }
	}
}
