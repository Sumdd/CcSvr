using Core_v1;
using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Model_v1;
using CenoSocket;
using CenoFsSharp;

namespace WebSocket_v1
{
    internal class m_cInWebSocketWebApiDo
    {
        internal static void MainStep(IWebSocketConnection m_pWebSocket, string m_sMessage)
        {
            try
            {
                JObject m_pJObject = JObject.Parse(m_sMessage);
                string m_sUse = m_pJObject["m_sUse"].ToString();
                switch (m_sUse)
                {
                    case m_mWebSocketJsonCmd._m_sLogin:
                        #region 登陆
                        {
                            try
                            {
                                //可以加一个登录回复
                                string m_sID = m_pJObject["m_oObject"]["m_sID"].ToString();
                                if (m_sID == "WebApi")
                                {
                                    Log.Instance.Success($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][{m_sID}:login success:{m_pWebSocket.ConnectionInfo.ClientIpAddress},{m_pWebSocket.ConnectionInfo.ClientPort},{m_sMessage}]");
                                }
                                else
                                {
                                    Log.Instance.Warn($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][{m_sID}:guest login:{m_pWebSocket.ConnectionInfo.ClientIpAddress},{m_pWebSocket.ConnectionInfo.ClientPort},{m_sMessage}]");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][{m_pWebSocket.ConnectionInfo.ClientIpAddress},{m_pWebSocket.ConnectionInfo.ClientPort},{m_sMessage}:{ex.Message}]");
                            }
                        }
                        #endregion
                        break;
                    case m_mWebSocketJsonCmd._m_sDialTask:
                        #region 拨号任务
                        {
                            string m_sUUID = string.Empty;
                            try
                            {
                                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();
                                string m_sSendMessage = m_pJObject["m_oObject"]["m_sSendMessage"].ToString();

                                _m_mWebSocketJson.m_sUse = m_mWebSocketJsonCmd._m_sDialTask;
                                //加入队列中
                                if (InWebSocketMain.m_fDialTask != null)
                                {
                                    _m_mWebSocketJson = InWebSocketMain.m_fDialTask(m_sSendMessage, m_mWebSocketJsonCmd._m_sDialTask, m_sUUID);
                                }
                                else
                                {
                                    _m_mWebSocketJson.m_oObject = new
                                    {
                                        m_sStatus = -1,
                                        m_sUUID = m_sUUID,
                                        m_sResultMessage = "未设置拨号任务委托"
                                    };
                                }
                                string m_sWebSocketJson = JsonConvert.SerializeObject(_m_mWebSocketJson);
                                m_pWebSocket.Send(m_sWebSocketJson);
                                Log.Instance.Success($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][{m_sUUID} reply:{m_sWebSocketJson}]");
                            }
                            catch (Exception ex)
                            {
                                if (!string.IsNullOrWhiteSpace(m_sUUID))
                                {
                                    m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                    _m_mWebSocketJson.m_sUse = m_mWebSocketJsonCmd._m_sDialTask;
                                    _m_mWebSocketJson.m_oObject = new
                                    {
                                        m_sStatus = -1,
                                        m_sUUID = m_sUUID,
                                        m_sResultMessage = ex.Message
                                    };
                                    m_pWebSocket.Send(JsonConvert.SerializeObject(_m_mWebSocketJson));
                                    Log.Instance.Success($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][{m_sUUID} reply:{ex.Message}]");
                                }
                                Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][{m_pWebSocket.ConnectionInfo.ClientIpAddress},{m_pWebSocket.ConnectionInfo.ClientPort},{m_sMessage}:{ex.Message}]");
                            }
                        }
                        #endregion
                        break;
                    case m_cIpCmd._m_sIpDial://后续将此内容直接整合入拨号,因为都是同一个逻辑,维护俩份过于麻烦
                        #region ***IP话机拨号
                        {
                            //交互唯一标识
                            string m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();
                            //字符串转对象
                            JObject _m_pJObject = JObject.Parse(m_pJObject["m_oObject"]["m_sSendMessage"].ToString());
                            //登录名,这里一是登录名好记,二是方便使用下挂的电话号码
                            string m_sLoginName = _m_pJObject.GetValue("m_sLoginName").ToString();
                            //录音唯一标识,加字段,保证准确性,delete
                            //string m_sRecUUID = m_pJObject.GetValue("m_sRecUUID").ToString();
                            //要拨打的手机号码
                            string m_sPhoneNumber = _m_pJObject.GetValue("m_sPhoneNumber").ToString();
                            //执行IP话机拨号
                            CenoSocket.m_cIp.m_fExecuteDial(m_pWebSocket, m_sUUID, m_sLoginName, m_sPhoneNumber, string.Empty, Special.Common);
                        }
                        #endregion
                        break;
                    case m_cIpCmd._m_sIpDialv2://后续将此内容直接整合入拨号,因为都是同一个逻辑,维护俩份过于麻烦
                    case m_cIpCmd._m_sIpDialv3:
                        #region ***IP话机拨号版本2,IP话机拨号版本3
                        {
                            //交互唯一标识
                            string m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();
                            //字符串转对象
                            JObject _m_pJObject = JObject.Parse(m_pJObject["m_oObject"]["m_sSendMessage"].ToString());
                            //登录名,这里一是登录名好记,二是方便使用下挂的电话号码
                            string m_sLoginName = _m_pJObject.GetValue("m_sLoginName").ToString();
                            //要拨打的手机号码
                            string m_sPhoneNumber = _m_pJObject.GetValue("m_sPhoneNumber").ToString();
                            //主叫号码
                            string m_sCaller = _m_pJObject.GetValue("m_sCaller").ToString();
                            //号码类别
                            string m_sNumberType = _m_pJObject.GetValue("m_sNumberType").ToString();

                            ///<![CDATA[
                            /// 追加拨号强制问题
                            /// ]]>

                            //号码强制
                            int m_uMustNbr = 0;
                            try
                            {
                                if (_m_pJObject.ContainsKey("m_uMustNbr"))
                                {
                                    m_uMustNbr = Convert.ToInt32(_m_pJObject.GetValue("m_uMustNbr").ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][Get m_uMustNbr:{ex.Message}]");
                            }

                            #region IP话机拨号版本3
                            int m_uDescMode = 0;///脱敏模式
                            int m_uDecryptMode = 0;///解密模式
                            if (m_sUse == m_cIpCmd._m_sIpDialv3)
                            {
                                try
                                {
                                    if (_m_pJObject.ContainsKey("m_uDescMode"))
                                    {
                                        m_uDescMode = Convert.ToInt32(_m_pJObject.GetValue("m_uDescMode").ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][Get m_uDescMode:{ex.Message}]");
                                }
                                try
                                {
                                    if (_m_pJObject.ContainsKey("m_uDecryptMode"))
                                    {
                                        m_uDecryptMode = Convert.ToInt32(_m_pJObject.GetValue("m_uDecryptMode").ToString());
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][Get m_uDecryptMode:{ex.Message}]");
                                }
                            }
                            #endregion

                            //执行IP话机拨号
                            CenoSocket.m_cIp.m_fExecuteDial(m_pWebSocket, m_sUUID, m_sLoginName, m_sPhoneNumber, m_sCaller, m_sNumberType, m_uMustNbr, m_uDescMode, m_uDecryptMode);
                        }
                        #endregion
                        break;
                    case m_cIpCmd._m_sGetShare:
                        #region ***获取共享号码
                        {
                            //交互唯一标识
                            string m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();
                            //获取共享号码
                            List<share_number> m_lShareNumber = Redis2.m_fGetShareNumberList();
                            if (m_lShareNumber == null) m_lShareNumber = new List<share_number>();
                            m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                            _m_mWebSocketJson.m_sUse = m_cIpCmd._m_sGetShare;
                            _m_mWebSocketJson.m_oObject = new
                            {
                                m_sStatus = 0,
                                m_sUUID = m_sUUID,
                                m_sResultMessage = JsonConvert.SerializeObject(m_lShareNumber)
                            };
                            //回复消息
                            m_pWebSocket.Send(JsonConvert.SerializeObject(_m_mWebSocketJson));
                        }
                        #endregion
                        break;
                    case m_cIpCmd._m_sGetApply:
                        #region ***获取申请式号码
                        {
                            string m_sUUID = string.Empty;
                            try
                            {
                                ///记录一下时间
                                DateTime m_dtNow = DateTime.Now;
                                //交互唯一标识
                                m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();
                                //字符串转对象
                                JObject _m_pJObject = JObject.Parse(m_pJObject["m_oObject"]["m_sSendMessage"].ToString());
                                ///IP
                                string m_sIP = _m_pJObject.GetValue("m_sIP").ToString();
                                ///登录名
                                string m_sLoginName = _m_pJObject.GetValue("m_sLoginName").ToString();
                                ///外呼号码
                                string m_sPhoneNumber = _m_pJObject.GetValue("m_sPhoneNumber").ToString();
                                ///MD5
                                string m_sMD5 = _m_pJObject.GetValue("m_sMD5").ToString();
                                ///支持跨服务器拨打,可固定号码
                                Model_v1.AddRecByRec m_pAddRecByRec = DB.Basic.m_fDialLimit.m_fGetAgentByLoginName(m_sIP, m_sLoginName);

                                if (m_pAddRecByRec == null)
                                    throw new Exception("无坐席信息");

                                if (string.IsNullOrWhiteSpace(m_pAddRecByRec.UAID))
                                    throw new Exception("无Ua");

                                //获取共享号码
                                int m_sStatus = -1;
                                string m_sErrMsg = "Err未知";

                                ///绑定号码,如果有绑定的号码,直接使用而不进行数据库的查询
                                bool m_bBind = false;
                                List<string> m_lNumber = new List<string>();
                                string m_sBindNumber = string.Empty;
                                if (_m_pJObject.ContainsKey("m_sBindNumber"))
                                {
                                    m_sBindNumber = _m_pJObject.GetValue("m_sBindNumber").ToString();
                                }
                                if (!string.IsNullOrWhiteSpace(m_sBindNumber))
                                {
                                    m_bBind = true;
                                    m_lNumber.Add(m_sBindNumber);
                                }
                                else
                                {
                                    m_lNumber = DB.Basic.m_fDialLimit.m_fXxUse(m_sIP, m_sLoginName);
                                }

                                ///申请号码
                                share_number m_pShareNumber = Redis2.m_fApplyXx(m_sIP, m_pAddRecByRec.UAID, m_pAddRecByRec.m_uAgentID, m_pAddRecByRec.m_uChannelID, DB.Basic.Call_ParamUtil.m_uShareNumSetting, m_bBind, m_lNumber, out m_sStatus, out m_sErrMsg);

                                ///优先使用个性化的续联接口
                                string m_sXxHttp = DB.Basic.Call_ParamUtil.m_sXxHttp;
                                if (!string.IsNullOrWhiteSpace(m_pShareNumber?.XxHttp))
                                {
                                    m_sXxHttp = m_pShareNumber?.XxHttp;
                                    Log.Instance.Warn($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][special XxHttp:{m_sXxHttp}]");
                                }
                                else if (!string.IsNullOrWhiteSpace(m_sXxHttp))
                                    Log.Instance.Warn($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][default XxHttp:{m_sXxHttp}]");
                                else
                                    throw new Exception("未配置续联接口");

                                #region ***续联电话呼出
                                ///直接拨号,如果提示成功即可返回成功
                                bool m_bResetNow = false;
                                if (m_pShareNumber != null)
                                {
                                    try
                                    {
                                        string m_sQueryString = $"queryString={{\"agentId\":\"{m_pShareNumber.xxUa}\",\"number\":\"{m_sPhoneNumber}\"}}";
                                        string m_sResult = string.Empty;

                                        ///走MD5方式
                                        if (m_sMD5 == "MD5")
                                        {
                                            m_sResult = m_cHttp.m_fPOST($"{m_sXxHttp}/Home/F_5MD5CALL", m_sQueryString);
                                        }
                                        else
                                        {
                                            m_sResult = m_cHttp.m_fPOST($"{m_sXxHttp}/Home/F_5CALL", m_sQueryString);
                                        }

                                        Log.Instance.Debug(m_sResult);
                                        Newtonsoft.Json.Linq.JObject m_pJObj = Newtonsoft.Json.Linq.JObject.Parse(m_sResult);
                                        int m_uStatus = Convert.ToInt32(m_pJObj.GetValue("status")?.ToString());
                                        if (m_uStatus == 0)
                                        {
                                            m_bResetNow = false;
                                            m_sStatus = 0;
                                        }
                                        else
                                        {
                                            ///判断代码是否为未登录,如果是,则先登录再拨打
                                            if (m_pJObj.ContainsKey("data") && m_pJObj["data"].ToString() == "010")
                                            {
                                                ///首先登录
                                                string m_sLoginMsg = string.Empty;
                                                if (DB.Basic.m_fDialLimit.m_fXxLogin(m_sXxHttp, m_pShareNumber.xxUa, m_pShareNumber.xxPwd, out m_sLoginMsg))
                                                {
                                                    m_bResetNow = true;
                                                    m_uStatus = -1;
                                                    m_sErrMsg = $"ErrApi:未登录;已自动登录,请重拨!";
                                                }
                                                else
                                                {
                                                    m_bResetNow = true;
                                                    m_uStatus = -1;
                                                    m_sErrMsg = $"ErrApi:未登录;自动登录失败:{m_sLoginMsg};请重试!";
                                                }
                                            }
                                            else
                                            {
                                                m_bResetNow = true;
                                                m_uStatus = -1;
                                                m_sErrMsg = $"ErrApi:{m_pJObj.GetValue("msg")?.ToString()}";
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        m_bResetNow = true;
                                        Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][m_fPOST][Exception][{ex.Message}]");
                                        m_sStatus = -1;
                                        m_sErrMsg = $"Err{ex.Message}";
                                    }

                                    ///是否需要延时回发
                                    if (m_bResetNow)
                                    {
                                        Redis2.m_fResetShareNumber(m_pShareNumber.agentID, m_pShareNumber, string.Empty, string.Empty, m_bResetNow);
                                    }
                                    else
                                    {
                                        ///线程
                                        new System.Threading.Thread(new System.Threading.ThreadStart(() =>
                                        {
                                            try
                                            {
                                                while (true)
                                                {
                                                    ///10秒钟后无变化可以直接开放即可
                                                    if (((TimeSpan)(DateTime.Now - m_dtNow)).TotalSeconds > 10)
                                                    {
                                                        ///查看状态,如果不是TALKING,直接回发即可,逻辑已经呈现
                                                        Redis2.m_fResetShareNumber(m_pShareNumber.agentID, m_pShareNumber, string.Empty, string.Empty, m_bResetNow);
                                                        break;
                                                    }
                                                    System.Threading.Thread.Sleep(100);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Thread][Exception][{ex.Message}]");
                                            }

                                        })).Start();
                                    }
                                }
                                #endregion

                                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                _m_mWebSocketJson.m_sUse = m_cIpCmd._m_sGetApply;
                                _m_mWebSocketJson.m_oObject = new
                                {
                                    m_sStatus = m_sStatus,
                                    m_sUUID = m_sUUID,
                                    //参数字符串
                                    m_sResultMessage = new
                                    {
                                        m_sErrMsg = m_sErrMsg,
                                        m_pShareNumber = m_pShareNumber == null ? null : JsonConvert.SerializeObject(m_pShareNumber)
                                    }
                                };
                                //回复消息
                                m_pWebSocket.Send(JsonConvert.SerializeObject(_m_mWebSocketJson));
                            }
                            catch (Exception ex)
                            {
                                if (!string.IsNullOrWhiteSpace(m_sUUID))
                                {
                                    m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                    _m_mWebSocketJson.m_sUse = m_cIpCmd._m_sGetApply;
                                    _m_mWebSocketJson.m_oObject = new
                                    {
                                        m_sStatus = -1,
                                        m_sUUID = m_sUUID,
                                        m_sResultMessage = new
                                        {
                                            m_sErrMsg = $"Err{ex.Message}"
                                        }
                                    };
                                    //回复消息
                                    m_pWebSocket.Send(JsonConvert.SerializeObject(_m_mWebSocketJson));
                                }
                            }
                        }
                        #endregion
                        break;
                    case m_cFSCmdType._m_sFSCmd:
                        #region ***发送并执行freeswitch命令,服务端处理事宜
                        {
                            ///解析命令
                            string m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();
                            string m_sSendMessage = m_pJObject["m_oObject"]["m_sSendMessage"].ToString();

                            string m_sEslResult = "-ERR No Response";
                            Task.Run(async () =>
                            {
                                ///判断特定Ua参数,主动替换external
                                if (m_sSendMessage.StartsWith(m_cFSCmd.m_sCmd_sofia_profile_external_killgw_))
                                {
                                    m_sSendMessage = m_sSendMessage.Replace(m_cFSCmd.m_sCmd_external, DB.Basic.Call_ParamUtil.m_sFreeSWITCHUaPath);
                                }
                                else if (m_sSendMessage.Equals(m_cFSCmd.m_sCmd_sofia_profile_external_rescan))
                                {
                                    m_sSendMessage = m_sSendMessage.Replace(m_cFSCmd.m_sCmd_external, DB.Basic.Call_ParamUtil.m_sFreeSWITCHUaPath);
                                }
                                else if (m_sSendMessage.Equals(m_cFSCmd.m_sCmd_sofia_profile_external_restart))
                                {
                                    m_sSendMessage = m_sSendMessage.Replace(m_cFSCmd.m_sCmd_external, DB.Basic.Call_ParamUtil.m_sFreeSWITCHUaPath);
                                }

                                m_sEslResult = await InboundMain.m_fCmnEsl(m_sSendMessage);
                                Log.Instance.Debug(m_sEslResult);

                            }).Wait();

                            ///将消息直接回复即可
                            m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                            _m_mWebSocketJson.m_sUse = m_cFSCmdType._m_sFSCmd;
                            _m_mWebSocketJson.m_oObject = new
                            {
                                m_sStatus = 0,
                                m_sUUID = m_sUUID,
                                m_sResultMessage = m_sEslResult
                            };
                            m_pWebSocket?.Send($"{m_mWebSocketJsonPrefix._m_sFSCmd}{JsonConvert.SerializeObject(_m_mWebSocketJson)}");
                        }
                        #endregion
                        break;
                    case m_cFSCmdType._m_sDeleteGateway:
                        #region ***删除网关文件,防止重新注册
                        {
                            string m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();
                            string m_sSendMessage = m_pJObject["m_oObject"]["m_sSendMessage"].ToString();
                            StringBuilder m_sb = new StringBuilder();
                            foreach (string item in m_sSendMessage.Split(','))
                            {
                                try
                                {
                                    //默认路径即可
                                    string m_sFile = $"{DB.Basic.Call_ParamUtil.m_sFreeSWITCHPath}/conf/sip_profiles/{DB.Basic.Call_ParamUtil.m_sFreeSWITCHUaPath}/{item}.xml";
                                    if (System.IO.File.Exists(m_sFile))
                                    {
                                        System.IO.File.Delete(m_sFile);
                                        m_sb.Append($"网关[{item}]文件存在,删除;");
                                    }
                                    else
                                    {
                                        m_sb.Append($"网关[{item}]文件不存在;");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    m_sb.AppendLine($"网关[{item}]文件删除错误:{ex.Message};");
                                    Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_cFSCmdType._m_sDeleteGateway}][Exception][{ex.Message}]");
                                }
                            }
                            ///将消息直接回复即可
                            m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                            _m_mWebSocketJson.m_sUse = m_cFSCmdType._m_sDeleteGateway;
                            _m_mWebSocketJson.m_oObject = new
                            {
                                m_sStatus = 0,
                                m_sUUID = m_sUUID,
                                m_sResultMessage = m_sb.ToString()
                            };
                            m_pWebSocket?.Send($"{m_mWebSocketJsonPrefix._m_sFSCmd}{JsonConvert.SerializeObject(_m_mWebSocketJson)}");
                        }
                        #endregion
                        break;
                    case m_cFSCmdType._m_sReadGateway:
                        #region ***读取网关XML内容
                        {
                            int m_sStatus = -1;
                            string m_sResultMessage = "-ERR Unknown Error";
                            string m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();

                            try
                            {
                                string m_sSendMessage = m_pJObject["m_oObject"]["m_sSendMessage"].ToString();
                                string m_sGatewayFile = $"{DB.Basic.Call_ParamUtil.m_sFreeSWITCHPath}/conf/sip_profiles/{DB.Basic.Call_ParamUtil.m_sFreeSWITCHUaPath}/{m_sSendMessage}.xml";
                                if (System.IO.File.Exists(m_sGatewayFile))
                                {
                                    using (System.IO.StreamReader sr = new System.IO.StreamReader(m_sGatewayFile, Encoding.UTF8))
                                    {
                                        m_sResultMessage = sr.ReadToEnd();
                                    }
                                    m_sStatus = 0;
                                }
                                else
                                {
                                    m_sStatus = -1;
                                    m_sResultMessage = "-ERR Not Found";
                                }
                                ///将消息直接回复即可
                                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                _m_mWebSocketJson.m_sUse = m_cFSCmdType._m_sReadGateway;
                                _m_mWebSocketJson.m_oObject = new
                                {
                                    m_sStatus = m_sStatus,
                                    m_sUUID = m_sUUID,
                                    m_sResultMessage = m_sResultMessage
                                };
                                m_pWebSocket?.Send($"{m_mWebSocketJsonPrefix._m_sFSCmd}{JsonConvert.SerializeObject(_m_mWebSocketJson)}");
                            }
                            catch (Exception ex)
                            {
                                ///将错误消息直接回复即可
                                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                _m_mWebSocketJson.m_sUse = m_cFSCmdType._m_sReadGateway;
                                _m_mWebSocketJson.m_oObject = new
                                {
                                    m_sStatus = -1,
                                    m_sUUID = m_sUUID,
                                    m_sResultMessage = $"-ERR {ex.Message}"
                                };
                                m_pWebSocket?.Send($"{m_mWebSocketJsonPrefix._m_sFSCmd}{JsonConvert.SerializeObject(_m_mWebSocketJson)}");
                                Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][{m_sMessage}:{ex.Message}]");
                            }
                        }
                        #endregion
                        break;
                    case m_cFSCmdType._m_sCreateGateway:
                        #region ***添加网关文件
                        {
                            int m_sStatus = -1;
                            string m_sResultMessage = "-ERR Unknown Error";
                            string m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();

                            try
                            {
                                string m_sSendMessage = m_pJObject["m_oObject"]["m_sSendMessage"].ToString();
                                ///解析对应参数
                                JObject m_oObject = JObject.Parse(m_sSendMessage);
                                string m_sName = m_oObject["m_sName"].ToString();
                                string m_sXML = m_oObject["m_sXML"].ToString();
                                ///将XML字符串传转换成XML对象进行解析,后续容错也放置到这里即可
                                ///C:/Program Files/FreeSWITCH/conf/sip_profiles/external
                                string m_sGatewayFile = $"{DB.Basic.Call_ParamUtil.m_sFreeSWITCHPath}/conf/sip_profiles/{DB.Basic.Call_ParamUtil.m_sFreeSWITCHUaPath}/{m_sName}.xml";
                                ///容错后续处理
                                string m_sGatewayFolder = System.IO.Path.GetDirectoryName(m_sGatewayFile);
                                if (!System.IO.Directory.Exists(m_sGatewayFolder)) System.IO.Directory.CreateDirectory(m_sGatewayFolder);
                                ///创建XML文件并写入
                                if (!System.IO.File.Exists(m_sGatewayFile))
                                {
                                    using (System.IO.FileStream fs = new System.IO.FileStream(m_sGatewayFile, System.IO.FileMode.Create))
                                    {
                                        System.IO.StreamWriter sw = new System.IO.StreamWriter(fs, System.Text.Encoding.UTF8);
                                        sw.Write(m_sXML);
                                        sw.Close();
                                    }
                                    m_sStatus = 0;
                                    m_sResultMessage = $"+OK 网关[{m_sName}]添加成功";
                                }
                                else
                                {
                                    m_sStatus = -1;
                                    m_sResultMessage = $"-ERR 网关[{m_sName}]已存在,请核实";
                                }
                                ///将消息直接回复即可
                                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                _m_mWebSocketJson.m_sUse = m_cFSCmdType._m_sCreateGateway;
                                _m_mWebSocketJson.m_oObject = new
                                {
                                    m_sStatus = m_sStatus,
                                    m_sUUID = m_sUUID,
                                    m_sResultMessage = m_sResultMessage
                                };
                                m_pWebSocket?.Send($"{m_mWebSocketJsonPrefix._m_sFSCmd}{JsonConvert.SerializeObject(_m_mWebSocketJson)}");
                            }
                            catch (Exception ex)
                            {
                                ///将错误消息直接回复即可
                                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                _m_mWebSocketJson.m_sUse = m_cFSCmdType._m_sCreateGateway;
                                _m_mWebSocketJson.m_oObject = new
                                {
                                    m_sStatus = -1,
                                    m_sUUID = m_sUUID,
                                    m_sResultMessage = $"-ERR {ex.Message}"
                                };
                                m_pWebSocket?.Send($"{m_mWebSocketJsonPrefix._m_sFSCmd}{JsonConvert.SerializeObject(_m_mWebSocketJson)}");
                                Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][{m_sMessage}:{ex.Message}]");
                            }
                        }
                        #endregion
                        break;
                    case m_cFSCmdType._m_sWriteGateway:
                        #region ***写入网关XML文件
                        {
                            int m_sStatus = -1;
                            string m_sResultMessage = "-ERR Unknown Error";
                            string m_sUUID = m_pJObject["m_oObject"]["m_sUUID"].ToString();

                            try
                            {
                                string m_sSendMessage = m_pJObject["m_oObject"]["m_sSendMessage"].ToString();
                                ///解析对应参数
                                JObject m_oObject = JObject.Parse(m_sSendMessage);
                                string m_sName = m_oObject["m_sName"].ToString();
                                string m_sXML = m_oObject["m_sXML"].ToString();
                                ///将XML字符串传转换成XML对象进行解析,后续容错也放置到这里即可
                                ///C:/Program Files/FreeSWITCH/conf/sip_profiles/external
                                string m_sGatewayFile = $"{DB.Basic.Call_ParamUtil.m_sFreeSWITCHPath}/conf/sip_profiles/{DB.Basic.Call_ParamUtil.m_sFreeSWITCHUaPath}/{m_sName}.xml";
                                ///容错后续处理
                                string m_sGatewayFolder = System.IO.Path.GetDirectoryName(m_sGatewayFile);
                                if (!System.IO.Directory.Exists(m_sGatewayFolder)) System.IO.Directory.CreateDirectory(m_sGatewayFolder);
                                ///创建XML文件并写入
                                if (System.IO.File.Exists(m_sGatewayFile))
                                {
                                    using (System.IO.FileStream fs = new System.IO.FileStream(m_sGatewayFile, System.IO.FileMode.Create))
                                    {
                                        System.IO.StreamWriter sw = new System.IO.StreamWriter(fs, System.Text.Encoding.UTF8);
                                        sw.Write(m_sXML);
                                        sw.Close();
                                    }
                                    m_sStatus = 0;
                                    m_sResultMessage = $"+OK 网关[{m_sName}]修改成功";
                                }
                                else
                                {
                                    m_sStatus = -1;
                                    m_sResultMessage = $"-ERR 网关[{m_sName}]不存在,请核实";
                                }
                                ///将消息直接回复即可
                                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                _m_mWebSocketJson.m_sUse = m_cFSCmdType._m_sWriteGateway;
                                _m_mWebSocketJson.m_oObject = new
                                {
                                    m_sStatus = m_sStatus,
                                    m_sUUID = m_sUUID,
                                    m_sResultMessage = m_sResultMessage
                                };
                                m_pWebSocket?.Send($"{m_mWebSocketJsonPrefix._m_sFSCmd}{JsonConvert.SerializeObject(_m_mWebSocketJson)}");
                            }
                            catch (Exception ex)
                            {
                                ///将错误消息直接回复即可
                                m_mWebSocketJson _m_mWebSocketJson = new m_mWebSocketJson();
                                _m_mWebSocketJson.m_sUse = m_cFSCmdType._m_sWriteGateway;
                                _m_mWebSocketJson.m_oObject = new
                                {
                                    m_sStatus = -1,
                                    m_sUUID = m_sUUID,
                                    m_sResultMessage = $"-ERR {ex.Message}"
                                };
                                m_pWebSocket?.Send($"{m_mWebSocketJsonPrefix._m_sFSCmd}{JsonConvert.SerializeObject(_m_mWebSocketJson)}");
                                Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][Exception][{m_sMessage}:{ex.Message}]");
                            }
                        }
                        #endregion
                        break;
                    default:
                        #region 默认
                        Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][{m_sUse}][unknown,{m_pWebSocket.ConnectionInfo.ClientIpAddress},{m_pWebSocket.ConnectionInfo.ClientPort},{m_sMessage}]");
                        #endregion
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[WebSocket_v1][m_cInWebSocketWebApiDo][MainStep][Exception][{m_sMessage}:{ex.Message}]");
            }
            return;
        }
    }

    internal class m_mWebSocketJsonPrefix
    {
        public const string _m_sPrefix = "{JSON-AUTO-DIAL-TASK}";
        public const string _m_sHttpCmd = "{JSON-HTTP-CMD}";
        public const string _m_sP2PMsgCmd = "{JSON-P2PMSG-CMD}";
        public const string _m_sFSCmd = "{JSON-FS-CMD}";
    }

    internal class m_cFSCmd
    {
        /// <summary>
        /// 查看所有网关状态
        /// </summary>
        public const string m_sCmd_sofia_xmlstatus_gateway = "sofia xmlstatus gateway";
        /// <summary>
        /// 杀死特定网关
        /// </summary>
        public const string m_sCmd_sofia_profile_external_killgw_ = "sofia profile external killgw ";
        /// <summary>
        /// 保护性重启external
        /// </summary>
        public const string m_sCmd_sofia_profile_external_rescan = "sofia profile external rescan";
        /// <summary>
        /// 重启external
        /// </summary>
        public const string m_sCmd_sofia_profile_external_restart = "sofia profile external restart";
        /// <summary>
        /// 默认ua:external
        /// </summary>
        public const string m_sCmd_external = "external";
    }
}
