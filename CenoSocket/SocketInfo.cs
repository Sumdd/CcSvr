using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using DB.Basic;

namespace CenoSocket
{
	public class SocketInfo
	{
		protected string _HeadInfo;
		public string HeadInfo
		{
			get { return _HeadInfo; }
		}

		protected Hashtable _Content;
		public Hashtable Content
		{
			get { return _Content; }
		}

		public SocketInfo(string Data)
		{
			string SocektData = Data;
			int w = SocektData.IndexOf('{');
			_HeadInfo = SocektData.Substring(0, w);
			if (!call_socketcommand_util.HeadInfoContain(_HeadInfo))
				return;

			string information = SocektData.Substring(w + 1, SocektData.Length - w - 2);
			string[] Info = information.Split(';');
			_Content = new Hashtable();
			for (int i = 0; i < Info.Length; i++)
			{
				if (!string.IsNullOrEmpty(Info[i].ToString()))
				{
					string[] HTinfo = Info[i].ToString().Split(':');
					char[] NewChar = call_socketcommand_util.GetNewChar(HTinfo[0]).ToCharArray();
					char[] OldChar = call_socketcommand_util.GetOldChar(HTinfo[0]).ToCharArray();
					for (int j = 0; j < NewChar.Length; j++)
					{
						HTinfo[1].ToString().Replace(NewChar[j], OldChar[j]);
					}
					Content.Add(HTinfo[0].ToString(), HTinfo[1].ToString());
				}
			}
		}

		public static string GetValueByKey(Hashtable _HashTable, string Key)
		{
			string values = string.Empty;
			foreach (DictionaryEntry Element in _HashTable)
			{
				if (Element.Key.ToString() == Key)
				{
					values = Element.Value.ToString();
				}
			}
			return values;
		}
	}
}
