using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DB.Model
{
    public class sip_log_model
    {
        public Int64 id { get; set; }

        public string sip_auth_user { get; set; }

        public string sip_auth_realm { get; set; }

        public string contact { get; set; }

        public string status { get; set; }

        public string agent { get; set; }

        public string host { get; set; }

        public DateTime addtime { get; set; }
    }
}
