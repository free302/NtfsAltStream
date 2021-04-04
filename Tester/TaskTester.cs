using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using WindowsService1;

namespace Tester
{
    public partial class TaskTester : Form
    {
        public TaskTester()
        {
            InitializeComponent();
            ShowInTaskbar = false;

            this.Size = new Size(600, 400);
            this.Font = new Font("Consolas", 12F);
            _tb = new RichTextBox();
            _tb.Multiline = true;
            _tb.Dock = DockStyle.Fill;
            _tb.ScrollBars = RichTextBoxScrollBars.Both;
            Controls.Add(_tb);

            OnStart();
        }
        RichTextBox _tb;
        void log(object message)
        {
            if (InvokeRequired) Invoke((Action<object>)add, message);
            else add(message);
            void add(object msg)
            {
                _tb.AppendText($"{msg}\r\n");
                _tb.Refresh();
            }
            //Debug.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {message}");
        }
        void OnStart()
        {
            log($"entering OnStart()...");

            // Set up a timer that triggers every minute.
            var timer = new System.Timers.Timer();
            timer.Interval = 5000;
            timer.Elapsed += new ElapsedEventHandler(work);
            timer.Start();

            log($"...exiting OnStart()");

            void work(object s, ElapsedEventArgs e)
            {
                log($"u1={MyTask.getUserName()}, u2={MyTask.getUser2()}");
                log($"Monitoring the System");

                if (!MyTask.Running)
                {
                    try
                    {
                        log($"{nameof(MyTask)}.Run()...");
                        timer.Stop();
                        MyTask.Run(log, @"..\..\..\TestDir");
                    }
                    finally
                    {
                        log($"SystemStorage tiemr restart()...");
                        timer.Interval = 20000;
                        timer.Start();
                    }
                }
            }
        }
    }
}
