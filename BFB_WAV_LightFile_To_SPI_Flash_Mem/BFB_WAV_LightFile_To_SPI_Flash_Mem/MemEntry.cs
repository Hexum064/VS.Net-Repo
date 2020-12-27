using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

        public MemEntry(ushort lightCount, IEnumerable<byte> audioBytes, IEnumerable<byte> lightsBytes) : this(lightCount)
        {
            IncludeLights = lightsBytes.Count() > 0;
            AudioBytes = audioBytes.ToArray();
            AudioFileName = getAudioFileNameFromBytes(AudioBytes);
            AudioRunTime = getRunTimeFromBytes(AudioBytes);
            if (IncludeLights)
            {
                loadLightsFromBytes(lightCount, lightsBytes.ToArray());
            }
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

        public decimal AudioRunTime
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

        public bool LoadAudioFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException();
                }

                WavToBfbAudio wavToBfbAudio = new WavToBfbAudio(path);
                List<byte> bytes = new List<byte>(wavToBfbAudio.ConvertWavFileData());
                AudioRunTime = wavToBfbAudio.RunTime;
                AudioFileName = Path.GetFileName(path);
                bytes.AddRange(getAudioFileNameBytes(AudioFileName));
                AudioBytes = bytes.ToArray();

                File.WriteAllBytes("test.bin", AudioBytes);

                RaisePropertyChanged(nameof(ByteCount));
                RaisePropertyChanged(nameof(AudioRunTime));
                RaisePropertyChanged(nameof(AudioFileName));
                return true;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return false;
            }
        }



        private List<byte> getAudioFileNameBytes(string name)
        {
            List<byte> data = new List<byte>();

            if (!string.IsNullOrEmpty(name))
            {

                if (name.Length > 255)
                {
                    name = name.Substring(0, 255);
                }

                data.Add((byte)name.Length);
                data.AddRange(name.ToCharArray()
                    .Select((c) => (byte)c));
                   
            }

            return data;

        }

        private string getAudioFileNameFromBytes(byte[] bytes)
        {
            byte numChannels = bytes[4];
            uint sampleCount = BitConverter.ToUInt32(bytes, 5);
            int audioSize = 9 + (numChannels * (int)sampleCount * 2);
            return Encoding.Default.GetString(bytes.Skip(audioSize + 1).Take(bytes[audioSize]).ToArray());
        }

        private decimal getRunTimeFromBytes(byte[] bytes)
        {
            uint sampleRate = BitConverter.ToUInt32(bytes, 0);
            byte numChannels = bytes[4];
            uint sampleCount = BitConverter.ToUInt32(bytes, 5);
            return Math.Round((decimal)sampleCount / (decimal)(sampleRate * numChannels), 3);
        }

        private void loadLightsFromBytes(ushort lightCount, byte[] bytes)
        {
            _lightMapSequence = new LightMapSequence(lightCount, bytes);
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
