using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Model_v1;
using Newtonsoft.Json;

namespace Core_v1
{
    public class Redis2
    {
        public static List<dial_area> dialarea_list;
        public static List<share_number> sharenum_list;
        public static dial_area m_EsyDialArea;
        public static dial_area m_EsyMainDialArea;
        public static int m_uShareNumSetting = 0;

        public static bool use = false;
        public static bool reUse = false;
        public static string host = "127.0.0.1";
        public const string defaultHost = "127.0.0.1";
        public static int port = 6379;
        public const int defaultPort = 6379;
        public static string password = null;
        public static int db = 15;
        public const int defaultDb = 15;

        /// <summary>
        /// 更新锁
        /// </summary>
        private static object m_oUpdateLock = new object();
        /// <summary>
        /// 立即更新锁标志
        /// </summary>
        public const string m_sUpdateNow = "UPDATE-NOW";
        /// <summary>
        /// 结束更新锁标志
        /// </summary>
        public const string m_sUpdateUseEnded = "UPDATE-USE-ENDED";
        /// <summary>
        /// 共享号码JSON单条
        /// </summary>
        public const string m_sJSONPrefix = "SHARE-JSON-SIMPLE-DATA";
        /// <summary>
        /// 结束更新数据缓存
        /// </summary>
        public const string m_sUPDATEPrefix = "SHARE-JSON-SIMPLE-UPDATE";
        /// <summary>
        /// 共享号码JSON单条锁
        /// </summary>
        public const string m_sLockPrefix = "SHARE-JSON-SIMPLE-LOCK";
        /// <summary>
        /// 号码域名称
        /// </summary>
        public const string m_sDialAreaName = "SHARE-DIAL-AREA";

        #region ***Redis单例模式
        private static RedisClient m_pRedisClient;

        public static RedisClient Instance
        {
            get
            {
                try
                {
                    if (reUse)
                    {
                        m_pRedisClient = new RedisClient(host, port, password, db);
                        reUse = false;
                    }
                    else if (m_pRedisClient == null)
                    {
                        m_pRedisClient = new RedisClient(host, port, password, db);
                    }
                    return m_pRedisClient;
                }
                catch (Exception ex)
                {
                    Log.Instance.Fail($"[Core_v1][Redis2][Instance][get][{ex.Message}]");
                }
                return null;
            }
        }
        #endregion

        #region ***域载入Redis中
        /// <summary>
        /// 域载入Redis中
        /// </summary>
        public static void m_fSetDialArea()
        {
            try
            {
                if (!Redis2.use)
                {
                    Log.Instance.Warn($"[Core_v1][Redis2][m_fSetDialArea][not use redis]");
                    return;
                }
                //共享域保存字符串,便于查看内容
                Redis2.Instance.Set(Redis2.m_sDialAreaName, JsonConvert.SerializeObject(dialarea_list), DateTime.MaxValue);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[Core_v1][Redis2][m_fSetDialArea][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region ***Redis加载共享号码
        public static void m_fSetShareNum()
        {
            try
            {
                if (!Redis2.use)
                {
                    Log.Instance.Warn($"[Core_v1][Redis2][m_fSetShareNum][not use redis]");
                    return;
                }
                //加载域
                Redis2.m_fSetDialArea();
                //清除所有前缀键
                string[] m_lDelKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sJSONPrefix) || x.StartsWith(m_sUPDATEPrefix) || x.StartsWith(m_sLockPrefix))?.ToArray();
                if (m_lDelKeys != null && m_lDelKeys.Count() > 0)
                    Redis2.Instance.Del(m_lDelKeys);
                //添加所有共享号码
                foreach (share_number item in sharenum_list)
                {
                    string m_sJson = JsonConvert.SerializeObject(item);
                    Redis2.Instance.Set($"{m_sJSONPrefix}:{item.uuid}", m_sJson, DateTime.MaxValue);
                }
                Log.Instance.Success($"[Core_v1][Redis2][m_fGetShareNum][set share number success]");
            }
            catch (Exception ex)
            {
                Log.Instance.Fail($"[Core_v1][Redis2][m_fGetShareNum][Exception][set share number fail:{ex.Message}]");
            }
        }
        #endregion

        #region ***清除共享号码
        [Obsolete("暂时不使用清除共享号码")]
        public static void m_fClearShare()
        {
            try
            {
                Redis2.Instance.Del(Redis2.m_sDialAreaName);
                //清除所有前缀键
                string[] m_lDelDataKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sJSONPrefix))?.ToArray();
                if (m_lDelDataKeys != null && m_lDelDataKeys.Count() > 0)
                    Redis2.Instance.Del(m_lDelDataKeys);
                string[] m_lDelLockKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sLockPrefix))?.ToArray();
                if (m_lDelLockKeys != null && m_lDelLockKeys.Count() > 0)
                    Redis2.Instance.Del(m_lDelLockKeys);
                Log.Instance.Success($"[Core_v1][Redis2][m_fClearShare][clear share number success]");
            }
            catch (Exception ex)
            {
                Log.Instance.Fail($"[Core_v1][Redis2][m_fClearShare][Exception][clear share number fail:{ex.Message}]");
            }
        }
        #endregion

        #region ***Redis拨号查询共享号码,以及加锁
        public static share_number m_fGetTheShareNumber(string m_sUUID, int m_uAgentID, string m_sCaller, string m_sCallee, int m_uShareSetting, out string m_sErrMsg)
        {
            try
            {
                if (Redis2.use)
                {
                    ///有无配置本机域
                    if (Redis2.m_EsyDialArea == null)
                    {
                        Log.Instance.Warn($"[Core_v1][Redis2][m_fGetTheShareNumber][not find my area in mysql]");
                        m_sErrMsg = "Err无本机域";
                        return null;
                    }

                    ///是否已加入域
                    dial_area m_pDialArea = null;
                    switch (m_uShareSetting)
                    {
                        case 1:
                            m_pDialArea = Redis2.m_EsyDialArea;
                            break;
                        case 2:
                            m_pDialArea = Redis2.m_EsyMainDialArea;
                            break;
                        case 0:
                        default:
                            Log.Instance.Warn($"[Core_v1][Redis2][m_fGetTheShareNumber][no use share area]");
                            m_sErrMsg = "Err未启用域";
                            return null;
                    }

                    List<dial_area> m_lDialArea = JsonConvert.DeserializeObject<List<dial_area>>(Redis2.Instance.Get<string>(Redis2.m_sDialAreaName));
                    var m_uCount = m_lDialArea.Where(x => x.aip == m_pDialArea?.aip && (x.astate == 2 || x.astate == 4))?.Count();
                    if (m_uCount <= 0)
                    {
                        Log.Instance.Warn($"[Core_v1][Redis2][m_fGetTheShareNumber][no find my area in redis]");
                        m_sErrMsg = "Err未加入域";
                        return null;
                    }

                    string[] m_lDataKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sJSONPrefix))?.ToArray();
                    string[] m_lLockKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sLockPrefix))?.ToArray();
                    if (m_lLockKeys == null) m_lLockKeys = new string[0];
                    if (m_lDataKeys != null && m_lDataKeys.Count() > 0)
                    {
                        List<share_number> m_lShareNumber = (from r in Redis2.Instance.GetAll<string>(m_lDataKeys.ToList())
                                                             .Select(x =>
                                                             {
                                                                 return JsonConvert.DeserializeObject<share_number>(x.Value);
                                                             })
                                                             where
                                                             //电话状态为空闲
                                                             r.state == SHARE_NUM_STATUS.IDLE
                                                             //电话号码
                                                             &&
                                                             r.number == m_sCaller
                                                             //限制配置设置
                                                             &&
                                                             (
                                                                //总次数
                                                                (r.limitcount == 0 || r.limitcount > r.usecount)
                                                                &&
                                                                //总时长
                                                                (r.limitduration == 0 || r.limitduration > r.useduration)
                                                                &&
                                                                (
                                                                    //当日次数
                                                                    ((r.limitthecount == 0 || r.limitthecount > r.usethecount) && Cmn_v1.Cmn.m_fEqualsDate(r.usethetime)) || Cmn_v1.Cmn.m_fLessDate(r.usethetime)
                                                                    &&
                                                                    //当日时长
                                                                    ((r.limittheduration == 0 || r.limittheduration > r.usetheduration) && Cmn_v1.Cmn.m_fEqualsDate(r.usethetime)) || Cmn_v1.Cmn.m_fLessDate(r.usethetime)
                                                                )
                                                             )
                                                             //没有锁
                                                             &&
                                                             !m_lLockKeys.Contains($"{Redis2.m_sLockPrefix}:{r.uuid}")
                                                             //暂时去掉同号码限呼逻辑
                                                             select r)?.ToList();

                        if (m_lShareNumber?.Count > 0)
                        {
                            share_number m_pShareNumber = m_lShareNumber.FirstOrDefault();
                            string m_sLockKey = $"{Redis2.m_sLockPrefix}:{m_pShareNumber.uuid}";
                            string m_sDataKey = $"{Redis2.m_sJSONPrefix}:{m_pShareNumber.uuid}";
                            if (Redis2.Instance.SetNX(m_sLockKey, Encoding.UTF8.GetBytes(m_sUUID)) == 1)
                            {
                                //1小时自动解锁即可
                                Redis2.Instance.Expire(m_sLockKey, 60 * 60);
                                //号码状态修改
                                m_pShareNumber.state = SHARE_NUM_STATUS.DIAL;
                                Redis2.Instance.Set(m_sDataKey, JsonConvert.SerializeObject(m_pShareNumber), DateTime.MaxValue);
                                m_sErrMsg = string.Empty;
                                Log.Instance.Success($"[Core_v1][Redis2][m_fGetTheShareNumber][{m_uAgentID} lock {m_sCaller} success]");
                                return m_pShareNumber;
                            }
                            else
                            {
                                m_sErrMsg = "Err资源锁定";
                                Log.Instance.Warn($"[Core_v1][Redis2][m_fGetTheShareNumber][{m_uAgentID} lock {m_sCaller} fail]");
                                return null;
                            }
                        }
                    }
                    m_sErrMsg = "Err拨号限制";
                    return null;
                }
                else
                {
                    m_sErrMsg = "Redis未启用";
                    Log.Instance.Warn($"[Core_v1][Redis2][m_fGetTheShareNumber][not use redis]");
                    return null;
                }
            }
            catch (Exception ex)
            {
                m_sErrMsg = "ErrRedis";
                Log.Instance.Error($"[Core_v1][Redis2][m_fGetTheShareNumber][Exception][{m_uAgentID} lock {m_sCaller} fail:{ex.Message}]");
                return null;
            }
        }
        #endregion

        #region ***Redis拨号缓存数据的修改
        public static dial_area m_fEditShareNumber(int m_uAgentID, string m_sUUID, share_number m_pShareNumber, int m_uDuration = 0)
        {
            try
            {
                if (!Redis2.use) return null;
                if (m_pShareNumber == null) return null;
                if (string.IsNullOrWhiteSpace(m_pShareNumber.uuid)) return null;
                if (string.IsNullOrWhiteSpace(m_sUUID)) return null;
                string m_sLockKey = $"{Redis2.m_sLockPrefix}:{m_pShareNumber.uuid}";
                string m_sDataKey = $"{Redis2.m_sJSONPrefix}:{m_pShareNumber.uuid}";
                string m_sValue = Redis2.Instance.Get<string>(m_sLockKey);
                if (m_sValue == m_sUUID || m_sValue == Redis2.m_sUpdateUseEnded)
                {
                    List<dial_area> m_lDialArea = JsonConvert.DeserializeObject<List<dial_area>>(Redis2.Instance.Get<string>(Redis2.m_sDialAreaName));
                    dial_area m_pDialArea = m_lDialArea.Where(x => x.id == m_pShareNumber.areaid)?.FirstOrDefault();
                    if (m_pDialArea != null)
                    {
                        if (Cmn_v1.Cmn.m_fEqualsDate(m_pShareNumber.usethetime))
                        {
                            m_pShareNumber.usecount++;
                            m_pShareNumber.useduration += m_uDuration;
                            m_pShareNumber.usethecount++;
                            m_pShareNumber.usetheduration += m_uDuration;
                        }
                        else
                        {
                            m_pShareNumber.usecount++;
                            m_pShareNumber.useduration += m_uDuration;
                            m_pShareNumber.usethecount = 1;
                            m_pShareNumber.usetheduration = m_uDuration;
                            m_pShareNumber.usethetime = DateTime.Now;
                        }
                        m_pShareNumber.state = SHARE_NUM_STATUS.IDLE;
                        //更新数据
                        bool m_bSet = true;
                        if (m_sValue == Redis2.m_sUpdateUseEnded)
                        {
                            string m_sUpdateKey = $"{Redis2.m_sUPDATEPrefix}:{m_pShareNumber.uuid}";
                            string m_sUpdateValue = Redis2.Instance.Get<string>(m_sUpdateKey);
                            if (!string.IsNullOrWhiteSpace(m_sUpdateValue))
                            {
                                //更新锁更新数据
                                Log.Instance.Warn($"[Core_v1][Redis2][m_fEditShareNumber][{m_uAgentID} edit -> update {m_sDataKey}]");
                                share_number _m_pShareNumber = JsonConvert.DeserializeObject<share_number>(m_sUpdateValue);
                                m_pShareNumber.number = _m_pShareNumber.number;
                                m_pShareNumber.limitthedial = _m_pShareNumber.limitthedial;
                                //m_pShareNumber.usecount = _m_pShareNumber.usecount;
                                //m_pShareNumber.useduration = _m_pShareNumber.useduration;
                                m_pShareNumber.limitcount = _m_pShareNumber.limitcount;
                                //m_pShareNumber.usethetime = _m_pShareNumber.usethetime;
                                m_pShareNumber.limitduration = _m_pShareNumber.limitduration;
                                //m_pShareNumber.usethecount = _m_pShareNumber.usethecount;
                                //m_pShareNumber.usetheduration = _m_pShareNumber.usetheduration;
                                m_pShareNumber.limitthecount = _m_pShareNumber.limitthecount;
                                m_pShareNumber.limittheduration = _m_pShareNumber.limittheduration;
                                m_pShareNumber.isuse = _m_pShareNumber.isuse;
                                m_pShareNumber.dialprefix = _m_pShareNumber.dialprefix;
                                m_pShareNumber.areacode = _m_pShareNumber.areacode;
                                m_pShareNumber.areaname = _m_pShareNumber.areaname;
                                m_pShareNumber.isusedial = _m_pShareNumber.isusedial;
                                m_pShareNumber.isusecall = _m_pShareNumber.isusecall;
                                m_pShareNumber.dtmf = _m_pShareNumber.dtmf;
                                m_pShareNumber.gw = _m_pShareNumber.gw;
                                m_pShareNumber.gwtype = _m_pShareNumber.gwtype;
                            }
                            else
                            {
                                //更新锁移除数据
                                Log.Instance.Warn($"[Core_v1][Redis2][m_fEditShareNumber][{m_uAgentID} edit -> del {m_sDataKey}]");
                                if (Redis2.Instance.Del(m_sDataKey) != 1)
                                {
                                    Log.Instance.Fail($"[Core_v1][Redis2][m_fEditShareNumber][{m_uAgentID} edit -> del {m_sDataKey} fail]");
                                }
                                m_bSet = false;
                            }
                            Redis2.Instance.Del(m_sUpdateKey);
                        }
                        if (m_bSet)
                        {
                            if (!Redis2.Instance.Set(m_sDataKey, JsonConvert.SerializeObject(m_pShareNumber), DateTime.MaxValue))
                            {
                                Log.Instance.Fail($"[Core_v1][Redis2][m_fEditShareNumber][{m_uAgentID} edit {m_sDataKey} fail]");
                            }
                        }
                        Redis2.Instance.Del(m_sLockKey);
                        Log.Instance.Success($"[Core_v1][Redis2][m_fEditShareNumber][{m_uAgentID} edit {m_sDataKey} success]");
                        return m_pDialArea;
                    }
                    else
                    {
                        Log.Instance.Warn($"[Core_v1][Redis2][m_fEditShareNumber][{m_uAgentID} edit {m_sDataKey} fail:dial area null]");
                    }
                }
                else
                {
                    Log.Instance.Warn($"[Core_v1][Redis2][m_fEditShareNumber][{m_uAgentID} edit {m_sDataKey} fail:UUID different]");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[Core_v1][Redis2][m_fEditShareNumber][Exception][{ex.Message}]");
                Log.Instance.Debug(ex);
            }
            return null;
        }
        #endregion

        #region ***Redis拨号缓存数据的复位与锁删除
        public static void m_fResetShareNumber(int m_uAgentID, share_number m_pShareNumber, string m_sUUID, string m_sFreeSWITCHIPv4 = null)
        {
            try
            {
                if (!Redis2.use) return;
                if (m_pShareNumber == null) return;
                if (string.IsNullOrWhiteSpace(m_sUUID)) return;
                string m_sLockKey = $"{Redis2.m_sLockPrefix}:{m_pShareNumber.uuid}";
                string m_sDataKey = $"{Redis2.m_sJSONPrefix}:{m_pShareNumber.uuid}";
                string m_sValue = Redis2.Instance.Get<string>(m_sLockKey);
                if (m_sValue == m_sUUID || m_sValue == Redis2.m_sUpdateUseEnded)
                {
                    m_pShareNumber.state = SHARE_NUM_STATUS.IDLE;
                    //更新数据
                    bool m_bSet = true;
                    if (m_sValue == Redis2.m_sUpdateUseEnded)
                    {
                        string m_sUpdateKey = $"{Redis2.m_sUPDATEPrefix}:{m_pShareNumber.uuid}";
                        string m_sUpdateValue = Redis2.Instance.Get<string>(m_sUpdateKey);
                        if (!string.IsNullOrWhiteSpace(m_sUpdateValue))
                        {
                            //更新锁更新数据
                            Log.Instance.Warn($"[Core_v1][Redis2][m_fResetShareNumber][{m_uAgentID} reset -> update {m_sDataKey}]");
                            share_number _m_pShareNumber = JsonConvert.DeserializeObject<share_number>(m_sUpdateValue);
                            m_pShareNumber.number = _m_pShareNumber.number;
                            m_pShareNumber.limitthedial = _m_pShareNumber.limitthedial;
                            m_pShareNumber.usecount = _m_pShareNumber.usecount;
                            m_pShareNumber.useduration = _m_pShareNumber.useduration;
                            m_pShareNumber.limitcount = _m_pShareNumber.limitcount;
                            m_pShareNumber.usethetime = _m_pShareNumber.usethetime;
                            m_pShareNumber.limitduration = _m_pShareNumber.limitduration;
                            m_pShareNumber.usethecount = _m_pShareNumber.usethecount;
                            m_pShareNumber.usetheduration = _m_pShareNumber.usetheduration;
                            m_pShareNumber.limitthecount = _m_pShareNumber.limitthecount;
                            m_pShareNumber.limittheduration = _m_pShareNumber.limittheduration;
                            m_pShareNumber.isuse = _m_pShareNumber.isuse;
                            m_pShareNumber.dialprefix = _m_pShareNumber.dialprefix;
                            m_pShareNumber.areacode = _m_pShareNumber.areacode;
                            m_pShareNumber.areaname = _m_pShareNumber.areaname;
                            m_pShareNumber.isusedial = _m_pShareNumber.isusedial;
                            m_pShareNumber.isusecall = _m_pShareNumber.isusecall;
                            m_pShareNumber.dtmf = _m_pShareNumber.dtmf;
                            m_pShareNumber.gw = _m_pShareNumber.gw;
                            m_pShareNumber.gwtype = _m_pShareNumber.gwtype;
                        }
                        else
                        {
                            //更新锁移除数据
                            Log.Instance.Warn($"[Core_v1][Redis2][m_fResetShareNumber][{m_uAgentID} reset -> del {m_sDataKey}]");
                            if (Redis2.Instance.Del(m_sDataKey) != 1)
                            {
                                Log.Instance.Fail($"[Core_v1][Redis2][m_fResetShareNumber][{m_uAgentID} reset -> del {m_sDataKey} fail]");
                            }
                            m_bSet = false;
                        }
                        Redis2.Instance.Del(m_sUpdateKey);
                    }
                    if (m_bSet)
                    {
                        if (!Redis2.Instance.Set(m_sDataKey, JsonConvert.SerializeObject(m_pShareNumber), DateTime.MaxValue))
                        {
                            Log.Instance.Fail($"[Core_v1][Redis2][m_fResetShareNumber][{m_uAgentID} reset {m_sDataKey} fail]");
                        }
                    }
                    Redis2.Instance.Del(m_sLockKey);
                    Log.Instance.Success($"[Core_v1][Redis2][m_fResetShareNumber][{m_uAgentID} reset {m_sLockKey} success]");
                }
                else
                {
                    Log.Instance.Warn($"[Core_v1][Redis2][m_fResetShareNumber][{m_uAgentID} reset {m_sLockKey} fail:UUID different]");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[Core_v1][Redis2][m_fResetShareNumber][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region ***根据IP查询共享域
        public static dial_area m_fGetDialAreaByIPv4(string m_sIPv4)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(m_sIPv4))
                    return null;

                List<dial_area> m_lDialArea = JsonConvert.DeserializeObject<List<dial_area>>(Redis2.Instance.Get<string>(Redis2.m_sDialAreaName));
                return m_lDialArea.Where(x => x.aip == m_sIPv4)?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[Core_v1][Redis2][m_fGetDialAreaByIPv4][Exception][{ex.Message}]");
            }
            return null;
        }
        #endregion

        #region ***Redis来电查询,及加锁
        public static int m_fGetTheCall(string m_sCallee, string m_sUUID, out share_number _m_pShareNumber)
        {
            _m_pShareNumber = null;
            try
            {
                if (!Redis2.use) return 0;
                if (string.IsNullOrWhiteSpace(m_sUUID)) return 0;
                if (string.IsNullOrWhiteSpace(m_sCallee)) return 0;
                string[] m_lKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sJSONPrefix))?.ToArray();
                if (m_lKeys != null && m_lKeys.Count() > 0)
                {
                    List<share_number> m_lShareNumber = (from r in Redis2.Instance.GetAll<string>(m_lKeys.ToList())
                                                         .Select(x =>
                                                         {
                                                             return JsonConvert.DeserializeObject<share_number>(x.Value);
                                                         })
                                                         where r.number.Contains(m_sCallee) && r.state != SHARE_NUM_STATUS.UNIDLE
                                                         select r)?.ToList();
                    if (m_lShareNumber?.Count() > 0)
                    {
                        share_number m_pShareNumber = m_lShareNumber?.FirstOrDefault();
                        _m_pShareNumber = m_pShareNumber;
                        if (_m_pShareNumber.state == SHARE_NUM_STATUS.IDLE)
                        {
                            string m_sLockKey = $"{Redis2.m_sLockPrefix}:{m_pShareNumber.uuid}";
                            string m_sDataKey = $"{Redis2.m_sJSONPrefix}:{m_pShareNumber.uuid}";
                            if (Redis2.Instance.SetNX(m_sLockKey, Encoding.UTF8.GetBytes(m_sUUID)) == 1)
                            {
                                //1小时自动解锁即可
                                Redis2.Instance.Expire(m_sLockKey, 60 * 60);
                                //号码状态修改
                                m_pShareNumber.state = SHARE_NUM_STATUS.CALL;
                                Redis2.Instance.Set(m_sDataKey, JsonConvert.SerializeObject(m_pShareNumber), DateTime.MaxValue);
                                Log.Instance.Success($"[Core_v1][Redis2][m_fGetTheCall][{m_sUUID} lock {m_sCallee} success]");
                                return 1;
                            }
                            else
                            {
                                Log.Instance.Warn($"[Core_v1][Redis2][m_fGetTheCall][{m_sUUID} lock {m_sCallee} fail]");
                                return 2;
                            }
                        }
                        else
                        {
                            Log.Instance.Warn($"[Core_v1][Redis2][m_fGetTheCall][{m_sUUID} await {m_sCallee}]");
                            return 3;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[Core_v1][Redis2][m_fGetTheCall][Exception][{ex.Message}]");
                Log.Instance.Debug(ex);
            }
            return 0;
        }
        #endregion

        #region ***共享号码的信息同步
        /// <summary>
        /// <![CDATA[
        /// 同步逻辑:写一个定时器,同步未加锁的数据
        /// 实际1:直接使用更新
        /// ]]>
        /// </summary>
        public static void m_fShareSynchronize()
        {
            try
            {
                if (!Redis2.use)
                {
                    Log.Instance.Warn($"[Core_v1][Redis2][m_fShareSynchronize][not use redis]");
                    return;
                }

                lock (Redis2.m_oUpdateLock)
                {
                    //共享域直接更新
                    Redis2.Instance.Set(Redis2.m_sDialAreaName, JsonConvert.SerializeObject(dialarea_list), DateTime.MaxValue);
                    //查询号码池内容
                    string[] m_lDataKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sJSONPrefix))?.ToArray();
                    if (m_lDataKeys == null) m_lDataKeys = new string[0];
                    if (m_lDataKeys != null && m_lDataKeys.Count() > 0)
                    {
                        List<share_number> m_lShareNumber = (from r in Redis2.Instance.GetAll<string>(m_lDataKeys.ToList())
                                                                .Select(x =>
                                                                {
                                                                    return JsonConvert.DeserializeObject<share_number>(x.Value);
                                                                })
                                                             select r)?.ToList();
                        if (m_lShareNumber != null && m_lShareNumber.Count > 0)
                        {
                            foreach (share_number m_pShareNumber in m_lShareNumber)
                            {
                                string m_sLockKey = $"{Redis2.m_sLockPrefix}:{m_pShareNumber.uuid}";
                                string m_sDataKey = $"{Redis2.m_sJSONPrefix}:{m_pShareNumber.uuid}";
                                share_number _m_pShareNumber = sharenum_list.Where(x => x.areaid == m_pShareNumber.areaid && x.id == m_pShareNumber.id)?.FirstOrDefault();
                                if (Redis2.Instance.SetNX(m_sLockKey, Encoding.UTF8.GetBytes(Redis2.m_sUpdateNow)) == 1)
                                {
                                    //5分钟过期
                                    Redis2.Instance.Expire(m_sLockKey, 60 * 5);
                                    if (_m_pShareNumber != null)
                                    {
                                        //需要转换uuid
                                        _m_pShareNumber.uuid = m_pShareNumber.uuid;
                                        //更新数据
                                        if (!Redis2.Instance.Set(m_sDataKey, JsonConvert.SerializeObject(_m_pShareNumber), DateTime.MaxValue))
                                        {
                                            Log.Instance.Fail($"[Core_v1][Redis2][m_fShareSynchronize][{m_sDataKey} set data fail]");
                                        }
                                    }
                                    else
                                    {
                                        //删除数据
                                        if (Redis2.Instance.Del(m_sDataKey) != 1)
                                        {
                                            Log.Instance.Fail($"[Core_v1][Redis2][m_fShareSynchronize][{m_sDataKey} del data fail]");
                                        }
                                    }
                                    //删除锁
                                    if (Redis2.Instance.Del(m_sLockKey) != 1)
                                    {
                                        Log.Instance.Fail($"[Core_v1][Redis2][m_fShareSynchronize][{m_sLockKey} del update lock fail]");
                                    }
                                }
                                else
                                {
                                    //设置锁内容为结束更新锁
                                    if (!Redis2.Instance.Set(m_sLockKey, Redis2.m_sUpdateUseEnded, DateTime.MaxValue))
                                    {
                                        Log.Instance.Fail($"[Core_v1][Redis2][m_fShareSynchronize][{m_sDataKey} set update use ended lock fail]");
                                    }
                                    //非空写入需更新的数据
                                    if (_m_pShareNumber != null)
                                    {
                                        string m_sUpdateKey = $"{Redis2.m_sUPDATEPrefix}:{m_pShareNumber.uuid}";
                                        if (!Redis2.Instance.Set(m_sUpdateKey, JsonConvert.SerializeObject(_m_pShareNumber), DateTime.MaxValue))
                                        {
                                            Log.Instance.Fail($"[Core_v1][Redis2][m_fShareSynchronize][{m_sUpdateKey} set update key fail]");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //将新的号码加入其中号码池中
                    List<share_number> _m_lShareNumber = Redis2.sharenum_list.Where(x => !m_lDataKeys.Contains($"{Redis2.m_sJSONPrefix}:{x.areaid}_{x.id}_{x.number}|", new RedisKeyComparer()))?.ToList();
                    if (_m_lShareNumber != null && _m_lShareNumber.Count > 0)
                    {
                        foreach (share_number m_pShareNumber in _m_lShareNumber)
                        {
                            string m_sJson = JsonConvert.SerializeObject(m_pShareNumber);
                            string m_sDataKey = $"{Redis2.m_sJSONPrefix}:{m_pShareNumber.uuid}";
                            if (!Redis2.Instance.Set($"{m_sDataKey}", m_sJson, DateTime.MaxValue))
                            {
                                Log.Instance.Fail($"[Core_v1][Redis2][m_fShareSynchronize][{m_sDataKey} set new share number fail]");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[Core_v1][Redis2][m_fShareSynchronize][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region ***获取共享号码
        public static List<share_number> m_fGetShareNumberList()
        {
            try
            {
                if (Redis2.use)
                {
                    ///有无配置本机域
                    if (Redis2.m_EsyDialArea == null)
                    {
                        Log.Instance.Warn($"[Core_v1][Redis2][m_fGetShareNumberList][not find my area in mysql]");
                        return null;
                    }

                    ///是否已加入域
                    List<dial_area> m_lDialArea = JsonConvert.DeserializeObject<List<dial_area>>(Redis2.Instance.Get<string>(Redis2.m_sDialAreaName));
                    var m_uCount = m_lDialArea.Where(x => x.aip == Redis2.m_EsyDialArea?.aip && (x.astate == 2 || x.astate == 4))?.Count();
                    if (m_uCount <= 0)
                    {
                        Log.Instance.Warn($"[Core_v1][Redis2][m_fGetShareNumberList][no find my area in redis]");
                        return null;
                    }

                    string[] m_lDataKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sJSONPrefix))?.ToArray();
                    string[] m_lLockKeys = Redis2.Instance.GetAllKeys().Where(x => x.StartsWith(m_sLockPrefix))?.ToArray();
                    if (m_lLockKeys == null) m_lLockKeys = new string[0];
                    if (m_lDataKeys != null && m_lDataKeys.Count() > 0)
                    {
                        List<share_number> m_lShareNumber = (from r in Redis2.Instance.GetAll<string>(m_lDataKeys.ToList())
                                                             .Select(x => { return JsonConvert.DeserializeObject<share_number>(x.Value); })
                                                             where
                                                             //电话状态为空闲
                                                             r.state == SHARE_NUM_STATUS.IDLE
                                                             //限制配置设置
                                                             &&
                                                             (
                                                                //总次数
                                                                (r.limitcount == 0 || r.limitcount > r.usecount)
                                                                &&
                                                                //总时长
                                                                (r.limitduration == 0 || r.limitduration > r.useduration)
                                                                &&
                                                                (
                                                                    //当日次数
                                                                    ((r.limitthecount == 0 || r.limitthecount > r.usethecount) && Cmn_v1.Cmn.m_fEqualsDate(r.usethetime)) || Cmn_v1.Cmn.m_fLessDate(r.usethetime)
                                                                    &&
                                                                    //当日时长
                                                                    ((r.limittheduration == 0 || r.limittheduration > r.usetheduration) && Cmn_v1.Cmn.m_fEqualsDate(r.usethetime)) || Cmn_v1.Cmn.m_fLessDate(r.usethetime)
                                                                )
                                                             )
                                                             //没有锁
                                                             &&
                                                             !m_lLockKeys.Contains($"{Redis2.m_sLockPrefix}:{r.uuid}")
                                                             //排序
                                                             orderby r.areaname ascending, r.number ascending
                                                             //暂时去掉同号码限呼逻辑
                                                             select r)?.ToList();

                        return m_lShareNumber;
                    }
                    else
                    {
                        Log.Instance.Warn($"[Core_v1][Redis2][m_fGetShareNumberList][no share number]");
                    }
                }
                else
                {
                    Log.Instance.Warn($"[Core_v1][Redis2][m_fGetShareNumberList][not use redis]");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[Core_v1][Redis2][m_fGetShareNumberList][Exception][{ex.Message}]");
                Log.Instance.Debug(ex);
            }
            return null;
        }
        #endregion
    }

    #region ***Redis键比较方式
    public class RedisKeyComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Contains(y);
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
    #endregion
}