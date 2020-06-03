using DataBaseModel;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
	public class call_switchtactics
	{
		public static call_switchtactics_model GetModelByChUid(string ChUid)
		{
			var model = new call_switchtactics_model();
			string sql="select ID,UniqueID,ChUid,GwUid,TactcisTypeID,ChannelUid,ChannelGroupUid,GatewayUid,GatewayGroupUid,RingCntPickup,RingCnt,RecordingMoment,TacticsModelID,IvrID,AutoPlayFile,AutoPlayCnt,AutoPlayDetectDtmfStop,MemVerID,ChBusID,TransferOtherNumber,TransferAssignationChannelID,TransferAssignationGroupID,TransferPlayChannelNumber,NoAnswerRemind,Remark from call_switchtactics where ChUid=?ChUid limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?ChUid", MySqlDbType.VarChar,36)
				};
			parameters[0].Value = ChUid;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.UniqueID = dr["UniqueID"].ToString();
					model.ChUid = dr["ChUid"].ToString();
					model.GwUid = dr["GwUid"].ToString();
					model.TactcisTypeID = int.Parse(dr["TactcisTypeID"].ToString());
					model.ChannelUid = dr["ChannelUid"].ToString();
					model.ChannelGroupUid = dr["ChannelGroupUid"].ToString();
					model.GatewayUid = dr["GatewayUid"].ToString();
					model.GatewayGroupUid = dr["GatewayGroupUid"].ToString();
					model.RingCntPickup = dr["RingCntPickup"].ToString();
					model.RingCnt = int.Parse(dr["RingCnt"].ToString());
					model.RecordingMoment = dr["RecordingMoment"].ToString();
					model.TacticsModelID = int.Parse(dr["TacticsModelID"].ToString());
					model.IvrID = int.Parse(dr["IvrID"].ToString());
					model.AutoPlayFile = dr["AutoPlayFile"].ToString();
					model.AutoPlayCnt = int.Parse(dr["AutoPlayCnt"].ToString());
					model.AutoPlayDetectDtmfStop = int.Parse(dr["AutoPlayDetectDtmfStop"].ToString());
					model.MemVerID = int.Parse(dr["MemVerID"].ToString());
					model.ChBusID = int.Parse(dr["ChBusID"].ToString());
					model.TransferOtherNumber = dr["TransferOtherNumber"].ToString();
					model.TransferAssignationChannelID = int.Parse(dr["TransferAssignationChannelID"].ToString());
					model.TransferAssignationGroupID = int.Parse(dr["TransferAssignationGroupID"].ToString());
					model.TransferPlayChannelNumber = int.Parse(dr["TransferPlayChannelNumber"].ToString());
					model.NoAnswerRemind = int.Parse(dr["NoAnswerRemind"].ToString());
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}


		public static call_switchtactics_model GetModelByGwUid(string GwUid)
		{
			var model = new call_switchtactics_model();
			string sql="select ID,UniqueID,ChUid,GwUid,TactcisTypeID,ChannelUid,ChannelGroupUid,GatewayUid,GatewayGroupUid,RingCntPickup,RingCnt,RecordingMoment,TacticsModelID,IvrID,AutoPlayFile,AutoPlayCnt,AutoPlayDetectDtmfStop,MemVerID,ChBusID,TransferOtherNumber,TransferAssignationChannelID,TransferAssignationGroupID,TransferPlayChannelNumber,NoAnswerRemind,Remark from call_switchtactics where GwUid=?GwUid limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?GwUid", MySqlDbType.VarChar,36)
				};
			parameters[0].Value = GwUid;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.UniqueID = dr["UniqueID"].ToString();
					model.ChUid = dr["ChUid"].ToString();
					model.GwUid = dr["GwUid"].ToString();
					model.TactcisTypeID = int.Parse(dr["TactcisTypeID"].ToString());
					model.ChannelUid = dr["ChannelUid"].ToString();
					model.ChannelGroupUid = dr["ChannelGroupUid"].ToString();
					model.GatewayUid = dr["GatewayUid"].ToString();
					model.GatewayGroupUid = dr["GatewayGroupUid"].ToString();
					model.RingCntPickup = dr["RingCntPickup"].ToString();
					model.RingCnt = int.Parse(dr["RingCnt"].ToString());
					model.RecordingMoment = dr["RecordingMoment"].ToString();
					model.TacticsModelID = int.Parse(dr["TacticsModelID"].ToString());
					model.IvrID = int.Parse(dr["IvrID"].ToString());
					model.AutoPlayFile = dr["AutoPlayFile"].ToString();
					model.AutoPlayCnt = int.Parse(dr["AutoPlayCnt"].ToString());
					model.AutoPlayDetectDtmfStop = int.Parse(dr["AutoPlayDetectDtmfStop"].ToString());
					model.MemVerID = int.Parse(dr["MemVerID"].ToString());
					model.ChBusID = int.Parse(dr["ChBusID"].ToString());
					model.TransferOtherNumber = dr["TransferOtherNumber"].ToString();
					model.TransferAssignationChannelID = int.Parse(dr["TransferAssignationChannelID"].ToString());
					model.TransferAssignationGroupID = int.Parse(dr["TransferAssignationGroupID"].ToString());
					model.TransferPlayChannelNumber = int.Parse(dr["TransferPlayChannelNumber"].ToString());
					model.NoAnswerRemind = int.Parse(dr["NoAnswerRemind"].ToString());
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}

		public static call_switchtactics_model GetModelByUniqueID(string UniqueID)
		{
			var model = new call_switchtactics_model();
			string sql="select ID,UniqueID,ChUid,TactcisTypeID,ChannelUid,ChannelGroupUid,GatewayUid,GatewayGroupUid,RingCntPickup,RingCnt,RecordingMoment,TacticsModelID,IvrID,AutoPlayFile,AutoPlayCnt,AutoPlayDetectDtmfStop,MemVerID,ChBusID,TransferOtherNumber,TransferAssignationChannelID,TransferAssignationGroupID,TransferPlayChannelNumber,NoAnswerRemind,Remark from call_switchtactics where UniqueID=?UniqueID limit 1";
			MySqlParameter[] parameters = {
	 new MySqlParameter("?UniqueID", MySqlDbType.VarChar,36)
				};
			parameters[0].Value = UniqueID;
			using (var dr = MySQL_Method.ExecuteDataReader(sql, parameters))
			{
				if (dr.Read())
				{
					model.ID = int.Parse(dr["ID"].ToString());
					model.UniqueID = dr["UniqueID"].ToString();
					model.ChUid = dr["ChUid"].ToString();
					model.TactcisTypeID = int.Parse(dr["TactcisTypeID"].ToString());
					model.ChannelUid = dr["ChannelUid"].ToString();
					model.ChannelGroupUid = dr["ChannelGroupUid"].ToString();
					model.GatewayUid = dr["GatewayUid"].ToString();
					model.GatewayGroupUid = dr["GatewayGroupUid"].ToString();
					model.RingCntPickup = dr["RingCntPickup"].ToString();
					model.RingCnt = int.Parse(dr["RingCnt"].ToString());
					model.RecordingMoment = dr["RecordingMoment"].ToString();
					model.TacticsModelID = int.Parse(dr["TacticsModelID"].ToString());
					model.IvrID = int.Parse(dr["IvrID"].ToString());
					model.AutoPlayFile = dr["AutoPlayFile"].ToString();
					model.AutoPlayCnt = int.Parse(dr["AutoPlayCnt"].ToString());
					model.AutoPlayDetectDtmfStop = int.Parse(dr["AutoPlayDetectDtmfStop"].ToString());
					model.MemVerID = int.Parse(dr["MemVerID"].ToString());
					model.ChBusID = int.Parse(dr["ChBusID"].ToString());
					model.TransferOtherNumber = dr["TransferOtherNumber"].ToString();
					model.TransferAssignationChannelID = int.Parse(dr["TransferAssignationChannelID"].ToString());
					model.TransferAssignationGroupID = int.Parse(dr["TransferAssignationGroupID"].ToString());
					model.TransferPlayChannelNumber = int.Parse(dr["TransferPlayChannelNumber"].ToString());
					model.NoAnswerRemind = int.Parse(dr["NoAnswerRemind"].ToString());
					model.Remark = dr["Remark"].ToString();
				}
			}
			return model;
		}

	}
}
