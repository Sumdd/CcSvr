using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Model
{
	public class call_phonenum_list_model
	{
		public int id { get; set; }

		public string uniqueid { get; set; }

		/// <summary>
		/// black or write
		/// </summary>
		public string list_type { get; set; }

		/// <summary>
		/// outbound or inbound dial or call
		/// </summary>
		public string bound { get; set; }

		/// <summary>
		/// if the list_type is black and the bound is out,the value is '0' means not allow user dial local number, and the value is '1' means allow  user dial local number.
		/// if the list_type is black and the bound is in, the value is '0' means not allow user call local number, and the value is '1' means allow  user call local number.
		/// if the list_type is write and the bound is out, the value is '0' means not allow user dial local number, and the value is '1' means allow  user dial local number.
		/// if the list_type is write and the bound is in, the value is '0' means not allow local number calls, and the value is '1' means allow  local number calls.
		/// </summary>
		public int localflag { get; set; }

		/// <summary>
		/// if the list_type is black and the bound is out,the value is '0' means not allow user dial remote number, and the value is '1' means allow  user dial remote number.
		/// if the list_type is black and the bound is in, the value is '0' means not allow user call remote number, and the value is '1' means allow  user call remote number.
		/// if the list_type is write and the bound is out, the value is '0' means not allow user dial remote number, and the value is '1' means allow  user dial remote number.
		/// if the list_type is write and the bound is in, the value is '0' means not allow remote number calls, and the value is '1' means allow  remote number calls.
		/// </summary>
		public int remoteflag { get; set; }

		/// <summary>
		/// the frist number
		/// </summary>
		public string start_num { get; set; }

		public string end_num { get; set; }

		public DateTime addtime { get; set; }

		public DateTime validtime { get; set; }

		public string limituser { get; set; }

		public string adduser { get; set; }

		/// <summary>
		/// normal or sell or bilk or harass
		/// </summary>
		public string addreason { get; set; }

		public string remark { get; set; }

	}
}
