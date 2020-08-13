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
    public class m_mRoute
    {
        public int ID;
        public string rnumber;
        public float ordernum;
        public Regex regex;
        public int ctype;
        public int rtype;
        /// <summary>
        /// 将作用范围枚举为坐席ID即可
        /// </summary>
        public List<int> routeua;
    }
    public class m_cRoute
    {
        public static List<m_mRoute> m_lRoute;
        public static bool m_bInitRouting = false;

        public static void m_fInit()
        {
            if (m_bInitRouting) return;

            try
            {
                m_bInitRouting = true;

                m_lRoute = new List<m_mRoute>();

                string m_sSQL = $@"
SELECT
	`call_route`.`ID`,
	`call_route`.`rnumber`,
	`call_route`.`ctype`,
	`call_route`.`rtype`,
	`call_route`.`ordernum` 
FROM
	`call_route` 
ORDER BY
	`call_route`.`ordernum`;
SELECT
	`call_routeua`.`rid`,
	`call_routeua`.`mtype`,
	`call_routeua`.`muuid` 
FROM
	`call_routeua`;
SELECT
	ID,
	TeamID 
FROM
	call_agent;
";
                DataSet m_pDataSet = DB.Basic.MySQL_Method.GetDataSetAll(m_sSQL);
                if (m_pDataSet != null && m_pDataSet.Tables.Count == 3 && m_pDataSet.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow item in m_pDataSet.Tables[0].Rows)
                    {
                        m_mRoute _m_mRoute = new m_mRoute();
                        _m_mRoute.ID = Convert.ToInt32(item["ID"]);
                        _m_mRoute.rnumber = item["rnumber"].ToString();

                        Regex m_rIsMatchRegex = new Regex($@"^[0-9*#]{{1,20}}$");
                        if (_m_mRoute.rnumber == "*")
                            _m_mRoute.regex = new Regex($@"^[\s\S]*$");
                        else if (m_rIsMatchRegex.IsMatch(_m_mRoute.rnumber))
                            _m_mRoute.regex = new Regex($@"^({_m_mRoute.rnumber})$");
                        else
                            _m_mRoute.regex = new Regex($@"{_m_mRoute.rnumber}");

                        _m_mRoute.ordernum = float.Parse(item["ordernum"].ToString());
                        _m_mRoute.ctype = Convert.ToInt32(item["ctype"]);
                        _m_mRoute.rtype = Convert.ToInt32(item["rtype"]);
                        _m_mRoute.routeua = new List<int>();
                        if (_m_mRoute.rtype == 1)
                        {
                            ///循环查询缓存,并将对象直接处理成通道号即可.账号最大
                            DataRow[] m_lAccountDataRow = m_pDataSet.Tables[1].Select($" [rid] = '{_m_mRoute.ID}' AND [mtype] = 'A' ");
                            if (m_lAccountDataRow?.Count() > 0)
                            {
                                foreach (DataRow m_pAccount in m_lAccountDataRow)
                                {
                                    _m_mRoute.routeua.Add(Convert.ToInt32(m_pAccount["muuid"]));
                                }
                                m_lRoute.Add(_m_mRoute);
                                continue;
                            }

                            ///循环查询缓存,并将对象直接处理成通道号即可.账号最大
                            DataRow[] m_lTeamDataRow = m_pDataSet.Tables[1].Select($" [rid] = '{_m_mRoute.ID}' AND [mtype] = 'T' ");
                            if (m_lTeamDataRow?.Count() > 0)
                            {
                                DataRow[] _m_lAccountDataRow = m_pDataSet.Tables[2].Select($" ( [TeamID] = '{string.Join("' OR [TeamID] = '", m_lTeamDataRow.Select(x => x.Field<object>("muuid")))}') ");
                                _m_mRoute.routeua.AddRange((from r in _m_lAccountDataRow.AsEnumerable()
                                                            select Convert.ToInt32(r["ID"])));
                                m_lRoute.Add(_m_mRoute);
                                continue;
                            }
                        }
                        else
                        {
                            ///全部赋值,统一逻辑即可
                            _m_mRoute.routeua.AddRange((from r in m_pDataSet.Tables[2].AsEnumerable()
                                                        select Convert.ToInt32(r["ID"])));
                            m_lRoute.Add(_m_mRoute);
                            continue;
                        }
                    }
                    Log.Instance.Success($"[DB.Basic][m_cRoute][m_fInit][route init success:{m_lRoute.Count}]");
                }
                else
                {
                    Log.Instance.Warn($"[DB.Basic][m_cRoute][m_fInit][route init finished]");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_cRoute][m_fInit][Exception][{ex.Message}]");
            }
            finally
            {
                m_bInitRouting = false;
            }
        }
    }
}
