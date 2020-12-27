using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace PB_WriteSingeFileToFlash
{
    class Program
    {
        private const string COM_PORT = "COM3";
        private const int BAUD = 1000000;
        private const uint START_ADDR = 0x8000;
        private const int PAGE_SIZE = 256;

        static void Main(string[] args)
        {

            SerialPort serialPort = new SerialPort(COM_PORT);
            string fileName;
            string input;
            
            InitPort(serialPort);

            if (SetNewBaud(serialPort) && CheckRawSpiMode(serialPort))// && VerifyMemoryId(serialPort))
            {

                Console.WriteLine("Enter 'w' to write, 'r' to read, or anything else to quit.");
                input = Console.ReadLine();

                if (string.Equals(input, "w", StringComparison.OrdinalIgnoreCase))
                {
                    if (GetFileName(out fileName) && EraseMemory(serialPort))
                    {
                        LoadFileInMem(serialPort, fileName);
                    }
                }
                else if (string.Equals(input, "r", StringComparison.OrdinalIgnoreCase))
                {
                    ReadFirstFATEntry(serialPort);
                    ReadFirstFileFromMem(serialPort);
                }
                else
                {
                    return;
                }


                


            }

            //exit raw spi and reset
            ResetAndClose(serialPort);

            Console.WriteLine("Press enter to quit.");
            Console.ReadLine();
            return;




        }

        private static void ResetAndClose(SerialPort serialPort)
        {
            serialPort.Write(new byte[] { 0x00 }, 0, 1);
            Thread.Sleep(100);
            serialPort.Write(new byte[] { 0x0F }, 0, 1);
            serialPort.Close();
        }

        private static void ReadFirstFATEntry(SerialPort serialPort)
        {
            byte[] buff;
            SetCS(serialPort, true);
            serialPort.Write(new byte[] { (byte)(16 + (4 - 1)) }, 0, 1);
            serialPort.Write(new byte[] { 0x03, 0x00, 0x00, 0x00 }, 0, 4);

            Thread.Sleep(100);
            serialPort.ReadExisting();

            serialPort.Write(new byte[] { (byte)(16 + (10 - 1)) }, 0, 1);
            serialPort.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, 0, 10);
            buff = GetBytes(serialPort, 10);
            SetCS(serialPort, false);

            

            Console.WriteLine($"File Count: {buff[1]}, Size: { buff[6] << 24 + buff[7] << 16 + buff[8] << 8 + buff[9] << 0}.");

        }



        private static void ReadFirstFileFromMem(SerialPort serialPort)
        {
            int lineCount = 256;
            int byteCount = 16*lineCount;
            byte[] buff;
            int startAddr = 92176;
            byte[] addrBytes = new byte[3];
            addrBytes[0] = (byte)((startAddr >> 16) & 0xFF);
            addrBytes[1] = (byte)((startAddr >> 8) & 0xFF);
            addrBytes[2] = (byte)((startAddr >> 0) & 0xFF);
            startAddr =  62473;
            SetCS(serialPort, true);
            serialPort.Write(new byte[] { (byte)(16 + (4 - 1)) }, 0, 1);
            serialPort.Write(new byte[] { 0x03, addrBytes[0], addrBytes[1], addrBytes[2] }, 0, 4);

            Thread.Sleep(100);

            serialPort.ReadExisting();

            for (int i = 0; i < lineCount; i++)
            {
                serialPort.Write(new byte[] { (byte)(16 + (16 - 1)) }, 0, 1);
                serialPort.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, 0, 16);
            }

            buff = GetBytes(serialPort, 17 * lineCount);
            SetCS(serialPort, false);
            for (int i = 0; i < lineCount; i++)
            {
                Console.Write((i * 16).ToString("X6") + " ");
                PrintLineBytes(buff.Skip(i * 17 + 1).Take(16).ToArray());
            }


            
        }

        private static void PrintLineBytes(byte[] bytes)
        {
            File.WriteAllBytes("out.bin", bytes);


            for (int j = 0; j < bytes.Length; j++)
            {
                Console.Write(bytes[j].ToString("X2") + " ");
            }

            Console.Write("\t");

            for (int j = 0; j < bytes.Length; j++)
            {
                Console.Write((bytes[j] == 0 || bytes[j] == 10 || bytes[j] == 13) ? "." : Convert.ToChar(bytes[j]).ToString());
            }

            Console.WriteLine("");
        }

        private static bool EraseMemory(SerialPort serialPort)
        {
            ResetMem(serialPort);

            while(IsMemBusy(serialPort))
            {
                Console.WriteLine("Mem Busy...");
                Thread.Sleep(500);
            }

            if (EnableMemWrite(serialPort))
            {

                SendBytes(serialPort, new byte[] { 0xC7 });

                while(true)
                {
                    Thread.Sleep(500);

                    if (IsMemBusy(serialPort))
                    {
                        Console.WriteLine("Erasing...");
                    }
                    else
                    {
                        break;
                    }
                }

                Console.WriteLine("Done erasing.");
                return true;

            }

            Console.WriteLine("Could not erase memory.");

            return false;
        }

        private static bool IsMemBusy(SerialPort serialPort)
        {
            serialPort.ReadExisting();

            SendBytes(serialPort, new byte[] { 0x05, 0xFF });

            byte[] bytes = GetBytes(serialPort, 5);

            if (bytes.Length < 4)
            {
                Console.WriteLine("Could not check memory busy status.");
                return false;
            }

            return (bytes[3] & 0b00000001) != 0;
        }

        private static bool EnableMemWrite(SerialPort serialPort, bool skipVerify = false)
        {
            SendBytes(serialPort, new byte[] { 0x06 });

            if (!skipVerify)
            {
                Thread.Sleep(100);
                serialPort.ReadExisting();
            }
            SendBytes(serialPort, new byte[] { 0x05, 0xFF });

            if (!skipVerify)
            {
                byte[] bytes = GetBytes(serialPort, 5);

                if (bytes.Length < 4 || (bytes[3] & 0b00000010) == 0)
                {
                    Console.WriteLine("Could not enable write.");
                    return false;
                }
            }

            return true;
        }

        private static void SetCS(SerialPort serialPort, bool csLow)
        {
            serialPort.Write(new byte[] { (byte)(csLow ? 2 : 3) }, 0, 1); // 0000 0010 = CS low 0000 0011 = CS hi
        }

        private static void SendBytes(SerialPort serialPort, IEnumerable<byte> bytes)
        {
            int count = bytes.Count();

            if (count < 1 || count > 16)
            {
                throw new InvalidOperationException("Number of bytes must be 1 to 16");
            }

            SetCS(serialPort, true);
            serialPort.Write(new byte[] { (byte)(16 + (count - 1)) }, 0, 1);
            serialPort.Write(bytes.ToArray(), 0, count);
            SetCS(serialPort, false);
        }

        private static bool CheckRawSpiMode(SerialPort serialPort)
        {
            Console.WriteLine($"Press enter to enter raw SPI mode.");
            Console.ReadLine();
            serialPort.ReadExisting();

            serialPort.ReceivedBytesThreshold = 1;




            serialPort.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 20);



            serialPort.Write(new byte[] { 1 }, 0, 1);




            serialPort.Write(new byte[] { 99 }, 0, 1);
            serialPort.Write(new byte[] { 138 }, 0, 1);


            return true;
        }

        private static bool VerifyMemoryId(SerialPort serialPort)
        {
            Console.WriteLine($"Press enter to verify memory.");
            Console.ReadLine();
            serialPort.ReadExisting();

            serialPort.Write(new byte[] { 2 }, 0, 1); // 0000 0010 = CS low
            serialPort.Write(new byte[] { 16 + 5 }, 0, 1);
            serialPort.Write(new byte[] { 144, 255, 255, 255, 255, 255 }, 0, 6);
            serialPort.Write(new byte[] { 3 }, 0, 1); // 0000 0011 = CS hi

            if (!GetBytes(serialPort, 9).SequenceEqual(new byte[] { 1, 1, 0xFF, 0xFF, 0xFF, 0xFF, 0x14, 0xEF, 1}))
            {
                Console.WriteLine("Memory could not be verified.");
                return false;
            }

            serialPort.Write(new byte[] { 2 }, 0, 1); // 0000 0010 = CS low
            serialPort.Write(new byte[] { 16 + 3 }, 0, 1);
            serialPort.Write(new byte[] { 159, 255, 255, 255 }, 0, 4);
            serialPort.Write(new byte[] { 3 }, 0, 1); // 0000 0011 = CS hi

            if (!GetBytes(serialPort, 7).SequenceEqual(new byte[] { 1, 1, 0xFF, 0xEF, 0x40, 0x15, 1 })) 
            {
                Console.WriteLine("Memory could not be verified.");
                return false;
            }

            return true;
        }

        private static byte[] GetBytes(SerialPort serialPort, int byteCount)
        {


            int b;
            List<byte> bytes = new List<byte>();
            serialPort.ReadTimeout = 500;

            for (int i = 0; i < byteCount; i++)
            {
                try
                {
                    b = serialPort.ReadByte();

                    bytes.Add(Convert.ToByte(b));
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Read Timeout!");
                    break;
                }
            }

            return bytes.ToArray();
        }


        private static bool LoadFileInMem(SerialPort serialPort, string fileName)
        {
            long pages;
            int currentAddr = 0;
            Console.WriteLine("Loading file.");

            FileStream file = File.Open(fileName, FileMode.Open);



            //pages = (file.Length / 8); //going to write 8 bytes at a time so we are going to do an 8-byte page
            pages = (file.Length / PAGE_SIZE); //We are going to write 256 byte pages

            //FAT: [0-1] = number of files, first wav file entry [2-5] = addr start, file size [6-9], first light map file entry [10-13] = addr start, file size [14-17]


            EnableMemWrite(serialPort);
            SendBytes(serialPort, new byte[] { 0x02, 0, 0, 0, 1, 0 }); //Write the number of files to the starting address 0x000000. Only 1 in this case.
            byte[] addrBytes = BitConverter.GetBytes(START_ADDR);
            EnableMemWrite(serialPort);
            SendBytes(serialPort, new byte[] { 0x02, 0, 0, 0x2,
                addrBytes[0],addrBytes[1],addrBytes[2],addrBytes[3]}); //write starting address for file and length
            currentAddr = (int)START_ADDR;

            byte[] buff = new byte[PAGE_SIZE];

            for (int i = 0; i < pages; i++)
            {
                


                file.Read(buff, 0, PAGE_SIZE);
                WritePageToMem(serialPort, currentAddr, buff);
                currentAddr += PAGE_SIZE;
            }

            if (file.Length % PAGE_SIZE > 0)
            {
                buff = new byte[file.Length - file.Position];
                file.Read(buff, 0, buff.Length);
                WritePageToMem(serialPort, currentAddr, buff);
            }

            //byte[] buff = new byte[8];

            //for (long i = 0; i < pages; i++)
            //{
            //    file.Read(buff, 0, 8);
            //    WriteToMem(serialPort, currentAddr, buff);
            //    currentAddr += 8;
            //}


            //if ((pages * 8) < file.Length) //We have a few bytes left over to write
            //{
            //    buff = new byte[file.Length - file.Position];
            //    file.Read(buff, 0, buff.Length);
            //    WriteToMem(serialPort, currentAddr, buff);
            //}

            ResetMem(serialPort);

            return false;


        }

        private static void WritePageToMem(SerialPort serialPort, int address, byte[] data)
        {
            if (data.Length > PAGE_SIZE)
            {
                data = data.Take(PAGE_SIZE).ToArray();
            }
           // Console.WriteLine("Writing Page...");


            EnableMemWrite(serialPort, true);

            //start the write
            SetCS(serialPort, true);
            serialPort.Write(new byte[] { (byte)(16 + (4 - 1)) }, 0, 1);//Tell bus pirate we are sending 4 bytes
            serialPort.Write(new byte[] { 0x02, (byte)(address >> 16), (byte)(address >> 8), (byte)(address >> 0) }, 0, 4); //Send Write command and starting address.

            int sections = data.Length / 16;

            for (int i = 0; i < sections; i++) //We will transfer 16 bytes at a time
            {

                serialPort.Write(new byte[] { (byte)(16 + (16 - 1)) }, 0, 1);//Tell bus pirate we are sending 16 bytes
                serialPort.Write(data.Skip(i * 16).Take(16).ToArray(), 0, 16); //Send 16 bytes of data
                serialPort.ReadExisting();
            }
            int leftOver = data.Length % 16;

            if (leftOver > 0) //If we have bytes left over
            {
                serialPort.Write(new byte[] { (byte)(16 + (leftOver - 1)) }, 0, 1);//Tell bus pirate we are sending 'leftOver' bytes
                serialPort.Write(data.Skip(sections * 16).Take(leftOver).ToArray(), 0, leftOver); //Send leftOver number of bytes of data
            }

            SetCS(serialPort, false);

            //Thread.Sleep(50); //Wait for write to finish.
      
        }

        private static void WriteToMem(SerialPort serialPort, int address, byte[] data)
        {
            List<byte> buff = new List<byte>() { 0x02, (byte)(address >> 16), (byte)(address >> 8), (byte)(address >> 0) };
            buff.AddRange(data);
            EnableMemWrite(serialPort, true);
            SendBytes(serialPort, buff.ToArray());
        }

        private static void ResetMem(SerialPort serialPort)
        {
            SendBytes(serialPort, new byte[] { 0x66 });
            SendBytes(serialPort, new byte[] { 0x99 });
            Thread.Sleep(1);
        }

        private static bool GetFileName(out string fileName)
        {
            string input;

            while (true)
            {
                Console.WriteLine("Enter path to file to write or 'q' to quit.");
                input = Console.ReadLine();


                //TODO: For Testing. Remove
                if (string.IsNullOrEmpty(input))
                {
                    fileName = "";
                    return true;
                }

                if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = "";
                    return false;
                }

                if (File.Exists(input))
                {
                    fileName = input;
                    return true;
                }
                else
                {
                    Console.WriteLine("File not found.");
                }
            }
        }

        private static void InitPort(SerialPort serialPort)
        {
            serialPort.BaudRate = 115200;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = Handshake.None;
            
            //serialPort.DataReceived += (s, e) => Console.Write($"'{serialPort.ReadExisting()}'");
        }

        private static bool SetNewBaud(SerialPort serialPort)
        {
            try
            {
                Console.WriteLine("Press enter to connect.");
                Console.ReadLine();

                Console.WriteLine($"Connectiong to port {COM_PORT} and setting baud to {BAUD}.");

                serialPort.Open();

                serialPort.WriteLine("b"); //baud mode
                serialPort.WriteLine("10"); //custom baud
                serialPort.WriteLine("3"); //BRG 

                serialPort.Close();
                serialPort.BaudRate = BAUD;
                serialPort.Open();

                serialPort.Write(" ");

                //EnterSPIMode(serialPort);

                Console.WriteLine("Done.");
                return true;

            }
            catch (Exception exc)
            {
                Console.WriteLine($"Could not set new baud:\n{exc}");
                return false;
            }
        }


        #region extra

        private static void EnterSPIMode(SerialPort serialPort)
        {
            Console.WriteLine("Entering SPI mode.");

            serialPort.WriteLine("m"); //mode menu
            Thread.Sleep(100);
            serialPort.WriteLine("5"); //5 = spi
            Thread.Sleep(100);
            serialPort.WriteLine("4"); //speed = 1MHz
            Thread.Sleep(100);
            serialPort.WriteLine("1"); //clk idle low
            Thread.Sleep(100);
            serialPort.WriteLine("2"); //clk active to idle
            Thread.Sleep(100);
            serialPort.WriteLine("1"); //Middle sample phase
            Thread.Sleep(100);
            serialPort.WriteLine("2"); // /CS
            Thread.Sleep(100);
            serialPort.WriteLine("2"); // H = 3.3v output, L = gnd
            Thread.Sleep(100);
            serialPort.WriteLine("l"); // MSB mode
            Thread.Sleep(100);





        }

        private static byte[] FromHex(string hex)
        {
            string[] hexs = hex.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return hexs
                .Select((h) => Convert.ToByte(h))
                .ToArray();



        }

        #endregion
    }
}
