using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CenoCommon {
    public class GlobalParam {
        public static string GetNowDateTime {
            get {
                return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  ";
            }
        }

        public static bool WriteConsole {
            get;
            set;
        }
    }
}
