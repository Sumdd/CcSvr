using CenoCommon;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CenoSipBusiness {

    public class gatewaylib {
        private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void add_gateway() {
            try {

                _Ilog.Info("please input gateway name");
                string gateway_name = Console.ReadLine();
                _Ilog.Info("please input user name");
                string user_name = Console.ReadLine();
                _Ilog.Info("please input real name");
                string real_name = Console.ReadLine();
                _Ilog.Info("please input password");
                string password = Console.ReadLine();
                _Ilog.Info("please input proxy");
                string proxy = Console.ReadLine();
                _Ilog.Info("please input register-proxy");
                string register_proxy = Console.ReadLine();
                _Ilog.Info("please input register flag (0=false,1=true)");
                string register = Console.ReadLine();

                StringBuilder gateway_info = new StringBuilder();
                gateway_info.AppendFormat("<include>\r\n" +
                                                "\t<gateway name=\"{0}\">\r\n" +
                                                    "\t\t<!--/// account username *required* ///-->\r\n" +
                                                    "\t\t<param name=\"username\" value=\"{1}\"/>\r\n" +
                                                    "\t\t<!--/// auth realm: *optional* same as gateway name, if blank ///-->\r\n" +
                                                    "\t\t<param name=\"realm\" value=\"{2}\"/>\r\n" +
                                                    "\t\t<!--/// username to use in from: *optional* same as  username, if blank ///-->\r\n" +
                                                    "\t\t<!--<param name=\"from-user\" value=\"cluecon\"/>-->\r\n" +
                                                    "\t\t<!--/// domain to use in from: *optional* same as  realm, if blank ///-->\r\n" +
                                                    "\t\t<!--<param name=\"from-domain\" value=\"asterlink.com\"/>-->\r\n" +
                                                    "\t\t<!--/// account password *required* ///-->\r\n" +
                                                    "\t\t<param name=\"password\" value=\"{3}\"/>\r\n" +
                                                    "\t\t<!--/// extension for inbound calls: *optional* same as username, if blank ///-->\r\n" +
                                                    "\t\t<!--<param name=\"extension\" value=\"cluecon\"/>-->\r\n" +
                                                    "\t\t<!--/// proxy host: *optional* same as realm, if blank ///-->\r\n" +
                                                    "\t\t<param name=\"proxy\" value=\"{4}\"/>\r\n" +
                                                    "\t\t<!--/// send register to this proxy: *optional* same as proxy, if blank ///-->\r\n" +
                                                    "\t\t<param name=\"register-proxy\" value=\"{5}\"/>\r\n" +
                                                    "\t\t<!--/// expire in seconds: *optional* 3600, if blank ///-->\r\n" +
                                                    "\t\t<!--<param name=\"expire-seconds\" value=\"60\"/>-->\r\n" +
                                                    "\t\t<!--/// do not register ///-->\r\n" +
                                                    "\t\t<param name=\"register\" value=\"{6}\"/>\r\n" +
                                                    "\t\t<!-- which transport to use for register -->\r\n" +
                                                    "\t\t<!--<param name=\"register-transport\" value=\"udp\"/>-->\r\n" +
                                                    "\t\t<!--How many seconds before a retry when a failure or timeout occurs -->\r\n" +
                                                    "\t\t<!--<param name=\"retry-seconds\" value=\"30\"/>-->\r\n" +
                                                    "\t\t<!--Use the callerid of an inbound call in the from field on outbound calls via this gateway -->\r\n" +
                                                    "\t\t<!--<param name=\"caller-id-in-from\" value=\"false\"/>-->\r\n" +
                                                    "\t\t<!--extra sip params to send in the contact-->\r\n" +
                                                    "\t\t<!--<param name=\"contact-params\" value=\"tport=tcp\"/>-->\r\n" +
                                                    "\t\t<!-- Put the extension in the contact -->\r\n" +
                                                    "\t\t<!--<param name=\"extension-in-contact\" value=\"true\"/>-->\r\n" +
                                                    "\t\t<!--send an options ping every x seconds, failure will unregister and/or mark it down-->\r\n" +
                                                    "\t\t<!--<param name=\"ping\" value=\"25\"/>-->\r\n" +
                                                    "\t\t<!--<param name=\"cid-type\" value=\"rpid\"/>-->\r\n" +
                                                "\t</gateway>\r\n" +
                                            "</include>",
                                            gateway_name,
                                            user_name,
                                            real_name,
                                            password,
                                            proxy,
                                            register_proxy,
                                            register == "1" ? "true" : "false");
                string FileName = DB.Basic.Call_ParamUtil.m_sFreeSWITCHPath + $"\\conf\\sip_profiles\\{DB.Basic.Call_ParamUtil.m_sFreeSWITCHUaPath}\\" + gateway_name + ".xml";

                using(FileStream FileS = new FileStream(FileName, FileMode.OpenOrCreate)) {
                    using(StreamWriter sw = new StreamWriter(FileS)) {
                        sw.Write(gateway_info);
                    }
                }
                _Ilog.Info("成功创建 " + FileName + " 网关！！！");
            } catch(Exception ex) {
                _Ilog.Info("创建网关失败,原因:" + ex.Message);
            }
        }
    }
}
