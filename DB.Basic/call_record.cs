using Core_v1;
using DB.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic {
    public class call_record {




        public static bool Insert(call_record_model model, bool m_bShare = false, Model_v1.dial_area m_pDialArea = null, bool m_bCall = false) {
            string sql = "insert into call_record(UniqueID,CallType,ChannelID,LinkChannelID,LocalNum,T_PhoneNum,C_PhoneNum,PhoneAddress,DtmfNum,PhoneTypeID,PhoneListID,PriceTypeID,CallPrice,AgentID,CusID,ContactID,RecordFile,C_Date,C_StartTime,C_RingTime,C_AnswerTime,C_EndTime,C_WaitTime,C_SpeakTime,CallResultID,CallForwordFlag,CallForwordChannelID,SerOp_ID,SerOp_DTMF,SerOp_LeaveRec,Detail,Uhandler,Remark,recordName,isshare,FreeSWITCHIPv4,UAID,fromagentid,fromagentname,fromloginname,tnumber) values (?UniqueID,?CallType,?ChannelID,?LinkChannelID,?LocalNum,?T_PhoneNum,?C_PhoneNum,?PhoneAddress,?DtmfNum,?PhoneTypeID,?PhoneListID,?PriceTypeID,?CallPrice,?AgentID,?CusID,?ContactID,?RecordFile,?C_Date,?C_StartTime,?C_RingTime,?C_AnswerTime,?C_EndTime,?C_WaitTime,?C_SpeakTime,?CallResultID,?CallForwordFlag,?CallForwordChannelID,?SerOp_ID,?SerOp_DTMF,?SerOp_LeaveRec,?Detail,?Uhandler,?Remark,?recordName,?isshare,?FreeSWITCHIPv4,?UAID,?fromagentid,?fromagentname,?fromloginname,?tnumber)";
            MySqlParameter[] parameters = {
     new MySqlParameter("?UniqueID", MySqlDbType.VarChar,36),
     new MySqlParameter("?CallType", MySqlDbType.Int32),
     new MySqlParameter("?ChannelID", MySqlDbType.Int32),
     new MySqlParameter("?LinkChannelID", MySqlDbType.Int32),
     new MySqlParameter("?LocalNum", MySqlDbType.VarChar,50),
     new MySqlParameter("?T_PhoneNum", MySqlDbType.VarChar,50),
     new MySqlParameter("?C_PhoneNum", MySqlDbType.VarChar,50),
     new MySqlParameter("?PhoneAddress", MySqlDbType.VarChar,50),
     new MySqlParameter("?DtmfNum", MySqlDbType.VarChar,50),
     new MySqlParameter("?PhoneTypeID", MySqlDbType.Int32),
     new MySqlParameter("?PhoneListID", MySqlDbType.Int32),
     new MySqlParameter("?PriceTypeID", MySqlDbType.Int32),
     new MySqlParameter("?CallPrice", MySqlDbType.Double),
     new MySqlParameter("?AgentID", MySqlDbType.Int32),
     new MySqlParameter("?CusID", MySqlDbType.Int32),
     new MySqlParameter("?ContactID", MySqlDbType.Int32),
     new MySqlParameter("?RecordFile", MySqlDbType.VarChar,200),
     new MySqlParameter("?C_Date", MySqlDbType.VarChar,10),
     new MySqlParameter("?C_StartTime", MySqlDbType.VarChar,16),
     new MySqlParameter("?C_RingTime", MySqlDbType.VarChar,16),
     new MySqlParameter("?C_AnswerTime", MySqlDbType.VarChar,16),
     new MySqlParameter("?C_EndTime", MySqlDbType.VarChar,16),
     new MySqlParameter("?C_WaitTime", MySqlDbType.Int32),
     new MySqlParameter("?C_SpeakTime", MySqlDbType.Int32),
     new MySqlParameter("?CallResultID", MySqlDbType.Int32),
     new MySqlParameter("?CallForwordFlag", MySqlDbType.Int32),
     new MySqlParameter("?CallForwordChannelID", MySqlDbType.VarChar,50),
     new MySqlParameter("?SerOp_ID", MySqlDbType.Int32),
     new MySqlParameter("?SerOp_DTMF", MySqlDbType.VarChar,10),
     new MySqlParameter("?SerOp_LeaveRec", MySqlDbType.VarChar,50),
     new MySqlParameter("?Detail", MySqlDbType.VarChar,200),
     new MySqlParameter("?Uhandler", MySqlDbType.Int32),
     new MySqlParameter("?Remark", MySqlDbType.VarChar,200),
     new MySqlParameter("?recordName", MySqlDbType.VarChar,255),
     new MySqlParameter("?isshare", MySqlDbType.Int32),
     new MySqlParameter("?FreeSWITCHIPv4", MySqlDbType.VarChar,15),
     new MySqlParameter("?UAID", MySqlDbType.VarChar,200),
     new MySqlParameter("?fromagentid", MySqlDbType.Int32),
     new MySqlParameter("?fromagentname", MySqlDbType.VarChar,50),
     new MySqlParameter("?fromloginname", MySqlDbType.VarChar,50),
     new MySqlParameter("?tnumber", MySqlDbType.VarChar,50)
                };
            parameters[0].Value = model.UniqueID;
            parameters[1].Value = model.CallType;
            parameters[2].Value = model.ChannelID;
            parameters[3].Value = model.LinkChannelID;
            parameters[4].Value = model.LocalNum;
            parameters[5].Value = model.T_PhoneNum;
            parameters[6].Value = model.C_PhoneNum;
            parameters[7].Value = model.PhoneAddress;
            parameters[8].Value = model.DtmfNum;
            parameters[9].Value = model.PhoneTypeID;
            parameters[10].Value = model.PhoneListID;
            parameters[11].Value = model.PriceTypeID;
            parameters[12].Value = model.CallPrice;
            parameters[13].Value = model.AgentID;
            parameters[14].Value = model.CusID;
            parameters[15].Value = model.ContactID;
            parameters[16].Value = model.RecordFile;
            parameters[17].Value = model.C_Date;
            parameters[18].Value = model.C_StartTime;
            parameters[19].Value = model.C_RingTime;
            parameters[20].Value = model.C_AnswerTime;
            parameters[21].Value = model.C_EndTime;
            parameters[22].Value = model.C_WaitTime;
            parameters[23].Value = model.C_SpeakTime;
            parameters[24].Value = model.CallResultID;
            parameters[25].Value = model.CallForwordFlag;
            parameters[26].Value = model.CallForwordChannelID;
            parameters[27].Value = model.SerOp_ID;
            parameters[28].Value = model.SerOp_DTMF;
            parameters[29].Value = model.SerOp_LeaveRec;
            parameters[30].Value = model.Detail;
            parameters[31].Value = model.Uhandler;
            parameters[32].Value = model.Remark;
            parameters[33].Value = model.recordName;
            parameters[34].Value = model.isshare;
            parameters[35].Value = model.FreeSWITCHIPv4;
            parameters[36].Value = model.UAID;
            parameters[37].Value = model.AgentID;
            parameters[38].Value = model.fromagentname;
            parameters[39].Value = model.fromloginname;
            parameters[40].Value = model.tnumber;

            if (m_bShare)
            {
                try
                {
                    ///<![CDATA[
                    /// 保存在主服务器上一份录音记录即可
                    /// ]]>
                    string m_sConnStr = MySQLDBConnectionString.m_fConnStr(m_pDialArea);
                    if (!m_sConnStr.Equals(MySQLDBConnectionString.ConnectionString))
                    {
                        bool m_bSame = false;
                        if (
                            m_pDialArea.aip == MySQLDBConnectionString.DB_Server &&
                            m_pDialArea.adb == MySQLDBConnectionString.DB_Name
                            )
                        {
                            m_bSame = true;
                        }

                        ///<![CDATA[
                        /// 进行参数的调配
                        /// ]]>
                        if (!m_bSame)
                        {
                            if (m_bCall)
                            {
                                parameters[13].Value = model.AgentID;
                            }
                            else
                            {
                                parameters[13].Value = -1;
                            }
                            MySQL_Method.ExecuteNonQuery(sql, parameters, m_sConnStr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Fail($"[DB.Basic][call_record][Insert][Share][Exception][{ex.Message}]");
                }
            }

            if (m_bCall)
            {
                parameters[13].Value = -1;
            }
            else
            {
                parameters[13].Value = model.AgentID;
            }
            return MySQL_Method.ExecuteNonQuery(sql, parameters) > 0;
        }

        public static int Update(string UniqueID, params object[] KeyValues) {
            try {
                StringBuilder sb = new StringBuilder();
                List<MySqlParameter> parameters = new List<MySqlParameter>();
                if(KeyValues.Length % 2 != 0 && KeyValues.Length > 0)
                    throw new Exception("参数需为非零偶数个");
                sb.Append("update call_record set ");
                for(int i = 0; i < KeyValues.Length; i += 2) {
                    sb.Append(KeyValues[i] + "=@" + KeyValues[i] + ",");
                    parameters.Add(new MySqlParameter("@" + KeyValues[i], KeyValues[i + 1]));
                }
                sb.Remove(sb.Length - 1, 1);
                sb.Append(" where UniqueID=@UniqueID");
                parameters.Add(new MySqlParameter("@UniqueID", UniqueID));
                return MySQL_Method.ExecuteNonQuery(sb.ToString(), parameters.ToArray());
            } catch(Exception ex) {
                Log.Instance.Error($"[DB.Basic][call_record][Update][{ex.Message}]");
                return 0;
            }
        }

        public static void m_fGetShareFromName(int m_uAgentID, string m_sConnStr, out string m_sFromAgentName, out string m_sFromLoginName)
        {
            m_sFromAgentName = null;
            m_sFromLoginName = null;
            try
            {
                if (string.IsNullOrWhiteSpace(m_sConnStr))
                    return;

                string m_sSQL = $@"
SELECT
	`call_agent`.`AgentName`,
	`call_agent`.`LoginName` 
FROM
	`call_agent` 
WHERE
	`call_agent`.`ID` = {m_uAgentID} 
	LIMIT 1;
";
                DataTable m_pDataTable = MySQL_Method.BindTable(m_sSQL, null, m_sConnStr);
                if (m_pDataTable != null && m_pDataTable.Rows.Count > 0)
                {
                    m_sFromAgentName = m_pDataTable.Rows[0]["AgentName"]?.ToString();
                    m_sFromLoginName = m_pDataTable.Rows[0]["LoginName"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][call_record][m_fGetShareFromName][Exception][{ex.Message}]");
            }
        }
    }
}
