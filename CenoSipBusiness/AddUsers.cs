using CenoCommon;
using log4net;
using System;
using System.IO;
using System.Text;

namespace CenoSipBusiness
{
    public class AddUsers
    {
        private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Add_Users()
        {
            try
            {
                ///用户内容不在读取文件,而是默认一个文件,防止目前出现的密码被拷贝的问题
                ///默认密码1234即可

                string m_sUa = @"<include>
  <user id=""1000"">
    <params>
      <param name=""password"" value=""$${default_password}""/>
      <param name=""vm-password"" value=""1000""/>
    </params>
    <variables>
      <variable name=""toll_allow"" value=""domestic,international,local""/>
      <variable name=""accountcode"" value=""1000""/>
      <variable name=""user_context"" value=""default""/>
      <variable name=""effective_caller_id_name"" value=""Extension 1000""/>
      <variable name=""effective_caller_id_number"" value=""1000""/>
      <variable name=""outbound_caller_id_name"" value=""$${outbound_caller_name}""/>
      <variable name=""outbound_caller_id_number"" value=""$${outbound_caller_id}""/>
      <variable name=""callgroup"" value=""techsupport""/>
    </variables>
  </user>
</include>";

                _Ilog.Info("请输入开始用户名!!!必须是数字");
                int StartUser = int.Parse(Console.ReadLine());

                ///输入用户的密码,如果为空,不做替换
                _Ilog.Info("请输入用户密码,为空默认使用1234");
                string m_sPassword = Console.ReadLine();

                _Ilog.Info("请输入要添加用户的个数!!!");
                string UserCountStr = Console.ReadLine();
                int UserCount = int.Parse(UserCountStr);
                _Ilog.Info(("要添加 " + UserCountStr + " 个用户吗？？？？   Y/N"));
                string CmdFlag = Console.ReadLine();
                if (CmdFlag == "Y" || CmdFlag == "y")
                {
                    Console.WriteLine();
                    string lastContent = m_sUa;

                    ///在这里做密码替换
                    m_sPassword = m_sPassword?.Replace(" ", "")?.Replace("\r\n", "");
                    if (!string.IsNullOrWhiteSpace(m_sPassword))
                    {
                        lastContent = lastContent.Replace("$${default_password}", m_sPassword);
                    }

                    for (int i = 0; i < UserCount; i++)
                    {
                        string NewContent = lastContent.Replace("1000", (StartUser + i).ToString());
                        if (!Directory.Exists(DB.Basic.Call_ParamUtil.m_sFreeSWITCHPath + @"\conf\directory\default"))
                        {
                            throw new DirectoryNotFoundException();
                        }
                        string NewFile = DB.Basic.Call_ParamUtil.m_sFreeSWITCHPath + @"\conf\directory\default\" + (StartUser + i).ToString() + ".xml";
                        try
                        {
                            using (FileStream FileS = new FileStream(NewFile, FileMode.OpenOrCreate))
                            {
                                FileS.Seek(0, SeekOrigin.Begin);
                                FileS.SetLength(0);
                                using (StreamWriter sw = new StreamWriter(FileS))
                                {
                                    sw.Write(NewContent);
                                }
                            }
                            _Ilog.Info(("成功创建用户文件：" + NewFile));
                        }
                        catch (Exception ex)
                        {
                            _Ilog.Error(("创建用户文件失败：" + ex.Message));
                        }
                    }

                    _Ilog.Info(("成功创建 " + UserCount.ToString() + " 个用户！！！"));
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                _Ilog.Error(("-ERROR:" + ex.Message));
            }

        }
    }
}
