using Core_v1;
using Fleck;
//using Model_v1;
//using Newtonsoft.Json;
using System;

namespace WebSocket_v1 {
    internal sealed class OutWebSocketDo {
        internal static void MainStep(IWebSocketConnection socket, string message) {
            //try {
            //    M_InWebSocketDialog m_SocketDialog = JsonConvert.DeserializeObject<M_InWebSocketDialog>(message);
            //    switch(m_SocketDialog.operate) {
            //        default: {
            //                Log.Instance.Warn($"[OutWebSocketDo][MainStep][default][{OutWebSocketMain.Prefix}检测为无效的操作类型:{m_SocketDialog.operate}]");
            //            }
            //            break;
            //    }
            //} catch(JsonException ex) {
            //    Log.Instance.Error($"[OutWebSocketDo][MainStep][JsonException][{ex.Message}][数据内容:{message}]");
            //} catch(ArgumentNullException ex) {
            //    Log.Instance.Error($"[OutWebSocketDo][MainStep][ArgumentNullException][{ex.Message}][数据内容:{message}]");
            //} catch(Exception ex) {
            //    Log.Instance.Error($"[OutWebSocketDo][MainStep][Exception][{ex.Message}][数据内容:{message}]");
            //}
        }
    }
}
