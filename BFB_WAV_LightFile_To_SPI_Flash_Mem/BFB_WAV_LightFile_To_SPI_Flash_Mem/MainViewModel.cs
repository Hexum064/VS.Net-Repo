using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    class MainViewModel<MemApiType> : INotifyPropertyChanged where MemApiType : IMemApi 
    {
        private IMemApi _memApi;


        public MainViewModel(ushort lightCount)
        {
            LightCount = lightCount;
            initCommands();
            loadComPorts();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand AddEntryCommand { get; private set; }
        public RelayCommand RemoveEntryCommand { get; private set; }
        public RelayCommand MoveEntryUpCommand { get; private set; }
        public RelayCommand MoveEntryDownCommand { get; private set; }
        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand WriteCommand { get; private set; }
        public RelayCommand SetAudioCommand { get; private set; }
        public RelayCommand SetLightSequenceCommand { get; private set; }
        public RelayCommand LoadFromMemCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand LoadCommand { get; private set; }
        public ushort LightCount
        {
            get;
            private set;
        }

        public ObservableCollection<MemEntry> MemEntries
        {
            get;
            private set;
        } = new ObservableCollection<MemEntry>();

        public ObservableCollection<string> ComPorts
        {
            get;
            private set;
        } = new ObservableCollection<string>();

        private string _status;
        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                RaisePropertyChanged();
            }
        }

        public int EntryCount
        {
            get
            {
                return MemEntries.Count;
            }
        }

        public int TotalByteCount
        {
            get
            {
                return MemEntries.Sum((e) => e.ByteCount);
            }
        }

        private void initCommands()
        {
            AddEntryCommand = new RelayCommand(addEntry);
            RemoveEntryCommand = new RelayCommand((o) => removeEntry(o as MemEntry));
            MoveEntryUpCommand = new RelayCommand((o) => moveEntryUp(o as MemEntry));
            MoveEntryDownCommand = new RelayCommand((o) => moveEntryDown(o as MemEntry));
            ConnectCommand = new RelayCommand((o) => connect(o as string));
            WriteCommand = new RelayCommand(write);
            SetAudioCommand = new RelayCommand((o) => setAudio(o as MemEntry));
            SetLightSequenceCommand = new RelayCommand((o) => setLightSequence(o as MemEntry));
            LoadFromMemCommand = new RelayCommand(loadFromMem);
            SaveCommand = new RelayCommand(save);
            LoadCommand = new RelayCommand(load);
        }

        private void loadComPorts()
        {
            foreach(string port in SerialPort.GetPortNames())
            {
                ComPorts.Add(port);
            }
        }

        private void addEntry()
        {
            MemEntry memEntry = new MemEntry(LightCount);
            MemEntries.Add(memEntry);
            RaisePropertyChanged(nameof(EntryCount));
            RaisePropertyChanged(nameof(TotalByteCount));
            string totalByteCountName = nameof(TotalByteCount);

            memEntry.PropertyChanged += (o, e) => RaisePropertyChanged(totalByteCountName);
        }

        private void removeEntry(MemEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            MemEntries.Remove(entry);
            RaisePropertyChanged(nameof(EntryCount));
            RaisePropertyChanged(nameof(TotalByteCount));
        }

        private void moveEntryUp(MemEntry entry)
        {
            int index = MemEntries.IndexOf(entry);

            if (index >= MemEntries.Count - 1)
            {
                return;
            }

            MemEntries.Move(index, index + 1);
        }

        private void moveEntryDown(MemEntry entry)
        {
            int index = MemEntries.IndexOf(entry);

            if (index == 0)
            {
                return;
            }

            MemEntries.Move(index, index - 1);
        }

        private void connect(string comPort)
        {
            if (string.IsNullOrEmpty(comPort))
            {
                return;
            }

            SerialPort serialPort = new SerialPort(comPort);
            serialPort.BaudRate = 115200;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = Handshake.None;

            _memApi?.Dispose();

            _memApi = Activator.CreateInstance<MemApiType>();
            _memApi.SetStatusUpdateCallback((s) => Status = s);
            _memApi.InitMem(serialPort);

        }

        private void loadFromMem()
        {

        }

        private void write()
        {
            File.WriteAllBytes("all.bin", MemEntries.GetData().ToArray());
            Status = "Writing FAT";
            _memApi.EraseAll();
            _memApi.WriteData(0, MemEntries.GetFat());
            Status = "Writing Data";
            _memApi.WriteData(Extensions.STARTING_ADDRESS, MemEntries.GetData());
            Status = "Finish writing";
        }

        private void setAudio(MemEntry entry)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "wav|*.wav";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == true)
            {
                if (entry.LoadAudioFile(openFileDialog.FileName))
                {
                    Status = "Audio file loaded";
                }
                else
                {
                    Status = "Could not load audio file.";
                }
            }

            RaisePropertyChanged(nameof(TotalByteCount));
        }

        private void setLightSequence(MemEntry entry)
        {
            LightMapSequencer lightMapSequencer = new LightMapSequencer();
            LightMapSequenceViewModel lightMapSequenceViewModel = new LightMapSequenceViewModel(entry.LightMapSequence);
            lightMapSequencer.DataContext = lightMapSequenceViewModel;
            lightMapSequenceViewModel.SaveCallback = (lms) => entry.LightMapSequence = lms;
            lightMapSequencer.ShowDialog();

            RaisePropertyChanged(nameof(TotalByteCount));
        }

        private void save()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                List<byte> bytes = new List<byte>();
                bytes.AddRange(MemEntries.GetFat());
                bytes.AddRange(MemEntries.GetData());
                File.WriteAllBytes(saveFileDialog.FileName, bytes.ToArray());
            }
        }

        private void load()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            
            if (openFileDialog.ShowDialog() == true)
            {
                byte[] bytes = File.ReadAllBytes(openFileDialog.FileName);
                ushort entries = BitConverter.ToUInt16(bytes, 0);
                //4+4 bytes per fat entry + 2 for entry count
                uint fatSize = (uint)(entries * (4+4) + 2);
                for (ushort i = 0; i < entries; i++)
                {
                    uint audioAddress = BitConverter.ToUInt32(bytes, i * 8 + 2) - Extensions.STARTING_ADDRESS;
                    uint lightsAddress = BitConverter.ToUInt32(bytes, i * 8 + 6);
                    byte[] lightBytes = new byte[0];

                    //Add in the size of the addresses and the entry count
                    audioAddress += fatSize;

                    if (lightsAddress > 0)
                    {
                        lightsAddress -= Extensions.STARTING_ADDRESS;
                        lightsAddress += fatSize;
                        lightBytes = getLightBytes(lightsAddress, bytes);
                    }
                    MemEntries.Add(new MemEntry(LightCount, getAudioBytes(audioAddress, bytes), lightBytes));
                   
                }
            }
           
        }

        private byte[] getAudioBytes(uint address, byte[] bytes)
        {
            //skip sampleRate bytes;
            byte numChannels = bytes[address + 4];
            uint sampleCount = BitConverter.ToUInt32(bytes, (int)address + 5);
            int audioSize = ((int)numChannels * (int)sampleCount * 2) + 9; //*2 for 16 bit audio, +9 for sample rate, num channels, and sample count bytes
            byte nameSize = bytes[audioSize + 1];
            return bytes
                .Skip((int)address)
                .Take(audioSize + nameSize) 
                .ToArray();
        }

        private byte[] getLightBytes(uint address, byte[] bytes)
        {
            ushort mapCount = BitConverter.ToUInt16(bytes, (int)address);
            LightCount = BitConverter.ToUInt16(bytes, (int)address + 2);
            return bytes
                .Skip((int)address) 
                .Take(mapCount * (LightCount * 3 + 2) + 4) //+2 for the hold time bytes +4 for the map count and light count bytes
                .ToArray();
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
