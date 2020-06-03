using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace CenoSipFactory
{
	public class AccGroup
	{
		public AccGroup()
		{

		}
		public AccGroup(string GroupXml)
		{


		}

		private string _GroupName = "";
		public string GroupName
		{
			get { return _GroupName; }
			set { _GroupName = value; }
		}

		public List<AccountInfo> Account
		{
			get;
			set;
		}
	}

	public class IntiAccGroup
	{
		public static List<AccGroup> GetAccGroup(string GroupXml, List<AccountInfo> _AccList)
		{
			List<AccGroup> accgroup = new List<AccGroup>();
			if (!File.Exists(GroupXml))
				return accgroup;

			XmlDocument XmlDoc = new XmlDocument();
			XmlDoc.Load(GroupXml);

			XmlNodeList GroupXnl = XmlDoc.GetElementsByTagName("group");

			foreach (XmlNode xn in GroupXnl)
			{
				AccGroup _accgoup = new AccGroup();
				_accgoup.GroupName = xn.Attributes[0].Value;
				_accgoup.Account = new List<AccountInfo>();

				if (xn.FirstChild.ChildNodes.OfType<XmlNode>().FirstOrDefault<XmlNode>(x => (x.Name == "user" || x.Name == "X-PRE-PROCESS") && x.Attributes[0].Value == "include" && x.Attributes[1].Value == "default/*.xml") != null)
					_accgoup.Account = _AccList;
				else
				{
					foreach (XmlNode _xn in xn.FirstChild.ChildNodes)
					{
						if (_xn.Name == "user" || _xn.Name == "X-PRE-PROCESS")
						{
							_accgoup.Account.Add(_AccList.Find(x => x.userid == _xn.Attributes[0].Value));
						}
					}
				}
				accgroup.Add(_accgoup);
			}

			return accgroup;
		}
	}
}
