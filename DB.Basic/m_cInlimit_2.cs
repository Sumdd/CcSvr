using Core_v1;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
    public class m_mInlimit_2
    {
        public int inlimit_2id;
        public int useuser;
        public float ordernum;
        public string inlimit_2starttime;
        public string inlimit_2endtime;
        public string inlimit_2number;
        public int inlimit_2whatday;
        public int inlimit_2way;
        public int inlimit_2trycount;
        public string number;
        public string m_sGatewayNameStr;
        public string m_sGatewayType;
        public bool m_bGatewayType;
        /// <summary>
        /// 是否未内呼号码项
        /// </summary>
        public bool type;
    }

    public class m_cInlimit_2
    {
        public static List<m_mInlimit_2> m_lInlimit_2;
        public static bool m_bInitInlimit_2 = false;

        public static void m_fInit()
        {
            if (m_cInlimit_2.m_bInitInlimit_2) return;

            try
            {
                m_cInlimit_2.m_bInitInlimit_2 = true;

                m_cInlimit_2.m_lInlimit_2 = new List<m_mInlimit_2>();

                ///简化呼叫内转,只能接设置内转或不设置内转,去掉模式一说,暂不单独记通话记录
                ///这里直接将网关加入,直接融入逻辑里
                ///拓展呼叫内转逻辑

                string m_sSQL = $@"
SELECT
	* 
FROM
	(
	SELECT
		`dial_inlimit_2`.`inlimit_2id`,
		`dial_limit`.`useuser`,
		`dial_limit`.`ordernum`,
		`dial_inlimit_2`.`inlimit_2starttime`,
		`dial_inlimit_2`.`inlimit_2endtime`,
		`dial_inlimit_2`.`inlimit_2number`,
		`dial_inlimit_2`.`inlimit_2whatday`,
		`dial_inlimit_2`.`inlimit_2way`,
		`dial_inlimit_2`.`inlimit_2trycount`,
		`dial_limit`.`number`,
		`call_gateway`.`gw_name` AS `gw`,
		`call_gateway`.`gwtype`,
		1 AS `type` 
	FROM
		`dial_inlimit_2`
		INNER JOIN `dial_limit` ON `dial_inlimit_2`.`inlimit_2id` = `dial_limit`.`id`
		INNER JOIN `call_gateway` ON `dial_limit`.`gwuid` = `call_gateway`.`UniqueID` 
	WHERE
		`dial_limit`.`isuse` = 1 
		AND `dial_limit`.`isshare` = - 2 
		AND `dial_limit`.`isdel` = 0 UNION ALL
	SELECT
		- 1 AS `inlimit_2id`,
		- 1 AS `useuser`,
		0 AS `ordernum`,
		ifnull( ( SELECT v FROM dial_parameter WHERE k = 'inlimit_2starttime' LIMIT 1 ), '19:00:00' ) AS inlimit_2starttime,
		ifnull( ( SELECT v FROM dial_parameter WHERE k = 'inlimit_2endtime' LIMIT 1 ), '08:00:00' ) AS inlimit_2endtime,
		'' AS `inlimit_2number`,
		ifnull( ( SELECT v FROM dial_parameter WHERE k = 'inlimit_2whatday' LIMIT 1 ), 127 ) AS inlimit_2whatday,
		2 AS `inlimit_2way`,
		1 AS `inlimit_2trycount`,
		`call_gateway`.`inlimit_2caller` AS `number`,
		`call_gateway`.`gw_name` AS `gw`,
		`call_gateway`.`gwtype` AS `gwtype`,
		0 AS `type` 
	FROM
		`call_gateway` 
	WHERE
		1 = 1 
		AND `call_gateway`.`isinlimit_2` = 1 
	) `T0` 
ORDER BY
	`T0`.`type` DESC,
	`T0`.`ordernum` DESC;
";
                DataSet m_pDataSet = DB.Basic.MySQL_Method.GetDataSetAll(m_sSQL);
                if (m_pDataSet != null && m_pDataSet.Tables.Count == 1 && m_pDataSet.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow item in m_pDataSet.Tables[0].Rows)
                    {
                        m_mInlimit_2 _m_mInlimit_2 = new m_mInlimit_2();
                        _m_mInlimit_2.inlimit_2id = Convert.ToInt32(item["inlimit_2id"]);
                        _m_mInlimit_2.useuser = Convert.ToInt32(item["useuser"]);
                        _m_mInlimit_2.ordernum = float.Parse(item["ordernum"].ToString());
                        _m_mInlimit_2.inlimit_2starttime = item["inlimit_2starttime"].ToString();
                        _m_mInlimit_2.inlimit_2endtime = item["inlimit_2endtime"].ToString();
                        _m_mInlimit_2.inlimit_2number = item["inlimit_2number"].ToString();
                        _m_mInlimit_2.inlimit_2whatday = Convert.ToInt32(item["inlimit_2whatday"]);
                        _m_mInlimit_2.inlimit_2way = Convert.ToInt32(item["inlimit_2way"]);
                        _m_mInlimit_2.inlimit_2trycount = Convert.ToInt32(item["inlimit_2trycount"]);
                        _m_mInlimit_2.number = item["number"].ToString();
                        _m_mInlimit_2.m_sGatewayNameStr = item["gw"].ToString();
                        _m_mInlimit_2.m_sGatewayType = item["gwtype"].ToString();
                        _m_mInlimit_2.m_bGatewayType = item["gwtype"].ToString() == Model_v1.Special.Gateway;
                        _m_mInlimit_2.type = item["type"]?.ToString() == "1" ? true : false;
                        m_cInlimit_2.m_lInlimit_2.Add(_m_mInlimit_2);
                    }
                    Log.Instance.Success($"[DB.Basic][m_cInlimit_2][m_fInit][inlimit_2 init success:{m_lInlimit_2.Count}]");
                }
                else
                {
                    Log.Instance.Warn($"[DB.Basic][m_cInlimit_2][m_fInit][inlimit_2 init finished]");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_cInlimit_2][m_fInit][Exception][{ex.Message}]");
            }
            finally
            {
                m_cInlimit_2.m_bInitInlimit_2 = false;
            }
        }
    }
}
