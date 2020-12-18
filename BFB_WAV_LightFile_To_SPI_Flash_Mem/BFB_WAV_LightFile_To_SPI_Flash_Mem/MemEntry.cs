using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public class MemEntry : INotifyPropertyChanged
    {
        
        public event PropertyChangedEventHandler PropertyChanged;

        public MemEntry(ushort lightCount)
        {
            LightMapSequence = new LightMapSequence(lightCount);
            IncludeAudio = true;
            IncludeLights = true;
        }


        private bool _includeAudio;
        public bool IncludeAudio
        {
            get
            {
                return _includeAudio;
            }
            set
            {
                _includeAudio = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ByteCount));
            }
        }

        private bool _includeLights;
        public bool IncludeLights
        {
            get
            {
                return _includeLights;
            }
            set
            {
                _includeLights = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ByteCount));
            }
        }

        private LightMapSequence _lightMapSequence;
        public LightMapSequence LightMapSequence
        {
            get
            {
                return _lightMapSequence;
            }

            set
            {
                _lightMapSequence = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ByteCount));

            }
        }

        public string AudioFileName
        {
            get;
            private set;
        }


        public byte[] AudioBytes
        {
            get;
            set;
        } = new byte[0];

        public int ByteCount
        {
            get
            {
                int count = 0;
                if (IncludeLights)
                {
                    count += LightMapSequence.ByteCount;
                }
                if (_includeAudio)
                {
                    count += AudioBytes.Length;
                }

                return count;
            }
        }

        public byte[] GetAllBytes()
        {
            return null;

        }

        public bool LoadAudioFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException();
                }

                WavToBfbAudio wavToBfbAudio = new WavToBfbAudio(path);
                AudioBytes = wavToBfbAudio.ConvertWavFileData();
                RaisePropertyChanged(nameof(ByteCount));
                AudioFileName = Path.GetFileName(path);
                RaisePropertyChanged(nameof(AudioFileName));
                return true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return false;
            }
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
