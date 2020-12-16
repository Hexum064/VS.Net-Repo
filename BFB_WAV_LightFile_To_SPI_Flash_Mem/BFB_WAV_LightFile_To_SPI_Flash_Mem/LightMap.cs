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
    }
}
