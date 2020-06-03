using DB.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
	public class call_gateway
	{
		public static call_gateway_model GetModel_UniqueID(string UniqueID)
		{
			var model = new call_gateway_model();
			string sql = "select ID,UniqueID,gw_name,username,password,realm,from_user,from_domain,extension,proxy,register_proxy,expire_seconds,register,register_transport,retry_seconds,caller_id_in_from,contact_params,ping,rfc_5626,reg_id,remark,gwtype from call_gateway where UniqueID=?UniqueID limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?UniqueID", MySqlDbType.VarChar,36)
				};
			parameters[0].Value = UniqueID;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					//model.ID = int.Parse(dr["ID"].ToString());
					//model.UniqueID = dr["UniqueID"].ToString();
					model.gw_name = dr["gw_name"].ToString();
					//model.username = dr["username"].ToString();
					//model.password = dr["password"].ToString();
					//model.realm = dr["realm"].ToString();
					//model.from_user = dr["from_user"].ToString();
					//model.from_domain = dr["from_domain"].ToString();
					//model.extension = dr["extension"].ToString();
					//model.proxy = dr["proxy"].ToString();
					//model.register_proxy = dr["register_proxy"].ToString();
					//model.expire_seconds = int.Parse(dr["expire_seconds"].ToString());
					//model.register = int.Parse(dr["register"].ToString());
					//model.register_transport = dr["register_transport"].ToString();
					//model.retry_seconds = int.Parse(dr["retry_seconds"].ToString());
					//model.caller_id_in_from = int.Parse(dr["caller_id_in_from"].ToString());
					//model.contact_params = dr["contact_params"].ToString();
					//model.ping = int.Parse(dr["ping"].ToString());
					//model.rfc_5626 = int.Parse(dr["rfc_5626"].ToString());
					//model.reg_id = int.Parse(dr["reg_id"].ToString());
					model.remark = dr["remark"].ToString();
                    model.gwtype = dr["gwtype"].ToString();
				}
			}
			return model;
		}

		public static IList<call_gateway_model> GetList(int top = 1000)
		{
			IList<call_gateway_model> list = new List<call_gateway_model>();
			string sql = "select ID,UniqueID,gw_name,username,password,realm,from_user,from_domain,extension,proxy,register_proxy,expire_seconds,register,register_transport,retry_seconds,caller_id_in_from,contact_params,ping,rfc_5626,reg_id,remark from call_gateway limit ?top";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?top", MySqlDbType.Int32)
				};
			parameters[0].Value = top;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr == null || !dr.HasRows)
				{
					return list;
				}
				while (dr.Read())
				{
					list.Add(new call_gateway_model()
					{
						ID = int.Parse(dr["ID"].ToString()),
						UniqueID = dr["UniqueID"].ToString(),
						gw_name = dr["gw_name"].ToString(),
						username = dr["username"].ToString(),
						password = dr["password"].ToString(),
						realm = dr["realm"].ToString(),
						from_user = dr["from_user"].ToString(),
						from_domain = dr["from_domain"].ToString(),
						extension = dr["extension"].ToString(),
						proxy = dr["proxy"].ToString(),
						register_proxy = dr["register_proxy"].ToString(),
						expire_seconds = int.Parse(dr["expire_seconds"].ToString()),
						register = int.Parse(dr["register"].ToString()),
						register_transport = dr["register_transport"].ToString(),
						retry_seconds = int.Parse(dr["retry_seconds"].ToString()),
						caller_id_in_from = int.Parse(dr["caller_id_in_from"].ToString()),
						contact_params = dr["contact_params"].ToString(),
						ping = int.Parse(dr["ping"].ToString()),
						rfc_5626 = int.Parse(dr["rfc_5626"].ToString()),
						reg_id = int.Parse(dr["reg_id"].ToString()),
						remark = dr["remark"].ToString()
					});
				}
			}
			return list;
		}
	}
}
