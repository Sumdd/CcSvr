using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DB.Model;
using System.Data;
using MySql.Data.MySqlClient;

namespace DB.Basic {
    public class Call_ParamUtil {
        public Call_ParamUtil() {
        }

        public static bool Update(string P_Name, string P_Value) {
            try {
                if(MySQL_Method.ExecuteNonQuery("update Call_Param set P_Value='" + P_Value + "' where P_Name='" + P_Name + "'") > 0)
                    return true;
                return false;
            } catch {
                return false;
            }
        }

        public static bool Insert(string P_Name, string P_Value, string P_Description = null, string P_Group = null) {
            string ExecuteCmd = string.Empty;

            DataTable dt = MySQL_Method.BindTable("select id from call_param where P_Name='" + P_Name + "'");
            if(dt.Rows.Count > 0)
                ExecuteCmd = string.Format("update Call_Param set P_Name='{0}',P_Value='{1}',P_Description='{2}',P_Group='{3}'  where P_Name='{0}'", P_Name, P_Value, P_Description, P_Group);

            ExecuteCmd = string.Format("insert into Call_Param (P_Name,P_Value,P_Description,P_Group) values ('{0}','{1}','{2}','{3}')", P_Name, P_Value, P_Description, P_Group);
            if(MySQL_Method.ExecuteNonQuery(ExecuteCmd) > 0)
                return true;
            return false;
        }

        public static call_param_model GetModel(string P_Name) {
            var model = new call_param_model();
            string sql = "select ID,P_Name,P_Value,P_Description,CreateTime,LoseTime,P_Group,Remark from call_param where P_Name=?P_Name limit 1";
            MySqlParameter[] parameters = {
     new MySqlParameter("?P_Name", MySqlDbType.VarChar,50)
                };
            parameters[0].Value = P_Name;
            using(var dr = MySQL_Method.ExecuteDataReader(sql, parameters)) {
                if(dr.Read()) {
                    model.ID = int.Parse(dr["ID"].ToString());
                    model.P_Name = dr["P_Name"].ToString();
                    model.P_Value = dr["P_Value"].ToString();
                    model.P_Description = dr["P_Description"].ToString();
                    model.CreateTime = DateTime.Parse(dr["CreateTime"].ToString());
                    model.LoseTime = DateTime.Parse(dr["LoseTime"].ToString());
                    model.P_Group = dr["P_Group"].ToString();
                    model.Remark = dr["Remark"].ToString();
                }
            }
            return model;
        }

        public static call_param_model GetModel(int ID) {
            var model = new call_param_model();
            string sql = "select ID,P_Name,P_Value,P_Description,CreateTime,LoseTime,P_Group,Remark from call_param where ID=?ID limit 1";
            MySqlParameter[] parameters = {
                new MySqlParameter("?ID", MySqlDbType.Int32)
                };
            parameters[0].Value = ID;
            using(var dr = MySQL_Method.ExecuteDataReader(sql, parameters)) {
                if(dr.Read()) {
                    model.ID = int.Parse(dr["ID"].ToString());
                    model.P_Name = dr["P_Name"].ToString();
                    model.P_Value = dr["P_Value"].ToString();
                    model.P_Description = dr["P_Description"].ToString();
                    model.CreateTime = DateTime.Parse(dr["CreateTime"].ToString());
                    model.LoseTime = DateTime.Parse(dr["LoseTime"].ToString());
                    model.P_Group = dr["P_Group"].ToString();
                    model.Remark = dr["Remark"].ToString();
                }
            }
            return model;
        }

        public static IList<call_param_model> GetList(string P_Group, int top = 10) {
            IList<call_param_model> list = new List<call_param_model>();
            string sql = "select ID,P_Name,P_Value,P_Description,CreateTime,LoseTime,P_Group,Remark from call_param where P_Group=?P_Group limit ?top";
            MySqlParameter[] parameters = {
                new MySqlParameter("?P_Group", MySqlDbType.VarChar,50),
                new MySqlParameter("?top", MySqlDbType.Int32)
                };
            parameters[0].Value = P_Group;
            parameters[1].Value = top;
            using(var dr = MySQL_Method.ExecuteDataReader(sql, parameters)) {
                while(dr.Read()) {
                    list.Add(new call_param_model() {
                        ID = int.Parse(dr["ID"].ToString()),
                        P_Name = dr["P_Name"].ToString(),
                        P_Value = dr["P_Value"].ToString(),
                        P_Description = dr["P_Description"].ToString(),
                        CreateTime = DateTime.Parse(dr["CreateTime"].ToString()),
                        LoseTime = DateTime.Parse(dr["LoseTime"].ToString()),
                        P_Group = dr["P_Group"].ToString(),
                        Remark = dr["Remark"].ToString()
                    });
                }
            }
            return list;
        }

        public static string GetParamValueByName(string P_Name, string defaultValue = "", string m_sConnStr = "") {
            try {
                string SqlStr = "select P_Value from Call_Param where P_Name='" + P_Name + "'";
                DataTable dt = MySQL_Method.BindTable(SqlStr, null, m_sConnStr);
                if(dt.Rows.Count > 0) {
                    return dt.Rows[0]["P_Value"].ToString();
                } else {
                    return defaultValue;
                }
            } catch {
                return defaultValue;
            }
        }

        #region 测试内联模式
        private static bool? _InboundTest;
        public static bool InboundTest {
            get {
                if(_InboundTest != null) {
                    return Convert.ToBoolean(_InboundTest);
                }
                var _value = GetParamValueByName("InboundTest");
                if(!string.IsNullOrWhiteSpace(_value)) {
                    _InboundTest = (_value == "1" ? true : false);
                } else {
                    _InboundTest = false;
                }
                return Convert.ToBoolean(_InboundTest);
            }
            set {
                Update("InboundTest", value ? "1" : "0");
                _InboundTest = value;
            }
        }
        #endregion
        #region 程序
        private static string __application;
        public static string _application {
            get {
                if(!string.IsNullOrWhiteSpace(__application)) {
                    return __application;
                }
                var _value = GetParamValueByName("_application");
                if(!string.IsNullOrWhiteSpace(_value)) {
                    __application = _value;
                } else {
                    __application = "park";
                }
                return __application;
            }
            set {
                Update("_application", value);
                __application = value;
            }
        }
        #endregion
        #region 忽略早期媒体
        private static bool? ___ignore_early_media;
        public static bool __ignore_early_media {
            get {
                if(___ignore_early_media != null) {
                    return Convert.ToBoolean(___ignore_early_media);
                }
                var _value = GetParamValueByName("__ignore_early_media");
                if(!string.IsNullOrWhiteSpace(_value)) {
                    ___ignore_early_media = Convert.ToBoolean(_value);
                } else {
                    ___ignore_early_media = false;
                }
                return Convert.ToBoolean(___ignore_early_media);
            }
            set {
                Update("__ignore_early_media", value.ToString());
                ___ignore_early_media = value;
            }
        }
        #endregion
        #region 通用的超时时间
        private static int? ___timeout_seconds;
        public static int __timeout_seconds {
            get {
                try {
                    if(___timeout_seconds != null) {
                        return Convert.ToInt32(___timeout_seconds);
                    }
                    var _value = GetParamValueByName("__timeout_seconds");
                    if(!string.IsNullOrWhiteSpace(_value)) {
                        ___timeout_seconds = Convert.ToInt32(_value);
                    } else {
                        ___timeout_seconds = 60;
                    }
                    return Convert.ToInt32(___timeout_seconds);
                } catch {
                    return 60;
                }
            }
            set {
                Update("__timeout_seconds", value.ToString());
                ___timeout_seconds = value;
            }
        }
        #endregion
        #region 拨号处理规则
        private static string _IEM_Do;
        public static string IEM_Do {
            get {
                try {
                    if(string.IsNullOrWhiteSpace(_IEM_Do)) {
                        _IEM_Do = GetParamValueByName("IEM_Do");
                    }
                    return _IEM_Do;
                } catch {
                    return "1";
                }
            }
            set {
                Update("IEM_Do", value.ToString());
                _IEM_Do = value;
            }
        }
        #endregion
        #region 拨号号码简易处理
        private static string _DialDealMethod;
        [Obsolete("该变量尽量不再使用,统一处理所有电话,如果有多个网关,这里就需要判断了,该字段就不行了")]
        public static string DialDealMethod {
            get {
                if(string.IsNullOrWhiteSpace(_DialDealMethod))
                    _DialDealMethod = Call_ParamUtil.GetParamValueByName("DialDealMethod", "no");
                return _DialDealMethod;
            }
            set {
                Call_ParamUtil.Update("DialDealMethod", value);
                _DialDealMethod = value;
            }
        }
        #endregion
        #region 录音文件名称,这里主要是为了通话质检
        private static string __rec_t;
        public static string _rec_t {
            get {
                if(string.IsNullOrWhiteSpace(__rec_t)) {
                    __rec_t = GetParamValueByName("rec_t");
                }
                return __rec_t;
            }
            set {
                Update("rec_t", value);
                __rec_t = value;
            }
        }
        #endregion
        #region 是否多号码
        private static bool? _IsMultiPhone;
        public static bool IsMultiPhone {
            get {
                if(_IsMultiPhone != null) {
                    return Convert.ToBoolean(_IsMultiPhone);
                }
                var _value = GetParamValueByName("IsMultiPhone");
                if(!string.IsNullOrWhiteSpace(_value)) {
                    _IsMultiPhone = Convert.ToBoolean(_value);
                } else {
                    _IsMultiPhone = false;
                }
                return Convert.ToBoolean(_IsMultiPhone);
            }
            set {
                Update("IsMultiPhone", value.ToString());
                _IsMultiPhone = value;
            }
        }
        #endregion
        #region 是否联网
        private static bool? _IsLinkNet;
        public static bool IsLinkNet
        {
            get
            {
                if (_IsLinkNet != null)
                {
                    return Convert.ToBoolean(_IsLinkNet);
                }
                var _value = GetParamValueByName("IsLinkNet");
                if (!string.IsNullOrWhiteSpace(_value))
                {
                    _IsLinkNet = Convert.ToBoolean(_value);
                }
                else
                {
                    _IsLinkNet = false;
                }
                return Convert.ToBoolean(_IsLinkNet);
            }
            set
            {
                Update("IsLinkNet", value.ToString());
                _IsLinkNet = value;
            }
        }
        #endregion
        #region 呼叫主叫超时时间
        private static int? _ALegTimeoutSeconds;
        public static int ALegTimeoutSeconds
        {
            get
            {
                try
                {
                    if (_ALegTimeoutSeconds == null)
                    {
                        _ALegTimeoutSeconds = Convert.ToInt32(GetParamValueByName("ALegTimeoutSeconds"));
                    }
                    return Convert.ToInt32(_ALegTimeoutSeconds);
                }
                catch
                {
                    return 15;
                }
            }
            set
            {
                Update("ALegTimeoutSeconds", value.ToString());
                _ALegTimeoutSeconds = value;
            }
        }
        #endregion
        #region 自动拨号文件播放次数
        private static int? _m_uPlayLoops;
        public static int m_uPlayLoops
        {
            get
            {
                try
                {
                    if (_m_uPlayLoops == null)
                    {
                        _m_uPlayLoops = Convert.ToInt32(Call_ParamUtil.GetParamValueByName("DialTaskPlayLoops"));
                    }
                    return Convert.ToInt32(_m_uPlayLoops);
                }
                catch
                {
                    return 1;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskPlayLoops", value.ToString());
                    _m_uPlayLoops = value;
                }
                catch { }
            }
        }
        #endregion
        #region 自动拨号TTS路径
        public static string _m_sTTSUrl = null;
        public static string m_sTTSUrl
        {
            get
            {
                try
                {
                    if (_m_sTTSUrl == null)
                    {
                        Call_ParamUtil._m_sTTSUrl = Call_ParamUtil.GetParamValueByName("DialTaskTTSUrl");
                    }
                    return _m_sTTSUrl == null ? _m_sTTSUrl = "" : _m_sTTSUrl;
                }
                catch
                {
                    return _m_sTTSUrl = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskTTSUrl", value);
                    _m_sTTSUrl = value;
                }
                catch { }
            }
        }
        #endregion
        #region 是否使用自动拨号上报接口
        private static bool? _m_bUseDialTaskInterface;
        public static bool m_bUseDialTaskInterface
        {
            get
            {
                try
                {
                    if (_m_bUseDialTaskInterface == null)
                    {
                        _m_bUseDialTaskInterface = Call_ParamUtil.GetParamValueByName("UseDialTaskInterface") == "1";
                    }
                    return Convert.ToBoolean(_m_bUseDialTaskInterface);
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("UseDialTaskInterface", value ? "1" : "0");
                    _m_bUseDialTaskInterface = value;
                }
                catch { }
            }
        }
        #endregion
        #region 自动拨号上报接口
        public static string _m_sUpInterface = null;
        public static string m_sUpInterface
        {
            get
            {
                try
                {
                    if (_m_sUpInterface == null)
                    {
                        Call_ParamUtil._m_sUpInterface = Call_ParamUtil.GetParamValueByName("DialTaskInterface");
                    }
                    return _m_sUpInterface == null ? _m_sUpInterface = "" : _m_sUpInterface;
                }
                catch
                {
                    return _m_sUpInterface = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskInterface", value);
                    _m_sUpInterface = value;
                }
                catch { }
            }
        }
        #endregion
        #region 是否启动自动拨号中的语音识别功能
        private static bool? _m_bIsDialTaskAsr;
        public static bool m_bIsDialTaskAsr
        {
            get
            {
                try
                {
                    if (_m_bIsDialTaskAsr == null)
                        _m_bIsDialTaskAsr = Call_ParamUtil.GetParamValueByName("IsDialTaskAsr") == "1";
                    return Convert.ToBoolean(_m_bIsDialTaskAsr);
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                Call_ParamUtil.Update("IsDialTaskAsr", value ? "1" : "0");
                _m_bIsDialTaskAsr = value;
            }
        }
        #endregion
        #region 自动拨号未接通的拨打次数
        private static int? _m_uDialCount;
        public static int m_uDialCount
        {
            get
            {
                try
                {
                    if (_m_uDialCount == null)
                    {
                        _m_uDialCount = Convert.ToInt32(Call_ParamUtil.GetParamValueByName("DialTaskDialCount"));
                    }
                    return Convert.ToInt32(_m_uDialCount);
                }
                catch
                {
                    return 1;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskDialCount", value.ToString());
                    _m_uDialCount = value;
                }
                catch { }
            }
        }
        #endregion
        #region 自动拨号录音路径分离配置
        public static string _m_sDialTaskRecPath;
        public static string m_sDialTaskRecPath
        {
            get
            {
                try
                {
                    if (_m_sDialTaskRecPath == null)
                    {
                        Call_ParamUtil._m_sDialTaskRecPath = Call_ParamUtil.GetParamValueByName("DialTaskRecPath");
                    }
                    return _m_sDialTaskRecPath;
                }
                catch
                {
                    return _m_sDialTaskRecPath = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskRecPath", value);
                    _m_sDialTaskRecPath = value;
                }
                catch { }
            }
        }
        #endregion
        #region 录音下载HTTP模式
        public static string _m_sDialTaskRecDownLoadHTTP;
        public static string m_sDialTaskRecDownLoadHTTP
        {
            get
            {
                try
                {
                    if (_m_sDialTaskRecDownLoadHTTP == null)
                    {
                        Call_ParamUtil._m_sDialTaskRecDownLoadHTTP = Call_ParamUtil.GetParamValueByName("DialTaskRecDownLoadHTTP");
                    }
                    return _m_sDialTaskRecDownLoadHTTP;
                }
                catch
                {
                    return _m_sDialTaskRecDownLoadHTTP = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskRecDownLoadHTTP", value);
                    _m_sDialTaskRecDownLoadHTTP = value;
                }
                catch { }
            }
        }
        #endregion
        #region 自动外呼app(为了发送一次rtp流)
        public static string _m_sDialTaskApp;
        public static string m_sDialTaskApp
        {
            get
            {
                try
                {
                    if (_m_sDialTaskApp == null)
                    {
                        Call_ParamUtil._m_sDialTaskApp = Call_ParamUtil.GetParamValueByName("DialTaskApp");
                    }
                    return _m_sDialTaskApp;
                }
                catch
                {
                    return _m_sDialTaskApp = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskApp", value);
                    _m_sDialTaskApp = value;
                }
                catch { }
            }
        }
        #endregion
        #region 自动外呼Tts提供方(兼容联信)
        public static string _m_sDialTaskTTSProvider;
        public static string m_sDialTaskTTSProvider
        {
            get
            {
                try
                {
                    if (_m_sDialTaskTTSProvider == null)
                    {
                        Call_ParamUtil._m_sDialTaskTTSProvider = Call_ParamUtil.GetParamValueByName("DialTaskTTSProvider");
                    }
                    return _m_sDialTaskTTSProvider;
                }
                catch
                {
                    return _m_sDialTaskTTSProvider = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskTTSProvider", value);
                    _m_sDialTaskTTSProvider = value;
                }
                catch { }
            }
        }
        #endregion
        #region 自动外呼Tts配置
        public static int? m_iDialTaskTTSSetting;
        public static string _m_sDialTaskTTSSetting;
        public static string m_sDialTaskTTSSetting
        {
            get
            {
                try
                {
                    if (_m_sDialTaskTTSSetting == null)
                    {
                        Call_ParamUtil._m_sDialTaskTTSSetting = Call_ParamUtil.GetParamValueByName("DialTaskTTSSetting");
                        if (!string.IsNullOrWhiteSpace(Call_ParamUtil._m_sDialTaskTTSSetting))
                        {
                            int _m_iSpeed = 0;
                            if (int.TryParse(Call_ParamUtil._m_sDialTaskTTSSetting, out _m_iSpeed))
                            {
                                m_iDialTaskTTSSetting = Convert.ToInt32(_m_iSpeed);
                            }
                        }
                    }
                    return _m_sDialTaskTTSSetting;
                }
                catch
                {
                    return _m_sDialTaskTTSSetting = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("DialTaskTTSSetting", value);
                    _m_sDialTaskTTSSetting = value;
                }
                catch { }
            }
        }
        #endregion
        #region 网页电话需要安全连接
        public static string _m_sWebWebSocketS;
        public static string m_sWebWebSocketS
        {
            get
            {
                try
                {
                    if (_m_sWebWebSocketS == null)
                    {
                        Call_ParamUtil._m_sWebWebSocketS = Call_ParamUtil.GetParamValueByName("_m_sWebWebSocketS".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sWebWebSocketS;
                }
                catch
                {
                    return _m_sWebWebSocketS = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sWebWebSocketS".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sWebWebSocketS = value;
                }
                catch { }
            }
        }
        #endregion
        #region 备份录音路径
        public static string _m_sBackupRecords;
        public static string m_sBackupRecords
        {
            get
            {
                try
                {
                    if (_m_sBackupRecords == null)
                    {
                        Call_ParamUtil._m_sBackupRecords = Call_ParamUtil.GetParamValueByName("_m_sBackupRecords".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sBackupRecords;
                }
                catch
                {
                    return _m_sBackupRecords = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sBackupRecords".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sBackupRecords = value;
                }
                catch { }
            }
        }
        #endregion
        #region 来电无法接听原因播报次数
        private static int? _m_uCasePlayLoops;
        public static int m_uCasePlayLoops
        {
            get
            {
                try
                {
                    if (_m_uCasePlayLoops == null)
                    {
                        Call_ParamUtil._m_uCasePlayLoops = Convert.ToInt32(Call_ParamUtil.GetParamValueByName("_m_uCasePlayLoops".Replace("_m_u", "").Replace("m_u", "")));
                    }
                    return Convert.ToInt32(_m_uCasePlayLoops);
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_uCasePlayLoops".Replace("_m_u", "").Replace("m_u", ""), value.ToString());
                    _m_uCasePlayLoops = value;
                }
                catch { }
            }
        }
        #endregion
        #region 来电无法接听原因应答方式
        private static string _m_sCaseAnswer;
        public static string m_sCaseAnswer
        {
            get
            {
                string m_sValue = "uuid_pre_answer";
                try
                {
                    if (_m_sCaseAnswer == null)
                    {
                        Call_ParamUtil._m_sCaseAnswer = Call_ParamUtil.GetParamValueByName("_m_sCaseAnswer".Replace("_m_s", "").Replace("m_s", ""), m_sValue);
                    }
                    return _m_sCaseAnswer;
                }
                catch
                {
                    return _m_sCaseAnswer = m_sValue;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sCaseAnswer".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sCaseAnswer = value;
                }
                catch { }
            }
        }
        #endregion
        #region dtmf发送方式

        public const string inbound = "inbound";
        //public const string rfc2833 = "rfc2833";
        public const string clientSignal = "clientSignal";
        public const string bothSignal = "bothSignal";

        private static string _m_sDTMFSendMethod;
        public static string m_sDTMFSendMethod
        {
            get
            {
                string m_sValue = clientSignal;

                ///<![CDATA[
                /// 暂定3种
                /// 1.inbound
                /// 2.clientSignal
                /// 3.bothSignal
                /// ]]>

                try
                {
                    if (_m_sDTMFSendMethod == null)
                    {
                        Call_ParamUtil._m_sDTMFSendMethod = Call_ParamUtil.GetParamValueByName("_m_sDTMFSendMethod".Replace("_m_s", "").Replace("m_s", ""), m_sValue);
                    }
                    return _m_sDTMFSendMethod;
                }
                catch
                {
                    return _m_sDTMFSendMethod = m_sValue;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sDTMFSendMethod".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sDTMFSendMethod = value;
                }
                catch { }
            }
        }
        #endregion
        #region 登陆方式
        public static string _m_sLoginType;
        public static string m_sLoginType
        {
            get
            {
                try
                {
                    if (_m_sLoginType == null)
                    {
                        Call_ParamUtil._m_sLoginType = Call_ParamUtil.GetParamValueByName("_m_sLoginType".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sLoginType;
                }
                catch
                {
                    return _m_sLoginType = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sLoginType".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sLoginType = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***是否启用HasRedis
        public static bool? _m_bIsHasRedis;
        public static bool m_bIsHasRedis
        {
            get
            {
                try
                {
                    if (_m_bIsHasRedis == null)
                    {
                        _m_bIsHasRedis = Call_ParamUtil.GetParamValueByName("IsHasRedis") == "1";
                    }
                    return Convert.ToBoolean(_m_bIsHasRedis);
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("IsHasRedis", value ? "1" : "0");
                    _m_bIsHasRedis = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***共享号码配置:0.无;1.主录音服务器;2.副录音服务器;
        public static int? _m_uShareNumSetting;
        public static int m_uShareNumSetting
        {
            get
            {
                try
                {
                    if (_m_uShareNumSetting == null)
                    {
                        Call_ParamUtil._m_uShareNumSetting = Convert.ToInt32(Call_ParamUtil.GetParamValueByName("_m_uShareNumSetting".Replace("_m_u", "").Replace("m_u", "")));
                    }
                    return Convert.ToInt32(_m_uShareNumSetting);
                }
                catch
                {
                    return Convert.ToInt32(_m_uShareNumSetting = 0);
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_uShareNumSetting".Replace("_m_u", "").Replace("m_u", ""), value.ToString());
                    _m_uShareNumSetting = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***共享文件夹HTTP
        private static bool? _m_bIsUseHttpShare;
        public static bool m_bIsUseHttpShare
        {
            get
            {
                try
                {
                    if (_m_bIsUseHttpShare == null)
                    {
                        _m_bIsUseHttpShare = Call_ParamUtil.GetParamValueByName("m_bIsUseHttpShare".Replace("_m_b", "").Replace("m_b", "")) == "1";
                    }
                    return Convert.ToBoolean(_m_bIsUseHttpShare);
                }
                catch (Exception ex)
                {
                    Core_v1.Log.Instance.Error($"[DB.Basic][Call_ParamUtil][m_bIsUseHttpShare][get][Exception][{ex.Message}]");
                    return false;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("m_bIsUseHttpShare".Replace("_m_b", "").Replace("m_b", ""), value ? "1" : "0");
                    _m_bIsUseHttpShare = value;
                }
                catch (Exception ex)
                {
                    Core_v1.Log.Instance.Error($"[DB.Basic][Call_ParamUtil][m_bIsUseHttpShare][set][Exception][{ex.Message}]");
                }
            }
        }
        #endregion
        #region ***共享文件夹HTTP路径
        public static string _m_sHttpShareUrl;
        public static string m_sHttpShareUrl
        {
            get
            {
                try
                {
                    if (_m_sHttpShareUrl == null)
                    {
                        _m_sHttpShareUrl = Call_ParamUtil.GetParamValueByName("_m_sHttpShareUrl".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sHttpShareUrl;
                }
                catch (Exception ex)
                {
                    Core_v1.Log.Instance.Error($"[DB.Basic][Call_ParamUtil][m_sHttpShareUrl][get][Exception][{ex.Message}]");
                    return string.Empty;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sHttpShareUrl".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sHttpShareUrl = value;
                }
                catch (Exception ex)
                {
                    Core_v1.Log.Instance.Error($"[DB.Basic][Call_ParamUtil][m_sHttpShareUrl][set][Exception][{ex.Message}]");
                }
            }
        }
        #endregion
        #region ***自动更新号码池信息时间;0.不自动更新;时间不能小于60秒;单位秒
        public static int? _m_uAutoUpdateShareSeconds;
        public static int m_uAutoUpdateShareSeconds
        {
            get
            {
                try
                {
                    if (_m_uAutoUpdateShareSeconds == null)
                    {
                        Call_ParamUtil._m_uAutoUpdateShareSeconds = Convert.ToInt32(Call_ParamUtil.GetParamValueByName("_m_uAutoUpdateShareSeconds".Replace("_m_u", "").Replace("m_u", "")));
                    }
                    return Convert.ToInt32(_m_uAutoUpdateShareSeconds);
                }
                catch
                {
                    return Convert.ToInt32(_m_uAutoUpdateShareSeconds = 0);
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_uAutoUpdateShareSeconds".Replace("_m_u", "").Replace("m_u", ""), value.ToString());
                    _m_uAutoUpdateShareSeconds = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***IP话机外显归属地
        private static bool? _m_bIsIpShowWhere;
        public static bool m_bIsIpShowWhere
        {
            get
            {
                try
                {
                    if (_m_bIsIpShowWhere == null)
                    {
                        _m_bIsIpShowWhere = Call_ParamUtil.GetParamValueByName("IsIpShowWhere") == "1";
                    }
                    return Convert.ToBoolean(_m_bIsIpShowWhere);
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("IsIpShowWhere", value ? "1" : "0");
                    _m_bIsIpShowWhere = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***是否查询录音ID
        private static bool? _m_bIsQueryRecUUID;
        public static bool m_bIsQueryRecUUID
        {
            get
            {
                try
                {
                    if (_m_bIsQueryRecUUID == null)
                    {
                        _m_bIsQueryRecUUID = Call_ParamUtil.GetParamValueByName("IsQueryRecUUID") == "1";
                    }
                    return Convert.ToBoolean(_m_bIsQueryRecUUID);
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("IsQueryRecUUID", value ? "1" : "0");
                    _m_bIsQueryRecUUID = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***呼入路由规则
        public static int? _m_uCallRule;
        public static int m_uCallRule
        {
            get
            {
                try
                {
                    if (_m_uCallRule == null)
                    {
                        Call_ParamUtil._m_uCallRule = Convert.ToInt32(Call_ParamUtil.GetParamValueByName("_m_uCallRule".Replace("_m_u", "").Replace("m_u", "")));
                    }
                    if (Call_ParamUtil._m_uCallRule != null)
                    {
                        int CallRule = Convert.ToInt32(Call_ParamUtil._m_uCallRule);
                        if (CallRule >= 1 && CallRule <= 3)
                        {
                            return Convert.ToInt32(CallRule);
                        }
                    }
                    throw new Exception("代数和仅可为1,2,3");
                }
                catch (Exception ex)
                {
                    Core_v1.Log.Instance.Error($"[DB.Basic][Call_ParamUtil][m_uCallRule][Exception][{ex.Message}]");
                    return Convert.ToInt32(_m_uCallRule = 3);
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_uCallRule".Replace("_m_u", "").Replace("m_u", ""), value.ToString());
                    _m_uCallRule = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***呼入回铃
        public static string _m_sCallMusic;
        public static string m_sCallMusic
        {
            get
            {
                try
                {
                    if (_m_sCallMusic == null)
                    {
                        Call_ParamUtil._m_sCallMusic = Call_ParamUtil.GetParamValueByName("_m_sCallMusic".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sCallMusic;
                }
                catch
                {
                    return _m_sCallMusic = "C:/Program Files/FreeSWITCH/conf/sounds/calling.wav";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sCallMusic".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sCallMusic = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***FreeSwitch所在目录
        public static string _m_sFreeSWITCHPath;
        public static string m_sFreeSWITCHPath
        {
            get
            {
                try
                {
                    if (_m_sFreeSWITCHPath == null)
                    {
                        Call_ParamUtil._m_sFreeSWITCHPath = Call_ParamUtil.GetParamValueByName("_m_sFreeSWITCHPath".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sFreeSWITCHPath;
                }
                catch
                {
                    return _m_sFreeSWITCHPath = "C:/Program Files/FreeSWITCH";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sFreeSWITCHPath".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sFreeSWITCHPath = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***FreeSwitch网关写入Ua文件夹名
        public static string _m_sFreeSWITCHUaPath;
        public static string m_sFreeSWITCHUaPath
        {
            get
            {
                try
                {
                    if (_m_sFreeSWITCHUaPath == null)
                    {
                        Call_ParamUtil._m_sFreeSWITCHUaPath = Call_ParamUtil.GetParamValueByName("_m_sFreeSWITCHUaPath".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sFreeSWITCHUaPath;
                }
                catch
                {
                    return _m_sFreeSWITCHUaPath = "external";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sFreeSWITCHUaPath".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sFreeSWITCHUaPath = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***转码后最终扩展名
        public static string _m_sEndExt;
        public static string m_sEndExt
        {
            get
            {
                try
                {
                    if (_m_sEndExt == null)
                    {
                        Call_ParamUtil._m_sEndExt = Call_ParamUtil.GetParamValueByName("_m_sEndExt".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sEndExt;
                }
                catch
                {
                    return _m_sEndExt = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sEndExt".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sEndExt = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***归属地表自动更新间隔参数
        public static int? _m_uTaskUpdPhoneInterval;
        public static int m_uTaskUpdPhoneInterval
        {
            get
            {
                try
                {
                    if (_m_uTaskUpdPhoneInterval == null)
                    {
                        Call_ParamUtil._m_uTaskUpdPhoneInterval = Convert.ToInt32(Call_ParamUtil.GetParamValueByName("_m_uTaskUpdPhoneInterval".Replace("_m_u", "").Replace("m_u", "")));
                    }
                    return _m_uTaskUpdPhoneInterval.Value;
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_uTaskUpdPhoneInterval".Replace("_m_u", "").Replace("m_u", ""), value.ToString());
                    _m_uTaskUpdPhoneInterval = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***归属地表自动更新HTTP路径
        public static string _m_sTaskUpdPhoneURL;
        public static string m_sTaskUpdPhoneURL
        {
            get
            {
                try
                {
                    if (_m_sTaskUpdPhoneURL == null)
                    {
                        Call_ParamUtil._m_sTaskUpdPhoneURL = Call_ParamUtil.GetParamValueByName("_m_sTaskUpdPhoneURL".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sTaskUpdPhoneURL;
                }
                catch
                {
                    return _m_sTaskUpdPhoneURL = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sTaskUpdPhoneURL".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sTaskUpdPhoneURL = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***新生代续联HTTP接口
        public static string _m_sXxHttp;
        public static string m_sXxHttp
        {
            get
            {
                try
                {
                    if (_m_sXxHttp == null)
                    {
                        Call_ParamUtil._m_sXxHttp = Call_ParamUtil.GetParamValueByName("_m_sXxHttp".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sXxHttp;
                }
                catch
                {
                    return _m_sXxHttp = "";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sXxHttp".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sXxHttp = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***是否开启追加独立服务中的共享号码,申请式
        public static bool? _m_bUseApply;
        public static bool m_bUseApply
        {
            get
            {
                try
                {
                    if (_m_bUseApply == null)
                    {
                        _m_bUseApply = Call_ParamUtil.GetParamValueByName("UseApply") == "1";
                    }
                    return Convert.ToBoolean(_m_bUseApply);
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("UseApply", value ? "1" : "0");
                    _m_bUseApply = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***独立申请式的出局Ua
        public static string _m_sApiUa;
        public static string m_sApiUa
        {
            get
            {
                try
                {
                    if (_m_sApiUa == null)
                    {
                        Call_ParamUtil._m_sApiUa = Call_ParamUtil.GetParamValueByName("_m_sApiUa".Replace("_m_s", "").Replace("m_s", ""));
                    }
                    return _m_sApiUa;
                }
                catch
                {
                    return _m_sApiUa = "external";
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_sApiUa".Replace("_m_s", "").Replace("m_s", ""), value);
                    _m_sApiUa = value;
                }
                catch { }
            }
        }
        #endregion
        #region ***To拼接至主叫名称
        public static int? _m_uAppendTo;
        public static int m_uAppendTo
        {
            get
            {
                try
                {
                    if (_m_uAppendTo == null)
                    {
                        Call_ParamUtil._m_uAppendTo = Convert.ToInt32(Call_ParamUtil.GetParamValueByName("_m_uAppendTo".Replace("_m_u", "").Replace("m_u", "")));
                    }
                    return Call_ParamUtil._m_uAppendTo.Value;
                }
                catch
                {
                    return 2;
                }
            }
            set
            {
                try
                {
                    Call_ParamUtil.Update("_m_uAppendTo".Replace("_m_u", "").Replace("m_u", ""), value.ToString());
                    _m_uAppendTo = value;
                }
                catch { }
            }
        }
        #endregion
    }
}
