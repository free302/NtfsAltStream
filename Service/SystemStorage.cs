using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace WindowsService1
{
    public partial class SystemStorage : ServiceBase
    {
        public SystemStorage()
        {
            InitializeComponent();
            initTask();
        }

        #region ---- Service Interface Implimetation ----

        protected override void OnStop() => log($"SystemStorage.OnStop()");

        /// <summary>
        /// 서비스 시작 : timer start(), timer가 실제 작업 MyTask.Run() 실행
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            log($"entering OnStart()... : {Process.GetCurrentProcess().PriorityClass}, {Environment.CurrentDirectory}");

            SetThreadExecutionState(_ES.ES_SYSTEM_REQUIRED | _ES.ES_CONTINUOUS);
            _timer.Start();

            log($"...exiting OnStart()");
        }
        protected override void OnPause()
        {
            log($"OnPause()");
            _timer.Stop();
            base.OnPause();
        }
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            log($"OnPowerEvent(): {powerStatus}");
            return base.OnPowerEvent(powerStatus);
        }

        #endregion

        /// <summary>
        /// 사용자가 실행할 경우 - 서비스를 인스톨한다.
        /// 서비스로 실행될 경우 서비스 수행한다.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)//install service to system
            {
                installService(true);
                Thread.Sleep(1000);
                installService(false);
            }
            else Run(new SystemStorage());//run service

            void installService(bool install)
            {
                if (install) startService(true);

                var p = new Process();
                p.StartInfo.FileName = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe";
                p.StartInfo.Arguments = $"{(install ? "" : "/u")} {typeof(SystemStorage).Assembly.Location}";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;

                p.Start();
                var msg = p.StandardOutput.ReadToEnd();
                log(msg);
                p.WaitForExit();

                if (!install)
                {
                    setInteract();
                    startService(false);
                }

                void setInteract()
                {
                    var service = new System.Management.ManagementObject($"WIN32_Service.Name='SystemStorage'");
                    try
                    {
                        var paramList = new object[11];
                        paramList[5] = true;
                        service.InvokeMethod("Change", paramList);
                    }
                    finally { service.Dispose(); }
                }
            }//install()

            void startService(bool start)
            {
                //NtDll.AdjustPrivilege();

                var p = new Process();
                p.StartInfo.FileName = @"C:\Windows\System32\net.exe";
                p.StartInfo.Arguments = $"{(start ? "start" : "stop")} SystemStorage";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;

                p.Start();
                var msg = p.StandardOutput.ReadToEnd();
                log(msg);
                p.WaitForExit();
            }//stopService()           

        }//Main()

        #region ---- Task to excute ----

        Timer _timer;
        void initTask()
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(typeof(SystemStorage).Assembly.Location);
            _timer = new Timer(60000);// 60 seconds
            _timer.Elapsed += new ElapsedEventHandler(runTask);
        }
        void runTask(object s, ElapsedEventArgs e)
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            log($"Monitoring the system: {Thread.CurrentThread.Priority}");
            SetThreadExecutionState(_ES.ES_SYSTEM_REQUIRED | _ES.ES_CONTINUOUS);

            //if (DateTime.Now.DayOfWeek != DayOfWeek.Friday) return;
            //if (DateTime.Now.Hour < 19) return;

            if (MyTask.Running) return;
            try
            {
                log($"MyTask.Run()...");
                _timer.Stop();
                MyTask.Run(log);
            }
            finally
            {
                log($"SystemStorage timer start()");
                _timer.Start();
            }
        }//

        #endregion

        #region ---- PInvoke: Thread State ----
        enum _ES : uint
        {
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern _ES SetThreadExecutionState(_ES esFlags);

        #endregion

        static void log(object message)
        {
            var msg = $"[---SSS---][{DateTime.Now:HHmmss.fff}] {message}\r\n";
            Debug.WriteLine(msg);
            File.AppendAllText("error.log", msg);
        }

    }//class
}
