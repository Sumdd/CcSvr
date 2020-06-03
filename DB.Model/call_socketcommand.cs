using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DB.Model
{
	public class call_socketcommand_model
	{
		public int ID { get; set; }

		public string S_NO { get; set; }

		public string S_Name { get; set; }

		public string S_Value { get; set; }

		public string S_Description { get; set; }

		public string S_Type { get; set; }

		public string S_StartChar { get; set; }

		public string S_EndChar { get; set; }

		public string Rep_OldChar { get; set; }

		public string Rep_NewChar { get; set; }

		public int S_ParentID { get; set; }

		public int S_Order { get; set; }

		public string Remark { get; set; }

	}
}
