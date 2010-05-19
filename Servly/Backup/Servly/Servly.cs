using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Management;

namespace Servly
{
    class Servly
    {
        private static double totalMemory, availableMemory;

        static void Main(string[] args)
        {
            int mb = 1024 * 1024;
            // Get memory usage and information
            ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            ManagementObjectSearcher mos2 = new ManagementObjectSearcher("SELECT * FROM Win32_perfRawData_PerfOS_Memory");    
            // total memory
            foreach (ManagementObject mo in mos.Get()){ totalMemory = System.Convert.ToDouble(mo["TotalPhysicalMemory"].ToString()); }
            // available memory
            foreach (ManagementObject mo in mos2.Get()){ availableMemory = System.Convert.ToDouble(mo["AvailableBytes"].ToString()); }

            Console.WriteLine("Memory Usage: " + (availableMemory / totalMemory) * 100 + "%");



            // Get CPU usage over 20 seconds
            PerformanceCounter cpuCounter;
            double cpuUsage = 0;
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            for (int i = 0; i < 20; i++){ cpuUsage += cpuCounter.NextValue(); Thread.Sleep(1000); }
            cpuUsage /= 20;

            Console.WriteLine("CPU Usage: " + cpuUsage + "%");
           

            // Disks
            //get Fixed disk stats
            System.Management.ObjectQuery oQuery = new System.Management.ObjectQuery("select FreeSpace,Size,Name from Win32_LogicalDisk where DriveType=3");
            ManagementObjectSearcher oSearcher = new ManagementObjectSearcher(oQuery);
            ManagementObjectCollection oReturnCollection = oSearcher.Get();

            //variables for numerical conversions
            double fs = 0;
            double us = 0;
            double tot = 0;
            double up = 0;
            double fp = 0;

            //for string formating args
            object[] oArgs = new object[2];
            Console.WriteLine("*******************************************");
            Console.WriteLine("Hard Disks");
            Console.WriteLine("*******************************************");

            //loop through found drives and write out info
            foreach (ManagementObject oReturn in oReturnCollection)
            {
                // Disk name
                Console.WriteLine("Name : " + oReturn["Name"].ToString());

                //Free space in MB
                fs = Convert.ToInt64(oReturn["FreeSpace"]) / mb;

                //Used space in MB
                us = (Convert.ToInt64(oReturn["Size"]) - Convert.ToInt64(oReturn["FreeSpace"])) / mb;

                //Total space in MB
                tot = Convert.ToInt64(oReturn["Size"]) / mb;

                //used percentage
                up = us / tot * 100;

                //free percentage
                fp = fs / tot * 100;

                //used space args
                oArgs[0] = (object)us;
                oArgs[1] = (object)up;

                //write out used space stats
                Console.WriteLine("Used: {0:#,###.##} MB ({1:###.##})%", oArgs);

                //free space args
                oArgs[0] = fs;
                oArgs[1] = fp;

                //write out free space stats
                Console.WriteLine("Free: {0:#,###.##} MB ({1:###.##})%", oArgs);
                Console.WriteLine("Size :  {0:#,###.##} MB", tot);
                Console.WriteLine("*******************************************");
            }  
            Console.Read();

        }
    }
}
