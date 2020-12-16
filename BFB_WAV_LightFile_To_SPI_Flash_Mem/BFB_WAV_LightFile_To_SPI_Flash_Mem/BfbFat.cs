using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public class BfbFat
    {
        public ushort FileCount { get; set; }

        //The Tuple holds the address for the audio file and the associated lights file.
        //If the address is 0, then the entry has no file
        public List<Tuple<uint, uint>> AudioAndLightAddresses { get; private set; } = new List<Tuple<uint, uint>>();

        public byte[] ToBytes()
        {

            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(FileCount));
            AudioAndLightAddresses
                .ForEach((addr) =>
                {
                    bytes.AddRange(BitConverter.GetBytes(addr.Item1));
                    bytes.AddRange(BitConverter.GetBytes(addr.Item2));
                });

            return bytes.ToArray();
          
        }

    }
}
