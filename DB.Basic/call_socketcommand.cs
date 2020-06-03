using System;
using System.Data;
using System.Collections;

using DB.Model;
using System.Text;

namespace DB.Basic
{
	public class call_socketcommand_util
	{

		private call_socketcommand_model _Call_SocketCommandModel;

		public call_socketcommand_util()
		{
			this._Call_SocketCommandModel = new call_socketcommand_model();
		}

		public call_socketcommand_model Find(int ID)
		{
			//DataTable dt = SQL_Method.BindTable("select * from Call_SocketCommand where ID=" + ID.ToString());
			//this._Call_SocketCommandModel.CreateTime = Convert.ToDateTime(dt.Rows[0]["CreateTime"].ToString());
			//this._Call_SocketCommandModel.LoseTime = Convert.ToDateTime(dt.Rows[0]["LoseTime"].ToString());
			//this._Call_SocketCommandModel.P_Description = dt.Rows[0]["P_Description"].ToString();
			//this._Call_SocketCommandModel.P_Group = dt.Rows[0]["P_Group"].ToString();
			//this._Call_SocketCommandModel.P_Name = dt.Rows[0]["P_Name"].ToString();
			//this._Call_SocketCommandModel.P_Value = dt.Rows[0]["P_Value"].ToString();
			//this._Call_SocketCommandModel.Remark = dt.Rows[0]["Remark"].ToString();

			return this._Call_SocketCommandModel;
		}

		public call_socketcommand_model Find(string P_Name)
		{
			//DataTable dt = SQL_Method.BindTable("select * from Call_SocketCommand where P_Name='" + P_Name + "'");
			//this._Call_SocketCommandModel.CreateTime = Convert.ToDateTime(dt.Rows[0]["CreateTime"].ToString());
			//this._Call_SocketCommandModel.LoseTime = Convert.ToDateTime(dt.Rows[0]["LoseTime"].ToString());
			//this._Call_SocketCommandModel.P_Description = dt.Rows[0]["P_Description"].ToString();
			//this._Call_SocketCommandModel.P_Group = dt.Rows[0]["P_Group"].ToString();
			//this._Call_SocketCommandModel.P_Name = dt.Rows[0]["P_Name"].ToString();
			//this._Call_SocketCommandModel.P_Value = dt.Rows[0]["P_Value"].ToString();
			//this._Call_SocketCommandModel.Remark = dt.Rows[0]["Remark"].ToString();

			return this._Call_SocketCommandModel;
		}

		public static string[] GetEndStr()
		{
			DataTable dt = MySQL_Method.BindTable("select DISTINCT S_EndChar from Call_SocketCommand where S_EndChar<>'' and S_EndChar is not Null");
			if (dt.Rows.Count <= 0)
				return null;
			string[] str = new string[dt.Rows.Count];
			for (int i = 0; i < dt.Rows.Count; i++)
			{
				str[i] = dt.Rows[i]["S_EndChar"].ToString();
			}
			return str;
		}

		public static string GetEndStr(string S_NO)
		{
			return MySQL_Method.ExecuteScalar("select DISTINCT S_EndChar from Call_SocketCommand where S_EndChar<>'' and S_EndChar is not Null and S_NO='" + S_NO + "'").ToString();
		}

		public static string[] GetStartStr()
		{
			DataTable dt = MySQL_Method.BindTable("select DISTINCT S_StartChar from Call_SocketCommand where S_StartChar<>'' and S_StartChar is not Null");
			if (dt.Rows.Count <= 0)
				return null;
			string[] str = new string[dt.Rows.Count];
			for (int i = 0; i < dt.Rows.Count; i++)
			{
				str[i] = dt.Rows[i]["S_StartChar"].ToString();
			}
			return str;
		}

		public static string GetStartStr(string S_NO)
		{
			return MySQL_Method.ExecuteScalar("select DISTINCT S_StartChar from Call_SocketCommand where S_StartChar<>'' and S_StartChar is not Null and S_NO='" + S_NO + "'").ToString();
		}

		public static bool HeadInfoContain(string HeadInfo)
		{
			object obj = MySQL_Method.ExecuteScalar("select * from Call_SocketCommand where S_Name='" + HeadInfo + "'");
			if (obj == null)
				return false;
			return true;
		}

		public bool Update(string P_Name, string P_Value)
		{
			if (MySQL_Method.ExecuteNonQuery("update Call_Param set P_Value='" + P_Value + "' where P_Name='" + P_Name + "'") > 0)
				return true;
			return false;
		}
		public static string GetHeadInfo(string HeadInfo)
		{
			return MySQL_Method.ExecuteScalar("select S_NO from Call_SocketCommand where S_Name='" + HeadInfo + "'").ToString();
		}

		public static string GetS_NameByS_NO(string S_NO)
		{
			return MySQL_Method.ExecuteScalar("select S_Name from Call_SocketCommand where S_NO='" + S_NO + "'").ToString();
		}

		public static string GetNewChar(string S_Name)
		{
			return MySQL_Method.ExecuteScalar("select Rep_NewChar from Call_SocketCommand where S_Name='" + S_Name + "'").ToString();
		}

		public static string GetOldChar(string S_Name)
		{
			return MySQL_Method.ExecuteScalar("select Rep_OldChar from Call_SocketCommand where S_Name='" + S_Name + "'").ToString();
		}

		public static string[] GetParamByHeadName(string S_Name)
		{

			DataTable dt = MySQL_Method.BindTable("select S_Name from Call_SocketCommand where S_ParentID=(SELECT ID from Call_SocketCommand where S_NO='" + S_Name + "' limit 0,1) ORDER BY S_Order asc");
			if (dt.Rows.Count <= 0)
				return null;
			string[] str = new string[dt.Rows.Count];
			for (int i = 0; i < dt.Rows.Count; i++)
			{
				str[i] = dt.Rows[i]["S_Name"].ToString();
			}
			return str;
		}

		public static call_socketcommand_model[] GetModelNodeByHeadInfo(string S_NO)
		{
			DataTable dt = MySQL_Method.BindTable("select * from Call_SocketCommand where S_NO='" + S_NO + "' and S_ParentID<>0 order by S_Order ASC");
			if (dt.Rows.Count >= 0)
			{
				call_socketcommand_model[] _Model = new call_socketcommand_model[dt.Rows.Count];
				for (int i = 0; i < dt.Rows.Count; i++)
				{
					_Model[i] = new call_socketcommand_model();
					_Model[i].ID = int.Parse(dt.Rows[i]["ID"].ToString());
					_Model[i].Rep_NewChar = dt.Rows[i]["Rep_NewChar"].ToString();
					_Model[i].Rep_OldChar = dt.Rows[i]["Rep_OldChar"].ToString();
					_Model[i].S_EndChar = dt.Rows[i]["S_EndChar"].ToString();
					_Model[i].S_Name = dt.Rows[i]["S_Name"].ToString();
					_Model[i].S_NO = dt.Rows[i]["S_NO"].ToString();
					_Model[i].S_Order = int.Parse(dt.Rows[i]["S_Order"].ToString());
					_Model[i].S_ParentID = int.Parse(dt.Rows[i]["S_ParentID"].ToString());
					_Model[i].S_StartChar = dt.Rows[i]["S_StartChar"].ToString();
					_Model[i].S_Value = dt.Rows[i]["S_Value"].ToString();
				}
				return _Model;
			}
			return null;
		}

        public static string SendCommonStr(string S_NO, params string[] S_Values)
        {
            StringBuilder CommandStr = new StringBuilder();
            CommandStr.Append(call_socketcommand_util.GetStartStr(S_NO) + call_socketcommand_util.GetS_NameByS_NO(S_NO));
            CommandStr.Append("{");
            call_socketcommand_model[] _Model = call_socketcommand_util.GetModelNodeByHeadInfo(S_NO);
            if (_Model == null)
                return CommandStr.Append("}" + call_socketcommand_util.GetEndStr(S_NO)).ToString();

            for (int i = 0; i < _Model.Length; i++)
            {
                CommandStr.Append(_Model[i].S_Name + $":{S_Values[i]};");
            }
            return CommandStr.Append("}" + call_socketcommand_util.GetEndStr(S_NO)).ToString();
        }
    }
}




