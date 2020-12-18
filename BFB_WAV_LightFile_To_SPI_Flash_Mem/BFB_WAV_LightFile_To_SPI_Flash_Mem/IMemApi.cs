using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public interface IMemApi : IDisposable
    {
        bool InitMem(SerialPort serialPort, object info = null);
        bool WriteData(uint address, IEnumerable<byte> data);
        IEnumerable<byte> ReadData(uint address, int length);
        bool EraseAll();
        IEnumerable<byte> ReadSignature();
        void SetStatusUpdateCallback(Action<string> callBack);
    }
}
