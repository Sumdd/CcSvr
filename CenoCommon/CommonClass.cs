using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace CenoCommon
{
	public class CommonClass
	{
		public delegate bool ConsoleCtrlDelegate(int dwCtrlType);
		public const int CTRL_CLOSE_EVENT = 2;
		[DllImport("kernel32.dll")]
		public static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);


		public static Dictionary<string, string> GetFiles(string FilePath)
		{
			return GetFiles(FilePath, "*.*", SearchOption.AllDirectories);
		}

		public static Dictionary<string, string> GetFiles(string FilePath, string FileFilter)
		{
			return GetFiles(FilePath, FileFilter, SearchOption.AllDirectories);
		}

		public static Dictionary<string, string> GetFiles(string FilePath, string FileFilter, SearchOption searchOption)
		{
			Dictionary<string, string> DirFiles = new Dictionary<string, string>();
			if (Directory.Exists(FilePath))
			{
				foreach (string FileName in Directory.GetFiles(FilePath, FileFilter, searchOption))
				{
					DirFiles.Add(Path.GetFileName(FileName), FileName);
				}
			}
			return DirFiles;
		}

		/// <summary>
		/// check string is number and is not null
		/// </summary>
		/// <param name="Str"></param>
		/// <returns></returns>
		public static bool StringIsNumber(string Str)
		{
			if (string.IsNullOrEmpty(Str))
				return false;
			foreach (char c in Str)
			{
				if (c >= 48 && c <= 58)
					continue;
				else
					return false;
			}
			return true;
		}

		public static string GetRecordPathByName(string RecordName)
		{
			throw new NotImplementedException();
		}

		public static bool IsHaveChinaChar(string CString)
		{
			bool BoolValue = false;
			for (int i = 0;i < CString.Length;i++)
			{
				if (Convert.ToInt32(Convert.ToChar(CString.Substring(i, 1))) > Convert.ToInt32(Convert.ToChar(128)))
				{
					BoolValue = true;
				}

			}
			return BoolValue;
		}

		public static TimeSpan GetTimespanSubtract(string Time1, string Time2)
		{
			DateTime Datetime1=DateTime.Parse(Time1);
			DateTime Datetime2;
			if (Time1.StartsWith("23") && Time2.StartsWith("00"))
				Datetime2 = DateTime.Parse(Time2);
			else
				Datetime2 = DateTime.Parse(Time2);
			return Datetime2.Subtract(Datetime1);
		}
	}
}
