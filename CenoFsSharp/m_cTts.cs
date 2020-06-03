using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using i_core;
//using i_core.voice;

namespace CenoFsSharp
{
    internal class bb_tts
            //: i_tts, i_dependency
    {
        public string audio_file { get; set; }

        public string text { get; set; }

        public CancellationToken cancel_token { get; set; }

        //public i_tts_result tts_rst { get; set; }

        public int? speed { get; set; } = 4;

        public int samplerate { get; set; }

        //public i_log log { get; set; }

        //public tts_type type => tts_type.biaobei;

        //http://1.203.80.138:8001/tts?user_id=speech&domain=1&language=zh&audiotype=6&rate=1&speed=5&text=此处填写要合成的文字

        public Task<bool> run()
        {
            var task_source = new TaskCompletionSource<bool>();
            var cancel_source = new CancellationTokenSource();
            cancel_source.Token.Register(() => {
                task_source.TrySetResult(false);
            });

            try
            {
                var path_file = Path.GetDirectoryName(audio_file);
                if (!Directory.Exists(path_file))
                {
                    Directory.CreateDirectory(path_file);
                }

                if (File.Exists(audio_file))
                {
                    File.Delete(audio_file);
                }

                //http://1.203.80.138:8001/tts?user_id=speech&domain=1&language=zh&audiotype=6&rate=1&speed=5&text=
                //http://61.162.59.60:8100/tts?user_id=speech&domain=1&language=zh&speed=5&text=
                //http://192.168.1.88:8081/tts?access_token=access_token&language=zh&domain=1&voice_name=%E5%AD%90%E8%A1%BF&text=

                var base_url = "http://192.168.1.88:8081/tts?access_token=access_token&language=zh&domain=1&voice_name=果果";
                var full_url = $"{base_url}&audiotype=6&speed={speed ?? 4}&text={text}";
                var request = (HttpWebRequest)WebRequest.Create(full_url);
                request.Method = "GET";
                request.ContentType = "text/html;charset=UTF-8";
                request.UserAgent = null;
                request.Timeout = 200000;

                var response = request.GetResponse() as HttpWebResponse;
                var myResponseStream = response.GetResponseStream();

                using (var fs = new FileStream(audio_file, FileMode.Create))
                {
                    var bytes = new byte[1024];
                    var size = myResponseStream.Read(bytes, 0, bytes.Length);
                    while (size > 0)
                    {
                        fs.Write(bytes, 0, size);
                        size = myResponseStream.Read(bytes, 0, bytes.Length);
                    }
                }

                myResponseStream.Close();
                myResponseStream.Dispose();
                myResponseStream = null;

                task_source.TrySetResult(true);
            }
            catch (Exception ex)
            {
                Core_v1.Log.Instance.Error($"run tts(file={audio_file},text={text}) error:{ex.Message}");
                task_source.TrySetResult(false);
            }
            return task_source.Task;
        }
    }
}
