using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB.Model;
using MySql.Data.MySqlClient;
using System.Data;
using Core_v1;
using System.Xml;
using System.IO;
using System.Net;

namespace DB.Basic
{
	public class call_phoneaddress
	{
		public static call_phoneaddress_model GetModel(string PhoneNum)
		{
			var model = new call_phoneaddress_model();
			string sql = "select ID,PhoneNum,CardType,CityCode,CityName,ZipCode,Remark from call_phoneaddress where PhoneNum=?PhoneNum limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?PhoneNum", MySqlDbType.VarChar,50)
				};
			parameters[0].Value = PhoneNum;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.PhoneNum = dr["PhoneNum"].ToString();
					model.CardType = dr["CardType"].ToString();
					model.CityCode = dr["CityCode"].ToString();
					model.CityName = dr["CityName"].ToString();
					model.ZipCode = dr["ZipCode"].ToString();
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}

        public static string GetCityCode(string PhoneNum)
		{
			var model = new call_phoneaddress_model();
			string sql = "select ID,PhoneNum,CardType,CityCode,CityName,ZipCode,Remark from call_phoneaddress where PhoneNum like CONCAT('%',?PhoneNum,'%') limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?PhoneNum", MySqlDbType.VarChar,50)
				};
			parameters[0].Value = PhoneNum;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					return dr["CityName"].ToString();
				}
			}
			return "";
		}

		public static call_phoneaddress_model get_model_by_citycode(string CityCode)
		{
			var model = new call_phoneaddress_model();
			string sql = "select ID,PhoneNum,CardType,CityCode,CityName,ZipCode,Remark from call_phoneaddress where CityCode=?CityCode limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?CityCode", MySqlDbType.VarChar,50)
				};
			parameters[0].Value = CityCode;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.PhoneNum = dr["PhoneNum"].ToString();
					model.CardType = dr["CardType"].ToString();
					model.CityCode = dr["CityCode"].ToString();
					model.CityName = dr["CityName"].ToString();
					model.ZipCode = dr["ZipCode"].ToString();
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}

        public static string m_fGetCityNameByPhoneNumber(string m_sPhoneNumberStr, out string m_sCityCode)
        {
            ///<![CDATA[
            /// 手机号
            /// 此处追加电话号码网查、自动更新等逻辑即可
            /// ]]>

            DateTime? m_dtUpdTime = null;
            m_sCityCode = string.Empty;
            string m_sName = string.Empty;
            string sql = "select ID,PhoneNum,CardType,CityCode,CityName,ZipCode,Remark,upt from call_phoneaddress where PhoneNum like CONCAT('',?PhoneNum,'%') limit 1";
            MySqlParameter[] parameters = {
     new MySqlParameter("?PhoneNum", MySqlDbType.VarChar,50)
                };
            parameters[0].Value = m_sPhoneNumberStr;
            using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
            {
                if (dr.Read())
                {
                    m_dtUpdTime = Convert.ToDateTime(dr["upt"]);
                    m_sCityCode = dr["CityCode"].ToString();
                    m_sName = dr["CityName"].ToString();
                }
            }

            ///<![CDATA[
            /// 追加电话号码反查任务
            /// ]]>
            m_cPhone.m_fTaskUpdPhone(m_sPhoneNumberStr, m_dtUpdTime);

            ///返回归属地
            return m_sName;
        }

        public static string m_fGetCityNameByCityCode(string m_sCityCodeStr, out string m_sCityCode)
        {
            string sql = "select ID,PhoneNum,CardType,CityCode,CityName,ZipCode,Remark from call_phoneaddress where CityCode like CONCAT('',?CityCode,'%') limit 1";
            MySqlParameter[] parameters = {
     new MySqlParameter("?CityCode", MySqlDbType.VarChar,50)
                };
            parameters[0].Value = m_sCityCodeStr;
            using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
            {
                if (dr.Read())
                {
                    m_sCityCode = dr["CityCode"].ToString();
                    return dr["CityName"].ToString();
                }
            }
            m_sCityCode = string.Empty;
            return "";
        }

        /// <summary>
        /// 查询手机号码归属地
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string m_fGetPhoneAddressByNet(string number) {

            string m_sPhoneAddressStr = "未知";

            if (!Call_ParamUtil.IsLinkNet) {
                return m_sPhoneAddressStr;
            }

            ///<![CDATA[
            /// 访问号码归属地接口
            /// ]]>

            {
                //明天构造即可
            }

            return m_sPhoneAddressStr;
        }
    }
}
