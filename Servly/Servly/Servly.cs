using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Management;
using System.Net;
using System.IO;

using Echevil; // networking code used from: http://netcode.ru/dotnet/?lang=&katID=30&skatID=277&artID=7659

namespace Servly
{
    class Servly
    {
        static void Main(string[] args)
        {
            try
            {
                string servlyUrl = args[0];
                Console.WriteLine(args[0]);
            
                Dictionary<string,string> postData = new Dictionary<string,string>();

                Dictionary<string, double> Disk = DiskUsage();
                Dictionary<string, double> Memory = MemoryUsage();
                Dictionary<string, double> Network = NetworkUsage();
                double cpuUsage = CpuUsage();
                int Procs = ProcsRunning();
                double NetConns = NetworkConnections();

                Console.WriteLine("Processors: {0}", Environment.ProcessorCount);

                Console.WriteLine("Operating System: {0}", Environment.OSVersion.ToString());

                Console.WriteLine("Running Processes: {0}", Procs);

                Console.WriteLine("Memory Usage: " + (Memory["availableMemory"] / Memory["totalMemory"]) * 100 + "%");

                Console.WriteLine("CPU Usage: " + cpuUsage + "%");

                Console.WriteLine("Network Connections: " + NetConns);


                Console.WriteLine("Disk Free/Used/Total: {0} / {1} / {2}", Disk["free"] / (1024 * 1024 * 1024), Disk["used"] / (1024 * 1024 * 1024), (Disk["free"] + Disk["used"]) / (1024 * 1024 * 1024));

                Console.WriteLine("Upload/Download: {0}/{1}", Network["upload"], Network["download"]);

                postData.Add("os", "Windows");
                postData.Add("cpu_free", (100 - cpuUsage).ToString());
                postData.Add("disk_used", Disk["used"].ToString() );
                postData.Add("disk_size", (Disk["free"] + Disk["used"]).ToString() );
                postData.Add("mem_used", (Memory["totalMemory"] - Memory["availableMemory"] ).ToString() );
                postData.Add("mem_free", Memory["availableMemory"].ToString() );
                postData.Add("procs", Procs.ToString() );
                postData.Add("net_in", Network["download"].ToString() );
                postData.Add("net_out", Network["upload"].ToString());
                postData.Add("ncpus", Environment.ProcessorCount.ToString() );
                postData.Add("kernel", Environment.OSVersion.ToString() );
                postData.Add("connections", NetConns.ToString() );
                postData.Add("ps", ProcList());



                // this is what we are sending
                string post_data = Serialize(postData);
                Console.WriteLine("Post data: " + post_data);

                // this is where we will send it
                string uri = servlyUrl;

                // create a request
                HttpWebRequest request = (HttpWebRequest)
                WebRequest.Create(uri); request.KeepAlive = false;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Method = "POST";

                // turn our request string into a byte stream
                byte[] postBytes = Encoding.ASCII.GetBytes(post_data);

                // this is important - make sure you specify type this way
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postBytes.Length;
                Stream requestStream = request.GetRequestStream();

                // now send it
                requestStream.Write(postBytes, 0, postBytes.Length);
                requestStream.Close();

                // grab te response and print it out to the console along with the status code
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine(new StreamReader(response.GetResponseStream()).ReadToEnd());
                Console.WriteLine(response.StatusCode);

                //Console.Read();
            }
            catch (Exception e)
            {
                Console.WriteLine("You need to provide the URL");
            }

        }

        private static string Serialize(Dictionary<string,string> input)
        {
            string str = "";
            foreach (KeyValuePair<string, string> k in input)
            {
                str += "srvly[" + k.Key + "]=" + k.Value + "&";
            }
            return str;
        }

        private static Dictionary<string, double> NetworkUsage()
        {
            Dictionary<string, double> temp = new Dictionary<string, double>();

            double DownloadSpeed = 0;
            double UploadSpeed = 0;

            NetworkMonitor monitor = new NetworkMonitor();
            NetworkAdapter[] adapters = monitor.Adapters;

            // Start a timer to obtain new performance counter sample every second.
            monitor.StartMonitoring();

            for (int i = 0; i < 10; i++)
            {
                foreach (NetworkAdapter adapter in adapters)
                {
                    // The DownloadSpeedKbps and UploadSpeedKbps are
                    // double values. 
                   
                    DownloadSpeed += adapter.DownloadSpeedKbps;
                    UploadSpeed += adapter.UploadSpeedKbps;
                }
                System.Threading.Thread.Sleep(1000); // Sleeps for one second.
            }

            // Stop the timer. Properties of adapter become invalid.
            monitor.StopMonitoring();


            temp.Add("upload", (UploadSpeed/8)*1024);
            temp.Add("download", (DownloadSpeed/8)*1024);

            return temp;
        }

        private static string ProcList()
        {
            string str = "";
            Process[] processlist = Process.GetProcesses();
            foreach(Process p in processlist){
                str += "* \t " + p.Id +" \t * \t * \t * \t * \t * " + p.ProcessName + "\n";
            }

            return str;
        }

        private static int ProcsRunning()
        {
            Process[] processlist = Process.GetProcesses();
            return processlist.Length;
        }

        private static double CpuUsage()
        {
            // Get CPU usage over 20 seconds
            PerformanceCounter cpuCounter;
            double cpuUsage = 0;
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            for (int i = 0; i < 20; i++) { cpuUsage += cpuCounter.NextValue(); Thread.Sleep(1000); }
            cpuUsage /= 20;

            return cpuUsage;
        }

        private static Dictionary<string,double> MemoryUsage()
        {
            Dictionary<string, double> temp = new Dictionary<string, double>();
            double totalMemory = 0;
            double availableMemory = 0;
            // Get memory usage and information
            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            ManagementObjectSearcher mos2 = new ManagementObjectSearcher("SELECT * FROM Win32_perfRawData_PerfOS_Memory");
            // total memory
            foreach (ManagementObject mo in mos.Get()) { totalMemory = System.Convert.ToDouble(mo["TotalPhysicalMemory"].ToString()); }
            // available memory
            foreach (ManagementObject mo in mos2.Get()) { availableMemory = System.Convert.ToDouble(mo["AvailableBytes"].ToString()); }

            temp.Add("totalMemory", totalMemory);
            temp.Add("availableMemory", availableMemory);

            return temp;
        }

        private static double NetworkConnections()
        {
            // Network Information
            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c netstat -an");
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            // Do not create the black window.
            procStartInfo.CreateNoWindow = true;
            // Now we create a process, assign its ProcessStartInfo and start it
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo = procStartInfo;
            proc.Start();
            // Get the output into a string
            double result = proc.StandardOutput.ReadToEnd().ToString().Split('\n').Length;
            // Display the command output.
            return result;
        }

        private static Dictionary<string, double> DiskUsage()
        {

            Dictionary<string, double> temp = new Dictionary<string, double>();

            double freeSpace = 0;
            double usedSpace = 0;
            
            // get disk stats
            System.Management.ObjectQuery oQuery = new System.Management.ObjectQuery("select FreeSpace,Size,Name from Win32_LogicalDisk where DriveType=3");
            ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oQuery);
            ManagementObjectCollection oReturnCollection = oSearcher.Get();
            
            //loop through found drives and write out info
            foreach (ManagementObject oReturn in oReturnCollection)
            {
                //Free space in MB
                freeSpace += Convert.ToInt64(oReturn["FreeSpace"]);

                //Used space in MB
                usedSpace += (Convert.ToInt64(oReturn["Size"]) - Convert.ToInt64(oReturn["FreeSpace"]));
            }
            temp.Add("used", usedSpace);
            temp.Add("free", freeSpace);

            return temp;
        }
    }
}