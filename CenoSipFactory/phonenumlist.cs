using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoSipFactory
{
	public class phonenumlist
	{
		public Guid uid { get; set; }

		public _list_type list_type { get; set; }

		public _bound bound { get; set; }

		public bool localflag { get; set; }

		public bool remoteflag { get; set; }

		public string start_num { get; set; }

		public string end_num { get; set; }

		public DateTime valid_time { get; set; }

		public DateTime add_time { get; set; }

		public Guid limit_user { get; set; }


		public string addreason { get; set; }

		public string add_user { get; set; }

	}

	public enum _list_type
	{
		write,
		black
	}

	public enum _bound
	{
		outbound,
		inbound
	}
}
