using Core_v1;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
    public class m_mInrule
    {
        public int ID;
        public string inrulename;
        public float ordernum;
        public string inruleip;
        public int inruleport;
        public string inruleua;
        public string inrulesuffix;
        public int inrulemain;
        /// <summary>
        /// 是否为快捷电话薄项
        /// </summary>
        public bool type;
        /// <summary>
        /// 增加快捷电话簿功能,兼容原有逻辑
        /// </summary>
        public string inrulebookname { get; set; }
        public string inrulebookfkey { get; set; }
        public string inrulebooktkey { get; set; }
        public float inrulebookordernum { get; set; }
    }

    public class m_cInrule
    {
        public static m_mInrule m_pInrule;
        public static List<m_mInrule> m_lInrule;
        public static bool m_bInitInrule = false;

        public static void m_fInit()
        {
            if (m_bInitInrule) return;

            try
            {
                m_bInitInrule = true;

                ///清空本机内呼规则
                m_pInrule = null;

                m_lInrule = new List<m_mInrule>();

                string m_sSQL = $@"
SELECT
	* 
FROM
	(
	SELECT
		`call_inrule`.`inruleid` AS `ID`,
		`call_inrule`.`inrulename`,
		`call_inrule`.`inruleip`,
		`call_inrule`.`inruleport`,
		`call_inrule`.`inruleua`,
		`call_inrule`.`inrulesuffix`,
		`call_inrule`.`inrulemain`,
		`call_inrule`.`ordernum`,
		'0' AS `type`,
		0 AS `inrulebookordernum`,
		'' AS `inrulebookname`,
		'' AS `inrulebookfkey`,
		'' AS `inrulebooktkey` 
	FROM
		`call_inrule` UNION ALL
	SELECT
		`call_inrule`.`inruleid` AS `ID`,
		`call_inrule`.`inrulename`,
		`call_inrule`.`inruleip`,
		`call_inrule`.`inruleport`,
		`call_inrule`.`inruleua`,
		`call_inrule`.`inrulesuffix`,
		`call_inrule`.`inrulemain`,
		`call_inrule`.`ordernum`,
		'1' AS `type`,
		`call_inrulebook`.`inrulebookordernum`,
		`call_inrulebook`.`inrulebookname`,
		`call_inrulebook`.`inrulebookfkey`,
		`call_inrulebook`.`inrulebooktkey` 
	FROM
		`call_inrulebook`
		LEFT JOIN `call_inrule` ON `call_inrule`.`inruleid` = `call_inrulebook`.`inruleid` 
	WHERE
		`call_inrulebook`.`inruleid` 
	) `T0` 
ORDER BY
	`T0`.`ordernum`,
	`T0`.`inrulebookordernum`;
";
                DataSet m_pDataSet = DB.Basic.MySQL_Method.GetDataSetAll(m_sSQL);
                if (m_pDataSet != null && m_pDataSet.Tables.Count == 1 && m_pDataSet.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow item in m_pDataSet.Tables[0].Rows)
                    {
                        m_mInrule _m_mInrule = new m_mInrule();
                        _m_mInrule.ID = Convert.ToInt32(item["ID"]);
                        _m_mInrule.inrulename = item["inrulename"].ToString();
                        _m_mInrule.ordernum = float.Parse(item["ordernum"].ToString());
                        _m_mInrule.inruleip = item["inruleip"].ToString();
                        _m_mInrule.inruleport = Convert.ToInt32(item["inruleport"]);
                        _m_mInrule.inruleua = item["inruleua"].ToString();
                        _m_mInrule.inrulesuffix = item["inrulesuffix"].ToString();
                        _m_mInrule.inrulemain = Convert.ToInt32(item["inrulemain"]);
                        _m_mInrule.type = item["type"]?.ToString() == "1" ? true : false;
                        _m_mInrule.inrulebookname = item["inrulebookname"]?.ToString();
                        _m_mInrule.inrulebookfkey = item["inrulebookfkey"]?.ToString();
                        _m_mInrule.inrulebooktkey = item["inrulebooktkey"]?.ToString();
                        _m_mInrule.inrulebookordernum = float.Parse(item["inrulebookordernum"].ToString());
                        m_lInrule.Add(_m_mInrule);

                        ///加载本机内呼规则
                        if (_m_mInrule.inrulemain == 1 && !_m_mInrule.type)
                        {
                            m_cInrule.m_pInrule = _m_mInrule;
                        }
                    }
                    Log.Instance.Success($"[DB.Basic][m_cInrule][m_fInit][inrule init success:{m_lInrule.Count}]");
                }
                else
                {
                    Log.Instance.Warn($"[DB.Basic][m_cInrule][m_fInit][inrule init finished]");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_cInrule][m_fInit][Exception][{ex.Message}]");
            }
            finally
            {
                m_cInrule.m_bInitInrule = false;
            }
        }
    }
}
