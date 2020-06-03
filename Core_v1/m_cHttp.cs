using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Core_v1
{
    public class m_cHttp
    {
        #region ***Get
        public static string m_fGet(string Url, string postDataStr = "")
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);

                ///处理HTTPS
                if (Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((a, b, c, d) => { return true; });
                    request = WebRequest.Create(Url) as HttpWebRequest;
                    request.ProtocolVersion = HttpVersion.Version10;
                }
                else
                {
                    request = WebRequest.Create(Url) as HttpWebRequest;
                }

                request.Method = "GET";
                request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();

                Log.Instance.Debug($"[Core_v1][m_cHttp][m_fGet][Response:{retString}]");

                return retString;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
