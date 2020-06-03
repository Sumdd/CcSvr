using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core_v1;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace DB.Basic
{
    public class m_mDialLimit
    {
        public string m_sNumberStr;
        public int m_uDialCount;
        public string m_sGatewayNameStr;
        public bool m_bGatewayType;
        public string m_sGatewayType;
        public string m_sDialPrefixStr;
        public string m_sAreaCodeStr;
        public string m_sAreaNameStr;
        public bool m_bZflag;
        public string m_sDtmf;
        public string m_stNumberStr;
    }

    public class m_fDialLimit
    {
        /// <summary>
        /// 拨号限制方法
        /// </summary>
        /// <param name="nCh"></param>
        /// <returns></returns>
        public static DataTable m_fGetDialLimit(string _phonenum, int _useuser, string m_sCaller = null, string m_sConnStr = null)
        {
            List<MySqlParameter> m_pMySqlParameter = new List<MySqlParameter>();
            m_pMySqlParameter.Add(new MySqlParameter("?_phonenum", _phonenum));
            m_pMySqlParameter.Add(new MySqlParameter("?_useuser", _useuser));
            m_pMySqlParameter.Add(new MySqlParameter("?m_sCaller", m_sCaller));
            DataSet ds = MySQL_Method.ExecuteDataSetByProcedure(m_sConnStr, "proc_get_dial_limit", m_pMySqlParameter.ToArray());
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return ds.Tables[0];
            }
            return null;
        }

        public static m_mDialLimit m_fGetDialLimitObject(string _phonenum, int _useuser, string m_sCaller = null, string m_sConnStr = null)
        {
            DataTable dt = m_fGetDialLimit(_phonenum, _useuser, m_sCaller, m_sConnStr);
            if (dt != null && dt.Rows.Count > 0)
            {
                m_mDialLimit _m_mDialLimit = new m_mDialLimit();
                _m_mDialLimit.m_sNumberStr = dt.Rows[0]["number"].ToString();
                _m_mDialLimit.m_uDialCount = Convert.ToInt32(dt.Rows[0]["dialcount"]);
                _m_mDialLimit.m_sGatewayNameStr = dt.Rows[0]["gw"].ToString();
                _m_mDialLimit.m_bGatewayType = dt.Rows[0]["gwtype"].ToString() == "gateway";
                _m_mDialLimit.m_sGatewayType = dt.Rows[0]["gwtype"].ToString();
                _m_mDialLimit.m_sDialPrefixStr = dt.Rows[0]["dialprefix"].ToString();
                _m_mDialLimit.m_sAreaCodeStr = dt.Rows[0]["areacode"].ToString();
                _m_mDialLimit.m_sAreaNameStr = dt.Rows[0]["areaname"].ToString();
                _m_mDialLimit.m_bZflag = Convert.ToInt32(dt.Rows[0]["zflag"]) == 1;
                try
                {
                    _m_mDialLimit.m_sDtmf = dt.Rows[0]["dtmf"].ToString();
                    _m_mDialLimit.m_stNumberStr = dt.Rows[0]["tnumber"].ToString();
                }
                catch (Exception ex)
                {
                    Log.Instance.Error($"[DB.Basic][m_fDialLimit][m_fGetDialLimitObject][{ex.Message}]");
                    _m_mDialLimit.m_sDtmf = Call_ParamUtil.m_sDTMFSendMethod;
                    _m_mDialLimit.m_stNumberStr = string.Empty;
                }
                return _m_mDialLimit;
            }
            return null;
        }
        /// <summary>
        /// 写入拨号限制
        /// </summary>
        /// <param name="_thennum"></param>
        /// <param name="_useuser"></param>
        /// <param name="_duration"></param>
        public static void m_fSetDialLimit(string _thennum, int _useuser, int _duration, Model_v1.dial_area m_pDialArea = null)
        {
            ///if (Call_ParamUtil.IsMultiPhone)
            {
                string m_sConnStr = string.Empty;
                string m_sNumberType = Model_v1.Special.Common;
                if (m_pDialArea != null)
                {
                    m_sNumberType = Model_v1.Special.Share;
                    m_sConnStr = MySQLDBConnectionString.m_fConnStr(m_pDialArea);
                }

                List<MySqlParameter> m_pMySqlParameter = new List<MySqlParameter>();
                m_pMySqlParameter.Add(new MySqlParameter("?_thennum", _thennum));
                m_pMySqlParameter.Add(new MySqlParameter("?_useuser", _useuser));
                m_pMySqlParameter.Add(new MySqlParameter("?_duration", _duration));
                m_pMySqlParameter.Add(new MySqlParameter("?m_sNumberType", m_sNumberType));

                MySQL_Method.ExecuteDataSetByProcedure(m_sConnStr, "proc_set_dial_limit", m_pMySqlParameter.ToArray());
            }
        }
        /// <summary>
        /// 获取,即便禁用,也要接进来
        /// </summary>
        public static int m_fGetAgentID(string m_sCallee, out string m_stNumberStr, bool m_bOnlyLimit, string m_sCaller)
        {
            //得到系统的呼入路由查找规则
            ///<![CDATA[
            /// 1.需查找拨号限制
            /// 2.需查找通话记录
            /// ]]>
            int m_uCallRule = Call_ParamUtil.m_uCallRule;
            //坐席通道缓存
            int m_iLimitInt = -1;
            int m_iRecordInt = -1;
            //真实号码缓存
            string m_sLimittNumberStr = string.Empty;
            string m_sRecordtNumberStr = string.Empty;
            //真实号码,默认为空即可
            m_stNumberStr = string.Empty;
            //得到号码的呼入路由查找规则
            int m_iLimitCallRule = -1;
            //查询路由
            try
            {
                //需查找拨号限制
                if (m_bOnlyLimit || (m_uCallRule & 1) > 0)
                {
                    string m_sSQL = $@"
SELECT
	`dial_limit`.`useuser`,
	`dial_limit`.`tnumber`,
	`dial_limit`.`LimitCallRule` 
FROM
	`dial_limit` 
WHERE
	`dial_limit`.`number` = '{m_sCallee}'
	AND `dial_limit`.`isdel` = 0 
	AND `dial_limit`.`isshare` = 0 
	LIMIT 1;
";
                    DataTable dt = MySQL_Method.BindTable(m_sSQL);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        int.TryParse(dt.Rows[0]["LimitCallRule"]?.ToString(), out m_iLimitCallRule);
                        int.TryParse(dt.Rows[0]["useuser"]?.ToString(), out m_iLimitInt);
                        m_sLimittNumberStr = dt.Rows[0]["tnumber"]?.ToString();
                    }
                }

                //需查找通话记录
                if (!m_bOnlyLimit && ((m_iLimitCallRule & 2) > 0 || (m_uCallRule & 2) > 0))
                {
                    //处理出后缀
                    string _m_sCaller = m_sCaller.TrimStart('0');
                    //带入查询,查找反向索引即可,优化速度
                    string m_sSQL = $@"
SELECT
	AgentID AS `useuser`,
	`call_record`.`tnumber` 
FROM
	`call_record` 
WHERE
	`call_record`.`phone_desc` LIKE CONCAT( REVERSE( '{_m_sCaller}' ), '%' ) 
	AND `call_record`.`CallType` = 1 
ORDER BY
	`call_record`.`C_StartTime` DESC 
	LIMIT 1;
";
                    DataTable dt = MySQL_Method.BindTable(m_sSQL);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        int.TryParse(dt.Rows[0]["useuser"]?.ToString(), out m_iRecordInt);
                        m_sRecordtNumberStr = dt.Rows[0]["tnumber"]?.ToString();
                    }
                }

                //结果返回判断
                if (((m_iLimitCallRule & 2) > 0 || (m_uCallRule & 2) > 0) && m_iRecordInt != -1)
                {
                    m_stNumberStr = m_sRecordtNumberStr;
                    return m_iRecordInt;
                }
                if (((m_iLimitCallRule & 1) > 0 && (m_uCallRule & 1) > 0))
                {
                    m_stNumberStr = m_sLimittNumberStr;
                    return m_iLimitInt;
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_fDialLimit][get_agent][{ex.Message}]");
            }
            m_stNumberStr = string.Empty;
            return -1;
        }
        /// <summary>
        /// 获取拨号域
        /// </summary>
        public static void m_fGetDialArea()
        {
            Core_v1.Redis2.dialarea_list?.Clear();
            List<Model_v1.dial_area> m_lDialArea = new List<Model_v1.dial_area>();
            try
            {
                string m_sSQL = $@"
SELECT
	* 
FROM
	`dial_area` 
ORDER BY
	`dial_area`.`amain` DESC;
";
                DataTable dt = MySQL_Method.BindTable(m_sSQL);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow item in dt.Rows)
                    {
                        Model_v1.dial_area m_pDialArea = new Model_v1.dial_area();
                        m_pDialArea.id = Convert.ToInt32(item["id"]);
                        m_pDialArea.aname = item["aname"].ToString();
                        m_pDialArea.aip = item["aip"].ToString();
                        m_pDialArea.aport = Convert.ToInt32(item["aport"]);
                        m_pDialArea.adb = item["adb"].ToString();
                        m_pDialArea.auid = item["auid"].ToString();
                        m_pDialArea.apwd = item["apwd"].ToString();
                        m_pDialArea.amain = Convert.ToInt32(item["amain"]);
                        m_pDialArea.astate = Convert.ToInt32(item["astate"]);
                        ///<![CDATA[
                        /// 状态,加入是否可以使用共享号码的判断逻辑
                        /// ]]>
                        m_lDialArea.Add(m_pDialArea);

                        if (m_pDialArea.amain == 2 && m_pDialArea.astate == 2)
                            Redis2.m_EsyDialArea = m_pDialArea;
                        else if (m_pDialArea.amain == 1 && (m_pDialArea.astate == 2 || m_pDialArea.astate == 4))
                            Redis2.m_EsyMainDialArea = m_pDialArea;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_fDialLimit][m_fGetDialArea][Exception][{ex.Message}]");
            }

            Core_v1.Redis2.dialarea_list = m_lDialArea;
        }
        /// <summary>
        /// 获取共享号码
        /// </summary>
        public static void m_fGetShareNumber()
        {
            Core_v1.Redis2.sharenum_list?.Clear();
            List<Model_v1.share_number> m_lShareNumber = new List<Model_v1.share_number>();
            try
            {
                if (Core_v1.Redis2.dialarea_list != null && Core_v1.Redis2.dialarea_list.Count > 0)
                {
                    string m_sSQL = $@"
SELECT
	`dial_limit`.*,
	`call_gateway`.`gw_name` AS `gw`,
	`call_gateway`.`gwtype` 
FROM
	`dial_limit`
	INNER JOIN `call_gateway` ON `dial_limit`.`gwuid` = `call_gateway`.`UniqueID` 
WHERE
	`dial_limit`.`isdel` = 0 
	AND `dial_limit`.`isuse` = 1 
	AND `dial_limit`.`isshare` = 1;
";
                    //循环共享域服务器
                    foreach (Model_v1.dial_area item in Core_v1.Redis2.dialarea_list)
                    {
                        try
                        {
                            DataTable m_pDataTable = MySQL_Method.BindTable(m_sSQL, null, MySQLDBConnectionString.m_fConnStr(item));
                            if (m_pDataTable != null && m_pDataTable.Rows.Count > 0)
                            {
                                foreach (DataRow m_pDataRow in m_pDataTable.Rows)
                                {
                                    string m_sNumber = m_pDataRow["number"].ToString();
                                    //相同号码仅加载一次
                                    if (m_lShareNumber?.Where(x => x.number == m_sNumber)?.Count() <= 0)
                                    {
                                        Model_v1.share_number m_pShareNumber = new Model_v1.share_number();
                                        m_pShareNumber.areaid = item.id;
                                        m_pShareNumber.id = Convert.ToInt32(m_pDataRow["id"]);
                                        m_pShareNumber.number = m_sNumber;
                                        m_pShareNumber.limitthedial = Convert.ToInt32(m_pDataRow["limitthedial"]);
                                        m_pShareNumber.usecount = Convert.ToInt32(m_pDataRow["usecount"]);
                                        m_pShareNumber.useduration = Convert.ToInt32(m_pDataRow["useduration"]);
                                        m_pShareNumber.limitcount = Convert.ToInt32(m_pDataRow["limitcount"]);
                                        m_pShareNumber.usethetime = Convert.ToDateTime(m_pDataRow["usethetime"]);
                                        m_pShareNumber.limitduration = Convert.ToInt32(m_pDataRow["limitduration"]);
                                        m_pShareNumber.usethecount = Convert.ToInt32(m_pDataRow["usethecount"]);
                                        m_pShareNumber.usetheduration = Convert.ToInt32(m_pDataRow["usetheduration"]);
                                        m_pShareNumber.limitthecount = Convert.ToInt32(m_pDataRow["limitthecount"]);
                                        m_pShareNumber.limittheduration = Convert.ToInt32(m_pDataRow["limittheduration"]);
                                        m_pShareNumber.isuse = Convert.ToInt32(m_pDataRow["isuse"]);
                                        m_pShareNumber.dialprefix = m_pDataRow["dialprefix"].ToString();
                                        m_pShareNumber.areacode = m_pDataRow["areacode"].ToString();
                                        m_pShareNumber.areaname = m_pDataRow["areaname"].ToString();
                                        m_pShareNumber.isusedial = Convert.ToInt32(m_pDataRow["isusedial"]);
                                        m_pShareNumber.isusecall = Convert.ToInt32(m_pDataRow["isusecall"]);
                                        m_pShareNumber.dtmf = m_pDataRow["dtmf"].ToString();
                                        m_pShareNumber.state = Model_v1.SHARE_NUM_STATUS.IDLE;
                                        m_pShareNumber.gw = m_pDataRow["gw"].ToString();
                                        m_pShareNumber.gwtype = m_pDataRow["gwtype"].ToString();
                                        m_pShareNumber.tnumber = m_pDataRow["tnumber"].ToString();
                                        m_pShareNumber.ordernum = Convert.ToDecimal(m_pDataRow["ordernum"]);
                                        m_pShareNumber.uuid = $"{m_pShareNumber.areaid}_{m_pShareNumber.id}_{m_pShareNumber.number}|{item.aip}_{Guid.NewGuid()}";
                                        m_lShareNumber.Add(m_pShareNumber);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Instance.Error($"[DB.Basic][proc_dial_limit][m_fGetShareNumber][foreach][Exception][{item.aname},{item.aip},{item.aport},{item.adb},{item.auid}:{ex.Message}]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][proc_dial_limit][m_fGetShareNumber][Exception][{ex.Message}]");
            }

            Core_v1.Redis2.sharenum_list = m_lShareNumber;
        }
        /// <summary>
        /// 获取拨号限制
        /// </summary>
        /// <param name="m_pShareNumber"></param>
        /// <returns></returns>
        public static m_mDialLimit m_fGetDialLimitByShare(Model_v1.share_number m_pShareNumber)
        {
            if (m_pShareNumber == null)
                return null;
            m_mDialLimit _m_mDialLimit = new m_mDialLimit();
            _m_mDialLimit.m_sNumberStr = m_pShareNumber.number;
            //暂时不启用同号码限呼
            _m_mDialLimit.m_uDialCount = 0;
            _m_mDialLimit.m_sGatewayNameStr = m_pShareNumber.gw;
            _m_mDialLimit.m_bGatewayType = m_pShareNumber.gwtype == "gateway";
            _m_mDialLimit.m_sGatewayType = m_pShareNumber.gwtype;
            _m_mDialLimit.m_sDialPrefixStr = m_pShareNumber.dialprefix;
            _m_mDialLimit.m_sAreaCodeStr = m_pShareNumber.areacode;
            _m_mDialLimit.m_sAreaNameStr = m_pShareNumber.areaname;
            //默认自动加拨前缀
            _m_mDialLimit.m_bZflag = true;
            try
            {
                _m_mDialLimit.m_sDtmf = m_pShareNumber.dtmf;
                _m_mDialLimit.m_stNumberStr = m_pShareNumber.tnumber;
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_fDialLimit][m_fGetDialLimitByShare][{ex.Message}]");
                _m_mDialLimit.m_sDtmf = Call_ParamUtil.m_sDTMFSendMethod;
                _m_mDialLimit.m_stNumberStr = string.Empty;
            }
            return _m_mDialLimit;
        }
        /// <summary>
        /// 共享号码的呼入通过查询录音来判断接入哪个坐席
        /// <![CDATA[
        /// 这里后续修正
        /// 根据反序对方号码进行查询
        /// ]]>
        /// </summary>
        public static Model_v1.AddRecByRec m_fGetAgentByRecord(string m_sCaller, string m_sCallee, string m_sFreeSWITCHIPv4)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(m_sCaller))
                    return null;
                if (m_sCaller.ToLower() == "unknown")
                    return null;
                if (string.IsNullOrWhiteSpace(m_sCallee))
                    return null;
                if (m_sCallee.ToLower() == "unknown")
                    return null;

                //处理出后缀
                string _m_sCallee = m_sCallee.TrimStart('0');

                string m_sSQL = $@"
SELECT
	AgentID,
    fromagentid,
	ChannelID,
	FreeSWITCHIPv4,
	UAID
FROM
	`call_record` 
WHERE
	`call_record`.`isshare` = 1 
	AND `call_record`.`LocalNum` = '{m_sCaller}' 
	AND `call_record`.`phone_desc` LIKE CONCAT( REVERSE( '{_m_sCallee}' ), '%' ) 
	AND `call_record`.`CallType` = 1 
ORDER BY
	`call_record`.`C_StartTime` DESC 
	LIMIT 1;
";
                DataTable m_pDataTable = MySQL_Method.BindTable(m_sSQL);
                if (m_pDataTable != null && m_pDataTable.Rows.Count > 0)
                {
                    DataRow m_pDataRow = m_pDataTable.Rows[0];
                    Model_v1.AddRecByRec m_pAddRecByRec = new Model_v1.AddRecByRec();
                    m_pAddRecByRec.m_sFreeSWITCHIPv4 = m_pDataRow["FreeSWITCHIPv4"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(m_pAddRecByRec.m_sFreeSWITCHIPv4))
                    {
                        if (m_pAddRecByRec.m_sFreeSWITCHIPv4.Equals(m_sFreeSWITCHIPv4))
                        {
                            m_pAddRecByRec.m_sEndPointStr = $"user/{m_pDataRow["UAID"]}";
                        }
                        else
                        {
                            m_pAddRecByRec.m_sEndPointStr = $"sofia/external/sip:*{m_pDataRow["UAID"]}@{m_pDataRow["FreeSWITCHIPv4"]:5080}";
                        }
                    }
                    else
                    {
                        m_pAddRecByRec.m_sFreeSWITCHIPv4 = m_sFreeSWITCHIPv4;
                        m_pAddRecByRec.m_sEndPointStr = $"user/{m_pDataRow["UAID"]}";
                    }
                    m_pAddRecByRec.m_uAgentID = Convert.ToInt32(m_pDataRow["AgentID"]);
                    m_pAddRecByRec.m_uFromAgentID = Convert.ToInt32(m_pDataRow["fromagentid"]);
                    m_pAddRecByRec.m_uChannelID = Convert.ToInt32(m_pDataRow["ChannelID"]);
                    m_pAddRecByRec.UAID = m_pDataRow["UAID"].ToString();
                    return m_pAddRecByRec;
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_fDialLimit][m_fGetAgentByRecord][{ex.Message}]");
            }
            return null;
        }

        public static string m_fGetDialUUID(string m_sLoginName, out string m_dtString)
        {
            m_dtString = string.Empty;
            try
            {
                string m_sSQL = $@"
SELECT
	`call_dialuuid`.`recuuid` 
FROM
	`call_dialuuid` 
WHERE
	`call_dialuuid`.`loginname` = '{m_sLoginName}'
	AND `call_dialuuid`.`isdel` = 0;
UPDATE `call_dialuuid` 
SET `call_dialuuid`.`isdel` = 1 
WHERE
	`call_dialuuid`.`loginname` = '{m_sLoginName}' 
	AND `call_dialuuid`.`isdel` = 0;
";
                DataTable m_pDataTable = MySQL_Method.BindTable(m_sSQL);
                if (m_pDataTable != null && m_pDataTable.Rows.Count > 0)
                {
                    string m_sDialUUID = m_pDataTable.Rows[0]["recuuid"].ToString();

                    ///<![CDATA[
                    /// 必须是相同结构的录音名称
                    /// ]]>

                    Regex m_pRegex = new Regex("^[R][e][c][_][0-9]{8}[A-Za-z0-9_]{0,}$");
                    bool m_bIsMatch = m_pRegex.IsMatch(m_sDialUUID);
                    if (!m_bIsMatch) Log.Instance.Fail($"[DB.Basic][m_fDialLimit][m_fGetDialUUID][format fail]");
                    if (m_bIsMatch)
                    {
                        m_dtString = m_sDialUUID.Substring(4, 8);
                        return m_sDialUUID;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_fDialLimit][m_fGetDialUUID][{ex.Message}]");
            }
            return null;
        }

        /// <summary>
        /// 删除录音ID,有可能用不到,最好用着
        /// </summary>
        /// <param name="m_sLoginName"></param>
        public static void m_fDelDialUUID(string m_sLoginName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(m_sLoginName)) return;

                string m_sSQL = $@"
UPDATE `call_dialuuid` 
SET `call_dialuuid`.`isdel` = 1 
WHERE
	`call_dialuuid`.`loginname` = '{m_sLoginName}' 
	AND `call_dialuuid`.`isdel` = 0;
";
                int m_uCount = MySQL_Method.ExecuteNonQuery(m_sSQL);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_fDialLimit][m_fDelDialUUID][{ex.Message}]");
            }
        }
    }
}
