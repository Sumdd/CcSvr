using CenoSipFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB.Basic;
using DB.Model;
using CenoCommon;

namespace CenoFsSharp
{
	public class CallRecord
	{
		public static async Task<CallRecordResult> InsertCallRecord(CH_CALL_RECORD _Record)
		{
			return Task.Run<CallRecordResult>(() =>
			{
				try
				{
					if (_Record == null)
						throw new ArgumentNullException();

					call_record_model _model=new call_record_model();
					_model.UniqueID = Guid.NewGuid().ToString();
					_model.AgentID = _Record.AgentID;
					_model.C_AnswerTime = _Record.C_AnswerTime;
					_model.C_Date = _Record.Call_Date;
					_model.C_EndTime = _Record.C_EndTime;
					_model.C_PhoneNum = _Record.C_PhoneNum.ToString();
					_model.C_RingTime = _Record.C_RingTime;
					_model.C_StartTime = _Record.C_StartTime;
					_model.C_SpeakTime = CommonClass.GetTimespanSubtract(_model.C_AnswerTime, _model.C_EndTime).Seconds;
					_model.C_WaitTime = CommonClass.GetTimespanSubtract(_model.C_StartTime, _model.C_AnswerTime).Seconds;
					_model.CallForwordChannelID = _Record.CallForwardChannelID.ToString();
					_model.CallForwordFlag = _Record.CallForwardFlag;
					_model.CallPrice = Convert.ToDouble(_Record.CallPrice);
					_model.CallResultID = _Record.CallResultID;
					_model.CallType = _Record.CallType;
					_model.ChannelID = _Record.ChannelID;
					_model.ContactID = _Record.ContactID;
					_model.CusID = _Record.CusID;
					_model.Detail = _Record.CallDetail;
					_model.DtmfNum = _Record.DTMF_Str.ToString();
					_model.LinkChannelID = _Record.LinkChannelID;
					_model.LocalNum = _Record.LocalNum;
					_model.PhoneAddress = _Record.PhoneAddress;
					_model.PhoneListID = _Record.PhoneListID;
					_model.PhoneTypeID = _Record.PhoneTypeID;
					_model.PriceTypeID = _Record.PriceTypeID;
					_model.RecordFile = _Record.RecFile;
					_model.Remark = _Record.Remark;
					_model.SerOp_DTMF = _Record.ServiceOperateDTMF.ToString();
					_model.SerOp_ID = _Record.ServiceOperateID;
					_model.SerOp_LeaveRec = _Record.ServiceOperateLeaveRec;
					_model.T_PhoneNum = _Record.T_PhoneNum.ToString();

					call_record.Insert(_model);
					return new CallRecordResult(true, "");


				}
				catch (Exception ex)
				{
					return new CallRecordResult(false, ex.Message);
				}
			}).Result;

		}
	}
}
