using Core_v1;
using MySql.Data.MySqlClient;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
    public class m_cEsySQL
    {
        private static ConnectionConfig m_pConfig
        {
            get
            {
                return new ConnectionConfig()
                {
                    ConnectionString = Call_ParamUtil.m_sHomeConnString,
                    DbType = SqlSugar.DbType.SqlServer,
                    IsAutoCloseConnection = true
                };
            }
        }

        public static string m_fGetContact(string m_sPhoneNumberString)
        {
            try
            {
                SqlSugarClient esyClient = new SqlSugarClient(m_cEsySQL.m_pConfig);

                ///修正传往数据库的查询语句
                string m_sSQL = Call_ParamUtil.m_sHomeSelectString;
                if (!string.IsNullOrWhiteSpace(m_sSQL))
                {
                    if (m_sSQL.Contains("@args"))
                    {
                        object m_oObject = esyClient.Ado.GetScalar(Call_ParamUtil.m_sHomeSelectString, new { args = m_sPhoneNumberString });
                        if (m_oObject != null)
                            return m_oObject.ToString();
                    }
                    else if (m_sSQL.Contains("@where"))
                    {
                        ///非空,不查
                        if (m_sPhoneNumberString == null) return null;

                        ///可能1
                        string _1 = m_sPhoneNumberString.TrimStart('0');

                        ///过短,不查
                        if (_1.Length < 6) return null;

                        ///可能2
                        string _2 = $"0{_1}";

                        ///可能3
                        string _3 = $"00{_2}";

                        ///条件
                        string m_sWhere = $" Phone IN ( @_1, @_2, @_3 ) ";

                        object m_oObject = esyClient.Ado.GetScalar(Call_ParamUtil.m_sHomeSelectString.Replace("@where", m_sWhere), new { _1 = _1, _2 = _2, _3 = _3 });
                        if (m_oObject != null)
                            return m_oObject.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_cEsySQL][m_fGetContact][Exception][{ex.Message}]");
            }
            return null;
        }

        public static void m_fSetExpc(string m_sPhoneNumberString)
        {
            new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                try
                {
                    m_mContact _m_mContact = m_cEsySQL.m_fMySQLGetContact(m_sPhoneNumberString);
                    //时间与联系人姓名一同查询出来
                    if (!(_m_mContact != null && _m_mContact.m_dtUpdateTime > Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd 00:00:00"))))
                    {
                        //需要查询催收数据库
                        string _m_sRealNameString = m_cEsySQL.m_fGetContact(m_sPhoneNumberString);
                        if (!string.IsNullOrWhiteSpace(_m_sRealNameString))
                        {
                            //更新呼叫中心数据库
                            m_cEsySQL.m_fMySQLSetContact(m_sPhoneNumberString, _m_sRealNameString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Error($"[DB.Basic][m_cEsySQL][m_fSetExpc][Exception][{ex.Message}]");
                }

            })).Start();
        }

        public static m_mContact m_fMySQLGetContact(string m_sPhoneNumberString)
        {
            try
            {
                List<MySqlParameter> m_pMySqlParameter = new List<MySqlParameter>();
                m_pMySqlParameter.Add(new MySqlParameter("?args_number", m_sPhoneNumberString));
                DataSet ds = MySQL_Method.ExecuteDataSetByProcedure("proc_get_realname_by_phone", m_pMySqlParameter.ToArray());
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return new m_mContact()
                    {
                        m_sRealNameString = ds.Tables[0].Rows[0]["realname"].ToString(),
                        m_dtUpdateTime = Convert.ToDateTime(ds.Tables[0].Rows[0]["updatetime"])
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_cShowStyle][m_fGetContact][Exception][{ex.Message}]");
            }
            return null;
        }

        public static void m_fMySQLSetContact(string m_sPhoneNumberString, string m_sRealNameString)
        {
            try
            {
                List<MySqlParameter> m_pMySqlParameter = new List<MySqlParameter>();
                m_pMySqlParameter.Add(new MySqlParameter("?args_number", m_sPhoneNumberString));
                m_pMySqlParameter.Add(new MySqlParameter("?args_realname", m_sRealNameString));
                MySQL_Method.ExecuteDataSetByProcedure("proc_set_realname_by_phone", m_pMySqlParameter.ToArray());
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_cShowStyle][m_fSetContact][Exception][{ex.Message}]");
            }
        }
    }

    public class m_mContact
    {
        public string m_sRealNameString
        {
            get;
            set;
        }

        public DateTime m_dtUpdateTime
        {
            get;
            set;
        }
    }
}
