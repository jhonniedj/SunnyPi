using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Timers;
 
public class SerialPortTest
{
    public static SerialPort Serial_Pi;             //Used SerialPort
    public static string used_ser_port = "";        //Used SerialPort Name (COM1, COM2, etc..)
    public static int ser_baudrate = 115200;        //Used SerialPort Baudrate 
    public static int ser_timeout = 10000;          //Used SerialPort TimeOut
    public static int debug = 0;                    //Debug? 2=debug 1=verbose 0=no
    public static int running_minutes = 0;          //Used for running minutes
	public static void Main(string[] args)
	{
        string status = Serial_Init();
        if (debug >= 1) { Console.WriteLine("status: " + status); }
        if (status == "OK")
        {
            Console.WriteLine("== Soladin Inverter Simulator ==");
            Timer t = new Timer(1000);
            t.Elapsed += new ElapsedEventHandler(timer_tick_min);
            t.Enabled = true;
            
            while (true) { ; }
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
                { com_prt = port; }
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

    public static void Serial_Write(string message, bool echo=true)
    {
        Serial_Pi.Write(message);
        if (echo) { Console.WriteLine("send UART: "); }
    }

    public static void Serial_Write_Bytes(byte[] message_bytes, bool echo = true)
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

    public static void Serial_Write_Bytes_and_Checksum(byte[] message_bytes, bool echo = true)
    {
        byte checksum = 0x00;
        foreach (byte single in message_bytes)
        { checksum += single; }
        byte[] Data_incl_checksum = new byte[message_bytes.Length];
        Array.Copy(message_bytes, Data_incl_checksum, message_bytes.Length);
        Data_incl_checksum[Data_incl_checksum.Length - 1] = checksum;
        Serial_Write_Bytes(Data_incl_checksum);
    }

    public static void Serial_Enter()
    {
        Serial_Write_Bytes(new byte[] { 0x0D, 0x0A },false);
    }

    public static void Serial_New_Page()
    {
        Serial_Write_Bytes(new byte[] { 0x0C },false);
    }

    public static void Serial_Received(object sender, SerialDataReceivedEventArgs e)
    {
        int buffer_length=Serial_Pi.BytesToRead;
        byte[] serial_catcher;
        serial_catcher=new byte[buffer_length];
        Serial_Pi.Read(serial_catcher, 0, Serial_Pi.BytesToRead);
        
        Console.WriteLine("RX: ");
        foreach (ushort value in serial_catcher)
        {
            Console.Write("{0}(0x{1:X})\t",(char)value,value);
        }
        Console.WriteLine();

        //Init/ID
        if (compare_arr(serial_catcher, new byte[] { 0x00, 0x00, 0x00, 0x00, 0xC1, 0x00, 0x00, 0x00, 0xC1 }))
        {
            Console.WriteLine("Sending ID");
            Serial_Write_Bytes(new byte[] { 0x00, 0x00, 0x11, 0x00, 0xC1, 0xF3, 0x00, 0x00, 0xC5 });
        }
        //Firmware
        if (compare_arr(serial_catcher, new byte[] { 0x11, 0x00, 0x00, 0x00, 0xB4, 0x00, 0x00, 0x00, 0xC5 }))
        {
            Console.WriteLine("Sending FW");
            Serial_Write_Bytes(new byte[] { 0x00, 0x00, 0x11, 0x00, 0xB4, 0xF3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE3, 0x00, 0x09, 0x09, 0x34, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xDA });
        }
        //Data
        if (compare_arr(serial_catcher, new byte[] { 0x11, 0x00, 0x00, 0x00, 0xB6, 0x00, 0x00, 0x00, 0xC7 }))
        {
            Random random_voltage = new Random();
            byte voltage = Convert.ToByte(random_voltage.Next(210, 240));
            Random random_power = new Random();
            byte power = Convert.ToByte(random_power.Next(0, 100));

            byte running_1 = (byte)(running_minutes & 0xff);
            byte running_2 = (byte)((running_minutes >> 8) & 0xff);
            byte running_3 = (byte)((running_minutes >> 16) & 0xff);
            byte running_4 = (byte)(running_minutes >> 24);
            

            Console.WriteLine("Sending Data");
            byte[] Data_Bytes=new byte[] { 0x00, 0x00, 0x11, 0x00, 0xB6, 0xF3, 0x00, 0x00, 0x04, 0x03, 0x35, 0x00, 0x8A, 0x13, 0xE8, 0x00, 0x00, 0x00, 0x24, 0x00, 0x90, 0x0B, 0x00, 0x1F, running_1, running_2, running_3, running_4, 0x00, 0x00};
            Serial_Write_Bytes_and_Checksum(Data_Bytes);
        }
        //Max Power
        if (compare_arr(serial_catcher, new byte[] { 0x11, 0x00, 0x00, 0x00, 0xB9, 0x00, 0x00, 0x00, 0xCA }))
        {
            Console.WriteLine("Sending Max PWR");
            Serial_Write_Bytes(new byte[] { 0x00, 0x00, 0x11, 0x00, 0xB9, 0xF3, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x1B, 0x00, 0x21, 0x00, 0x22, 0x00, 0x00, 0x00, 0xE5, 0x02, 0x7E, 0x48, 0x36, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1E });
        }
        //Reset Max Power
        if (compare_arr(serial_catcher, new byte[] { 0x11, 0x00, 0x00, 0x00, 0x97, 0x01, 0x00, 0x00, 0xA9 }))
        {
            Console.WriteLine("Reset Max PWR");
            Serial_Write_Bytes(new byte[] { 0x00, 0x00, 0x11, 0x00, 0x97, 0x01, 0x00, 0x00, 0xA9 });
        }

        
        int old_debug = debug;
        debug = 1;
        if (debug > 1) { Console.WriteLine("RX: " + serial_catcher); }
        debug = old_debug;
    }

    public static void timer_tick_min(object sender, ElapsedEventArgs e)
    {
        running_minutes++;
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
}
		