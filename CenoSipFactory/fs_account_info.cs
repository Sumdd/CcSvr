using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoSipFactory
{
	public class fs_account_info
	{
		public fs_account_info()
		{

		}

		private string _user = "";
		public string user
		{
			get { return _user; }
		}

		private string _uniqueid;
		public string uniqueid
		{
			get
			{
				return _password;
			}
			set
			{
				_uniqueid = value;
			}
		}

		private string _password = "";
		public string password
		{
			get { return _password; }
			set
			{
				_password = value;
			}
		}

		private string _vm_password = "";
		public string vm_password
		{
			get { return _vm_password; }
			set
			{
				_vm_password = value;
			}
		}

		private string _toll_allow = "";
		public string toll_allow
		{
			get { return _toll_allow; }
			set
			{
				_toll_allow = value;
			}
		}

		private string _accountcode = "";
		public string accountcode
		{
			get { return _accountcode; }
			set
			{
				_accountcode = value;
			}
		}

		private string _user_context = "";
		public string user_context
		{
			get { return _user_context; }
			set
			{
				_user_context = value;
			}
		}

		private string _effective_caller_id_name = "";
		public string effective_caller_id_name
		{
			get { return _effective_caller_id_name; }
			set
			{
				_effective_caller_id_name = value;
			}
		}

		private string _effective_caller_id_number = "";
		public string effective_caller_id_number
		{
			get { return _effective_caller_id_number; }
			set
			{
				_effective_caller_id_number = value;
			}
		}

		private string _outbound_caller_id_name = "";
		public string outbound_caller_id_name
		{
			get { return _outbound_caller_id_name; }
			set
			{
				_outbound_caller_id_name = value;
			}
		}

		private string _outbound_caller_id_number = "";
		public string outbound_caller_id_number
		{
			get { return _outbound_caller_id_number; }
			set
			{
				_outbound_caller_id_number = value;
			}
		}

		private string _call_group = "";
		public string call_group
		{
			get { return _call_group; }
			set
			{
				_call_group = value;
			}
		}
	}
}
