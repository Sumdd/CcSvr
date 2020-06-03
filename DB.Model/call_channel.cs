using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DB.Model
{
	public class call_channel_model
	{
		public int ID { get; set; }

		public string UniqueID { get; set; }

		public int ChNo { get; set; }

		public int ChType { get; set; }

		public string ChNum { get; set; }

		public string ChPassword { get; set; }

		/// <summary>
		/// 显示名
		/// </summary>
		public string ShowName { get; set; }

		/// <summary>
		/// 注册服务器
		/// </summary>
		public string SipServerIp { get; set; }

		/// <summary>
		/// 域名
		/// </summary>
		public string DomainName { get; set; }

		/// <summary>
		/// 注册端口号
		/// </summary>
		public string SipPort { get; set; }

		/// <summary>
		/// 注册间隔
		/// </summary>
		public int RegTime { get; set; }

		public string ChName { get; set; }

		public int ChVad { get; set; }

		public string BoardName { get; set; }

		public int BoardNo { get; set; }

		public int GroupID { get; set; }

		public int GroupLevel { get; set; }

		public int CallType { get; set; }

		public string CallRole { get; set; }

		public int AutoDialCh { get; set; }

		public int IsLock { get; set; }

		public int Usable { get; set; }

		public string Remark { get; set; }

        public int IsRegister { get; set; }
    }
}
