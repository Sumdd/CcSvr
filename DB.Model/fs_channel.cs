using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Model
{
	public class fs_channel_model
	{
		public int ID { get; set; }

		public string UniqueID { get; set; }

		public string user { get; set; }

		public string password { get; set; }

		public string vm_password { get; set; }

		public string toll_allow { get; set; }

		public string accountcode { get; set; }

		public string user_context { get; set; }

		public string effective_caller_id_name { get; set; }

		public string effective_caller_id_number { get; set; }

		public string outbound_caller_id_name { get; set; }

		public string outbound_caller_id_number { get; set; }

		public string call_group { get; set; }

	}
}
