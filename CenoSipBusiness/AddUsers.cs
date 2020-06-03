using CenoCommon;
using log4net;
using System;
using System.IO;

namespace CenoSipBusiness
{
	public class AddUsers
	{
		private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void Add_Users()
		{
			try
			{
				string xmlfile = DB.Basic.Call_ParamUtil.m_sFreeSWITCHPath + @"\conf\directory\default\1000.xml";

				if (!File.Exists(xmlfile))
				{
					_Ilog.Error("-ERROR:don't find default user profile in path");
					return;
				}

				_Ilog.Info("请输入开始用户名!!!必须是数字");
				int StartUser = int.Parse(Console.ReadLine());

				_Ilog.Info("请输入要添加用户的个数!!!");
				string UserCountStr = Console.ReadLine();
				int UserCount = int.Parse(UserCountStr);
				_Ilog.Info(("要添加 " + UserCountStr + " 个用户吗？？？？   Y/N"));
				string CmdFlag = Console.ReadLine();
				if (CmdFlag == "Y" || CmdFlag == "y")
				{
					Console.WriteLine();
					using (FileStream fs = new FileStream(xmlfile, FileMode.Open))
					{
						using (StreamReader sr = new StreamReader(fs))
						{
							string lastContent = sr.ReadToEnd();
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
