using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DB.Basic;
using System.IO;
using DB.Model;
using CenoSipFactory;

namespace CenoSocket {
    /// <summary>
    /// 发送消息内容
    /// </summary>
    public class SocketCommand {
        public static string SendRecordStr(string RecordStr) {
            StringBuilder CommandStr = new StringBuilder();
            CommandStr.Append(call_socketcommand_util.GetStartStr("FSLY") + call_socketcommand_util.GetS_NameByS_NO("FSLY"));
            CommandStr.Append("{");
            call_socketcommand_model[] _Model = call_socketcommand_util.GetModelNodeByHeadInfo(call_socketcommand_util.GetS_NameByS_NO("FSLY"));
            string[] Param = new string[2] { Path.GetFileName(RecordStr), Path.GetDirectoryName(RecordStr) };
            if(_Model == null)
                return CommandStr.Append("}").ToString();

            for(int i = 0; i < _Model.Length; i++) {
                char[] NewChar = _Model[i].Rep_NewChar.ToCharArray();
                char[] OldChar = _Model[i].Rep_NewChar.ToCharArray();
                for(int j = 0; j < NewChar.Length; j++) {
                    RecordStr.ToString().Replace(NewChar[j], OldChar[j]);
                }
                CommandStr.Append(_Model[i].S_Name + ":" + Param[i] + ";");
            }
            return CommandStr.Append("}").ToString();
        }

        public static string SendDialInfo(string Status, string Reason) {
            StringBuilder CommandStr = new StringBuilder();
            CommandStr.Append(call_socketcommand_util.GetStartStr("BHZT") + call_socketcommand_util.GetS_NameByS_NO("BHZT"));
            CommandStr.Append("{");
            call_socketcommand_model[] _Model = call_socketcommand_util.GetModelNodeByHeadInfo(call_socketcommand_util.GetS_NameByS_NO("BHZT"));
            string[] Param = new string[2] { Status, Reason };
            if(_Model == null)
                return CommandStr.Append("}").ToString();

            for(int i = 0; i < _Model.Length; i++) {
                char[] NewChar = _Model[i].Rep_NewChar.ToCharArray();
                char[] OldChar = _Model[i].Rep_NewChar.ToCharArray();
                for(int j = 0; j < NewChar.Length; j++) {
                    Param[i].ToString().Replace(NewChar[j], OldChar[j]);
                }
                CommandStr.Append(_Model[i].S_Name + ":" + Param[i] + ";");
            }
            return CommandStr.Append("}").ToString();
        }

        public static string SendPhoneStr(string PhoneStr) {
            return "$EvenMsg{phone:" + PhoneStr + "}%";
        }

        public static string SendPickUpStr() {
            return "$EvenMsg{state:pickup;}%";
        }

        public static string SendSipNotConnectStr() {
            return "$EvenMsg{sipconnectinfo}%";
        }

        public static string SendFailStr() {
            return "$EvenMsg{state:fail;}%";
        }

        public static string SendInCallStr() {
            return "$EvenMsg{state:incall;}%";
        }

        public static string SendHungUp() {
            return "$EvenMsg{hangup:;}%";
        }

        public static string SendCallForwordStr() {
            return "$EvenMsg{state:fnumber;}%";
        }

        public static string SendTestSocketStr() {
            StringBuilder CommandStr = new StringBuilder();
            CommandStr.Append(call_socketcommand_util.GetStartStr("JCSLJ") + call_socketcommand_util.GetS_NameByS_NO("JCSLJ"));
            CommandStr.Append("{");
            call_socketcommand_model[] _Model = call_socketcommand_util.GetModelNodeByHeadInfo("JCSLJ");
            if(_Model == null)
                return CommandStr.Append("}" + call_socketcommand_util.GetEndStr("JCSLJ")).ToString();

            for(int i = 0; i < _Model.Length; i++) {
                CommandStr.Append(_Model[i].S_Name + ":;");
            }
            return CommandStr.Append("}" + call_socketcommand_util.GetEndStr("JCSLJ")).ToString();
        }

        public static string SendConnectStr() {
            StringBuilder CommandStr = new StringBuilder();
            CommandStr.Append(call_socketcommand_util.GetStartStr("LJFWQJG") + call_socketcommand_util.GetS_NameByS_NO("LJFWQJG"));
            CommandStr.Append("{");
            call_socketcommand_model[] _Model = call_socketcommand_util.GetModelNodeByHeadInfo("LJFWQJG");
            if(_Model == null)
                return CommandStr.Append("}" + call_socketcommand_util.GetEndStr("LJFWQJG")).ToString();

            for(int i = 0; i < _Model.Length; i++) {
                CommandStr.Append(_Model[i].S_Name + ":success;");
            }
            return CommandStr.Append("}" + call_socketcommand_util.GetEndStr("LJFWQJG")).ToString();
        }

        /// <summary>
        /// 通用的方法,直接传入S_NO即可
        /// </summary>
        /// <param name="S_NO"></param>
        /// <param name="S_Values">参数需一一对应上</param>
        /// <returns></returns>
        public static string SendCommonStr(string S_NO, params string[] S_Values) {
            StringBuilder CommandStr = new StringBuilder();
            CommandStr.Append(call_socketcommand_util.GetStartStr(S_NO) + call_socketcommand_util.GetS_NameByS_NO(S_NO));
            CommandStr.Append("{");
            call_socketcommand_model[] _Model = call_socketcommand_util.GetModelNodeByHeadInfo(S_NO);
            if(_Model == null)
                return CommandStr.Append("}" + call_socketcommand_util.GetEndStr(S_NO)).ToString();

            for(int i = 0; i < _Model.Length; i++) {
                CommandStr.Append(_Model[i].S_Name + $":{S_Values[i]};");
            }
            return CommandStr.Append("}" + call_socketcommand_util.GetEndStr(S_NO)).ToString();
        }

        public static string SendRecordFile(bool Result, string FileName) {
            return "$LoadRecordFile{Result:" + (Result ? "true" : "false") + ";File:" + FileName + ";}%";
        }

        public static string SystemExit(string Reason) {
            return "$SystemExit{reason:" + Reason + ";}%";
        }

        /// <summary>
        /// 发送批量下载录音进度
        /// </summary>
        /// <param name="Result">下载结果</param>
        /// <param name="ProgressInfo">进度描述</param>
        /// <returns></returns>
        public static string SendBotchRecordLoadProgress(string Result, string ProgressInfo) {
            return "$BotchRecordLoadProgress{Result:" + Result + ";Progress:" + ProgressInfo + ";ServerIp:" + ParamLib.CallIpAddress + ";}%";
        }
    }
}
