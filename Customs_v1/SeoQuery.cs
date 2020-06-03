using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB.Basic;
using DB.Model;
using Core_v1;
using System.Xml;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace Customs_v1 {
    public static class SeoQuery {
        /// <summary>
        /// 查询电话号码归属地
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public static string PhoneAddress(string phoneNubmer) {
            string result = "";
            try {
                if(phoneNubmer.StartsWith("*"))
                    return "内呼";

                //号码处理
                string temp = phoneNubmer.TrimStart(new char[] { '0' });
                if(temp.Length < 2)
                    return "";
                int startZeroCount = phoneNubmer.Length - temp.Length;
                string number = "";
                if(temp.StartsWith("1") && temp.ToCharArray()[1] >= '3' && temp.Length >= 7) {
                    //手机号码
                    number = temp.Substring(0, 7);
                } else {
                    //固定电话
                    switch(temp.Substring(0, 1)) {
                        case "1":
                        case "2":  //部分省级市
                            number = "0" + temp.Substring(0, 2);
                            break;
                        default:   //省级代码
                            {
                                string num = temp.Substring(0, 3);
                                if(num == "852" || num == "853" || num == "856") {
                                    number = (startZeroCount >= 2 ? "00" : "0") + num;
                                } else {
                                    number = "0" + num;
                                }
                            }
                            break;
                    }
                }

                if(number.Length < 3)
                    return "";

                string _CityName = string.Empty;
                try {
                    _CityName = call_phoneaddress.GetCityCode(number);
                } catch(Exception ex) {
                    Log.Instance.Success($"[Customs_v1][SeoQuery][PhoneAddress][本地查询错误:{ex.Message}]");
                }

                if(!string.IsNullOrWhiteSpace(_CityName)) {
                    result = _CityName;// + " , " + dt.Rows[0][1].ToString();
                } else {
                    if(number.Length == 7 && !Call_ParamUtil.ExitsAccessNETLimitFlag) {
                        //联网查询
                        result = NetQueryPhoneAddress(number).Rows[0]["location"].ToString();
                    }
                    result = "未知归属地";
                }
            } catch(Exception ex) {
                Log.Instance.Success($"[Customs_v1][SeoQuery][PhoneAddress][联网查询错误:{ex.Message}]");
            }
            return result;
        }
        /// <summary>
        /// 查询手机号码归属地
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static System.Data.DataTable NetQueryPhoneAddress(string number) {
            System.Data.DataTable resultT = new DataTable();
            resultT.Columns.Add("location");
            DataRow dr = resultT.NewRow();
            try {
                //TODO:确定网络可用
                //有道API接口
                string url = "http://www.youdao.com/smartresult-xml/search.s?type=mobile&q=" + number;
                XmlDocument XMLResponse = new XmlDocument();
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                using(WebResponse wr = req.GetResponse()) {
                    using(StreamReader sr = new StreamReader(wr.GetResponseStream(), Encoding.GetEncoding("gbk"))) {
                        string Response = sr.ReadToEnd();
                        sr.Close();
                        XMLResponse.LoadXml(Response);
                    }
                }
                XmlNode XMLResult = XMLResponse.SelectSingleNode("smartresult");
                if(XMLResult != null) {
                    if(XMLResult.HasChildNodes) {//有结果
                        XmlNode Address = XMLResult.ChildNodes[0].SelectSingleNode("location");
                        if(Address != null) {
                            string str = Address.InnerText.ToString();
                            dr["location"] = str;
                        }
                    } else {//没有结果

                    }
                }
            } catch(Exception ex) {
                Log.Instance.Success($"[Customs_v1][SeoQuery][NetQueryPhoneAddress][联网查询错误:{ex.Message}]");
            }
            resultT.Rows.Add(dr);
            return resultT;
        }
        /// <summary>
        /// 检测电话,已经拼写正确
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public static string PhoneRegex(string phoneNumber) {
            var _phoneNumber = phoneNumber;

            //852香港
            //853澳门
            //886台湾

            //这种情况是港澳台或者错误
            if(_phoneNumber.StartsWith("0")) {
                if(_phoneNumber.Length >= 4) {
                    if(_phoneNumber.StartsWith("0852") || _phoneNumber.StartsWith("0853") || _phoneNumber.StartsWith("0886")) {
                        return "0" + _phoneNumber;
                    }
                }
            } else {
                if(_phoneNumber.Length < 7)
                    return _phoneNumber;
            }

            //判断是否是固话
            Regex rg1 = new Regex("^[2-9]{2}[0-9]*");
            if(rg1.IsMatch(_phoneNumber))
                return "0" + _phoneNumber;

            //以正常电话返回,包括错误
            return _phoneNumber;
        }
    }
}
