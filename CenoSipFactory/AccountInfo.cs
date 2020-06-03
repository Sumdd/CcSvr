using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.IO;
using System.Data;
using DB.Basic;

namespace CenoSipFactory
{
	public class AccountInfo
	{
		XmlDocument XmlDoc;
		XmlNodeList ParamXnl, VarXnl;

		public AccountInfo()
		{

		}
		public AccountInfo(Dictionary<string, object> _AccInfo)
		{
			this._userid = _AccInfo["userid"].ToString();
			this._RegInfo = (RegistrationInfo)_AccInfo["RegInfo"];
		}

		/// <summary>
		/// get user info based on xml doc,
		/// </summary>
		/// <param name="_AccXml"></param>
		public AccountInfo(string _AccXml)
		{
			if (!File.Exists(_AccXml))
				return;

			try
			{
				XmlDoc = new XmlDocument();
				XmlDoc.Load(_AccXml);

				AccXml = _AccXml;

				XmlNodeList _XnlNodeList = XmlDoc.GetElementsByTagName("user");
				_userid = _XnlNodeList[0].Attributes["id"].Value;

				ParamXnl = XmlDoc.GetElementsByTagName("param");
				VarXnl = XmlDoc.GetElementsByTagName("variable");

				foreach (XmlNode xn in ParamXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "password"))
					if (xn != null)
					{
						_password = xn.Attributes[1].Value;
						break;
					}
				foreach (XmlNode xn in ParamXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "vm-password"))
					if (xn != null)
					{
						_vm_password = xn.Attributes[1].Value;
						break;
					}

				foreach (XmlNode xn in VarXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "toll_allow"))
					if (xn != null)
					{
						_toll_allow = xn.Attributes[1].Value;
						break;
					}
				foreach (XmlNode xn in VarXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "accountcode"))
					if (xn != null)
					{
						_accountcode = xn.Attributes[1].Value;
						break;
					}
				foreach (XmlNode xn in VarXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "user_context"))
					if (xn != null)
					{
						_user_context = xn.Attributes[1].Value;
						break;
					}
				foreach (XmlNode xn in VarXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "effective_caller_id_name"))
					if (xn != null)
					{
						_effective_caller_id_name = xn.Attributes[1].Value;
						break;
					}
				foreach (XmlNode xn in VarXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "effective_caller_id_number"))
					if (xn != null)
					{
						_effective_caller_id_number = xn.Attributes[1].Value;
						break;
					}
				foreach (XmlNode xn in VarXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "outbound_caller_id_name"))
					if (xn != null)
					{
						_outbound_caller_id_name = xn.Attributes[1].Value;
						break;
					}
				foreach (XmlNode xn in VarXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "outbound_caller_id_number"))
					if (xn != null)
					{
						_outbound_caller_id_number = xn.Attributes[1].Value;
						break;
					}
				foreach (XmlNode xn in VarXnl.OfType<XmlNode>().Where<XmlNode>(x => x.Attributes[0].Value == "callgroup"))
					if (xn != null)
					{
						_call_group = xn.Attributes[1].Value;
						break;
					}


				DataSet Ds = SQLiteHelper.ExecuteDataSet("select * from sip_registrations where sip_user='" + userid + "'");
				if (Ds != null && Ds.Tables.Count > 0 && Ds.Tables[0].Rows.Count > 0)
				{
					RegState = true;
					RegInfo = new RegistrationInfo(Ds.Tables[0]);
				}
				else
				{
					RegState = false;
					RegInfo = null;
				}
			}
			catch (Exception ex)
			{

			}
		}

		public string AccXml
		{
			get;
			set;
		}

		private string _userid = "";
		public string userid
		{
			get { return _userid; }
		}

		private string _password = "";
		public string password
		{
			get { return _password; }
			set
			{
				if (_password == value)
					return;

				XmlNode xn = ParamXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "password");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _password = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _vm_password = "";
		public string vm_password
		{
			get { return _vm_password; }
			set
			{
				if (_vm_password == value)
					return;

				XmlNode xn = ParamXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "vm-password");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _vm_password = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _toll_allow = "";
		public string toll_allow
		{
			get { return _toll_allow; }
			set
			{
				if (_toll_allow == value)
					return;

				XmlNode xn = VarXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "toll_allow");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _toll_allow = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _accountcode = "";
		public string accountcode
		{
			get { return _accountcode; }
			set
			{
				if (_accountcode == value)
					return;

				XmlNode xn = VarXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "accountcode");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _accountcode = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _user_context = "";
		public string user_context
		{
			get { return _user_context; }
			set
			{
				if (_user_context == value)
					return;

				XmlNode xn = VarXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "user_context");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _user_context = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _effective_caller_id_name = "";
		public string effective_caller_id_name
		{
			get { return _effective_caller_id_name; }
			set
			{
				if (_effective_caller_id_name == value)
					return;

				XmlNode xn = VarXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "effective_caller_id_name");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _effective_caller_id_name = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _effective_caller_id_number = "";
		public string effective_caller_id_number
		{
			get { return _effective_caller_id_number; }
			set
			{
				if (_effective_caller_id_number == value)
					return;

				XmlNode xn = VarXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "effective_caller_id_number");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _effective_caller_id_number = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _outbound_caller_id_name = "";
		public string outbound_caller_id_name
		{
			get { return _outbound_caller_id_name; }
			set
			{
				if (_outbound_caller_id_name == value)
					return;

				XmlNode xn = VarXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "outbound_caller_id_name");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _outbound_caller_id_name = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _outbound_caller_id_number = "";
		public string outbound_caller_id_number
		{
			get { return _outbound_caller_id_number; }
			set
			{
				if (_outbound_caller_id_number == value)
					return;

				XmlNode xn = VarXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "outbound_caller_id_number");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _outbound_caller_id_number = value;
				XmlDoc.Save(AccXml);
			}
		}

		private string _call_group = "";
		public string call_group
		{
			get { return _call_group; }
			set
			{
				if (_call_group == value)
					return;

				XmlNode xn = VarXnl.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => x.Attributes[0].Value == "callgroup");
				if (xn == null)
					return;
				xn.Attributes[1].Value = _call_group = value;
				XmlDoc.Save(AccXml);
			}
		}

		public bool RegState
		{
			get;
			set;
		}

		public RegistrationInfo _RegInfo;
		public RegistrationInfo RegInfo
		{
			get
			{
				if (_RegInfo == null)
					_RegInfo = new RegistrationInfo(SQLiteHelper.ExecuteDataSet("select * from sip_registrations where sip_user='" + this.userid + "'").Tables[0]);
				return _RegInfo;
			}
			set
			{
				_RegInfo = value;
			}
		}
	}
}
