using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Model
{
	public class call_gateway_model
	{
		public int ID { get; set; }

		public string UniqueID { get; set; }

		public string gw_name { get; set; }

		public string username { get; set; }

		public string password { get; set; }

		public string realm { get; set; }

		public string from_user { get; set; }

		public string from_domain { get; set; }

		public string extension { get; set; }

		public string proxy { get; set; }

		public string register_proxy { get; set; }

		public int expire_seconds { get; set; }

		public int register { get; set; }

		public string register_transport { get; set; }

		public int retry_seconds { get; set; }

		public int caller_id_in_from { get; set; }

		public string contact_params { get; set; }

		public int ping { get; set; }

		public int rfc_5626 { get; set; }

		public int reg_id { get; set; }

		public string remark { get; set; }

        public string gwtype { get; set; }
    }
}
