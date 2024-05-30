using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using USBClassLibrary;
using System.Management;

namespace dryice_cmd_to_unit {
    class Program {
        static SerialPort mySerialPort = new SerialPort();
        static private List<USBClassLibrary.USBClass.DeviceProperties> ListOfUSBDeviceProperties;
        private static bool debug = false;
        private static bool flag_discom = false;
        private static System.Threading.Timer close_program;
        static void Main(string[] args) {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            string head = "1";
            while (true)
            {
                try
                { head = File.ReadAllText("../../config/head.txt"); break; } catch { Thread.Sleep(50); }
            }
            File.Delete("../../config/head.txt");
            File.WriteAllText("call_exe_tric.txt", "");
            string step = "non";
            int timeout = 10000;
            string tx = "ledp";
            int retest = 1;
            string rx = "PASS\n";
            string rxx_min = "5";
            string rxx_max = "90";
            string port_name = "COM5";
            step = File.ReadAllText("../../config/dryice_cmd_to_unit_" + head + "_step.txt");
            try
            { timeout = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_timeout.txt")); } catch { }
            try
            { debug = Convert.ToBoolean(File.ReadAllText("../../config/test_head_" + head + "_debug.txt")); } catch { }
            try
            { tx = File.ReadAllText("../../config/dryice_cmd_to_unit_" + head + "_data_tx.txt"); } catch { }
            try
            { port_name = File.ReadAllText("../../config/dryice_cmd_to_unit_" + head + "_port.txt"); } catch { }
            try
            { retest = Convert.ToInt32(File.ReadAllText("../../config/dryice_cmd_to_unit_" + head + "_retest.txt")); } catch { }
            try
            { rx = File.ReadAllText("../../config/dryice_cmd_to_unit_" + head + "_data_rx.txt"); } catch { }
            try
            { rxx_min = File.ReadAllText("../../config/dryice_cmd_to_unit_" + head + "_data_rx_min.txt"); } catch { }
            try
            { rxx_max = File.ReadAllText("../../config/dryice_cmd_to_unit_" + head + "_data_rx_max.txt"); } catch { }
            close_program = new System.Threading.Timer(TimerCallback, null, 0, (timeout * retest) + 10000);
            Console.WriteLine("step = " + step);
            Console.WriteLine();
            if (step == "get_all")
            {
                ListOfUSBDeviceProperties = new List<USBClass.DeviceProperties>();
                Nullable<UInt32> MI = 0;
                MI = null;
                string s = "";
                if (USBClass.GetUSBDevice(uint.Parse("0483", System.Globalization.NumberStyles.AllowHexSpecifier), uint.Parse("5740", System.Globalization.NumberStyles.AllowHexSpecifier), ref ListOfUSBDeviceProperties, true, MI))
                {
                    for (int iii = 0; iii < ListOfUSBDeviceProperties.Count; iii++)
                    {
                        Console.WriteLine(ListOfUSBDeviceProperties[iii].COMPort);
                        s += ListOfUSBDeviceProperties[iii].COMPort + "_";
                    }
                }
                if (debug)
                    Console.ReadKey();
                File.WriteAllText("../../config/dryice_cmd_to_unit_get_all.txt", s);
                File.WriteAllText("test_head_" + head + "_result.txt", ListOfUSBDeviceProperties.Count.ToString() + "\r\nPASS");
                GenLogPort(step, s, head);
                return;
            }
            if (step == "get_new")
            {
                string old = File.ReadAllText("../../config/dryice_cmd_to_unit_get_all.txt");
                ListOfUSBDeviceProperties = new List<USBClass.DeviceProperties>();
                Nullable<UInt32> MI = 0;
                MI = null;
                string s = "";
                string s_new = "";
                Stopwatch timeout_ = new Stopwatch();
                timeout_.Restart();
                while (timeout_.ElapsedMilliseconds < timeout)
                {
                    s = "";
                    if (USBClass.GetUSBDevice(uint.Parse("0483", System.Globalization.NumberStyles.AllowHexSpecifier), uint.Parse("5740", System.Globalization.NumberStyles.AllowHexSpecifier), ref ListOfUSBDeviceProperties, true, MI))
                    {
                        for (int iii = 0; iii < ListOfUSBDeviceProperties.Count; iii++)
                        {
                            s += ListOfUSBDeviceProperties[iii].COMPort + "_";
                        }
                    }
                    if (old == "")
                        s_new = s;
                    else
                    {
                        string[] vv = old.Split('_');
                        foreach (string b in vv)
                        {
                            if (b == "")
                                continue;
                            s = s.Replace(b + "_", "");
                        }
                        s_new = s;
                    }
                    Console.WriteLine("new unit = " + s_new);
                    if (debug)
                    {
                        timeout_.Stop();
                        if (Console.ReadLine() != "")
                            break;
                        timeout_.Start();
                    }
                    if (s_new != "")
                        break;
                }
                s_new = s_new.Replace("_", "");
                if (s_new != "")
                {
                    File.WriteAllText("../../config/dryice_cmd_to_unit_" + head + "_port.txt", s_new);
                    File.WriteAllText("test_head_" + head + "_result.txt", s_new + "\r\nPASS");
                }
                else
                {
                    File.WriteAllText("../../config/dryice_cmd_to_unit_" + head + "_port.txt", s_new);
                    File.WriteAllText("test_head_" + head + "_result.txt", "non\r\nFAIL");
                }
                GenLogPort(step, s_new, head);
                return;
            }
            mySerialPort.PortName = port_name;
            mySerialPort.BaudRate = 9600; //19200
            mySerialPort.DataBits = 8;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.Parity = Parity.None;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;
            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            if (debug)
            {
                Console.WriteLine("port name = " + mySerialPort.PortName);
                Console.ReadLine();
            }
            Stopwatch t = new Stopwatch();
        aaaa:
            t.Restart();
            while (t.ElapsedMilliseconds < 5000)
            {
                try
                {
                    mySerialPort.Open();
                    t.Stop();
                    break;
                } catch
                {
                    Thread.Sleep(250);
                }
                try
                { mySerialPort.Close(); } catch { }
            }
            if (t.IsRunning)
            {
                if (!flag_discom)
                {
                    try
                    { mySerialPort.Close(); } catch { }
                    discom("disable", port_name);
                    discom("enable", port_name);
                    flag_discom = true;
                    goto aaaa;
                }
                mySerialPort.Dispose();
                File.WriteAllText("test_head_" + head + "_result.txt", "can not open port\r\nFAIL");
                return;
            }
            Console.WriteLine("Port Name = " + mySerialPort.PortName);
            Console.WriteLine("Baud Rate = " + mySerialPort.BaudRate);

            flag_data = false;
            t.Restart();
            while (t.ElapsedMilliseconds < 500)
            {
                if (flag_data == true)
                { flag_data = false; t.Restart(); }
                Thread.Sleep(25);
            }

            for (int re = 0; re < retest; re++)
            {
                Console.WriteLine("send: " + tx);
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                rx_ = "";
                mySerialPort.Write(tx);
                if (step == "gen8m")
                {
                    File.WriteAllText("test_head_" + head + "_result.txt", "PASS\r\nPASS");
                    mySerialPort.Close();
                    mySerialPort.Dispose();
                    return;
                }
                t.Restart();
                while (t.ElapsedMilliseconds < timeout)
                {
                    if (flag_data != true)
                    { Thread.Sleep(100); continue; }
                    flag_data = false;
                    t.Stop();
                    break;
                }
                if (t.IsRunning)
                {
                    if (re < retest - 1)
                        continue;
                    if (!flag_discom)
                    {
                        mySerialPort.Close();
                        discom("disable", port_name);
                        discom("enable", port_name);
                        flag_discom = true;
                        goto aaaa;
                    }
                    Console.WriteLine("timeout!!!");
                    File.WriteAllText("test_head_" + head + "_result.txt", "timeout\r\nFAIL");
                    return;
                }
                double rx_min = 0;
                double rx_max = 0;
                try
                { rx_min = Convert.ToDouble(rxx_min); } catch { }
                try
                { rx_max = Convert.ToDouble(rxx_max); } catch { }
                if (debug == true)
                {
                    string ss = Console.ReadLine();
                    if (ss != "")
                    {

                    }
                }
                bool result = false;
                string str_result = "";
                switch (step)
                {
                    case "equal":
                        if (rx_ == rx && !t.IsRunning)
                            result = true;
                        else
                            result = false;
                        str_result = rx_;
                        break;
                    case "temp":
                        Console.WriteLine("rx min = " + rx_min);
                        Console.WriteLine("rx max = " + rx_max);
                        string[] rx_split = rx_.Split(' ');
                        if (rx_split.Count() != 3)
                        { result = false; str_result = "not format"; break; }
                        double rx_double = 0;
                        try
                        { rx_double = Convert.ToDouble(rx_split[0]); } catch { result = false; str_result = rx_split[0]; break; }
                        string rx_string = rx_split[1] + " " + rx_split[2];
                        File.WriteAllText("dryice_cmd_to_unit_" + head + "_temp_config.txt", rx_string);
                        if (rx_double >= rx_min && rx_double <= rx_max && !t.IsRunning)
                            result = true;
                        else
                            result = false;
                        str_result = rx_double.ToString();
                        break;
                }
                if (result)
                {
                    File.WriteAllText("test_head_" + head + "_result.txt", str_result + "\r\nPASS");
                    mySerialPort.Close();
                    mySerialPort.Dispose();
                    break;
                }
                else
                {
                    if (re < retest - 1)
                        continue;
                    File.WriteAllText("test_head_" + head + "_result.txt", str_result + "\r\nFAIL");
                    mySerialPort.Close();
                    mySerialPort.Dispose();
                }
            }
        }
        private static void GenLogPort(string step_, string port, string head) {
            return;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DateTime now = DateTime.Now;

            string datetime = now.Year.ToString() + "." + now.Month.ToString("00") +
                "." + now.Day.ToString("00") + " " + now.ToString("T");

            File.AppendAllText(path + "\\LogGetPort.txt", datetime + " : " + step_ + head + "=" + port + "\r\n");
        }

        static string rx_ = "";
        static bool flag_data = false;
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {
            Thread.Sleep(50);
            rx_ = mySerialPort.ReadExisting();
            Console.WriteLine("Read: " + rx_);
            rx_ = rx_.Replace("\n", "").Replace("\r", "");
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            flag_data = true;
        }

        private static bool flag_close = false;
        private static void TimerCallback(Object o) {
            if (!flag_close)
            { flag_close = true; return; }
            if (debug)
                return;
            if (flag_close)
                Environment.Exit(0);
        }
        private static void discom(string cmd, string comport) {//enable disable//
            ManagementObjectSearcher objOSDetails2 = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");
            ManagementObjectCollection osDetailsCollection2 = objOSDetails2.Get();
            foreach (ManagementObject usblist in osDetailsCollection2)
            {
                string arrport = usblist.GetPropertyValue("NAME").ToString();
                if (arrport.Contains(comport))
                {
                    Process devManViewProc = new Process();
                    devManViewProc.StartInfo.FileName = "DevManView.exe";
                    devManViewProc.StartInfo.Arguments = "/" + cmd + " \"" + arrport + "\"";
                    devManViewProc.Start();
                    devManViewProc.WaitForExit();
                }
            }
        }

        /// <summary>
        /// Event Exception Catch Program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MyHandler(object sender, UnhandledExceptionEventArgs args) {
            Exception e = (Exception)args.ExceptionObject;
            LogProgramCatch(e.StackTrace);
        }

        /// <summary>
        /// Log program catch to csv file
        /// </summary>
        /// <param name="text"></param>
        private static void LogProgramCatch(string text) {
            string path = "D:\\LogError\\DryIceCmdCatch";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            DateTime now = DateTime.Now;
            StreamWriter swOut = new StreamWriter(path + "\\" + now.Year + "_" + now.Month + ".csv", true);
            string time = now.Day.ToString("00") + ":" + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00");
            swOut.WriteLine(time + "," + text);
            swOut.Close();
        }
    }
}
