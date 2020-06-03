using CenoSipFactory;
using DB.Basic;
using DB.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace CenoFsSharp
{
	public class PhoneNumOperate
	{
        //电话号码操作

		public static KeyValuePair<string, string> get_out_dial_number(string T_PhoneNum)
		{
			string C_PhoneNum=string.Empty;
			C_PhoneNum = GetNumbers(T_PhoneNum);
			KeyValuePair<string, string> PhoneInfo=new KeyValuePair<string, string>(C_PhoneNum, "未知");


			if (C_PhoneNum.Length < 7)
				return PhoneInfo;

			if (C_PhoneNum.StartsWith("1"))
			{
				call_phoneaddress_model _model=call_phoneaddress.GetModel(C_PhoneNum.Substring(0, 7));
				if (_model == null)
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, "未知");

				if (_model.CityCode == ParamLib.CityCode)
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, _model.CityName);
				else
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum = "0" + C_PhoneNum, _model.CityName);
			}

			if (!C_PhoneNum.StartsWith("01"))
			{
				if (C_PhoneNum.StartsWith("021"))
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, "上海");
				if (C_PhoneNum.StartsWith("022"))
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, "天津");
				if (C_PhoneNum.StartsWith("023"))
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, "重庆");

				call_phoneaddress_model _model=call_phoneaddress.get_model_by_citycode(C_PhoneNum.Substring(0, 4));
				if (_model == null)
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, "未知");

				if (_model.CityCode == ParamLib.CityCode)
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, _model.CityName);
				else
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum = "0" + C_PhoneNum, _model.CityName);

			}

			if (C_PhoneNum.StartsWith("010"))
				return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, "北京");

			C_PhoneNum = C_PhoneNum.Substring(1);

			if (C_PhoneNum.Length > 7)
			{
				call_phoneaddress_model _model=call_phoneaddress.GetModel(C_PhoneNum.Substring(0, 7));
				if (_model == null)
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, "未知");

				if (_model.CityCode == ParamLib.CityCode)
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum, _model.CityName);
				else
					return PhoneInfo = new KeyValuePair<string, string>(C_PhoneNum = "0" + C_PhoneNum, _model.CityName);
			}


			return PhoneInfo;
		}

		public static string GetNumbers(string p_str)
		{
			string   strReturn   =   string.Empty;
			if (p_str == null || p_str.Trim() == "")
			{
				strReturn = "";
			}

			foreach (char   chrTemp in p_str)
			{
				if (Char.IsNumber(chrTemp))
				{
					strReturn += chrTemp.ToString();
				}
			}
			return strReturn;
		}
	}
}
