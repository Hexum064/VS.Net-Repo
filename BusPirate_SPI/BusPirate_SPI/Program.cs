using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace BusPirate_SPI
{
    class Program
    {
        private const string COM_PORT = "COM3";

        static void Main(string[] args)
        {
            SerialPort bpPort = new SerialPort(COM_PORT);
            string input;

            bpPort.BaudRate = 115200;
            bpPort.Parity = Parity.None;
            bpPort.StopBits = StopBits.One;
            bpPort.DataBits = 8;
            bpPort.Handshake = Handshake.None;

            bpPort.DataReceived += (s, e) => Console.Write(bpPort.ReadExisting());

            Console.WriteLine($"Opening port {COM_PORT}...");

            try
            {
                bpPort.Open();
            }
            catch(Exception exc)
            {
                Console.WriteLine($"Failed to open {COM_PORT}:\n{exc}");
                Console.WriteLine("Press enter to quit.");
                Console.ReadLine();
                return;
            }


            Console.WriteLine($"Connected to {COM_PORT}. Press enter to enter baud settings.");
            Console.ReadLine();

            bpPort.WriteLine("b");

            Console.WriteLine("Press enter to set custom baud.");
            Console.ReadLine();

            bpPort.WriteLine("10");

            Console.WriteLine("Press enter to set baud to 1000000.");
            Console.ReadLine();

            bpPort.WriteLine("3");

            Console.WriteLine("Press enter to reconnect.");
            Console.WriteLine();



            bpPort.Close();

            bpPort.BaudRate = 1000000;


            Console.WriteLine($"Opening port {COM_PORT}...");

            try
            {
                bpPort.Open();
                bpPort.Write(" ");
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Failed to open {COM_PORT}:\n{exc}");
                Console.WriteLine("Press enter to quit.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine($"Connected to {COM_PORT}. Press enter to enter bitbang mode.");
            Console.ReadLine();
            




            bpPort.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 20);

            Console.WriteLine($"Press enter to enter raw SPI mode.");
            Console.ReadLine();

            bpPort.Write(new byte[] { 1 }, 0, 1);


            Console.WriteLine($"Press enter to configure SPI mode: \n\t1MHz, 3.3v, low, active to idle, middle");
            Console.ReadLine();


            bpPort.Write(new byte[] { 99 }, 0, 1);
            bpPort.Write(new byte[] { 138 }, 0, 1);

            Console.WriteLine($"Press enter to sent test bytes.");
            Console.WriteLine("0 1 2 3 4 5 6 7 8 9 A B C D E F");
            Console.ReadLine();

            bpPort.Write(new byte[] { 31 }, 0, 1);
            bpPort.Write(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, 0, 16);

            Console.WriteLine("Press enter to quit");
            Console.ReadLine();

            bpPort.Close();

            //while(true)
            //{
            //    input = Console.ReadLine();

            //    if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
            //    {
            //        bpPort.Close();
            //        return;
            //    }

            //    bpPort.WriteLine(input);
            //}


        }
    }
}
