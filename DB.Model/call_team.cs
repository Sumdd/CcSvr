using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DB.Model
{
	public class call_team_model
	{
		public int ID { get; set; }

		public string TeamName { get; set; }

		public int AgentCount { get; set; }

		public int ManagerID { get; set; }

		public DateTime CreateTime { get; set; }

		public DateTime UpdateTime { get; set; }

	}
}
