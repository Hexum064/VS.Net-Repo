using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public class LightMap
    {


        public LightMap(ushort lightCount)
        {
            LightCount = lightCount;
            Lights = new Color[LightCount];
        }

        public LightMap(ushort lightCount, IEnumerable<byte> bytes) : this(lightCount)
        {
            loadFromBytes(bytes.ToArray());
        }

        public ushort LightCount { get; private set; }

        public ushort HoldTime { get; set; }

        public Color[] Lights { get; private set; }

        public void SetAllLightsColor(Color color)
        {
            for (int i = 0; i < LightCount; i++)
            {
                Lights[i] = color;
            }
        }

        public LightMap Clone()
        {
            LightMap newMap = new LightMap(LightCount);
            newMap.Lights = Lights.ToArray();
            newMap.HoldTime = HoldTime;
            return newMap;
        }

        private void loadFromBytes(byte[] bytes)
        {
            HoldTime = BitConverter.ToUInt16(bytes, 0);

            for (int i = 0; i < LightCount; i++)
            {
                Lights[i] = new Color()
                {
                    //+2 to skip the Hold Time bytes
                    G = bytes[i * 3 + 2],
                    R = bytes[i * 3 + 3],
                    B = bytes[i * 3 + 4],
                };
            }
        }
    }
}
