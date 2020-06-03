using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DB.Model
{
	public class call_param_model
	{

		public int ID { get; set; }

		public string P_Name { get; set; }

		public string P_Value { get; set; }

		public string P_Description { get; set; }

		public DateTime CreateTime { get; set; }

		public DateTime LoseTime { get; set; }

		public string P_Group { get; set; }

		public string Remark { get; set; }

	}
}
