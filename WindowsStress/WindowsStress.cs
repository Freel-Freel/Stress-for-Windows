using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsStressService
{
    public partial class WindowsStress : ServiceBase
    {
        public WindowsStress()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var usageStr = ConfigurationManager.AppSettings.Get("usage");
            if (!int.TryParse(usageStr, out var usage))
                throw new ArgumentException($"Invalid Usage {usageStr}");

            var coreCountStr = ConfigurationManager.AppSettings.Get("coreCount");
            if (!int.TryParse(coreCountStr, out var coreCount))
                throw new ArgumentException($"Invalid Usage {coreCountStr}");

            RunStress(usage, coreCount);
        }

        protected override void OnStop()
        {
            StopStress();
        }


        private List<Thread> threads = new List<Thread>();

        private void RunStress(int usage = 50, int coreCount = 0)
        {
            /*
             * The code here is a modifed (to be parameterizable) version of the code at: 
             * http://stackoverflow.com/questions/2514544/simulate-steady-cpu-load-and-spikes
             * another interesting article covering this topic is:
             * http://stackoverflow.com/questions/5577098/how-to-run-cpu-at-a-given-load-cpu-utilization
             */

            if (usage >= 100)
                usage = 99;

            threads = new List<Thread>();
            int targetThreadCount = coreCount == 0 ? Environment.ProcessorCount : coreCount;

            Process Proc = Process.GetCurrentProcess();
            Proc.PriorityClass = System.Diagnostics.ProcessPriorityClass.Idle;
            long AffinityMask = (long)Proc.ProcessorAffinity;
            for (int i = 0; i < targetThreadCount; i++)
            {
                Thread t = new Thread(new ParameterizedThreadStart(CPUKill));
                t.Priority = ThreadPriority.Lowest;
                t.Start(usage);
                threads.Add(t);
            }
        }

        private static void CPUKill(object cpuUsage)
        {
            Parallel.For(0, 1, new Action<int>((int i) =>
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                while (true)
                {
                    if (watch.ElapsedMilliseconds > (int)cpuUsage)
                    {
                        Thread.Sleep(100 - (int)cpuUsage);
                        watch.Reset();
                        watch.Start();
                    }
                }
            }));
        }

        private void StopStress()
        {
            foreach (var t in threads)
            {
                t.Abort();
            }
        }
    }
}
