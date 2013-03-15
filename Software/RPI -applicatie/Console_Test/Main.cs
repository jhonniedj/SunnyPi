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
    public static int ser_baudrate = 115200;        //Used SerialPort Baudrate 
    public static int ser_timeout = 10000;          //Used SerialPort TimeOut
    public static int debug = 0;                    //Debug? 2=debug 1=verbose 0=no
	public static void Main(string[] args)
	{
        string status = Serial_Init();
        if (debug >= 1) { Console.WriteLine("status: " + status); }
        if (status == "OK")
        {
            DateTime dt = DateTime.Now;
            //Console.Write(String.Format("{0:yyyyMMdd}", dt));
			//Console.Write(String.Format("{0:HH:mm}", dt));
			Post_Pvoutput(String.Format("{0:yyyyMMdd}", dt).ToString(), String.Format("{0:HH:mm}", dt).ToString(),"10","33","22.0","170");
			while (true) {; }
            //Serial_ID();
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
            Serial_Pi.DataReceived += new SerialDataReceivedEventHandler(Serial_Received);
        }


        catch
        { return "FAIL!"; }
        
        return "OK";
    }

    public static void Serial_Write(string message, bool echo)
    {
        Serial_Pi.Write(message);
        if (echo) { Console.WriteLine("send UART: "); }
    }

    public static void Serial_Write_Bytes(byte[] message_bytes,bool echo=true)
    {
    
        //0x0C=New Page     //0x0D=CR       //0x0A=LF
        Serial_Pi.Write(message_bytes, 0, message_bytes.Count());
        if (echo)
        {
            Console.WriteLine();
            Console.Write("send UART: ");
            foreach (byte item in message_bytes)
            { Console.Write("0x{0,2:X2} ", item); }
        }
    }

    public static void Serial_Enter()
    {
        Serial_Write_Bytes(new byte[] { 0x0D, 0x0A },false);
    }

    public static void Serial_New_Page()
    {
        Serial_Write_Bytes(new byte[] { 0x0C },false);
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

    public static void Serial_Received(object sender, SerialDataReceivedEventArgs e)
    {
        // Show all the incoming data in the port's buffer
        string serial_catcher;
        serial_catcher = Serial_Pi.ReadExisting();
        if (serial_catcher.StartsWith("\x00" + "\x00" ))
        { 
            Console.WriteLine("Header Found (0x00 0x00 0x11 0x00)!");
        }
        
        
        
        Console.WriteLine("RX: "+serial_catcher);
        
        
    }

    public static void File_Write(string file_to_write,string[] lines_to_write)
    {
        //string[] lines = { "some text1\r\nssome text2\r\nsome text3\r", "2e lijn" };
        File.WriteAllLines(@"/home/pi/BITCHES.txt", lines_to_write);
        //File.AppendText();
        
        //a=File.OpenText(@"/home/pi/BITCHES.txt");
        string line;
        using (StreamReader reader = new StreamReader(file_to_write))
        {
            line = reader.ReadLine();
        }
        Console.WriteLine(line);
    }


    
	public static void Post_Pvoutput(string date, string time, string watt_hours, string watts, string celcius, string volts)
    {	
		Console.WriteLine ("d="+date+"&t="+time+"&v1="+watt_hours+"&v2="+watts+"&v5="+celcius+"&v6="+volts);
        // this is what we are sending
		string post_data = "d="+date+"&t="+time+"&v1="+watt_hours+"&v2="+watts+"&v5="+celcius+"&v6="+volts;

        // this is where we will send it
		string uri = "http://pvoutput.org/service/r2/addstatus.jsp";

        // create a request
        HttpWebRequest request = (HttpWebRequest)
        WebRequest.Create(uri); request.KeepAlive = false;
        request.ProtocolVersion = HttpVersion.Version10;
        request.Method = "POST";
		request.Headers.Add("X-Pvoutput-Apikey", "fb93bae9930ee27de11de393a6b66fc27b1e0686");
		request.Headers.Add("X-Pvoutput-SystemId", "13116");


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

}
		