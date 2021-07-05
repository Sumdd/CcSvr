using CenoCommon;
using CenoSipBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB.Basic;
using Core_v1;
using System.IO;
using CenoSipFactory;

namespace CenoServer
{
    internal class MainWhileDo
    {
        private static string m_sPrefix = m_fGetConsolePathString();
        internal static void MainStep()
        {
            string m_sTempPrefixCmd = string.Empty;

            while (!CmnParam.IsExit)
            {

                try
                {
                    var readStr = Console.ReadLine();
                    var yaStr = readStr;
                    readStr = readStr.ToLower();

                    switch (readStr)
                    {

                        #region 帮助
                        case "help":
                            string m_sHelpString = @"
  help `打印此信息`

  gateway `添加网关`

  adduser `添加用户`

  inboundtest [on|off] `内联模式测试`

  application [park|hold] `呼通用户后使用的app,暂时只有park与hold`

  ignore early media [on|off] `呼叫忽略早期媒体`

  a leg timeout [seconds(>=3)] `呼叫主叫超时时间`

  timeout [seconds(>10)] `呼叫超时时间`

  set rec [.wav|.mp3] `设置录音文件格式`

  web ws [on|off] `web电话websocket支持`

  log level [ALL(>=5)|DEBUG(5)|INFO(4)|WARN(3)|ERROR(2)|FATAL(1)|OFF(0)|查看当前日志级别(-1)] `设置日志级别`

  stime [HH:mm:ss] `自动拨号开始时间`

  etime [HH:mm:ss] `自动拨号结束时间`

  up interface [on|off] `使用自动拨号结果上报接口`

  playback [count(>=1)] `自动拨号播放次数`

  asr [on|off] `语音识别开关`

  task dial [count(>=1)] `自动拨号未接通的拨打次数`

  common reload `通用重新加载,暂时有如下内容`
    
    1.自动拨号TTS网络路径(兼容软交换分离):DialTaskTTSUrl
    2.自动拨号录音路径分离配置:DialTaskRecPath
    3.录音下载HTTP模式:DialTaskRecDownLoadHTTP
    4.自动外呼app:DialTaskApp
    5.自动外呼Tts提供方:DialTaskTTSProvider
    6.自动外呼Tts配置:DialTaskTTSSetting
    7.网页电话需要安全连接:WebWebSocketS
    8.备份录音路径:BackupRecords
    9.呼入回铃:CallMusic
   10.FreeSwitch所在目录:FreeSWITCHPath
   11.FreeSwitch网关写入Ua文件夹名:FreeSWITCHUaPath
   12.转码后最终扩展名:EndExt
   13.归属地表自动更新间隔参数:TaskUpdPhoneInterval
   14.归属地表自动更新HTTP路径:TaskUpdPhoneURL
   15.新生代续联HTTP接口:XxHttp
   16.是否开启追加独立服务中的共享号码,申请式:UseApply
   18.独立申请式出局Ua:ApiUa
   19.To拼接至主叫名称:AppendTo
   20.启用催收系统查询联系人姓名:UseHomeSearch
   21.催收系统数据源地址:HomeConnString
   22.通过催收系统查出来电人姓名的语句:HomeSelectString
   23.桥接App:BridgeApp
   24.设置Bridge失败音0否1设置2加D标识:BridgeFailAudio

  case play [count(>=-1)] `来电无法接听原因播报次数,-1表示支持早期媒体`

  case answer [uuid_answer|uuid_pre_answer] `来电无法接听原因应答方式`

  tts [text] `文字转语音`

  dtmf [inbound|clientSignal|bothSignal] `dtmf发送方式`

  login [null|hs|yx] `设置客户端登陆界面`

  share `共享号码重加载`

  share http [on|off] `共享文件HTTP`

  auto update share second [0|>=60] `自动更新号码池信息时间;0:不自动更新`

  ip show where [on|off] `IP话机显示归属地`

  is query uuid [on|off] `是否查询录音ID`

  call rule [1|2|3] `代数和呼入规则:1.查拨号限制;2.查通话记录`

  show `查看状态`
    
  send esl [api] `转发消息至freeswitch`

  test redis [count] `测试Redis强度`

  send redis [keys *|del *|set|nx|getall *|getalldata|key] `Redis命令`

  json `测试json转换是否会出问题`

  cpu `查询cpu编码`

  auto call stop `自动外呼停止,所有未拨打数据退出队列`

  auto call start `自动外呼启动`

  sip second [0|>=60] `坐席注册状态心跳秒,0为不启用`

  ringback [on|off] `是否开启180回铃`

  exit `退出服务端`

";
                            Console.Write(m_sHelpString);

                            break;
                        #endregion

                        #region 添加网关
                        case "gateway":
                            gatewaylib.add_gateway();
                            break;
                        #endregion

                        #region 添加用户
                        case "adduser":
                            AddUsers.Add_Users();
                            break;
                        #endregion

                        #region 退出
                        case "exit":
                            CmnParam.IsExit = true;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][cmd exit start]");
                            CenoFsSharp.m_fQueueTask.Dispose();
                            CenoFsSharp.InboundMain.all_kill();
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][cmd exit][in ws stop]");
                            WebSocket_v1.InWebSocketMain.Stop();
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][cmd exit][web ws stop]");
                            WebSocket_v1.WebWebSocketMain.Stop();
                            Log.Instance.Warn($"[CenoServer][MainWhileDo][MainStep][cmd exit][await 3 seconds,for deal with last works]");
                            System.Threading.Thread.Sleep(3 * 1000);
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][cmd exit end]");
                            break;
                        #endregion

                        #region 开启内呼测试
                        case "inboundtest on":
                            Call_ParamUtil.InboundTest = true;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][inboundtest true]");
                            break;
                        #endregion

                        #region 取消内呼测试
                        case "inboundtest off":
                            Call_ParamUtil.InboundTest = false;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][inboundtest false]");
                            break;
                        #endregion

                        #region api,park
                        case "application park":
                            Call_ParamUtil._application = "park";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][application park]");
                            break;
                        #endregion

                        #region api,hold
                        case "application hold":
                            Call_ParamUtil._application = "hold";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][application hold]");
                            break;
                        #endregion

                        #region 开启忽略早期媒体
                        case "ignore early media on":
                            Call_ParamUtil.__ignore_early_media = true;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][ignore early media on]");
                            //Call_ParamUtil.IEM_Do = "1";
                            //Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][有早期媒体时,将早期媒体放入录音,通话以早媒开始计算]");
                            break;
                        #endregion

                        #region 关闭忽略早期媒体
                        case "ignore early media off":
                            Call_ParamUtil.__ignore_early_media = false;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][ignore early media off]");
                            break;
                        #endregion

                        #region 设置录音文件名称
                        case "set rec .wav":
                            Call_ParamUtil._rec_t = ".wav";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set rec .wav]");
                            break;
                        #endregion

                        #region 设置录音文件名称
                        case "set rec .mp3":
                            Call_ParamUtil._rec_t = ".mp3";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set rec .mp3]");
                            break;
                        #endregion

                        #region 启用多号码已经呼叫限制
                        case "set multi phone on":
                            Call_ParamUtil.IsMultiPhone = true;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set multi phone on]");
                            break;
                        #endregion

                        #region 禁用多号码已经呼叫限制
                        case "set multi phone off":
                            Call_ParamUtil.IsMultiPhone = false;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set multi phone off]");
                            break;
                        #endregion

                        #region 开启WebWebSocket
                        case "web ws on":
                            Call_ParamUtil.Update("IsUseWebWebSocket", "1");
                            WebSocket_v1.WebWebSocketMain.Stop();
                            WebSocket_v1.WebWebSocketMain.Start();
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][web ws on]");
                            break;
                        #endregion

                        #region 关闭WebWebSocket
                        case "web ws off":
                            Call_ParamUtil.Update("IsUseWebWebSocket", "0");
                            WebSocket_v1.WebWebSocketMain.Stop();
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][web ws off]");
                            break;
                        #endregion

                        #region 语音识别开
                        case "asr on":
                            Call_ParamUtil.m_bIsDialTaskAsr = true;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][asr on]");
                            break;
                        #endregion

                        #region 语音识别开
                        case "asr off":
                            Call_ParamUtil.m_bIsDialTaskAsr = false;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][asr off]");
                            break;
                        #endregion

                        #region 使用自动拨号结果上报接口
                        case "up interface on":
                            Call_ParamUtil.m_bUseDialTaskInterface = true;
                            Call_ParamUtil._m_sUpInterface = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][up interface on:{Call_ParamUtil.m_sUpInterface}]");
                            break;
                        #endregion

                        #region 不使用自动拨号结果上报接口
                        case "up interface off":
                            Call_ParamUtil.m_bUseDialTaskInterface = false;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][up interface off]");
                            break;
                        #endregion

                        #region 通用重新加载
                        case "common reload":
                            Call_ParamUtil._m_sTTSUrl = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][DialTaskTTSUrl:{Call_ParamUtil.m_sTTSUrl}]");
                            Call_ParamUtil._m_sDialTaskRecPath = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][DialTaskRecPath:{Call_ParamUtil.m_sDialTaskRecPath}]");
                            Call_ParamUtil._m_sDialTaskRecDownLoadHTTP = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][DialTaskRecDownLoadHTTP:{Call_ParamUtil.m_sDialTaskRecDownLoadHTTP}]");
                            Call_ParamUtil._m_sDialTaskApp = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][DialTaskApp:{Call_ParamUtil.m_sDialTaskApp}]");
                            Call_ParamUtil._m_sDialTaskTTSProvider = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][DialTaskTTSProvider:{Call_ParamUtil.m_sDialTaskTTSProvider}]");
                            Call_ParamUtil._m_sDialTaskTTSSetting = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][DialTaskTTSSetting:{Call_ParamUtil.m_sDialTaskTTSSetting}]");
                            Call_ParamUtil._m_sWebWebSocketS = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][WebWebSocketS:{Call_ParamUtil.m_sWebWebSocketS}]");
                            Call_ParamUtil._m_sBackupRecords = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][BackupRecords:{Call_ParamUtil.m_sBackupRecords}]");
                            Call_ParamUtil._m_sCallMusic = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][CallMusic:{Call_ParamUtil.m_sCallMusic}]");
                            Call_ParamUtil._m_sFreeSWITCHPath = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][FreeSWITCHPath:{Call_ParamUtil.m_sFreeSWITCHPath}]");
                            Call_ParamUtil._m_sFreeSWITCHUaPath = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][FreeSWITCHUaPath:{Call_ParamUtil.m_sFreeSWITCHUaPath}]");
                            Call_ParamUtil._m_sEndExt = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][EndExt:{Call_ParamUtil.m_sEndExt}]");
                            Call_ParamUtil._m_uTaskUpdPhoneInterval = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][TaskUpdPhoneInterval:{Call_ParamUtil.m_uTaskUpdPhoneInterval}]");
                            Call_ParamUtil._m_sTaskUpdPhoneURL = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][TaskUpdPhoneURL:{Call_ParamUtil.m_sTaskUpdPhoneURL}]");
                            Call_ParamUtil._m_sXxHttp = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][XxHttp:{Call_ParamUtil.m_sXxHttp}]");
                            Call_ParamUtil._m_bUseApply = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][UseApply:{Call_ParamUtil.m_bUseApply}]");
                            Call_ParamUtil._m_sApiUa = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][ApiUa:{Call_ParamUtil.m_sApiUa}]");
                            Call_ParamUtil._m_uAppendTo = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][AppendTo:{Call_ParamUtil.m_uAppendTo}]");
                            Call_ParamUtil._m_bUseHomeSearch = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][UseHomeSearch:{Call_ParamUtil.m_bUseHomeSearch}]");
                            Call_ParamUtil._m_sHomeConnString = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][HomeConnString:{Call_ParamUtil.m_sHomeConnString}]");
                            Call_ParamUtil._m_sHomeSelectString = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][HomeSelectString:{Call_ParamUtil.m_sHomeSelectString}]");
                            Call_ParamUtil._m_sBridgeApp = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][BridgeApp:{Call_ParamUtil.m_sBridgeApp}]");
                            Call_ParamUtil._m_uBridgeFailAudio = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][BridgeFailAudio:{Call_ParamUtil.m_uBridgeFailAudio}]");
                            break;
                        #endregion

                        #region 200应答
                        case "case answer uuid_answer":
                            Call_ParamUtil.m_sCaseAnswer = "uuid_answer";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][case answer uuid_answer]");
                            break;
                        #endregion

                        #region 183应答
                        case "case answer uuid_pre_answer":
                            Call_ParamUtil.m_sCaseAnswer = "uuid_pre_answer";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][case answer uuid_pre_answer]");
                            break;
                        #endregion

                        #region dtmf发送方式
                        case "dtmf inbound":
                            Call_ParamUtil.m_sDTMFSendMethod = Call_ParamUtil.inbound;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][dtmf inbound]");
                            break;
                        case "dtmf clientsignal":
                            Call_ParamUtil.m_sDTMFSendMethod = Call_ParamUtil.clientSignal;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][dtmf clientSignal]");
                            break;
                        case "dtmf bothsignal":
                            Call_ParamUtil.m_sDTMFSendMethod = Call_ParamUtil.bothSignal;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][dtmf bothSignal]");
                            break;
                        #endregion

                        #region 登陆方式
                        case "login null":
                            Call_ParamUtil.m_sLoginType = "";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][login null]");
                            break;
                        case "login hs":
                            Call_ParamUtil.m_sLoginType = "hs";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][login hs]");
                            break;
                        case "login yx":
                            Call_ParamUtil.m_sLoginType = "yx";
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][login yx]");
                            break;
                        #endregion

                        #region ***共享号码重加载
                        case "share":
                            Call_ParamUtil._m_uShareNumSetting = null;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][share reload start...]");
                            WebSocket_v1.InWebSocketMain.m_fLoadShare(false);
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][share reload end]");
                            break;
                        #endregion

                        #region ***共享文件夹HTTP
                        case "share http on":
                            Call_ParamUtil._m_sHttpShareUrl = null;
                            Call_ParamUtil.m_bIsUseHttpShare = true;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][share http on,{Call_ParamUtil.m_sHttpShareUrl}]");
                            break;
                        case "share http off":
                            Call_ParamUtil._m_sHttpShareUrl = null;
                            Call_ParamUtil.m_bIsUseHttpShare = false;
                            Log.Instance.Warn($"[CenoServer][MainWhileDo][MainStep][share http off,{Call_ParamUtil.m_sHttpShareUrl}]");
                            break;
                        #endregion

                        #region IP话机显示归属地开
                        case "ip show where on":
                            Call_ParamUtil.m_bIsIpShowWhere = true;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][ip show where on]");
                            break;
                        #endregion

                        #region IP话机显示归属地关
                        case "ip show where off":
                            Call_ParamUtil.m_bIsIpShowWhere = false;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][ip show where off]");
                            break;
                        #endregion

                        #region IP话机显示归属地开
                        case "is query uuid on":
                            Call_ParamUtil.m_bIsQueryRecUUID = true;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][is query uuid on]");
                            break;
                        #endregion

                        #region IP话机显示归属地关
                        case "is query uuid off":
                            Call_ParamUtil.m_bIsQueryRecUUID = false;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][is query uuid off]");
                            break;
                        #endregion

                        #region 呼入规则
                        case "call rule 1":
                            Call_ParamUtil.m_uCallRule = 1;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][call rule 1]");
                            break;
                        case "call rule 2":
                            Call_ParamUtil.m_uCallRule = 2;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][call rule 2]");
                            break;
                        case "call rule 3":
                            Call_ParamUtil.m_uCallRule = 3;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][call rule 3]");
                            break;
                        #endregion

                        #region 查看状态
                        case "show":
                            int? m_uCount = call_factory.agent_list?.Where(x => x?.LoginState == true)?.Count();
                            Console.WriteLine($"online:{m_uCount}");
                            break;
                        #endregion

                        #region ***测试JSON转换是否出问题
                        case "json":
                            {
                                if (Cmn_v1.Cmn.m_dfJSON != null) Cmn_v1.Cmn.m_dfJSON("");
                            }
                            break;
                        #endregion

                        #region ***查询CPU
                        case "cpu":
                            if (Cmn_v1.Cmn.m_dfGetCPU != null) Cmn_v1.Cmn.m_dfGetCPU();
                            break;
                        #endregion

                        #region ***自动外呼
                        case "auto call stop":
                            CenoFsSharp.m_fQueueTask.Dispose(true);
                            break;
                        case "auto call start":
                            CenoFsSharp.m_fQueueTask.m_fActivate();
                            break;
                        #endregion

                        #region 180回铃
                        case "ringback on":
                            Call_ParamUtil.m_uUseRingBack = 1;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][ringback on]");
                            break;
                        case "ringback off":
                            Call_ParamUtil.m_uUseRingBack = 0;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][ringback off]");
                            break;
                        #endregion

                        default:
                            break;
                    }

                    #region 设置呼叫主叫超时时间
                    if (readStr.StartsWith("a leg timeout "))
                    {
                        int m_uSeconds = 15;
                        string m_sSecondsString = readStr.Substring(13);
                        int.TryParse(m_sSecondsString, out m_uSeconds);
                        if (m_uSeconds >= 3)
                        {
                            Call_ParamUtil.ALegTimeoutSeconds = m_uSeconds;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set a leg timeout seconds is {m_uSeconds}]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][a leg timeout seconds must bigger]");
                        }
                    }
                    #endregion

                    #region 设置超时时间
                    if (readStr.StartsWith("timeout "))
                    {
                        int m_uSeconds = 20;
                        string m_sSecondsString = readStr.Substring(7);
                        int.TryParse(m_sSecondsString, out m_uSeconds);
                        if (m_uSeconds >= 10)
                        {
                            Call_ParamUtil.__timeout_seconds = m_uSeconds;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set timeout seconds is {m_uSeconds}]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][timeout seconds must bigger]");
                        }
                    }
                    #endregion

                    #region 设置日志级别
                    if (readStr.StartsWith("log level "))
                    {
                        int m_uLevel = -1;
                        string m_sLevel = readStr.Substring(9);
                        int.TryParse(m_sLevel, out m_uLevel);
                        if (m_uLevel <= -1)
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][now log level {Log.Instance.GetLogLevel()},please set [ALL(>=5)|DEBUG(5)|INFO(4)|WARN(3)|ERROR(2)|FATAL(1)|OFF(0)]]");
                        }
                        else
                        {
                            Log.Instance.SetLogLevel(m_uLevel);
                        }
                    }
                    #endregion

                    #region 设置自动拨号开始时间
                    if (readStr.StartsWith("stime "))
                    {
                        string m_sTime = readStr.Substring(5);
                        if (!string.IsNullOrWhiteSpace(m_sTime))
                        {
                            DateTime m_pDateTime = DateTime.Now;
                            bool m_bBool = DateTime.TryParse($"2000-01-01 {m_sTime}", out m_pDateTime);
                            if (m_bBool)
                            {
                                CenoSipFactory.ParamLib.AutoDIalStartTime = m_pDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                                Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][auto start time set success:{CenoSipFactory.ParamLib.AutoDIalStartTime}]");
                            }
                            else
                            {
                                Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][auto start time set error]");
                            }
                        }
                    }
                    #endregion

                    #region 设置自动拨号结束时间
                    if (readStr.StartsWith("etime "))
                    {
                        string m_sTime = readStr.Substring(5);
                        if (!string.IsNullOrWhiteSpace(m_sTime))
                        {
                            DateTime m_pDateTime = DateTime.Now;
                            bool m_bBool = DateTime.TryParse($"2000-01-01 {m_sTime}", out m_pDateTime);
                            if (m_bBool)
                            {
                                CenoSipFactory.ParamLib.AutoDialEndTime = m_pDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                                Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][auto end time set success:{CenoSipFactory.ParamLib.AutoDialEndTime}]");
                            }
                            else
                            {
                                Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][auto end time set error]");
                            }
                        }
                    }
                    #endregion

                    #region 自动播号播放次数
                    m_sTempPrefixCmd = "playback ";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        int m_uInt = 1;
                        string m_sInt = readStr.Substring(m_sTempPrefixCmd.Length);
                        int.TryParse(m_sInt, out m_uInt);
                        if (m_uInt >= 1)
                        {
                            Call_ParamUtil.m_uPlayLoops = m_uInt;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set playback count is {m_uInt}]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][playback count must bigger]");
                        }
                    }
                    #endregion

                    #region 自动拨号未接通的拨打次数
                    m_sTempPrefixCmd = "task dial ";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        int m_uInt = 1;
                        string m_sInt = readStr.Substring(m_sTempPrefixCmd.Length);
                        int.TryParse(m_sInt, out m_uInt);
                        if (m_uInt >= 1)
                        {
                            Call_ParamUtil.m_uDialCount = m_uInt;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set task dial count is {m_uInt}]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][task dial count must bigger]");
                        }
                    }
                    #endregion

                    #region 来电无法接听原因播报次数
                    m_sTempPrefixCmd = "case play ";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        int m_uInt = 0;
                        string m_sInt = readStr.Substring(m_sTempPrefixCmd.Length);
                        int.TryParse(m_sInt, out m_uInt);
                        if (m_uInt >= -1)
                        {
                            Call_ParamUtil.m_uCasePlayLoops = m_uInt;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set case play count is {m_uInt}]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][case play count must bigger]");
                        }
                    }
                    #endregion

                    #region 文字转语音
                    m_sTempPrefixCmd = "tts";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        string m_sTts = readStr.Substring(m_sTempPrefixCmd.Length);
                        string m_sPath = new CenoFsSharp.CreateSound().CreSound_bak(m_sTts, $"{DateTime.Now.ToString("yyyyMMddHHmmss_")}{Guid.NewGuid()}.wav", Call_ParamUtil.m_iDialTaskTTSSetting);
                        Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][tts path:{m_sPath}]");
                    }
                    #endregion

                    #region 自动更新号码池信息时间
                    m_sTempPrefixCmd = "auto update share second ";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        int m_uInt = 0;
                        string m_sInt = readStr.Substring(m_sTempPrefixCmd.Length);
                        int.TryParse(m_sInt, out m_uInt);
                        if (m_uInt >= 60 || m_uInt == 0)
                        {
                            Call_ParamUtil.m_uAutoUpdateShareSeconds = m_uInt;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][auto update share second is {m_uInt}]");
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][auto update share second must bigger or 0]");
                        }
                    }
                    #endregion

                    #region 通用发送Esl
                    m_sTempPrefixCmd = "send esl ";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        string m_sCmd = readStr.Substring(m_sTempPrefixCmd.Length);
                        string m_sEslResult = "-ERR No Response";

                        Task.Run(async () =>
                        {
                            m_sEslResult = await CenoFsSharp.InboundMain.m_fCmnEsl(m_sCmd);

                        }).Wait();

                        Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][{m_sCmd}]");
                        Console.WriteLine($"{m_sEslResult}");
                    }
                    #endregion

                    #region ***测试Redis强度
                    m_sTempPrefixCmd = "test redis ";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        int m_uInt = 0;
                        string m_sInt = readStr.Substring(m_sTempPrefixCmd.Length);
                        int.TryParse(m_sInt, out m_uInt);
                        if (m_uInt > 0)
                        {
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][set test redis count is {m_uInt}]");
                            Redis2.m_fTestRedis(m_uInt);
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep][test redis count must bigger]");
                        }
                    }
                    #endregion

                    #region 通用发送Redis
                    m_sTempPrefixCmd = "send redis ";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        string m_sCmd = yaStr.Substring(m_sTempPrefixCmd.Length);
                        Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][{m_sCmd}]");
                        Redis2.m_fCmdRedis(m_sCmd);
                    }
                    #endregion

                    #region 自动更新号码池信息时间
                    m_sTempPrefixCmd = "sip second ";
                    if (readStr.StartsWith(m_sTempPrefixCmd))
                    {
                        int m_uInt = 0;
                        string m_sInt = readStr.Substring(m_sTempPrefixCmd.Length);
                        int.TryParse(m_sInt, out m_uInt);
                        if (m_uInt >= 60 || m_uInt == 0)
                        {
                            Call_ParamUtil.m_uUaRegHeart = m_uInt;
                            Log.Instance.Success($"[CenoServer][MainWhileDo][MainStep][sip second is {m_uInt},restart sip timer]");
                            intilizate_services.m_fReStartSipTimer();
                        }
                        else
                        {
                            Log.Instance.Fail($"[CenoServer][MainWhileDo][MainStep]sip second must bigger or 0]");
                        }
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Log.Instance.Error($"[CenoServer][MainWhileDo][MainStep][Exception][{ex.Message}]");
                }
                finally
                {
                    Console.Write(m_sPrefix);
                }
            }
        }

        private static string m_fGetConsolePathString()
        {
            try
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).TrimEnd(new char[] { '/', '\\' }) + ">:";
            }
            catch
            {
                return "Console>:";
            }
        }
    }
}
