using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Fleck;
using DB.Basic;

namespace CenoSipFactory {
    public class ChannelInfo {
        public int nCh;
        public int channel_id;
        public string channel_uniqueid;
        public string channel_number;
        public string channel_call_uuid;
        /// <summary>
        /// 延申出的通道ID缓存
        /// </summary>
        public string channel_call_uuid_after;
        public string channel_call_other_uuid;
        public string channel_name;
        public int channel_type;
        public CALLTYPE channel_call_type;
        public StringBuilder channel_caller_number;
        public StringBuilder channel_callee_number;
        public Dictionary<int, char> channel_call_dtmf;
        public fs_account_info channel_account_info;
        public APP_USER_STATUS channel_call_status;
        public CH_CALL_RECORD channel_call_record_info;
        public ChSocket channel_socket;
        public IWebSocketConnection channel_websocket;
        public IWebSocketConnection channel_websocket_P;
        public IWebSocketConnection channel_websocket_W;
        public channel_switchtactics_info channel_switch_tactics;
        public int IsRegister;
    }
}
