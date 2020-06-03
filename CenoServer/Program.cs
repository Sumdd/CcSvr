using CenoCommon;
using CenoSipBusiness;
using log4net;
using System;
using Core_v1;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using NEventSocket.Logging;
using System.Reflection;

namespace CenoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //去掉日志
            LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider(LogLevel.Fatal));

            #region ***判断程序是否已经运行
            bool blnIsRunning;
            Mutex mutexApp = new Mutex(false, Assembly.GetExecutingAssembly().FullName, out blnIsRunning);
            if (!blnIsRunning)
            {
                Console.WriteLine("网关呼叫中心服务端-CenoSoft已经运行,请不要重复开启。");
                Console.ReadLine();
                return;
            }
            #endregion

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Exception ex = e?.ExceptionObject as Exception;
                Log.Instance.Error($"[CenoServer][Program][Main][UnhandledException][undeal start]");
                Log.Instance.Error($"[CenoServer][Program][Main][UnhandledException][error message:{ex?.Message},stack trace:{ex?.StackTrace}]");
                Log.Instance.Error($"[CenoServer][Program][Main][UnhandledException][undeal stop]");
            };

            Application.ThreadException += (sender, e) =>
            {
                Log.Instance.Error($"[CenoServer][Program][Main][ThreadException][undeal start]");
                Log.Instance.Error($"[CenoServer][Program][Main][ThreadException][error message:{e?.Exception?.Message},stack trace:{e?.Exception?.StackTrace}]");
                Log.Instance.Error($"[CenoServer][Program][Main][ThreadException][undeal stop]");
            };

            Program.DisbleClosebtn();

            Log.Instance.Success($"[CenoServer][Program][Main][call center starting...]");
            if (!intilizate_services.InitSysInfo())
            {
                Log.Instance.Fail($"[CenoServer][Program][Main][call center start fail]");
            }
            else
            {
                Log.Instance.Success($"[CenoServer][Program][Main][call center start success]");
            }
            MainWhileDo.MainStep();
        }

        #region 关闭按钮禁用
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        extern static IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);
        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        extern static IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);
        static void DisbleClosebtn()
        {
            Console.Title = "网关呼叫中心服务端-CenoSoft";
            Thread.Sleep(333);
            IntPtr windowHandle = FindWindow(null, Console.Title);
            IntPtr closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
            uint SC_CLOSE = 0xF060;
            RemoveMenu(closeMenu, SC_CLOSE, 0x0);
        }
        #endregion
    }
}
