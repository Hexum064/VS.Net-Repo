using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public interface ILightMapsCollection : INotifyPropertyChanged
    {
        IEnumerable<LightToRefColor> SelectedLightMapLights { get; }
    }
}
