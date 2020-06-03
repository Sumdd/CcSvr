using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoFsSharp
{
	public class CallRecordResult
	{
		public CallRecordResult(bool Success, string Result)
		{
			this.Success = Success;
			this.Result = Result;
		}
		private bool _Success;
		public bool Success
		{
			get { return _Success; }
			set { _Success = value; }
		}

		private string _Result;
		public string Result
		{
			get { return _Result; }
			set { _Result = value; }
		}
	}
}
