using DB.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
	public class fs_channel
	{
		public static IList<fs_channel_model> GetList(int top = 1000)
		{
			IList<fs_channel_model> list = new List<fs_channel_model>();
			string sql = "select ID,UniqueID,user,password,vm_password,toll_allow,accountcode,user_context,effective_caller_id_name,effective_caller_id_number,outbound_caller_id_name,outbound_caller_id_number,call_group from fs_channel limit ?top";
			MySqlParameter[] parameters = {
				new MySqlParameter("?top", MySqlDbType.Int32)
				};
			parameters[0].Value = top;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				while (dr != null && dr.Read())
				{
					list.Add(new fs_channel_model()
					{
						ID = int.Parse(dr["ID"].ToString()),
						UniqueID = dr["UniqueID"].ToString(),
						user = dr["user"].ToString(),
						password = dr["password"].ToString(),
						vm_password = dr["vm_password"].ToString(),
						toll_allow = dr["toll_allow"].ToString(),
						accountcode = dr["accountcode"].ToString(),
						user_context = dr["user_context"].ToString(),
						effective_caller_id_name = dr["effective_caller_id_name"].ToString(),
						effective_caller_id_number = dr["effective_caller_id_number"].ToString(),
						outbound_caller_id_name = dr["outbound_caller_id_name"].ToString(),
						outbound_caller_id_number = dr["outbound_caller_id_number"].ToString(),
						call_group = dr["call_group"].ToString()
					});
				}
			}
			return list;
		}
	}
}
