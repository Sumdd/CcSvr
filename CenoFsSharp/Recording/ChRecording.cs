using NEventSocket;
using NEventSocket.FreeSwitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using System.Reactive.Linq;
using log4net;
using CenoCommon;

using System.IO;
using CenoSipFactory;
using System.Text.RegularExpressions;

namespace CenoFsSharp {

    public class ChRecording {
        private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="_connection"></param>
        /// <param name="RECORD_HANGUP_ON_ERROR">the leg will hangup when record failure</param>
        /// <param name="RECORD_READ_ONLY"></param>
        /// <param name="RECORD_WRITE_ONLY"></param>
        /// <param name="RECORD_STEREO"></param>
        /// <param name="RECORD_STEREO_SWAP"></param>
        /// <param name="RECORD_ANSWER_REQ"></param>
        /// <param name="RECORD_BRIDGE_REQ"></param>
        /// <param name="RECORD_APPEND"></param>
        /// <param name="record_sample_rate"></param>
        /// <param name="enable_file_write_buffering"></param>
        /// <param name="RECORD_MIN_SEC"></param>
        /// <param name="RECORD_INITIAL_TIMEOUT_MS"></param>
        /// <param name="RECORD_FINAL_TIMEOUT_MS"></param>
        /// <param name="RECOED_SILENCE_THRESHOLD"></param>
        /// <param name="RECORD_TITLE"></param>
        /// <param name="RECORD_COPYRIGHT"></param>
        /// <param name="RECORD_SOFTWARE"></param>
        /// <param name="RECORD_ARTIST"></param>
        /// <param name="RECORD_COMMENT"></param>
        /// <param name="RECORD_DATE"></param>
        /// <returns></returns>
        public static async Task<RecordResult> Recording(string uuid, OutboundSocket _connection, ChannelInfo Ch_Info,
            bool RECORD_HANGUP_ON_ERROR = false, bool RECORD_READ_ONLY = false, bool RECORD_WRITE_ONLY = false,
            bool RECORD_STEREO = false, bool RECORD_STEREO_SWAP = false, bool RECORD_ANSWER_REQ = false, bool RECORD_BRIDGE_REQ = false, bool RECORD_APPEND = false,
            int record_sample_rate = 8000, bool enable_file_write_buffering = true,
            int RECORD_MIN_SEC = 3, int RECORD_INITIAL_TIMEOUT_MS = 10, int RECORD_FINAL_TIMEOUT_MS = 10, int RECOED_SILENCE_THRESHOLD = 200,
            string RECORD_TITLE = "", string RECORD_COPYRIGHT = "", string RECORD_SOFTWARE = "", string RECORD_ARTIST = "", string RECORD_COMMENT = "", string RECORD_DATE = "") {
            try {
                if(uuid == null)
                    throw new ArgumentNullException("uuid");

                if(_connection == null)
                    throw new ArgumentNullException("_connection");

                if(Ch_Info == null)
                    throw new ArgumentNullException("Ch_Info");


                KeyValuePair<bool, string> RecFilePath = GetRecFilePath(_connection);
                if(!RecFilePath.Key) {
                    return new RecordResult(RecFilePath);
                }

                Ch_Info.channel_call_record_info.RecFile = RecFilePath.Value;
                return await (
                    from x
                    in (_connection.BackgroundJob(string.Format("uuid_record {0} start {1}", uuid, RecFilePath.Value))
                        .ToObservable<BackgroundJobResult>())
                    select new RecordResult(x))
                    .ToTask<RecordResult>()
                    .ConfigureAwait(true);
            } catch(Exception ex) {
                return new RecordResult(new KeyValuePair<bool, string>(false, ex.Message));
            }

        }

        public static async Task<RecordResult> StopRecording(string uuid, OutboundSocket _connection, ChannelInfo Ch_Info) {
            try {
                if(uuid == null)
                    throw new ArgumentNullException("uuid");

                if(_connection == null)
                    throw new ArgumentNullException("_connection");

                if(Ch_Info == null)
                    throw new ArgumentNullException("Ch_Info");

                return await (
                    from x
                    in (_connection.BackgroundJob(string.Format("uuid_record {0} stop {1}", uuid, Ch_Info.channel_call_record_info.RecFile))
                        .ToObservable<BackgroundJobResult>())
                    select new RecordResult(x))
                    .ToTask<RecordResult>()
                    .ConfigureAwait(true);
            } catch(Exception ex) {
                return new RecordResult(new KeyValuePair<bool, string>(false, ex.Message));
            }
        }
        private static KeyValuePair<bool, string> GetRecFilePath(OutboundSocket _connection,string _type = "L") {
            KeyValuePair<bool, string> Rrult = new KeyValuePair<bool, string>();

            try {
                if(string.IsNullOrEmpty(ParamLib.RecordFilePath)) {
                    throw new Exception("the record file path is null");
                }

                if(CommonClass.IsHaveChinaChar(ParamLib.RecordFilePath)) {
                    throw new Exception("the record file path can't contain chinese char");
                }
                DateTime NowDate = DateTime.Now;

                Regex rg = new Regex("[^(0-9)]+");

                string RecordFilePath = string.Format(
                    ParamLib.RecordFilePath +
                    "\\{0}\\{1}\\{2}\\Rec_{3}_{4}_{7}_{5}{6}",
                    NowDate.Year,
                    NowDate.ToString("yyyyMM"),
                    NowDate.ToString("yyyyMMdd"),
                    NowDate.ToString("yyyyMMddHHmmss"),
                    rg.Replace(_connection.ChannelData.GetHeader("Channel-Caller-ID-Number"), ""),
                    rg.Replace(_connection.ChannelData.GetHeader("Channel-Destination-Number"), ""),
                    DB.Basic.Call_ParamUtil._rec_t,
                    _type
                    );
                if(!Directory.Exists(Path.GetDirectoryName(RecordFilePath))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(RecordFilePath));
                }

                Rrult = new KeyValuePair<bool, string>(true, RecordFilePath);
            } catch(Exception ex) {
                Rrult = new KeyValuePair<bool, string>(false, ex.Message);
            }
            return Rrult;

        }
    }
}
