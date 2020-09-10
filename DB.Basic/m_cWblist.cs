using Core_v1;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DB.Basic
{
    public class m_mWblist
    {
        public int ID;
        public string wbnumber;
        public float ordernum;
        public Regex regex;
        public int wbtype;
    }

    public class m_cWblist
    {
        public static List<m_mWblist> m_lWblist;
        public static bool m_bInitWblist = false;

        public static void m_fInit()
        {
            if (m_bInitWblist) return;

            try
            {
                m_bInitWblist = true;

                m_lWblist = new List<m_mWblist>();

                string m_sSQL = $@"
SELECT
	`call_wblist`.`wbid` AS `ID`,
	`call_wblist`.`wbnumber`,
	`call_wblist`.`wbtype`,
	`call_wblist`.`ordernum` 
FROM
	`call_wblist` 
ORDER BY
	`call_wblist`.`ordernum`;
";
                DataSet m_pDataSet = DB.Basic.MySQL_Method.GetDataSetAll(m_sSQL);
                if (m_pDataSet != null && m_pDataSet.Tables.Count == 1 && m_pDataSet.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow item in m_pDataSet.Tables[0].Rows)
                    {
                        m_mWblist _m_mWblist = new m_mWblist();
                        _m_mWblist.ID = Convert.ToInt32(item["ID"]);
                        _m_mWblist.wbnumber = item["wbnumber"].ToString();

                        Regex m_rIsMatchRegex = new Regex($@"^[0-9*#]{{1,20}}$");
                        if (_m_mWblist.wbnumber == "*")
                            _m_mWblist.regex = new Regex($@"^[\s\S]*$");
                        else if (m_rIsMatchRegex.IsMatch(_m_mWblist.wbnumber))
                            _m_mWblist.regex = new Regex($@"^({_m_mWblist.wbnumber})$");
                        else
                            _m_mWblist.regex = new Regex($@"{_m_mWblist.wbnumber}");

                        _m_mWblist.ordernum = float.Parse(item["ordernum"].ToString());
                        _m_mWblist.wbtype = Convert.ToInt32(item["wbtype"]);
                        m_lWblist.Add(_m_mWblist);
                    }
                    Log.Instance.Success($"[DB.Basic][m_cWblist][m_fInit][wblist init success:{m_lWblist.Count}]");
                }
                else
                {
                    Log.Instance.Warn($"[DB.Basic][m_cWblist][m_fInit][wblist init finished]");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_cWblist][m_fInit][Exception][{ex.Message}]");
            }
            finally
            {
                m_cWblist.m_bInitWblist = false;
            }
        }
    }
}
