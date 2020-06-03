using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using CenoFsSharp;
using Core_v1;

namespace CenoSipBusiness {
    public class MainServices {
        public static KeyValuePair<bool, string> StartServices() {
            KeyValuePair<bool, string> BootResult = new KeyValuePair<bool, string>(true, "");
            Task.Factory.StartNew(() => {
                try {
                    OutBoundListenService.begin_listen();
                    //InboundMain.Start();
                    Log.Instance.Success($"[CenoSipBusiness][MainServices][StartServices][change][InboundMain.Start() -> Task<InboundSocket> fs_cli]");
                } catch(Exception ex) {
                    BootResult = new KeyValuePair<bool, string>(false, ex.Message);
                }
            });

            return BootResult;
        }

        public static KeyValuePair<bool, string> StopServices() {
            KeyValuePair<bool, string> BootResult;
            try {
                BootResult = new KeyValuePair<bool, string>(true, "");
            } catch(Exception ex) {
                BootResult = new KeyValuePair<bool, string>(false, ex.Message);
            }
            return BootResult;
        }
    }
}
