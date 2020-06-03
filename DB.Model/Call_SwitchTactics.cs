// ClassName  : call_switchtacticsModel
// Creator    : Sun Zhongguan
// CreateTime : 2016-02-19 14:26:19
// Copyright  : Cenozoic Software Co.,Ltd  www.ceno-soft.net

using System;
using System.Data;
using System.Collections;
namespace DataBaseModel
{
	public class call_switchtactics_model
	{
		public call_switchtactics_model()
		{

		}

		public int ID { get; set; }

		public string UniqueID { get; set; }

		public string ChUid { get; set; }

		public string GwUid { get; set; }

		public int TactcisTypeID { get; set; }

		public string ChannelUid { get; set; }

		public string ChannelGroupUid { get; set; }

		public string GatewayUid { get; set; }

		public string GatewayGroupUid { get; set; }

		/// <summary>
		/// RIGHTNOW_PICKUP,PICKUP_USER_PICKUP,DEFINE_RINGCOUNT
		/// </summary>
		public string RingCntPickup { get; set; }

		public int RingCnt { get; set; }

		public string RecordingMoment { get; set; }

		public int TacticsModelID { get; set; }

		public int IvrID { get; set; }

		public string AutoPlayFile { get; set; }

		public int AutoPlayCnt { get; set; }

		/// <summary>
		/// will stop play when detect DTMF
		/// </summary>
		public int AutoPlayDetectDtmfStop { get; set; }

		public int MemVerID { get; set; }

		public int ChBusID { get; set; }

		public string TransferOtherNumber { get; set; }

		public int TransferAssignationChannelID { get; set; }

		public int TransferAssignationGroupID { get; set; }

		public int TransferPlayChannelNumber { get; set; }

		public int NoAnswerRemind { get; set; }

		public string Remark { get; set; }

	}
}