using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DB.Model;
using MySql.Data.MySqlClient;

namespace DB.Basic
{
	public class call_team
	{
		public static call_team_model GetModel(int ID)
		{
			var model = new call_team_model();
			string sql = "select ID,TeamName,AgentCount,ManagerID,CreateTime,UpdateTime from call_team where ID=?ID limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?ID", MySqlDbType.Int32)
				};
			parameters[0].Value = ID;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.TeamName = dr["TeamName"].ToString();
					model.AgentCount = int.Parse(dr["AgentCount"].ToString());
					model.ManagerID = int.Parse(dr["ManagerID"].ToString());
					model.CreateTime = DateTime.Parse(dr["CreateTime"].ToString());
					model.UpdateTime = DateTime.Parse(dr["UpdateTime"].ToString());
				}
			}
			return model;
		}
	}
}
