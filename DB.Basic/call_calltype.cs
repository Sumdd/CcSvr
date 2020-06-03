using DB.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
	public class call_calltype
	{
		public static call_calltype_model GetModel(string TypeValue)
		{
			var model = new call_calltype_model();
			string sql = "select ID,TypeName,TypeNo,TypeValue,TypeClass,Remark from call_calltype where TypeValue=?TypeValue limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?TypeValue", MySqlDbType.VarChar,50)
				};
			parameters[0].Value = TypeValue;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.TypeName = dr["TypeName"].ToString();
					model.TypeNo = dr["TypeNo"].ToString();
					model.TypeValue = dr["TypeValue"].ToString();
					model.TypeClass = dr["TypeClass"].ToString();
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}

	}
}
