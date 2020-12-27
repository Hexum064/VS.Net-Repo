using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public static class Extensions
    {
        public const uint STARTING_ADDRESS = 0x8000;

        public static List<byte> GetFat(this IEnumerable<MemEntry> memEntries)
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes((ushort)memEntries.Count()));
            uint address = STARTING_ADDRESS;
            foreach (MemEntry entry in memEntries)
            {
                

                if (entry.IncludeAudio)
                {
                    data.AddRange(BitConverter.GetBytes(address));
                    address += (uint)entry.AudioBytes.Length;
                }
                else
                {
                    data.AddRange(BitConverter.GetBytes((uint)0));
                }

                if (entry.IncludeLights)
                {
                    data.AddRange(BitConverter.GetBytes(address));
                    address += (uint)entry.LightMapSequence.ByteCount;
                }
                else
                {
                    data.AddRange(BitConverter.GetBytes((uint)0));
                }
            }

            return data;
        }

        public static List<byte> GetData(this IEnumerable<MemEntry> memEntries)
        {
            List<byte> data = new List<byte>();

            foreach (MemEntry entry in memEntries)
            {

                if (entry.IncludeAudio)
                {
                    data.AddRange(entry.AudioBytes);
                }


                if (entry.IncludeLights)
                {
                    data.AddRange(entry.LightMapSequence.ToBytes());
                }

            }

            return data;
        }


    }
}
