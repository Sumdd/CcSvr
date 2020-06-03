using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Model
{
    public class call_record_model
    {
        public int ID { get; set; }

        public string UniqueID { get; set; }

        public int CallType { get; set; }

        public int ChannelID { get; set; }

        public int LinkChannelID { get; set; }

        public string LocalNum { get; set; }

        public string T_PhoneNum { get; set; }

        public string C_PhoneNum { get; set; }

        public string PhoneAddress { get; set; }

        public string DtmfNum { get; set; }

        public int PhoneTypeID { get; set; }

        public int PhoneListID { get; set; }

        public int PriceTypeID { get; set; }

        public double CallPrice { get; set; }

        public int AgentID { get; set; }

        public int CusID { get; set; }

        public int ContactID { get; set; }

        public string RecordFile { get; set; }

        public string C_Date { get; set; }

        public string C_StartTime { get; set; }

        public string C_RingTime { get; set; }

        public string C_AnswerTime { get; set; }

        public string C_EndTime { get; set; }

        public int C_WaitTime { get; set; }

        public int C_SpeakTime { get; set; }

        public int CallResultID { get; set; }

        public int CallForwordFlag { get; set; }

        public string CallForwordChannelID { get; set; }

        public int SerOp_ID { get; set; }

        public string SerOp_DTMF { get; set; }

        public string SerOp_LeaveRec { get; set; }

        public string Detail { get; set; }

        public int Uhandler { get; set; }

        public string Remark { get; set; }

        public string recordName { get; set; }

        public int isshare { get; set; }

        public string FreeSWITCHIPv4 { get; set; }

        public string UAID { get; set; }

        public int fromagentid { get; set; }

        public string fromagentname { get; set; }

        public string fromloginname { get; set; }

        public string tnumber { get; set; }
        public call_record_model() { }

        public call_record_model(
            int _ChannelID,
            int _CallType = -1,
            string _LocalNum = null,
            string _T_PhoneNum = null,
            string _C_PhoneNum = null
            )
        {
            DateTime m_dtNow = DateTime.Now;
            UniqueID = Guid.NewGuid().ToString();
            CallType = _CallType;
            ChannelID = _ChannelID;
            LinkChannelID = -1;
            LocalNum = _LocalNum;
            T_PhoneNum = _T_PhoneNum;
            C_PhoneNum = _C_PhoneNum;
            PhoneAddress = "未知";
            DtmfNum = null;
            PhoneTypeID = -1;
            PhoneListID = -1;
            PriceTypeID = -1;
            CallPrice = -1;
            AgentID = -1;
            CusID = -1;
            ContactID = -1;
            RecordFile = null;
            C_Date = m_dtNow.ToString("yyyy-MM-dd HH:mm:ss");
            C_StartTime = m_dtNow.ToString("yyyy-MM-dd HH:mm:ss");
            C_RingTime = m_dtNow.ToString("yyyy-MM-dd HH:mm:ss");
            C_AnswerTime = null;
            C_EndTime = null;
            C_WaitTime = 0;
            C_SpeakTime = 0;
            CallResultID = -1;
            CallForwordFlag = -1;
            CallForwordChannelID = "-1";
            SerOp_ID = -1;
            SerOp_DTMF = null;
            SerOp_LeaveRec = null;
            Detail = null;
            Uhandler = 1;
            Remark = null;
            recordName = null;
            isshare = 0;
            FreeSWITCHIPv4 = null;
            UAID = null;
            fromagentid = -1;
            fromagentname = null;
            fromloginname = null;
            tnumber = null;
        }
    }
}
