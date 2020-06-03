using NEventSocket.FreeSwitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoFsSharp
{
	public class RecordResult : BasicMessage
	{
		private bool _Success;
		public bool Success
		{
			get
			{
				if (base.BodyText != null && base.BodyText[0] != '-')
					return _Success = true;
				return _Success;
			}
			set { _Success = value; }
		}
		private string _ErrorMessage;
		public string ErrorMessage
		{
			get
			{
				if (base.BodyText == null || !base.BodyText.StartsWith("-ERR"))
				{
					return _ErrorMessage;
				}
				return _ErrorMessage = base.BodyText.Substring(5, base.BodyText.Length - 5);
			}
			set { _ErrorMessage = value; }
		}
		internal RecordResult(BasicMessage basicMessage)
		{
			base.Headers = basicMessage.Headers;
			base.BodyText = basicMessage.BodyText;
		}
		internal RecordResult(KeyValuePair<bool, string> _Message)
		{
			this.Success = _Message.Key;
			this.ErrorMessage = _Message.Value;
		}
	}

}
