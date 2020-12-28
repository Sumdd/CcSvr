using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DB.Model
{
    public class call_agent_model
    {
        public int ID { get; set; }

        public string UniqueID { get; set; }

        public string AgentName { get; set; }

        public string LoginName { get; set; }

        public string LoginPassWord { get; set; }

        public int LoginState { get; set; }

        public string AgentNumber { get; set; }

        public string AgentPassword { get; set; }

        public string LastLoginIp { get; set; }

        public int TeamID { get; set; }

        public int StateID { get; set; }

        public int RoleID { get; set; }

        public int ChannelID { get; set; }

        public int ClientParamID { get; set; }

        public int Usable { get; set; }

        public int LinkUser { get; set; }

        public string LU_LoginName { get; set; }

        public string LU_Password { get; set; }

        public string Remark { get; set; }

        /// <summary>
        /// 先设定存储8个开关,暂时用不到那么多
        /// 1.全号显示:0禁止1启用
        /// </summary>
        public int opreate1_8 { get; set; }
    }
}
