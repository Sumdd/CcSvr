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
                string m_sSQL = $@"
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
	`call_gateway`.`gwtype` 
FROM
	`dial_inlimit_2`
	INNER JOIN `dial_limit` ON `dial_inlimit_2`.`inlimit_2id` = `dial_limit`.`id`
	INNER JOIN `call_gateway` ON `dial_limit`.`gwuid` = `call_gateway`.`UniqueID` 
WHERE
	`dial_limit`.`isuse` = 1 
	AND `dial_limit`.`isdel` = 0 
ORDER BY
	`dial_limit`.`ordernum` ASC;
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
