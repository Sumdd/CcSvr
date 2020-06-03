using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoSipFactory
{
	public class AGENT_INFO
	{
		public AGENT_INFO()
		{

		}

		public int AgentID
		{
			get;
			set;
		}

        public string AgentUUID
        {
            get;
            set;
        }

		public string LoginName
		{
			get;
			set;
		}

		public string AgentName
		{
			get;
			set;
		}

		public string LoginPsw
		{
			get;
			set;
		}

		public string LastLoginIp
		{
			get;
			set;
		}

		public ChannelInfo ChInfo
		{
			get;
			set;
		}

		public string AgentNum
		{
			get;
			set;
		}

		public string RoleName
		{
			get;
			set;
		}

		public string TeamName
		{
			get;
			set;
		}

		public bool LoginState
		{
			get;
			set;
		}
	}
}
