///////////////////////////////////////////////////////////////////////////////////////
// 文件名   : C:\Users\zhongguan\Desktop\11111\NhibernateBag\Models\Call_ServerList.cs
// 类名     : Call_ServerList
// 中文名   : 
// 创建描述 : 
// 创建人   : 
// 创建时间 : 2015-11-10 16:54:14
// 版权信息 : 青岛天路信息技术有限责任公司  www.topdigi.com.cn
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Data;

namespace DB.Basic
{
	/// <summary>
	/// 
	/// </summary>

	public class Call_ServerListUtil
	{
		public Call_ServerListUtil()
		{
			    DateTime initDatetime = new DateTime(1900, 1, 1);
		}
		public static DataTable GetCallServerInfo()
		{
			DataTable dt = new DataTable();
			string sqlstr = "select ServerIndex, ServerName,ServerIP,ServerPort,IsDefault from Call_Param left join Call_ServerList on Call_Param.ID=Call_ServerList.ParamID where Call_Param.P_Name='CallServer' and Call_Param.P_Value='CallServer' order by ServerIndex asc";
			dt = MySQL_Method.BindTable(sqlstr);
			return dt;
		}

		public static DataTable GetFtpServerInfo()
		{
			DataTable dt = new DataTable();
			string sqlstr = "select ServerIndex,LoginName,Password,ServerName,ServerIP,ServerPort,IsDefault from Call_Param left join Call_ServerList on Call_Param.ID=Call_ServerList.ParamID where Call_Param.P_Name='FtpServer' and Call_Param.P_Value='FtpServer' order by ServerIndex asc";
			dt = MySQL_Method.BindTable(sqlstr);
			return dt;
		}

		public static DataTable GetSipServerInfo()
		{
			DataTable dt = new DataTable();
			string sqlstr = "select ServerIndex,ServerName,DomainName,ConnectTime,ServerIP,ServerPort,IsDefault,Password from Call_Param left join Call_ServerList on Call_Param.ID=Call_ServerList.ParamID where Call_Param.P_Name='SipServer' and Call_Param.P_Value='SipServer' order by ServerIndex asc";
			dt = MySQL_Method.BindTable(sqlstr);
			return dt;
		}

		public static bool UpdateServerIndex(string ServerIndex, string ServerType)
		{
			DataTable dt = new DataTable();
			string sqlstr = "UPDATE Call_ServerList SET ServerIndex=ServerIndex-1 FROM Call_ServerList INNER JOIN Call_Param on Call_Param.ID=Call_ServerList.ParamID WHERE ServerIndex=" + ServerIndex + " AND  Call_Param.P_Name='" + ServerType + "'";
			dt = MySQL_Method.BindTable(sqlstr);
			return dt.Rows.Count > 0;
		}

		public static bool DeleteServerList(string ServerIndex, string ServerType)
		{
			DataTable dt = new DataTable();
			string sqlstr = "Delete Call_ServerList FROM Call_ServerList INNER JOIN Call_Param on Call_Param.ID=Call_ServerList.ParamID WHERE ServerIndex=" + ServerIndex + " AND  Call_Param.P_Name='" + ServerType + "'";
			dt = MySQL_Method.BindTable(sqlstr);
			return dt.Rows.Count > 0;
		}

		public static bool UpdateServerInfo(string ServerIndex, string ServerName, string ServerIP, string ServerPort, string DomainName, string LoginName, string ConnectTime, string Password, string IsDefault, string ServerType)
		{
			DataTable dt = new DataTable();
			string sqlstr = string.Empty;
			switch (ServerType)
			{
				case "CallServer":
					sqlstr = string.Format("UPDATE Call_ServerList set ServerName='{0}',ServerIP='{1}',ServerPort={2},IsDefault='{3}' FROM Call_ServerList INNER JOIN Call_Param on Call_Param.ID=Call_ServerList.ParamID WHERE ServerIndex=" + ServerIndex + " AND  Call_Param.P_Name='CallServer'", ServerName, ServerIP, ServerPort, IsDefault); break;
				case "FtpServer":
					sqlstr = string.Format("UPDATE Call_ServerList set ServerName='{0}',ServerIP='{1}',ServerPort={2},LoginName='{3}',Password='{4}',IsDefault='{5}' FROM Call_ServerList INNER JOIN Call_Param on Call_Param.ID=Call_ServerList.ParamID WHERE ServerIndex=" + ServerIndex + " AND  Call_Param.P_Name='FtpServer'", ServerName, ServerIP, ServerPort, LoginName, Password, IsDefault); break;
				case "SipServer":
					sqlstr = string.Format("UPDATE Call_ServerList set ServerName='{0}',ServerIP='{1}',ServerPort={2},DomainName='{3}',ConnectTime={4},IsDefault='{5}' FROM Call_ServerList INNER JOIN Call_Param on Call_Param.ID=Call_ServerList.ParamID WHERE ServerIndex=" + ServerIndex + " AND  Call_Param.P_Name='SipServer'", ServerName, ServerIP, ServerPort, DomainName, ConnectTime, IsDefault); break;
				default: break;
			}
			if (!string.IsNullOrEmpty(sqlstr))
				dt = MySQL_Method.BindTable(sqlstr);
			return dt.Rows.Count > 0;
		}

		public static bool InsertServerInfo(string ServerIndex, string ServerName, string ServerIP, string ServerPort, string DomainName, string LoginName, string ConnectTime, string Password, string IsDefault, string ServerType)
		{
			DataTable dt = new DataTable();
			string sqlstr = sqlstr = string.Format("Insert into Call_ServerList(ServerIndex, ServerName, ServerIP, ServerPort, DomainName, LoginName, Password, IsDefault, ConnectTime, ParamID) values ('{0}','{1}','{2}',{3},'{4}','{5}','{6}','{7}',{8},(select ID from Call_Param where P_Name='{9}'))", ServerIndex, ServerName, ServerIP, ServerPort, DomainName, LoginName, Password, IsDefault, string.IsNullOrEmpty(ConnectTime) ? "null" : ConnectTime, ServerType);
			//switch (ServerType)
			//{
			//    case "CallServer":
			//        sqlstr = string.Format("Insert into Call_ServerList values ('{0}','{1}',{2},'{3}',{4},'{5}','{6}','{7}',{8},'1',{9}) FROM Call_ServerList", ServerIndex, ServerName, ServerIP, ServerPort, DomainName, LoginName, Password, IsDefault, ConnectTime, ""); break;
			//    case "FtpServer":
			//        sqlstr = string.Format("Insert into Call_ServerList values ('{0}','{1}',{2},'{3}',{4},'{5}','{6}','{7}',{8},'2',{9}) FROM Call_ServerList", ServerIndex, ServerName, ServerIP, ServerPort, DomainName, LoginName, Password, IsDefault, ConnectTime, ""); break;
			//    case "SipServer":
			//        sqlstr = string.Format("Insert into Call_ServerList values ('{0}','{1}',{2},'{3}',{4},'{5}','{6}','{7}',{8},(select ID from Call_Param where P_Name='{9}'),{10}) FROM Call_ServerList", ServerIndex, ServerName, ServerIP, ServerPort, DomainName, LoginName, Password, IsDefault, ServerType, ConnectTime, ""); break;
			//    default: break;
			//}
			//if (!string.IsNullOrEmpty(sqlstr))
			dt = MySQL_Method.BindTable(sqlstr);
			return dt.Rows.Count > 0;
		}


	}
 }
