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
        /// 尚未完成成
        /// </summary>
        public int opreate1_8 { get; set; }
        /// <summary>
        /// 呼叫转移坐席本身的设定值缓存
        /// </summary>
        public bool isinlimit_2 { get; set; }
        public string inlimit_2number { get; set; }
        public string inlimit_2starttime { get; set; }
        public string inlimit_2endtime { get; set; }
        public int inlimit_2whatday { get; set; }
        /// <summary>
        /// 每坐席设置的同号码限呼
        /// </summary>
        public int limitthedial { get; set; }
        /// <summary>
        /// 拨号首发设定
        /// </summary>
        public int f99d999 { get; set; }
        /// <summary>
        /// 超时放音配置
        /// </summary>
        public int no_answer_timeout { get; set; }
        public string no_answer_music { get; set; }
        public string no_answer_api { get; set; }
    }
}
