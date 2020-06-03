using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Limilabs.FTP.Client;

namespace CenoFsSharp
{
	class FtpOperate
	{
		private void CheckFile()
		{
			Ftp FtpServer=new Ftp();
			FtpServer.Login("administrator", "admin");
			FtpServer.Connect("192.168.0.60");
			bool fsl=FtpServer.Connected;
		}
	}
}
