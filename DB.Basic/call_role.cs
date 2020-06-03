using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using DB.Model;

namespace DB.Basic
{
	public class call_role
	{
		public static call_role_model GetModel(int ID)
		{
			var model = new call_role_model();
			string sql = "select ID,RoleName,RoleNO,RoleDescription from call_role where ID=?ID limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?ID", MySqlDbType.Int32)
				};
			parameters[0].Value = ID;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.RoleName = dr["RoleName"].ToString();
					model.RoleNO = dr["RoleNO"].ToString();
					model.RoleDescription = dr["RoleDescription"].ToString();
				}
			}
			return model;
		}
	}
}
