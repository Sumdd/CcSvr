using CenoSipFactory;
using Core_v1;
using DB.Basic;
using DB.Model;
using log4net;
using NEventSocket;
using NEventSocket.FreeSwitch;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Model_v1;
using System.Reactive.Linq;
using Cmn_v1;

namespace CenoFsSharp
{
    public class InboundMain
    {
        #region ***变量
        private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static string ip;
        private static string port;
        private static string pwd;
        #endregion

        #region ***freeswitch 客户端
        public static async Task<InboundSocket> fs_cli(bool loop = true)
        {
            try
            {
                var client = await InboundSocket.Connect(ip, Convert.ToInt32(port), pwd);
                return client;
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][InboundMain][Start][Exception][{ex.Message}]");
                if (ex.Message.StartsWith("Timeout when trying to connect to") && loop)
                {
                    var client = await InboundMain.fs_cli(false);
                    return client;
                }
                //throw new Exception(ex.Message);
                return null;
            }
        }
        #endregion

        #region ***退出时,挂断所有通话
        public static async void all_kill()
        {
            try
            {
                var client = await InboundMain.fs_cli().ContinueWith(task =>
                {
                    if (task.IsCanceled)
                    {
                        Log.Instance.Error($"[CenoFsSharp][InboundMain][all_kill][Exception][InboundSocket cancel]");
                        return null;
                    }
                    else
                    {
                        return task.Result;
                    }
                });

                if (client == null)
                {
                    return;
                }

                await client.SendApi("fsctl hupall").ContinueWith(task =>
                {
                    try
                    {
                        if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][InboundMain][all_kill][SendApi cancel]");
                    }
                    catch
                    {
                        return;
                    }
                });

                await client.Exit().ContinueWith(task =>
                {
                    try
                    {
                        if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][InboundMain][all_kill][Exit cancel]");
                    }
                    catch
                    {
                        return;
                    }
                });

            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][InboundMain][all_kill][Exception][{ex.Message}]");
            }
        }
        #endregion

        #region ***通用Esl
        public static async Task<string> m_fCmnEsl(string m_sCmd)
        {
            string m_sEslResult = "-ERR No Response";
            try
            {
                var client = await InboundMain.fs_cli().ContinueWith(task =>
                {
                    if (task.IsCanceled)
                    {
                        Log.Instance.Error($"[CenoFsSharp][InboundMain][m_fCmnEsl][Exception][InboundSocket cancel]");
                        return null;
                    }
                    else
                    {
                        return task.Result;
                    }
                });

                if (client == null)
                {
                    return "-ERR Esl";
                }

                await client.SendApi(m_sCmd).ContinueWith(task =>
                {
                    try
                    {
                        if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][InboundMain][m_fCmnEsl][SendApi {m_sCmd} cancel]");
                        m_sEslResult = task?.Result?.BodyText;
                    }
                    catch
                    {
                        return;
                    }
                });

                await client.Exit().ContinueWith(task =>
                {
                    try
                    {
                        if (task.IsCanceled) Log.Instance.Error($"[CenoFsSharp][InboundMain][m_fCmnEsl][Exit cancel]");
                    }
                    catch
                    {
                        return;
                    }
                });

            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[CenoFsSharp][InboundMain][m_fCmnEsl][Exception][{ex.Message}]");
                m_sEslResult = $"-ERR {ex.Message}";
            }
            return m_sEslResult;
        }
        #endregion

        #region ***获取Esl参数
        public static void m_fGetEventSocketMemo()
        {
            var dt = DB.Basic.Call_ServerListUtil.GetSipServerInfo();
            if (dt.Rows.Count > 0)
            {
                ip = dt.Rows[0]["ServerIP"].ToString();
                port = dt.Rows[0]["ServerPort"].ToString();
                pwd = dt.Rows[0]["Password"].ToString();
                Log.Instance.Success($"[CenoFsSharp][InboundMain][Start][{ip}:{port}]");
            }
            else
            {
                Log.Instance.Fail($"[CenoFsSharp][InboundMain][Start][Missing SIP Server]");
                throw new Exception("Missing SIP Server");
            }
        }
        #endregion

        #region ***返回FreesSWITCHIPv4
        public static string FreesSWITCHIPv4
        {
            get
            {
                try
                {
                    if (InboundMain.ip != null && !string.IsNullOrWhiteSpace(InboundMain.ip))
                    {
                        return InboundMain.ip;
                    }
                }
                catch (Exception ex)
                {
                    string m_sIPv4 = Cmn_v1.Cmn.m_fRemoveSpace(DB.Basic.MySQLDBConnectionString.DB_Server);
                    if (!string.IsNullOrWhiteSpace(m_sIPv4))
                    {
                        return m_sIPv4;
                    }
                }
                return string.Empty;
            }
        }
        #endregion
    }
}
