using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public class BusPirateMemApi : IMemApi
    {
        private const int MEM_BUSY_RETRY = 20;
        private const int FAST_BAUD = 1000000;
        private const int START_ADDR = 0x8000;
        private const int PAGE_SIZE = 256;
        private const byte BP_CMD_WRITE = 0x10; //0x10 = bulk transfer. Lower nibble = len. 0  = 1 byte
        private const byte FLASH_READ = 0x03;
        private const byte FLASH_EN_WRITE = 0x06;
        private const byte FLASH_READ_STATUS = 0x05;
        private const byte FLASH_ERASE_ALL = 0xC7;
        private const byte FLASH_PAGE_PRG = 0x02;
        private const byte FLASH_READ_MAN_ID =  0x90;
        private const byte FLASH_READ_JEDEC = 0x9F;
        private const byte FLASH_READ_ID = 0x4B;

        private Action<string> _statusCallback = null;
        private SerialPort _serialPort = null;



        public BusPirateMemApi()
        {
            
        }

        public bool InitMem(SerialPort serialPort, object info = null)
        {
            _serialPort = serialPort;
            resetBusPirate();

            if (!(setFastBaudForBusPirate() && swtichToRawSpiMode()))
            {
                return false;
            }

            updateStatus("Memory Initialized");

            return true;            
        }


        public IEnumerable<byte> ReadData(uint address, int length)
        {
            
            List<byte> data = new List<byte>(length);

            if (!isInitialized() || !waitForMemBusy())
            {
                return null;
            }

            updateStatus($"Reading {length} byte{(length == 1 ? "" : "s")} from 0x{address.ToString("2x")}");

            do
            {
                data.AddRange(busPirateReadChunk(address, (byte)(length > 16 ? 16 : length)) ?? new byte[0]);

                //queue up the next address to read
                address += 16;
                //reduce count;
                length -= 16;
            } while (length > 0);

            updateStatus("Done reading.");

            return data;


        }

        public bool WriteData(uint address, IEnumerable<byte> data)
        {
            byte[] bytes = data.ToArray();
            int sections = bytes.Length / PAGE_SIZE;

            if (!isInitialized() || !waitForMemBusy())
            {
                return false;
            }



            updateStatus($"Writing {bytes.Length} byte{(bytes.Length == 1 ? "" : "s")} from 0x{address.ToString("2x")}");

            for (int i = 0; i < sections; i++)
            {
                if (!busPirateWritePage(address, getSegment(bytes, i * PAGE_SIZE, PAGE_SIZE)))
                {
                    return false;
                }
                //queue up the next address to read
                address += PAGE_SIZE;
     
            }

            int leftOver = bytes.Length % PAGE_SIZE;

            if (leftOver > 0)
            {
                if (!busPirateWritePage(address, getSegment(bytes, sections * PAGE_SIZE, leftOver)))
                {
                    return false;
                }
            }

            updateStatus("Done writing.");

            return true;

        }

        public bool EraseAll()
        {
            if (!isInitialized() || !waitForMemBusy())
            {
                return false;
            }

            if (!enableMemWrite())
            {
                updateStatus("Could not enable mem write.");
                return false;
            }

            sendBytes(new byte[] { FLASH_ERASE_ALL });

            while (isMemBusy())
            {
                Thread.Sleep(500);
                updateStatus("Erasing...");                
            }

            updateStatus("Done erasing");

            return true;

        }

        public IEnumerable<byte> ReadSignature()
        {

            byte[] sig = new byte[5];
            byte[] data;
            if (!isInitialized() || !waitForMemBusy())
            {
                return null;
            }

            try
            {
                //Clear out the serial port
                _serialPort.ReadExisting();
     
                setChipSelect(true);
                //Get Manufacturer ID
                //Tell BusPirate we are bulk reading/writing 6 bytes
                _serialPort.Write(new byte[] { BP_CMD_WRITE + (6 - 1) }, 0, 1);
                _serialPort.Write(new byte[] { FLASH_READ_MAN_ID, 255, 255, 255, 255, 255 }, 0, 6);

                data = getBytes(8); //2 extra bytes at the start for some reason
                sig[0] = data[6];
                sig[1] = data[7];

                //get ready for next command
                setChipSelect(false);
                setChipSelect(true);
                //Get Unique Id
                //Tell BusPirate we are bulk reading/writing 6 bytes
                _serialPort.Write(new byte[] { BP_CMD_WRITE + (4 - 1) }, 0, 1);
                _serialPort.Write(new byte[] { FLASH_READ_JEDEC, 255, 255, 255 }, 0, 4);
   
                data = getBytes(7); //3 extra bytes at the start for some reason
                sig[2] = data[4];
                sig[3] = data[5];
                sig[4] = data[6];

                return sig;
 
            }
            catch (Exception exc)
            {
                updateStatus($"Exception while reading mem sig: {exc}");
                return null;
            }
            finally
            {
                setChipSelect(false);
            }

        }

        public void SetStatusUpdateCallback(Action<string> callBack)
        {
            _statusCallback = callBack;
        }


        public void Dispose()
        {
            resetBusPirate();

            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Close();
                }
            }
            finally
            {
                _serialPort?.Dispose();
            }
        }

        private bool isInitialized()
        {
            if (_serialPort == null)
            {
                updateStatus("Mem not initialized.");
                return false;
            }

            return true;
        }

        private void updateStatus(string status)
        {
            _statusCallback?.BeginInvoke(status, null, null);
        }

        private bool waitForMemBusy()
        {
            int busyCount = MEM_BUSY_RETRY;
            while (isMemBusy())
            {
                Thread.Sleep(500);
                updateStatus("Mem busy...");

                if (busyCount-- < 0)
                {
                    updateStatus("Mem stuck in busy state.");
                    return false;
                }
            }

            return true;
        }

        private bool connectToSerialPort()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    updateStatus($"Connecting to {_serialPort.PortName}");
                    _serialPort.Open();
                }

                return true;
            }
            catch(Exception exc)
            {
                updateStatus($"Exception while connecting: {exc.Message}");
                return false;
            }
        }

        private bool setFastBaudForBusPirate()
        {
            if (!_serialPort.IsOpen && !connectToSerialPort())
            {
                return false;
            }

            updateStatus($"Updating BAUD...");
            _serialPort.WriteLine("b"); //baud mode
            _serialPort.WriteLine("10"); //custom baud
            _serialPort.WriteLine("3"); //BRG 
            
            _serialPort.Close();
            _serialPort.BaudRate = FAST_BAUD;
            if (!connectToSerialPort())
            {
                return false;
            }

            _serialPort.Write(" ");
            updateStatus($"BAUD Updated");

            return true;
        }


        private bool swtichToRawSpiMode()
        {
            updateStatus("Switching to raw SPI mode...");

            _serialPort.ReadExisting();

            _serialPort.ReceivedBytesThreshold = 1;
            _serialPort.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 20);
            _serialPort.Write(new byte[] { 1 }, 0, 1);
            _serialPort.Write(new byte[] { 99 }, 0, 1);
            _serialPort.Write(new byte[] { 138 }, 0, 1);

            //TODO: Read Raw SPI indicator from BusPirate
            updateStatus("Now in raw SPI mode.");
            return true;
        }

        private void resetBusPirate()
        {
            if (!_serialPort.IsOpen && !connectToSerialPort())
            {
                return;
            }

            updateStatus("Resetting BusPirate");
            _serialPort.Write(new byte[] { 0x00 }, 0, 1);
            Thread.Sleep(100);
            _serialPort.Write(new byte[] { 0x0F }, 0, 1);
        }

        private void setChipSelect(bool csLow)
        {
            _serialPort.Write(new byte[] { (byte)(csLow ? 2 : 3) }, 0, 1); // 0000 0010 = CS low 0000 0011 = CS hi
        }

        private byte[] addressToBytes24(uint address)
        {
            byte[] addrBytes = new byte[3];
            addrBytes[0] = (byte)((address >> 16) & 0xff);
            addrBytes[1] = (byte)((address >> 8) & 0xff);
            addrBytes[2] = (byte)((address >> 0) & 0xff);

            return addrBytes;
        }

        private byte[] busPirateReadChunk(uint address, byte size)
        {
            size = (byte)(size > 16 ? 16 : size);

            byte pbCmd = (byte)(BP_CMD_WRITE + (size - 1)); //(-1 because 0 = 1 byte)
            byte[] addrBytes = addressToBytes24(address);

            try
            {

                
                setChipSelect(true);
                //First send the BusPirate SPI out command with the length - 1 (-1 because 0 = 1 byte)
                _serialPort.Write(new byte[] { (BP_CMD_WRITE + (4 - 1)) }, 0, 1);
                //Then send the Flash Mem Read command with 3-byte address
                _serialPort.Write(new byte[] { FLASH_READ, addrBytes[0], addrBytes[1], addrBytes[2] }, 0, 4);

                //make sure everythign is sent.
                Thread.Sleep(100);
                _serialPort.ReadExisting();

                _serialPort.Write(new byte[] { pbCmd }, 0, 1);
                _serialPort.Write(Enumerable.Repeat<byte>(0xFF, size).ToArray(), 0, size);
                return getBytes(size);
               
            } 
            catch (Exception exc)
            {
                updateStatus($"Exception while reading: {exc}");
                return null;
            }
            finally
            {
                setChipSelect(false);
            }
        }

        private bool busPirateWritePage(uint address, byte[] data)
        {

            int sections = data.Length / 16;
            byte[] addrBytes = addressToBytes24(address);

            if (!enableMemWrite())
            {
                updateStatus("Could not enable mem write.");
                return false;
            }

            try
            {


                setChipSelect(true);
                //First send the BusPirate SPI out command with the length - 1 (-1 because 0 = 1 byte)
                _serialPort.Write(new byte[] { (BP_CMD_WRITE + (4 - 1)) }, 0, 1);
                //Then send the Flash Mem Write Page command with 3-byte address
                _serialPort.Write(new byte[] { FLASH_PAGE_PRG, addrBytes[0], addrBytes[1], addrBytes[2] }, 0, 4);


                for (int i = 0; i < sections; i++) //We will transfer 16 bytes at a time
                {
                    //Tell bus pirate we are sending 16 bytes (-1 because 0 = 1 byte)
                    _serialPort.Write(new byte[] { BP_CMD_WRITE + (16 - 1) }, 0, 1);
                    _serialPort.Write(getSegment(data, i * 16, 16), 0, 16); //Send 16 bytes of data
                    //Make sure everything is sent.
                    _serialPort.ReadExisting();
                }

                int leftOver = data.Length % 16;

                if (leftOver > 0) //If we have bytes left over
                {
                    //Tell bus pirate we are sending 'leftOver' bytes (-1 because 0 = 1 byte)
                    _serialPort.Write(new byte[] { (byte)(BP_CMD_WRITE + (leftOver - 1)) }, 0, 1);
                    _serialPort.Write(getSegment(data, sections * 16, leftOver), 0, leftOver); //Send leftOver number of bytes of data
                }

                //setChipSelect(false);

                return true;

            }
            catch (Exception exc)
            {
                updateStatus($"Exception while writing: {exc}");
                return false;
            }
            finally
            {
                setChipSelect(false);
            }
        }

        //More efficient than Skip().Take().ToArray()
        private byte[] getSegment(byte[] source, int offset, int length)
        {
            byte[] data = new byte[length];

            for (int i = 0; i < length; i++)
            {
                data[i] = source[offset + i];
            }

            return data;
        }

        private byte[] getBytes(int byteCount)
        {


            int b;
            List<byte> bytes = new List<byte>();
            _serialPort.ReadTimeout = 500;

            for (int i = 0; i < byteCount; i++)
            {
                try
                {
                    b = _serialPort.ReadByte();

                    bytes.Add(Convert.ToByte(b));
                }
                catch (TimeoutException)
                {
                    updateStatus("Serial Port Read Timeout!");
                    break;
                }
            }

            return bytes.ToArray();
        }

        private void sendBytes(IEnumerable<byte> bytes)
        {
            int size = bytes.Count();

            if (size < 1 || size > 16)
            {
                throw new InvalidOperationException("Number of bytes must be 1 to 16");
            }

            byte pbCmd = (byte)(BP_CMD_WRITE + (size - 1)); //(-1 because 0 = 1 byte)

            setChipSelect(true);
            //Send bulk read/write command.
            _serialPort.Write(new byte[] { pbCmd }, 0, 1);
            //Send data bytes
            _serialPort.Write(bytes.ToArray(), 0, size);
            setChipSelect(false);
        }

        private bool isMemBusy()
        {
            byte[] bytes = getMemStatus();

            if (bytes.Length < 4)
            {
                updateStatus("Could not check memory busy status.");
                return false;
            }

            return (bytes[3] & 0b00000001) != 0;
        }

        private bool isMemWriteEnabled()
        {
            byte[] bytes = getMemStatus();

            if (bytes.Length < 4)
            {
                updateStatus("Could not check memory write enabled status.");
                return false;
            }

            return (bytes[3] & 0b00000010) != 0;
        }

        private byte[] getMemStatus()
        {
            _serialPort.ReadExisting();

            sendBytes(new byte[] { FLASH_READ_STATUS, 0xFF });

            return getBytes(5);                        
        }

        private bool enableMemWrite()
        {
            sendBytes(new byte[] { FLASH_EN_WRITE });

           
            Thread.Sleep(50);
            _serialPort.ReadExisting();

            return isMemWriteEnabled();
        }
    }


}
