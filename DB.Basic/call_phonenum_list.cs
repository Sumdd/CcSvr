using DB.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
	public class call_phonenum_list
	{
		public static IList<call_phonenum_list_model> GetList(string list_type, int top = 10)
		{
			IList<call_phonenum_list_model> list = new List<call_phonenum_list_model>();
			string sql = "select id,uniqueid,list_type,bound,localflag,remoteflag,start_num,end_num,addtime,validtime,limituser,adduser,addreason,remark from call_phonenum_list where list_type=?list_type limit ?top";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?list_type", MySqlDbType.VarChar,10),
	 new MySqlParameter("?top", MySqlDbType.Int32)
				};
			parameters[0].Value = list_type;
			parameters[1].Value = top;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				while (dr.Read())
				{
					list.Add(new call_phonenum_list_model()
					{
						id = int.Parse(dr["id"].ToString()),
						uniqueid = dr["uniqueid"].ToString(),
						list_type = dr["list_type"].ToString(),
						bound = dr["bound"].ToString(),
						localflag = int.Parse(dr["localflag"].ToString()),
						remoteflag = int.Parse(dr["remoteflag"].ToString()),
						start_num = dr["start_num"].ToString(),
						end_num = dr["end_num"].ToString(),
						addtime = DateTime.Parse(dr["addtime"].ToString()),
						validtime = DateTime.Parse(dr["validtime"].ToString()),
						limituser = dr["limituser"].ToString(),
						adduser = dr["adduser"].ToString(),
						addreason = dr["addreason"].ToString(),
						remark = dr["remark"].ToString()
					});
				}
			}
			return list;
		}

	}
}
