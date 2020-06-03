using System;
using NEventSocket;
using System.Reactive.Linq;
using CenoSipFactory;
using Core_v1;

namespace Outbound_v1 {
    /// <summary>
    /// 外联模式主入口
    /// </summary>
    public class OutboundMain : IDisposable {
        private static OutboundListener listener = null;
        private static int port = 8041;
        public static void Start() {
            listener = new OutboundListener(port);
            listener.Connections.Subscribe(
                async socket => {
                    await socket.Connect();
                    Log.Instance.Success($"[Outbound_v1][OutboundMain][Start][新的呼入请求,UUID:{socket.ChannelData.UUID}]");

                    foreach(var item in socket.ChannelData.Headers) {
                        Log.Instance.Success(string.Format("[Outbound_v1][OutboundMain][Start][{0}:{1}]", item.Key, item.Value));
                    }

                    //await socket.Hangup(socket.ChannelData.UUID, NEventSocket.FreeSwitch.HangupCause.NormalClearing);
                    socket.ChannelEvents.Subscribe(
                        e => {
                            Log.Instance.Success($"[Outbound_v1][OutboundMain][Start][{e.EventName}]");
                        });
                    socket.ConferenceEvents.Subscribe(
                        e => {
                            Log.Instance.Success($"[Outbound_v1][OutboundMain][Start][{e.EventName}]");
                        });
                    socket.Events.Subscribe(
                        e => {
                            Log.Instance.Success($"[Outbound_v1][OutboundMain][Start][{e.EventName}]");
                        });
                });
            listener.Start();
            Log.Instance.Success($"[Outbound_v1][OutboundMain][Start][外联模式开启,监听端口:{port}]");
        }
        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose() {
            listener.Dispose();
        }
    }
}
