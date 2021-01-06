using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DB.Basic;

namespace CenoSipFactory {
    public class ParamLib {
        private static string[] _SocketEndStr;
        public static string[] SocketEndStr {
            get {
                if(_SocketEndStr == null)
                    _SocketEndStr = call_socketcommand_util.GetEndStr();
                return _SocketEndStr;
            }
        }

        private static string[] _SocketStartStr;
        public static string[] SocketStartStr {
            get {
                if(_SocketStartStr == null)
                    _SocketStartStr = call_socketcommand_util.GetStartStr();
                return _SocketStartStr;
            }
        }

        private static string _TcpPort;
        public static string TCPport {
            get {
                if(string.IsNullOrEmpty(_TcpPort))
                    _TcpPort = Call_ParamUtil.GetModel("TcpPort").P_Value;
                return _TcpPort;
            }
            set {
                Call_ParamUtil.Update("TcpPort", value);
            }
        }

        private static string _UdpPort;
        public static string UDPport {
            get {
                if(string.IsNullOrEmpty(_UdpPort))
                    _UdpPort = Call_ParamUtil.GetModel("UdpPort").P_Value;
                return _UdpPort;
            }
            set {
                Call_ParamUtil.Update("UdpPort", value);
            }
        }

        public static string VirtulPath {
            get; set;
        }

        public static string CallIpAddress {
            get; set;
        }

        public static string RecordPath {
            get; set;
        }

        private static string _FsServerAddress;
        public static string FsServerAddress {
            get {
                if(string.IsNullOrEmpty(_FsServerAddress))
                    _FsServerAddress = Call_ParamUtil.GetModel("FsServerAddress").P_Value;
                return _FsServerAddress;
            }
            set {
                Call_ParamUtil.Update("FsServerAddress", value);
            }
        }

        private static int? _FsTcpPort;
        public static int FsTcpPort {
            get {
                if(!_FsTcpPort.HasValue)
                    _FsTcpPort = int.Parse(Call_ParamUtil.GetModel("FsTcpPort").P_Value ?? "-1");
                return _FsTcpPort.Value;
            }
            set {
                Call_ParamUtil.Update("FsTcpPort", value.ToString());
            }
        }

        private static string _DialingAudioFile;
        public static string DialingAudioFile {
            get {
                if(string.IsNullOrEmpty(_DialingAudioFile))
                    _DialingAudioFile = Call_ParamUtil.GetModel("DialingAudioFile").P_Value;
                return _DialingAudioFile;
            }
            set {
                Call_ParamUtil.Update("DialingAudioFile", _DialingAudioFile = value);
            }
        }

        private static string _UserBusyAudioFile;
        public static string UserBusyAudioFile {
            get {
                if(string.IsNullOrEmpty(_UserBusyAudioFile))
                    _UserBusyAudioFile = Call_ParamUtil.GetModel("UserBusyAudioFile").P_Value;
                return _UserBusyAudioFile;
            }
            set {
                Call_ParamUtil.Update("UserBusyAudioFile", _UserBusyAudioFile = value);
            }
        }

        private static string _WaitingAudioFile;
        public static string WaitingAudioFile {
            get {
                if(string.IsNullOrEmpty(_WaitingAudioFile))
                    _WaitingAudioFile = Call_ParamUtil.GetModel("WaitingAudioFile").P_Value;
                return _WaitingAudioFile;
            }
            set {
                Call_ParamUtil.Update("WaitingAudioFile", _WaitingAudioFile = value);
            }
        }

        private static string _BgMusicAudioFile;
        public static string BgMusicAudioFile {
            get {
                if(string.IsNullOrEmpty(_BgMusicAudioFile))
                    _BgMusicAudioFile = Call_ParamUtil.GetModel("BgMusicAudioFile").P_Value;
                return _BgMusicAudioFile;
            }
            set {
                Call_ParamUtil.Update("BgMusicAudioFile", _BgMusicAudioFile = value);
            }
        }

        private static bool? _WaitAgentPickupAnswer;
        public static bool WaitAgentPickupAnswer {
            get {
                if(!_WaitAgentPickupAnswer.HasValue)
                    _WaitAgentPickupAnswer = Call_ParamUtil.GetModel("WaitAgentPickupAnswer").P_Value == "1";
                return _WaitAgentPickupAnswer.Value;
            }
            set {
                Call_ParamUtil.Update("WaitAgentPickupAnswer", value ? "1" : "0");
                _WaitAgentPickupAnswer = value;
            }
        }

        private static bool? _StartRecordingTime;
        /// <summary>
        /// 
        /// </summary>
        public static bool StartRecordingTime {
            get {
                if(!_StartRecordingTime.HasValue)
                    _StartRecordingTime = Call_ParamUtil.GetModel("StartRecordingTime").P_Value == "1";
                return _StartRecordingTime.Value;
            }
            set {
                Call_ParamUtil.Update("StartRecordingTime", value ? "1" : "0");
                _StartRecordingTime = value;
            }
        }

        private static string _RecordFilePath;
        public static string RecordFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_RecordFilePath))
                    _RecordFilePath = Call_ParamUtil.RecordFilePath;
                return _RecordFilePath;
            }
            set
            {
                Call_ParamUtil.Update("RecordPath", value);
                _RecordFilePath = value;
            }
        }

        private static string _CityCode;
        public static string CityCode {
            get {
                if(string.IsNullOrEmpty(_CityCode))
                    _CityCode = Call_ParamUtil.GetModel("CityCode").P_Value;
                return _CityCode;
            }
            set {
                Call_ParamUtil.Update("CityCode", value);
                _CityCode = value;
            }
        }

        private static string _CityName;
        public static string CityName {
            get {
                if(string.IsNullOrEmpty(_CityName))
                    _CityName = Call_ParamUtil.GetModel("CityName").P_Value;
                return _CityName;
            }
            set {
                Call_ParamUtil.Update("CityName", value);
                _CityName = value;
            }
        }

        private static string _Tts_AudioFilePath;

        public static string Tts_AudioFilePath {
            get {
                if(string.IsNullOrEmpty(_Tts_AudioFilePath))
                    _Tts_AudioFilePath = Call_ParamUtil.GetModel("Tts_AudioFilePath").P_Value;
                return _Tts_AudioFilePath;
            }
            set {
                Call_ParamUtil.Update("Tts_AudioFilePath", value);
                _Tts_AudioFilePath = value;
            }
        }

        public static string _gatewayDefault = "ipe105";
        public static bool IsAutoDial = false;
        /* 自动拨号开始时间 */

        private static string  _AutoDIalStartTime;

        public static string AutoDIalStartTime {
            get {
                if(string.IsNullOrWhiteSpace(_AutoDIalStartTime))
                    _AutoDIalStartTime = Call_ParamUtil.GetModel("AutoDIalStartTime").P_Value;
                return _AutoDIalStartTime;
            }
            set {
                Call_ParamUtil.Update("AutoDIalStartTime", value);
                _AutoDIalStartTime = value;
            }
        }

        /* 自动拨号结束时间 */

        private static string  _AutoDialEndTime;

        public static string AutoDialEndTime {
            get {
                if(string.IsNullOrWhiteSpace(_AutoDialEndTime))
                    _AutoDialEndTime = Call_ParamUtil.GetModel("AutoDialEndTime").P_Value;
                return _AutoDialEndTime;
            }
            set {
                Call_ParamUtil.Update("AutoDialEndTime", value);
                _AutoDialEndTime = value;
            }
        }

        private static string _DialInRole;
        /// <summary>
        /// 内线拨号原则
        /// </summary>
        public static string DialInRole {
            get {
                if(string.IsNullOrWhiteSpace(_DialInRole))
                    _DialInRole = Call_ParamUtil.GetModel("DialInRole").P_Value;
                return _DialInRole;
            }
            set {
                Call_ParamUtil.Update("DialInRole", value);
                _DialInRole = value;
            }
        }

        private static string _DialOutRole;
        /// <summary>
        /// 外线拨号原则
        /// </summary>
        public static string DialOutRole {
            get {
                if(string.IsNullOrWhiteSpace(_DialOutRole))
                    _DialOutRole = Call_ParamUtil.GetModel("DialOutRole").P_Value;
                return _DialOutRole;
            }
            set {
                Call_ParamUtil.Update("DialOutRole", value);
                _DialOutRole = value;
            }
        }

    }
}
