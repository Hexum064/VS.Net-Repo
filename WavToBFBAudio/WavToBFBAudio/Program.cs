using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WavToBFBAudio
{
    class Program
    {
        static void Main(string[] args)
        {
            string source;
            string destination;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Enter source file or 'q' to quit.");
                source = Console.ReadLine();

                if (string.IsNullOrEmpty(source))
                {
                    source = "m:\\downloads\\hw2.wav";
                }

                if (string.Equals("q", source, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (File.Exists(source))
                {
                    break;
                }

                Console.WriteLine($"'{source}' not found. Press enter to continue.");
                Console.ReadLine();
            }

            Console.WriteLine("Enter destination file or 'q' to quit.");
            destination = Console.ReadLine();

            if (string.IsNullOrEmpty(destination))
            {
                destination = "m:\\downloads\\hw2.bfba";
            }

            if (string.Equals("q", destination, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ConvertFile(source, destination);
        }

        static void ConvertFile(string source, string destination)
        {
            Console.WriteLine("Converting...");

            try
            {
                byte[] data = File.ReadAllBytes(source);
                int sampleRate = GetSampleRate(data);
                int channels = GetChannelCount(data);
                int sampleSize = GetSampleSize(data);
                List<byte> audioData = GetAudioData(data);

                WriteOutputFile(destination, sampleRate, channels, sampleSize, audioData);
                Console.WriteLine("File converted.");

            }
            catch(Exception exc)
            {
                Console.WriteLine(exc);

            }
            Console.WriteLine("Press enter to quit.");
            Console.ReadLine();
        }

        static void WriteOutputFile(string destination, int sampleRate, int channels, int sampleSize, List<byte> audioData)
        {
            List<byte> data = new List<byte>();
            audioData = sampleSize == 8 ? ConvertFrom8bit(audioData) : ConvertFrom16bit(audioData);
            //GraphData(audioData.Count, audioData);
            int sampleCount = audioData.Count / 2;
            //These 2 values will be left in little-endian format because they will be read diretly into a 32bit uint
            byte[] sampleRateData = { (byte)((sampleRate >> 0) & 0xFF), (byte)((sampleRate >> 8) & 0xFF), (byte)((sampleRate >> 16) & 0xFF), (byte)((sampleRate >> 24) & 0xFF) };
            byte[] sampleCountData = { (byte)((sampleCount >> 0) & 0xFF), (byte)((sampleCount >> 8) & 0xFF), (byte)((sampleCount >> 16) & 0xFF), (byte)((sampleCount >> 24) & 0xFF) };
            data.AddRange(sampleRateData);
            data.Add((byte)channels);
            data.AddRange(sampleCountData);

            //Audio data will be stored as big-endian because of the way it is read into the DAC
            //UPDATE: Switched to little-endian for test
            data.AddRange(audioData);

            File.WriteAllBytes(destination, data.ToArray());


        }

        static void GraphData(int len, List<byte> data)
        {
            Console.Write("0");
            Console.CursorLeft = Console.BufferWidth - 1;
            Console.Write("1");
            Console.WriteLine();
            decimal coef = (decimal)Console.BufferWidth / 65535;
            ushort val;
            for(int i = 0; i < len; i+=2)
            {
                val = BitConverter.ToUInt16(new byte[] { data[i + 1], data[i] }, 0);
                Console.CursorLeft = (int)(val * coef);
                Console.Write("|");
                Console.WriteLine();
            }
        }

        static List<byte> ConvertFrom8bit(List<byte> audioData)
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

        static List<byte> ConvertFrom16bit(List<byte> audioData)
        {
            //Turn the 16bit, 2s comp samples into 12bit samples
            List<byte> newData = new List<byte>();

            for (int i = 0; i < audioData.Count; i+=2)
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

        static int GetSampleRate(byte[] data)
        {
            return (int)(data[24] << 0) + (int)(data[25] << 8) + (int)(data[26] << 16) + (int)(data[27] << 24);
        }

        static int GetSampleSize(byte[] data)
        {
            return (int)(data[34] << 0) + (int)(data[35] << 8);
        }
        
        static int GetChannelCount(byte[] data)
        {
            return (int)(data[22] << 0) + (int)(data[23] << 8);
        }

        static int GetIndexOfFirstChunk(byte[] data)
        {
            int i = 0;

            while (i < data.Length)
            { 
                if (data[i] == 'd' && data[i+1] == 'a' && data[i+2] == 't' && data[i+3] == 'a')
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        static List<byte> GetAudioData(byte[] data)
        {
            List<byte> audioData = new List<byte>();
            int i = GetIndexOfFirstChunk(data) + 4; //+4 skip the "data" string

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
