using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
 
public class SerialPortTest
{
    public static SerialPort Serial_Pi;             //Used SerialPort
    public static string used_ser_port = "";        //Used SerialPort Name (COM1, COM2, etc..)
    public static int ser_baudrate = 115200;        //Used SerialPort Baudrate 115200
    public static int ser_timeout = 1000;          //Used SerialPort TimeOut
    public static int debug = 0;                    //Debug? 2=debug 1=verbose 0=no
    public static bool empty = true;                //check for received data
    public static string last_upload = "";                //check for received data
    public static DateTime dt = DateTime.Now;
   

    public static byte flags_l;
    public static byte flags_h;
    public static byte voltage_l;
    public static byte voltage_h;
    public static byte power_l;
    public static byte power_h;
    public static byte energy_l;
    public static byte energy_m;
    public static byte energy_h;
    public static byte temp;
    public static byte op_time_ll;
    public static byte op_time_lh;
    public static byte op_time_hl;
    public static byte op_time_hh;

	public static void Main(string[] args)
	{
        debug = 1;
        Read_Config();
        debug = 0;

        string status = Serial_Init();
        if (debug >= 1) { Console.WriteLine("status: " + status); }
        if (status == "OK")
        {
            while (true)
            {
                //START 
                dt = DateTime.Now;

                Console.WriteLine("=====================================");
                Console.WriteLine(String.Format("         Date: {3:00}-{2:00}-{1:0000} {0:HH:mm}", dt, dt.Year, dt.Month, dt.Day));
                string date=String.Format("{0:0000}{1:00}{2:00}",  dt.Year, dt.Month, dt.Day);
                string time=String.Format("{0:HH:mm}",dt);
                string date_dev = String.Format("{2:00}-{1:00}-{0:0000}",  dt.Year, dt.Month, dt.Day);

                Console.WriteLine();

                Serial_Write_Bytes(new byte[] { 0x11, 0x00, 0x00, 0x00, 0xB6, 0x00, 0x00, 0x00, 0xC7 });
                //while (empty) { }//wait till filled

                while((voltage_l == 0) && (power_l == 0) && (energy_l == 0) && (op_time_ll==0) && (temp==0))
                {
                    Serial_Received();
                }
                
                
                //errorflags
                int flags=flags_l + ((UInt16)flags_h << 8);
                if (flags == 0) { Console.WriteLine("       Errors: None, Operating normal"); }
                else { Console.WriteLine("       Errors: 0x{1,2:X2} 0x{0,2:X2} ", flags_l, flags_h); }

                //voltage
                int voltage = voltage_l + ((UInt16)voltage_h << 8);
                Console.WriteLine("      Voltage: {0,6:###0','0} Volts ", voltage);

                //power
                int power = power_l + ((UInt16)power_h << 8);
                Console.WriteLine("        Power: {0,6:0} Watts ", power);

                //energy
                UInt32 energy = (UInt32)(energy_l + ((UInt16)energy_m << 8) + ((UInt32)energy_h << 16));
                Console.WriteLine("       Energy: {0,6:###0','00} kWh ", energy);
                 
                //temperature
                Console.WriteLine("Inverter Temp: {0,6:0} °C", temp);


                //running time
                UInt32 running_time = (UInt32)(op_time_ll + ((UInt16)op_time_lh << 8) + ((UInt32)op_time_hl << 16)) + ((UInt32)op_time_hh << 24);
                Console.WriteLine("      Running: {0,6:0} minutes ", running_time);

                //Write_CSV();
                //Post_Pvoutput(String.Format("{0:yyyyMMdd}", dt).ToString(), String.Format("{0:HH:mm}", dt).ToString(),"10","33","22.0","170");

                Console.WriteLine("=====================================");
                //CSV
                string csv_contents = date_dev+";" + time + ";" + voltage + ";" + power + ";" + energy + ";" + temp + ";" + running_time;
                string csv_location = "/log/"+date_dev+".csv";
                File_Write(csv_location, new string[] { csv_contents });
                Console.Write("CSV saved at: [" +csv_location+"] with: \""+csv_contents+"\".\n");
                
                //PVoutput.org
                if (last_upload != date + " " + time)
                {
                    //Console.Write("Updating PVoutput with: \n -date:" + date + "\n -time:" + time + "\n -energy:" + energy + "\n -power:" + power + "\n -temp:" + temp + "\n -voltage:" + voltage + "\n -flags:" + flags + "\n\n");
                    Console.Write("Updating PVoutput with: \n -date:\t\t" + date + "\n -time:\t\t" + time + "\n -energy:\t" + energy*10 + "\n -power:\t" + power + "\n -temp:\t\t" + temp + "\n -voltage:\t" + voltage/10 + "\n -flags:\t" + flags + "\n\n");
                    Post_Pvoutput(date.ToString(), time.ToString(), (energy*10).ToString(), power.ToString(), temp.ToString(), (voltage/10).ToString(), flags.ToString());
                    last_upload = date + " " + time;
                }
                //Console.ReadKey();
                System.Threading.Thread.Sleep(10000);
            }
            //STOP
        }
        else
        { Console.WriteLine("Failed to write, Is SerialPort [" + used_ser_port + "] already open?"); }
        debug = 1;
        if (debug >= 1) { Console.WriteLine(); Console.WriteLine("Press ANY key to Close..."); }
        if (debug >= 1) { Console.ReadKey(); }
        debug = 0;
        
        Serial_Pi.Close();
        if (debug >= 1) { Console.WriteLine("Closing NOW!"); }
	}

    public static string Serial_Init()
    {
        //string used_com_port="";
        //AUTO-COM-PORT-SELECTER
        if (used_ser_port == "")
        {
            string[] ports = SerialPort.GetPortNames();
            string com_prt = "/dev/ttyAMA0";

            if (debug>=2) {Console.WriteLine("The following serial ports were found:");}
            // Display each port name to the console. 
            foreach (string port in ports)
            {
                if (debug >= 2) { Console.WriteLine(port); }
                if (port == "COM1")
                { com_prt = "COM1"; }
                if (File.Exists("/dev/rfcomm0"))
                { com_prt = "/dev/rfcomm0"; }
            }
            if (debug >= 1) { Console.WriteLine("Using: " + com_prt); }
            used_ser_port = com_prt;          
        }

        try
        {
            Serial_Pi = new SerialPort(used_ser_port, ser_baudrate, Parity.None, 8, StopBits.One);
            
            if (!Serial_Pi.IsOpen)
            { Serial_Pi.Open(); }
            Serial_Pi.ReadTimeout = ser_timeout;
            if (debug >= 1) { Console.WriteLine("Opened Serial Port"); }
            //Serial_Pi.DataReceived += new SerialDataReceivedEventHandler(Serial_Received);
            //Serial_Pi.ReceivedBytesThreshold = 1; //todo:not implented mono
        }
             


        catch
        { return "FAIL!"; }
        

        return "OK";
    }

    public static void Serial_Write(string message, bool echo=false)
    {
        Serial_Pi.Write(message);
        if (echo) { Console.WriteLine("TX: "); }
    }

    public static void Serial_Write_Bytes(byte[] message_bytes,bool echo=false)
    {
    
        //0x0C=New Page     //0x0D=CR       //0x0A=LF
        Serial_Pi.Write(message_bytes, 0, message_bytes.Count());
        if (echo)
        {
            Console.WriteLine();
            Console.Write("TX: ");
            foreach (byte item in message_bytes)
            { Console.Write("0x{0,2:X2} ", item); }
        }
        Console.WriteLine("");
    }

    public static void Serial_Enter()
    {
        Serial_Write_Bytes(new byte[] { 0x0D, 0x0A },false);
    }

    public static void Serial_New_Page()
    {
        Serial_Write_Bytes(new byte[] { 0x0C },false);
    }

    //public static void Serial_Received(object sender, SerialDataReceivedEventArgs e)
    public static void Serial_Received()
    {
        int buffer_length = Serial_Pi.BytesToRead;
        byte[] serial_catcher;
        serial_catcher = new byte[buffer_length];
        Serial_Pi.Read(serial_catcher, 0, Serial_Pi.BytesToRead);

        if (debug >= 1) { Console.WriteLine("RX: "); }
        foreach (ushort value in serial_catcher)
        {
            if (debug >= 1) { Console.Write("{0}(0x{1:X})\t", (char)value, value); }
        }
        if (debug >= 1) { Console.WriteLine(); }

        if (check_cmd(serial_catcher, new byte[] { 0x00, 0x00, 0x11, 0x00, 0xB6, 0xF3 })) //Data)
        {
            flags_l = serial_catcher[6];
            flags_h = serial_catcher[7];
            voltage_l = serial_catcher[8];
            voltage_h = serial_catcher[9];
            power_l = serial_catcher[18];
            power_h = serial_catcher[19];
            energy_l = serial_catcher[20];
            energy_m = serial_catcher[21];
            energy_h = serial_catcher[22];
            temp = serial_catcher[23];
            op_time_ll = serial_catcher[24];
            op_time_lh = serial_catcher[25];
            op_time_hl = serial_catcher[26];
            op_time_hh = serial_catcher[27];
            empty = false;
        }

        //Init/ID
        if (compare_arr(serial_catcher, new byte[] { 0x00, 0x00, 0x00, 0x00, 0xC1, 0x00, 0x00, 0x00, 0xC1 }))
        {
            Console.WriteLine("Sending ID");
            Serial_Write_Bytes(new byte[] { 0x00, 0x00, 0x11, 0x00, 0xC1, 0xF3, 0x00, 0x00, 0xC5 });
        }
    }

    public static bool check_cmd(byte[] to_be_checked, byte[] command)
    {
        bool correct= true;
        if (to_be_checked.Length != 0 && command.Length != 0 && command!=null && to_be_checked!=null&& command.Length<to_be_checked.Length)
        {
            //byte[] cmd_data = new byte[] { 0x00, 0x00, 0x11, 0x00, 0xB9, 0xF3 };
            for (int i = 0; i <= command.Length - 1; i++)
            {
                if (to_be_checked[i] != command[i]) { correct = false; }
            }
        }
        else { correct = false; }
        return correct;
    }
    

    public static bool compare_arr(byte[] array_a, byte[] array_b)
    {
        bool correct = true;
        if (array_a.Length != array_b.Length) { correct = false; }

        if (correct)
        {
            for (int i = 0; i <= array_a.Length - 1; i++)
            {
                if (array_a[i] != array_b[i]) { correct = false; }
            }
        }

        if (correct)
        {
            return true;
        }
        else
        {
            return false;
        }


    }

    public static void test_write(string[] lines_to_write)
    {
        File.WriteAllLines(@"/home/pi/Desktop/SunnyPi/log.txt", lines_to_write);
    }
    

    public static void File_Write(string filename, string[] lines_to_write)
    {
        string foldername = filename.Substring(0, filename.LastIndexOf('/')+1);


        if (!Directory.Exists(@"/home/pi/Desktop/SunnyPi" + foldername)) { Directory.CreateDirectory((@"/home/pi/Desktop/SunnyPi" + foldername)); }
        if (!File.Exists(@"/home/pi/Desktop/SunnyPi" + filename))
        {
            try { File.WriteAllText((@"/home/pi/Desktop/SunnyPi" + filename), ""); }
            catch{}
        }
        File.AppendAllLines(@"/home/pi/Desktop/SunnyPi" + filename, lines_to_write);
        
        //string li@"/home/pi/BITCHES.txtder reader = new StreamReader(file_to_write))
        //{
        //    line = reader.ReadLine();
        //}
        //Console.WriteLine(line);
    }

    public static void Read_Config()
    {
        if (File.Exists(@"/home/pi/Desktop/settings.ini")) //@"settings.ini"))
        {
            if (debug >= 1) { Console.WriteLine("Config Found:"); }
            if (debug >= 1) { Console.WriteLine("==========="); }

            string[] captured_txt = File.ReadAllLines((@"/home/pi/Desktop/settings.ini"));
            foreach (string line in captured_txt)
            {
                if (line.ToUpper().StartsWith("COM_PORT"))
                {
                    string[] linearray = line.Split('=');
                    used_ser_port = linearray[1];
                    if (debug >= 1) { Console.WriteLine(linearray[1]); }
                }

                //OPTIONAL SECOND CONFIG IN SETTINGS.INI
                /*if (line.ToUpper().StartsWith("COM_PORT"))
                {
                    string[] linearray = line.Split('=');
                    used_ser_port = linearray[1];
                    if (debug >= 1) { Console.WriteLine(linearray[1]); }
                }
                 */
            }
            
            if (debug >= 1) { Console.WriteLine("==========="); }
        }
        else
        { if (debug >= 1) { Console.WriteLine("No \'settings.ini\' config file found!, using default settings"); } }
    }

    
	public static void Post_Pvoutput(string date, string time, string watt_hours, string watts, string celcius, string volts,string errorflags)
    {	
		//Console.WriteLine ("d="+date+"&t="+time+"&v1="+watt_hours+"&v2="+watts+"&v5="+celcius+"&v6="+volts);
        // this is what we are sending
		string post_data = "d="+date+"&t="+time+"&v1="+watt_hours+"&v2="+watts+"&v4="+errorflags+"&v5="+celcius+"&v6="+volts;

        // this is where we will send it
		string uri = "http://pvoutput.org/service/r2/addstatus.jsp";

        // create a request
        HttpWebRequest request = (HttpWebRequest)
        WebRequest.Create(uri); request.KeepAlive = false;
        request.ProtocolVersion = HttpVersion.Version10;
        request.Method = "POST";
        request.Headers.Add("X-Pvoutput-Apikey", "24071b850fcf32699e66637f3d043e5762979894");
        request.Headers.Add("X-Pvoutput-SystemId", "14730");


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

    }

    public void Ask_Name()
    {
        Console.WriteLine("What is your name?");
        string name = Console.ReadLine();
        Console.WriteLine("Your name is: " + name);
        Console.ReadKey();
    }

    public static void Serial_ID()
    {
        //Serial_New_Page();
        Serial_Write_Bytes(new byte[] { 0x00, 0x00, 0x00, 0x00, 0xC1, 0x00, 0x00, 0x00, 0xC1 });
        Serial_Enter();


        //string serial_catcher="";
        //try
        //{
        //byte[] serial_header=new byte[4];

        //while (serial_header != new byte[] { 0x00, 0x00, 0x11, 0x00 })
        //{
        Console.WriteLine();
        Console.WriteLine("Reading Serial");
        //Serial_Pi.Read(serial_header, 0, serial_header.Length);
        int ok = Serial_Pi.ReadChar();
        Console.WriteLine("Got: " + ok.ToString());
        //}
        Console.WriteLine("DONE!");


        /*
        List<int> serial_buffer=new List<int>();
        serial_buffer.Add(Serial_Pi.ReadByte());

        if ((serial_buffer[1] == 0xB6) && (serial_buffer[2] == 0xF3))
        {
        }

        //Serial_Pi.ReadTo(serial_catcher);
        Console.WriteLine("line="+serial_catcher);
            
         */
        // }
        // catch (TimeoutException e)
        //  {
        //Console.WriteLine(e);
        //  Console.WriteLine();

        //    Console.WriteLine("+-----------------------------+");
        //    Console.WriteLine("| Serial Timed-Out, Try again |");
        //      Console.WriteLine("+-----------------------------+");
        //Serial_Write_Header();

        //  }

        Serial_Write_Bytes(new byte[] { 0x4b, 0x5A, 0x2B, 0x2B, 0x2B, 0x2B, 0x5A });
        Serial_Enter();

    }
}
		