using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
