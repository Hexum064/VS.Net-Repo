using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public class WavToBfbAudio
    {
        private byte[] _data;

        public WavToBfbAudio(string wavFilePath)
        {
            if (!File.Exists(wavFilePath))
            {
                throw new FileNotFoundException();
            }

            _data = File.ReadAllBytes(wavFilePath);
        }

        public WavToBfbAudio(byte[] wavFileBytes)
        {
            _data = wavFileBytes;
        }

        public byte[] ConvertWavFileData()
        {
            List<byte> data = new List<byte>();
            uint sampleRate = getSampleRate(_data);
            ushort channels = getChannelCount(_data);
            ushort sampleSize = getSampleSize(_data);
            List<byte> audioData = getAudioData(_data);

            audioData = sampleSize == 8 ? convertFrom8bit(audioData) : convertFrom16bit(audioData);

            data.AddRange(BitConverter.GetBytes(sampleRate));
            data.AddRange(BitConverter.GetBytes((byte)channels));
            data.AddRange(BitConverter.GetBytes((uint)(audioData.Count / 2)));
            data.AddRange(audioData);

            return data.ToArray();
        }

        private List<byte> convertFrom8bit(List<byte> audioData)
        {
            //Turn the 8bit samples into 12bit samples
            List<byte> newData = new List<byte>();

            audioData
                .ForEach((b) =>
                {
                    //Convert to big-endian. First 4 bits of the sample into the first byte and last 4 bits into the second
                    //UPDATE: Switched to little-endian for test
                    newData.Add((byte)(b << 4));
                    newData.Add((byte)(b >> 4));

                });

            return newData;
        }

        private List<byte> convertFrom16bit(List<byte> audioData)
        {
            //Turn the 16bit, 2s comp samples into 12bit samples
            List<byte> newData = new List<byte>();

            for (int i = 0; i < audioData.Count; i += 2)
            {
                //Dealing with 2 bytes at a time of little-endian data
                //Convert the bytes to an int, convert to unsigned, shift right by 4 to make it 12bit, conver to short
                int raw = BitConverter.ToInt16(new byte[] { audioData[i], audioData[i + 1] }, 0);
                raw += 32768; //Convert from signed by adding half the max value. This converts a 0 to 32768

                ushort sample = Convert.ToUInt16(raw >> 4);
                //Add bytes as big-endian
                //UPDATE: Switched to little-endian for test
                newData.Add((byte)((sample >> 0) & 0xFF));
                newData.Add((byte)((sample >> 8) & 0xFF));

            }

            return newData;
        }

        private uint getSampleRate(byte[] data)
        {
            return (uint)(data[24] << 0) + (uint)(data[25] << 8) + (uint)(data[26] << 16) + (uint)(data[27] << 24);
        }

        private ushort getSampleSize(byte[] data)
        {
            return (ushort)((data[34] << 0) + (data[35] << 8));
        }

        private ushort getChannelCount(byte[] data)
        {
            return (ushort)((data[22] << 0) + (data[23] << 8));
        }

        private int getIndexOfFirstChunk(byte[] data)
        {
            int i = 0;

            while (i < data.Length)
            {
                if (data[i] == 'd' && data[i + 1] == 'a' && data[i + 2] == 't' && data[i + 3] == 'a')
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        private List<byte> getAudioData(byte[] data)
        {
            List<byte> audioData = new List<byte>();
            int i = getIndexOfFirstChunk(data) + 4; //+4 skip the "data" string

            if (i < 4)
            {
                throw new Exception("First chunk could not be found.");
            }

            while (i < data.Length)
            {
                int size = (int)(data[i++] << 0) + (int)(data[i++] << 8) + (int)(data[i++] << 16) + (int)(data[i++] << 24);

                audioData.AddRange(data.Skip(i).Take(size));
                i += size + 4; //move to the next chunk and +4 to skip the "data" string
            }

            return audioData;

        }
    }
}
