using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Model
{
	public class call_calltype_model
	{
		public call_calltype_model()
		{
			this.ID = -1;
			this.TypeClass = "";
			this.TypeName = "";
			this.TypeNo = "";
			this.TypeValue = "";
		}

		public int ID { get; set; }

		public string TypeName { get; set; }

		public string TypeNo { get; set; }

		public string TypeValue { get; set; }

		public string TypeClass { get; set; }

		public string Remark { get; set; }

	}
}
