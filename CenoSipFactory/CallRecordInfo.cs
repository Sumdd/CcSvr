using DB.Basic;
using log4net;
using System;
using System.Linq;
using System.Text;

namespace CenoSipFactory
{
	public class CH_CALL_RECORD
	{
		private static log4net.ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private CH_CALL_RECORD()
		{

		}

		public static CH_CALL_RECORD Instance()
		{
			return new CH_CALL_RECORD();
		}

		public static CH_CALL_RECORD Instance(ChannelInfo _info)
		{
			CH_CALL_RECORD _Record = new CH_CALL_RECORD();
			try
			{
				var _agent_info=call_factory.agent_list.FirstOrDefault(x => x.ChInfo == _info);
				_Record.AgentID = _agent_info.AgentID;
				_Record.ChannelID = _info.channel_id;
				_Record.LocalNum = _agent_info.AgentNum;
				_Record.LinkChannelID = -1;
				_Record.Call_Date = DateTime.Now.ToString("yyyy-MM-dd");
				_Record.T_PhoneNum = string.IsNullOrEmpty(_info.channel_callee_number.ToString()) ? _info.channel_caller_number : _info.channel_callee_number;
                _Record.PhoneAddress = string.Empty;
                    //减小压力
                    //call_phoneaddress.GetModel(_info.channel_callee_number.ToString()).CityName;
				_Record.DTMF_Str = new StringBuilder();
				_Record.PhoneTypeID = -1;
				_Record.PhoneListID = -1;
				_Record.PriceTypeID = -1;
				_Record.CallPrice = -1;
				_Record.CusID = -1;
				_Record.ContactID = -1;
				_Record.CallForwardFlag = -1;
				_Record.CallForwardChannelID = -1;
				_Record.ServiceOperateID = -1;
				_Record.ServiceOperateDTMF = new StringBuilder();
				_Record.ServiceOperateLeaveRec = "";
				_Record.CallDetail = "";
				_Record.Remark = "";
			}
			catch (Exception ex)
			{
				throw ex;
			}

			return _Record;
		}

		public int ID;
		public int CallType;
		public int ChannelID;
		public int LinkChannelID;
		public StringBuilder T_PhoneNum;
		public StringBuilder C_PhoneNum;
		public string LocalNum;
		public string PhoneAddress;
		public StringBuilder DTMF_Str;

		public int PhoneTypeID;
		public int PhoneListID;
		public int PriceTypeID;
		public decimal CallPrice;

		public int AgentID;
		public int CusID;
		public int ContactID;

		public string RecFile;

		public string Call_Date;
		public string C_StartTime;
		public string C_RingTime;
		public string C_AnswerTime;
		public string C_EndTime;

		public int CallResultID;

		public int CallForwardFlag;
		public int CallForwardChannelID;

		public int ServiceOperateID;
		public StringBuilder ServiceOperateDTMF;
		public string ServiceOperateLeaveRec;

		public string CallDetail;
		public string Remark;
	}

}
