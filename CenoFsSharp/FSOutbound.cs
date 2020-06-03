using System;
using NEventSocket;
using CenoSipFactory;

using System.Reactive.Linq;
using log4net;
using Core_v1;

namespace CenoFsSharp {
    public class OutBoundListenService {
        public static readonly int ESL_SUCCESS = 1;

        private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void begin_listen() {
            try {
                OutboundListener _Listener = new OutboundListener(ParamLib.FsTcpPort);
                _Listener.Connections.Subscribe(
                    async connection => {
                        if(_Listener.IsStarted) {
                            try {
                                await connection.Connect();
                            } catch(Exception ex) {
                                Log.Instance.Debug($"[CenoFsSharp][OutBoundListenService][begin_listen][Exception][{ex.Message}]");
                                Log.Instance.Debug(ex);
                            }
                            if(connection.IsConnected) {
                                //connection.Events.Subscribe(
                                //    e => {
                                //        ReceiveEventFunc.ReceiveMessage(connection, e);
                                //    });
                                ChannelFunc.m_fDoCall(connection);
                            }
                        }
                    });
                _Listener.Start();
                Log.Instance.Success($"[CenoFsSharp][OutBoundListenService][begin_listen][outbound start success,port:{ParamLib.FsTcpPort}]");
            } catch(Exception ex) {
                Log.Instance.Error($"[CenoFsSharp][OutBoundListenService][begin_listen][Exception][outbound start error,port:{ParamLib.FsTcpPort},{ex.Message}]");
                Log.Instance.Debug(ex);
                Log.Instance.Debug(ex.StackTrace);
            }
        }
    }
}
