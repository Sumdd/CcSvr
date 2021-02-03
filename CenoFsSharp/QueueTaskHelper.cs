using Core_v1;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NEventSocket;
using CenoSipFactory;
using DB.Model;
using Model_v1;
using DB.Basic;

namespace CenoFsSharp
{
    public class m_mQueueTask
    {
        public int? ID;//int
        public string PhoneNum;
        public string pici;
        public string progressFlag;//int
        public string contentTxt;
        public string status;
        public string addTime;//DateTime
        public string callTime;//DateTime
        public string endTime;//DateTime
        public string result;
        public int IsUpdate;//int
        public string luyinId;
        public string CallNum;
        public int CallStatus;//int
        public int CallCount;//int
        public string source_id;//long
        public string ajid;
        public string inpici;
        public string shfzh18;
        public string czy;
        public int asr_status;//语音识别标志
    }

    public class m_mThread
    {
        public EventWaitHandle m_eEventWaitHandle = new AutoResetEvent(false);
        public Thread m_tThread;
        public ChannelInfo m_mChannelInfo;
        public m_mQueueTask _m_mQueueTask;
        public call_record_model m_mRecord;
        public bool m_bIsStart = false;
        public bool m_bIsExitThread = false;
        public DateTime m_pCreateTime = DateTime.Now;
        public bool m_bTodayUse = true;
    }

    public class m_fQueueTask
    {
        public static readonly object m_oQueueTaskLocker = new object();
        private static bool m_bIsExit = false;
        public static bool m_bIsSleep = true;
        public static Queue<m_mQueueTask> m_qQueueTaskList = new Queue<m_mQueueTask>();
        public static List<m_mThread> m_lThreadList = new List<m_mThread>();
        public static int m_uQueueMaxCount = 65535;

        public async void m_fWork(m_mThread _m_mThread)
        {
            while (true)
            {
                if (m_bIsSleep)
                {
                    if (CenoCommon.CmnParam.IsExit) break;
                    Thread.Sleep(5000);
                    continue;
                }

                if (!_m_mThread.m_bTodayUse)
                {
                    if (CenoCommon.CmnParam.IsExit) break;
                    DateTime m_dtNow = DateTime.Now;
                    if (m_dtNow.Date == _m_mThread.m_pCreateTime.Date)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }
                    else
                    {
                        _m_mThread.m_pCreateTime = m_dtNow;
                        _m_mThread.m_bTodayUse = true;
                    }
                }

                if (m_bIsExit)
                {
                    _m_mThread.m_bIsStart = false;
                    Log.Instance.Debug($"[CenoFsSharp][m_fQueueTask][m_fWork][{_m_mThread.m_tThread.Name} end...]");
                    break;
                }

                if (_m_mThread.m_bIsExitThread)
                {
                    _m_mThread.m_bIsStart = false;
                    Log.Instance.Warn($"[CenoFsSharp][m_fQueueTask][m_fWork][{_m_mThread.m_tThread.Name} thread end...]");
                    break;
                }

                if (_m_mThread?.m_mChannelInfo?.channel_type != Special.AUTO)
                {
                    _m_mThread.m_bIsStart = false;
                    Log.Instance.Warn($"[CenoFsSharp][m_fQueueTask][m_fWork][{_m_mThread.m_tThread.Name} not auto channel,end...]");
                    break;
                }

                _m_mThread._m_mQueueTask = null;
                _m_mThread.m_mRecord = new call_record_model();
                lock (m_oQueueTaskLocker)
                {
                    if (m_qQueueTaskList.Count > 0)
                    {
                        _m_mThread._m_mQueueTask = m_qQueueTaskList.Dequeue();
                    }
                }

                if (_m_mThread._m_mQueueTask != null)
                    await m_fSaveQueueTaskResult(_m_mThread);
                else
                {
                    if (_m_mThread.m_eEventWaitHandle != null && !_m_mThread.m_eEventWaitHandle.SafeWaitHandle.IsClosed)
                    {
                        Log.Instance.Debug($"[CenoFsSharp][m_fQueueTask][m_fWork][{_m_mThread.m_tThread.Name} waiting...]");
                        _m_mThread.m_eEventWaitHandle.WaitOne();
                    }
                    else
                    {
                        _m_mThread.m_bIsStart = false;
                        break;
                    }
                }
            }
        }

        public static void m_fEnqueueTask(List<m_mQueueTask> m_lQueueTaskList = null)
        {
            lock (m_oQueueTaskLocker)
            {
                ///当退出或参数为空时,不再读取数据
                if (m_fQueueTask.m_bIsExit && m_lQueueTaskList == null) return;

                if (m_lQueueTaskList == null)
                {
                    int m_uLimit = m_fQueueTask.m_uQueueMaxCount - m_qQueueTaskList.Count;
                    if (m_uLimit > 0)
                    {
                        ///每次查询25条,防止临时表过大,猜测
                        int _m_uLimit = (m_uLimit > 25 ? 25 : m_uLimit);
                        m_lQueueTaskList = m_cDataTableToQueueTask(DB.Basic.PhoneAutoCall.m_fGetEnQueueTaskDataTable(_m_uLimit));
                    }
                    else
                        m_lQueueTaskList = new List<m_mQueueTask>();
                }
                if (m_lQueueTaskList.Count > 0)
                {
                    m_lQueueTaskList.ForEach(x =>
                    {
                        m_mQueueTask entity = x;
                        m_qQueueTaskList.Enqueue(entity);
                    });
                    if (m_qQueueTaskList.Count > 0)
                    {
                        m_lThreadList.ForEach(x =>
                        {
                            x.m_eEventWaitHandle.Set();
                        });
                    }
                    Log.Instance.Debug($"[CenoFsSharp][m_fQueueTask][m_fWork][total:{m_qQueueTaskList.Count} all thread set...]");
                }
            }
        }

        public static bool m_fEnqueueTask(List<m_mQueueTask> m_lQueueTaskList, out string m_sResultMessage)
        {
            try
            {
                m_sResultMessage = string.Empty;
                lock (m_oQueueTaskLocker)
                {
                    if (m_lQueueTaskList != null && m_lQueueTaskList.Count > 0)
                    {
                        int m_uLimit = m_fQueueTask.m_uQueueMaxCount - m_qQueueTaskList.Count;
                        if (m_uLimit >= m_lQueueTaskList.Count)
                        {
                            m_lQueueTaskList.ForEach(x =>
                            {
                                m_mQueueTask entity = x;
                                m_qQueueTaskList.Enqueue(entity);
                            });
                            if (m_qQueueTaskList.Count > 0)
                            {
                                m_lThreadList.ForEach(x =>
                                {
                                    x.m_eEventWaitHandle.Set();
                                });
                            }
                            Log.Instance.Debug($"[CenoFsSharp][m_fQueueTask][m_fWork][total:{m_qQueueTaskList.Count} all thread set...]");
                            m_sResultMessage = "拨号任务加入队列";
                            return true;
                        }
                        else
                        {
                            m_sResultMessage = "拨号队列已满";
                            return false;
                        }
                    }
                    else
                    {
                        m_sResultMessage = "拨号任务为空";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                m_sResultMessage = ex.Message;
                return false;
            }
        }

        public static async Task m_fSaveQueueTaskResult(m_mThread _m_mThread)
        {
            while (
                _m_mThread.m_mChannelInfo.channel_call_status != APP_USER_STATUS.FS_USER_IDLE &&
                _m_mThread.m_mChannelInfo.channel_call_status != APP_USER_STATUS.FS_USER_AHANGUP &&
                _m_mThread.m_mChannelInfo.channel_call_status != APP_USER_STATUS.FS_USER_BHANGUP
                )
            {
                if (m_bIsExit)
                {
                    _m_mThread.m_bIsStart = false;
                    return;
                }
                Thread.Sleep(5000);
            }
            _m_mThread.m_mChannelInfo.channel_call_status = APP_USER_STATUS.FS_USER_BF_DIAL;
            Log.Instance.Debug($"[CenoFsSharp][m_fQueueTask][m_fWork][{_m_mThread.m_tThread.Name} doing...]");
            _m_mThread.m_mRecord = new call_record_model(_m_mThread.m_mChannelInfo.channel_id, 5, _m_mThread._m_mQueueTask.CallNum, _m_mThread._m_mQueueTask.PhoneNum, _m_mThread._m_mQueueTask.PhoneNum);
            await CenoFsSharp.m_fDoTaskClass.m_fDoTask(_m_mThread);
            Thread.Sleep(5000);
        }

        public static List<m_mQueueTask> m_cDataTableToQueueTask(DataTable m_pDataTable)
        {
            List<m_mQueueTask> _m_qQueueTaskList = new List<m_mQueueTask>();
            if (m_pDataTable != null && m_pDataTable.Rows.Count > 0)
            {
                foreach (DataRow m_pDataRow in m_pDataTable.Rows)
                {
                    m_mQueueTask _m_mQueueTask = new m_mQueueTask();
                    _m_mQueueTask.ID = Convert.ToInt32(m_pDataRow["ID"]);
                    _m_mQueueTask.PhoneNum = m_pDataRow["PhoneNum"].ToString();
                    _m_mQueueTask.pici = m_pDataRow["pici"].ToString();
                    _m_mQueueTask.progressFlag = m_pDataRow["progressFlag"].ToString();
                    _m_mQueueTask.contentTxt = m_pDataRow["contentTxt"].ToString();
                    _m_mQueueTask.status = m_pDataRow["status"].ToString();
                    _m_mQueueTask.addTime = m_pDataRow["addTime"].ToString();
                    _m_mQueueTask.callTime = m_pDataRow["callTime"].ToString();
                    _m_mQueueTask.endTime = m_pDataRow["endTime"].ToString();
                    _m_mQueueTask.result = m_pDataRow["result"].ToString();
                    _m_mQueueTask.IsUpdate = Convert.ToInt32(m_pDataRow["IsUpdate"]);
                    _m_mQueueTask.luyinId = m_pDataRow["luyinId"].ToString();
                    _m_mQueueTask.CallNum = m_pDataRow["CallNum"].ToString();
                    _m_mQueueTask.CallStatus = Convert.ToInt32(m_pDataRow["CallStatus"]);
                    _m_mQueueTask.CallCount = Convert.ToInt32(m_pDataRow["CallCount"]);
                    _m_mQueueTask.source_id = m_pDataRow["source_id"].ToString();
                    _m_mQueueTask.ajid = m_pDataRow["ajid"].ToString();
                    _m_mQueueTask.inpici = m_pDataRow["inpici"].ToString();
                    _m_mQueueTask.shfzh18 = m_pDataRow["shfzh18"].ToString();
                    _m_mQueueTask.czy = m_pDataRow["czy"].ToString();
                    _m_mQueueTask.asr_status = Convert.ToInt32(m_pDataRow["asr_status"]);
                    _m_qQueueTaskList.Add(_m_mQueueTask);
                }
            }
            return _m_qQueueTaskList;
        }
        public static void Dispose(bool m_bClearQueue = false)
        {
            lock (m_fQueueTask.m_oQueueTaskLocker)
            {
                m_bIsExit = true;
                m_lThreadList.ForEach(x =>
                {
                    x.m_eEventWaitHandle.Set();
                    x.m_tThread.Join();
                    x.m_eEventWaitHandle.Close();
                });

                ///清空队列
                if (m_bClearQueue)
                {
                    List<string> m_lSQL = new List<string>();
                    while (true)
                    {
                        if (CenoFsSharp.m_fQueueTask.m_qQueueTaskList != null && CenoFsSharp.m_fQueueTask.m_qQueueTaskList.Count > 0)
                        {
                            CenoFsSharp.m_mQueueTask _m_mQueueTask = CenoFsSharp.m_fQueueTask.m_qQueueTaskList.Dequeue();
                            ///拼接语句,防止SQL语句IN受限制
                            if (_m_mQueueTask != null)
                            {
                                if (_m_mQueueTask.ID != null) m_lSQL.Add($@"SELECT {_m_mQueueTask.ID} AS `ID`");
                                else
                                {
                                    #region 无ID处理

                                    PhoneAutoCall_model _m_mPhoneAutoCall = new PhoneAutoCall_model();
                                    _m_mPhoneAutoCall.PhoneNum = _m_mQueueTask.PhoneNum;
                                    _m_mPhoneAutoCall.pici = _m_mQueueTask.pici;
                                    try
                                    {
                                        if (string.IsNullOrWhiteSpace(_m_mQueueTask.progressFlag)) _m_mPhoneAutoCall.progressFlag = null;
                                        else _m_mPhoneAutoCall.progressFlag = Convert.ToInt32(_m_mQueueTask.progressFlag);
                                    }
                                    catch { }
                                    _m_mPhoneAutoCall.contentTxt = _m_mQueueTask.contentTxt;
                                    _m_mPhoneAutoCall.status = _m_mQueueTask.status;
                                    try
                                    {
                                        if (string.IsNullOrWhiteSpace(_m_mQueueTask.addTime)) _m_mPhoneAutoCall.addTime = DateTime.Now;
                                        else _m_mPhoneAutoCall.addTime = Convert.ToDateTime(_m_mQueueTask.addTime);
                                    }
                                    catch { }
                                    try
                                    {
                                        if (string.IsNullOrWhiteSpace(_m_mQueueTask.callTime)) _m_mPhoneAutoCall.callTime = null;
                                        else _m_mPhoneAutoCall.callTime = Convert.ToDateTime(_m_mQueueTask.callTime);
                                    }
                                    catch { }
                                    try
                                    {
                                        if (string.IsNullOrWhiteSpace(_m_mQueueTask.endTime)) _m_mPhoneAutoCall.endTime = null;
                                        else _m_mPhoneAutoCall.endTime = Convert.ToDateTime(_m_mQueueTask.endTime);
                                    }
                                    catch { }
                                    _m_mPhoneAutoCall.result = _m_mQueueTask.result;
                                    _m_mPhoneAutoCall.IsUpdate = _m_mQueueTask.IsUpdate;
                                    _m_mPhoneAutoCall.luyinId = _m_mQueueTask.luyinId;
                                    _m_mPhoneAutoCall.CallNum = _m_mQueueTask.CallNum;
                                    _m_mPhoneAutoCall.CallStatus = _m_mQueueTask.CallStatus;
                                    _m_mPhoneAutoCall.CallCount = _m_mQueueTask.CallCount;
                                    try
                                    {
                                        if (string.IsNullOrWhiteSpace(_m_mQueueTask.source_id)) _m_mPhoneAutoCall.source_id = null;
                                        else _m_mPhoneAutoCall.source_id = Convert.ToInt32(_m_mQueueTask.source_id);
                                    }
                                    catch { }
                                    _m_mPhoneAutoCall.ajid = _m_mQueueTask.ajid;
                                    _m_mPhoneAutoCall.inpici = _m_mQueueTask.inpici;
                                    _m_mPhoneAutoCall.shfzh18 = _m_mQueueTask.shfzh18;
                                    _m_mPhoneAutoCall.czy = _m_mQueueTask.czy;
                                    _m_mPhoneAutoCall.asr_status = _m_mQueueTask.asr_status;
                                    if (!Call_ParamUtil.m_bIsDialTaskAsr)
                                    {
                                        _m_mPhoneAutoCall.asr_status = 2;
                                    }

                                    //不需要上报,但需要保存记录
                                    bool m_bInsert = PhoneAutoCall.Insert(_m_mPhoneAutoCall);
                                    if (m_bInsert)
                                        Log.Instance.Success($"[CenoFsSharp][m_fQueueTask][Dispose][update -> insert auto dial task success]");
                                    else
                                        Log.Instance.Fail($"[CenoFsSharp][m_fQueueTask][Dispose][update -> insert auto dial task fail]");

                                    #endregion
                                }
                            }
                            else Log.Instance.Warn($"[CenoFsSharp][m_fQueueTask][Dispose][warning,null data]");
                        }
                        else break;
                    }

                    if (m_lSQL.Count > 0)
                    {
                        ///将这些自动拨号任务状态进行回发
                        string m_sSQL = $@"
UPDATE `phoneautocall`
INNER JOIN ( {string.Join("\r\nUNION\r\n", m_lSQL)} ) `T0` ON `T0`.`ID` = `phoneautocall`.`ID` 
SET `phoneautocall`.`CallStatus` = 0
";
                        int m_uCount = DB.Basic.MySQL_Method.ExecuteNonQuery(m_sSQL);
                        if (m_uCount == m_lSQL.Count) Log.Instance.Warn($"[CenoFsSharp][m_fQueueTask][Dispose][success,data:{m_lSQL.Count};reset:{m_uCount}]");
                        else Log.Instance.Warn($"[CenoFsSharp][m_fQueueTask][Dispose][warning,data:{m_lSQL.Count};reset:{m_uCount}]");
                    }
                    else Log.Instance.Warn($"[CenoFsSharp][m_fQueueTask][Dispose][warning,data:0]");
                }
            }
        }

        public static void m_fActivate()
        {
            lock (m_fQueueTask.m_oQueueTaskLocker)
            {
                try
                {
                    //查询出所有通道的最新的通道类型
                    List<call_channel_model> m_lAutoChannelList = new List<call_channel_model>(call_channel.GetList());
                    DateTime m_dtNow = DateTime.Now;

                    ///设置为开启状态
                    if (m_fQueueTask.m_bIsExit) m_fQueueTask.m_bIsExit = false;

                    //自动外呼队列唤醒
                    m_lThreadList.ForEach(x =>
                    {
                        int? m_uChannelType = m_lAutoChannelList.FirstOrDefault(q => q.ID == x.m_mChannelInfo.channel_id)?.ChType;
                        x.m_mChannelInfo.channel_type = m_uChannelType ?? -1;
                        x.m_pCreateTime = m_dtNow;
                        x.m_bTodayUse = true;
                        x.m_eEventWaitHandle.Set();
                    });

                    //所有缓存通道的通道类型更新
                    call_factory.channel_list.ForEach(x =>
                    {
                        int? m_uChannelType = m_lAutoChannelList.FirstOrDefault(q => q.ID == x.channel_id)?.ChType;
                        x.channel_type = m_uChannelType ?? -1;
                    });

                    //移除所有没有启动的队列
                    lock (m_lThreadList)
                    {
                        m_lThreadList.RemoveAll(q => q.m_bIsStart == false);
                    }

                    //查询出所有未启动的自动外呼通道队列
                    List<ChannelInfo> m_lNoStartAutoChannelList = (from t in call_factory.channel_list
                                                                   where !m_lThreadList.Select(q => q.m_mChannelInfo.channel_id).ToArray().Contains(t.channel_id)
                                                                   & t.channel_type == Special.AUTO
                                                                   select t).ToList();

                    //启动所有未启动的自动外呼通道队列
                    foreach (ChannelInfo m_mChannelInfo in m_lNoStartAutoChannelList)
                    {
                        CenoFsSharp.m_mThread entity = new m_mThread();
                        entity.m_eEventWaitHandle = new System.Threading.AutoResetEvent(false);
                        entity.m_mChannelInfo = m_mChannelInfo;
                        entity.m_bIsStart = true;
                        entity.m_tThread = new System.Threading.Thread(() =>
                        {
                            CenoFsSharp.m_fQueueTask m_fQueueTaskInstance = new CenoFsSharp.m_fQueueTask();
                            m_fQueueTaskInstance.m_fWork(entity);
                        });
                        entity.m_tThread.Name = $"{m_mChannelInfo.channel_number}";
                        CenoFsSharp.m_fQueueTask.m_lThreadList.Add(entity);
                        entity.m_tThread.Start();
                    }

                    Log.Instance.Warn($"[CenoFsSharp][m_fQueueTask][m_fActivate][all thread reset]");
                }
                catch (Exception ex)
                {
                    Log.Instance.Fail($"[CenoFsSharp][m_fQueueTask][m_fActivate][{ex.Message}]");
                }
            }
        }
    }
}
