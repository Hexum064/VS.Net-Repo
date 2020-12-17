using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public class LightMapSequenceViewModel : ILightMapsCollection
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private LightMapSequence _lightMapSequence;
        private bool _canPlay = true;


        public LightMapSequenceViewModel(ushort lightCount)
        {
            _lightMapSequence = new LightMapSequence(lightCount);
            initCommands();
            initSequenceEventHandlers();
            SelectedLightMapIndex = 0;
           
        }

        public LightMapSequenceViewModel(LightMapSequence lightMapSequence)
        {
            _lightMapSequence = lightMapSequence.Clone();
            initCommands();
            initSequenceEventHandlers();
            SelectedLightMapIndex = 0;
        }

        public RelayCommand AddNewCommand { get; private set; }
        public RelayCommand InsertNewCommand { get; private set; }
        public RelayCommand AddCopyCommand { get; private set; }
        public RelayCommand InsertCopyCommand { get; private set; }
        public RelayCommand SetAllLightsColorCommand { get; private set; }
        public RelayCommand ClearAllLightsColorCommand { get; private set; }
        public RelayCommand RemoveCommand { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public RelayCommand PlayCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }

        public int LightCount
        {
            get
            {
                return _lightMapSequence.LightCount;
            }
        }

        private Color _globalColor;
        public Color GlobalColor
        {
            get
            {
                return _globalColor;
            }
            set
            {
                _globalColor = value;
                RaisePropertyChanged();
            }
        }

        public uint HoldTime
        {
            get
            {
                return SelectedLightMap.HoldTime;
            }
            set
            {
                SelectedLightMap.HoldTime = (ushort)value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TotalPlayTime));
            }
        }

        private LightMap _selectedLightMap = null;
        public LightMap SelectedLightMap
        {
            get
            {
                return _selectedLightMap;
            }
            set
            {
                _selectedLightMap = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HoldTime));
                RaisePropertyChanged(nameof(SelectedLightMapLights));
            }
        }

        private int _selectedLightMapIndex = 0;
        public int SelectedLightMapIndex
        {
            get
            {
                return _selectedLightMapIndex;
            }
            set
            {
                if (value >= _lightMapSequence.MapCount)
                {
                    return;
                }

                _selectedLightMapIndex = value;
                SelectedLightMap = _lightMapSequence[_selectedLightMapIndex];
                RaisePropertyChanged();
                
            }
        }

        public int LightMapCount
        {
            get
            {
                return _lightMapSequence.MapCount;
            }
        }

        public int MaxIndex
        {
            get
            {
                return _lightMapSequence.MapCount - 1;
            }
        }

        public int TotalPlayTime
        {
            get
            {
                int time = 0;
                foreach(LightMap lightMap in _lightMapSequence.LightMaps)
                {
                    time += lightMap.HoldTime;
                }

                return time;
                    
            }
        }

        public IEnumerable<LightToRefColor> SelectedLightMapLights
        {
            get
            {
                int i = 0;
                return SelectedLightMap.Lights
                    .Select((l) =>
                    {
                        LightToRefColor lightToRefColor = new LightToRefColor() { Color = l, Index = i++ };
                        lightToRefColor.PropertyChanged += (o, e) => SelectedLightMap.Lights[(o as LightToRefColor).Index] = (o as LightToRefColor).Color;
                        return lightToRefColor;
                    });
            }
        }

        public Action<LightMapSequence> SaveCallback
        {
            get;
            set;
        }

        private void initCommands()
        {
            AddNewCommand = new RelayCommand(addNew);
            InsertNewCommand = new RelayCommand(insertNew);
            AddCopyCommand = new RelayCommand(addCopy);
            InsertCopyCommand = new RelayCommand(insertCopy);
            SetAllLightsColorCommand = new RelayCommand(setAllLightsColor);
            ClearAllLightsColorCommand = new RelayCommand(clearAllLightsColor);
            RemoveCommand = new RelayCommand(remove);
            SaveCommand = new RelayCommand((o) => save(o as ICloseable));
            CancelCommand = new RelayCommand((o) => cancel(o as ICloseable));
            PlayCommand = new RelayCommand(play);
            StopCommand = new RelayCommand(stop);
 
        }

        private void initSequenceEventHandlers()
        {
            _lightMapSequence.CollectionChanged += (o, e) =>
            {
                RaisePropertyChanged(nameof(LightMapCount));
                RaisePropertyChanged(nameof(MaxIndex));
                RaisePropertyChanged(nameof(TotalPlayTime));

                if (SelectedLightMapIndex >= _lightMapSequence.MapCount)
                {
                    SelectedLightMapIndex--;
                }

                RaisePropertyChanged(nameof(SelectedLightMap));
                RaisePropertyChanged(nameof(HoldTime));
            };
        }

        

        private void addNew()
        {
            _lightMapSequence.AddNew();            
        }

        private void insertNew()
        {
            _lightMapSequence.InsertNew(SelectedLightMapIndex);
        }

        private void addCopy()
        {
            _lightMapSequence.AddCopy(SelectedLightMapIndex);
        }

        private void insertCopy()
        {
            _lightMapSequence.InsertCopy(SelectedLightMapIndex, SelectedLightMapIndex);
        }

        private void setAllLightsColor()
        {
            SelectedLightMap.SetAllLightsColor(GlobalColor);
            RaisePropertyChanged(nameof(SelectedLightMapLights));
        }

        private void clearAllLightsColor()
        {
            SelectedLightMap.SetAllLightsColor(Colors.Black);
            RaisePropertyChanged(nameof(SelectedLightMapLights));
        }

        private void remove()
        {
            if (_lightMapSequence.MapCount > 0)
            {
                _lightMapSequence.Remove(SelectedLightMapIndex);
            }
        }

        private void save(ICloseable closeable)
        {
            closeable?.Close();
            SaveCallback?.Invoke(_lightMapSequence);
        }

        private void cancel(ICloseable closeable)
        {
            closeable?.Close();
        }

        private void play()
        {
            _canPlay = true;

            Task.Run(async  () =>
            {
                int i = SelectedLightMapIndex;
                while (_canPlay && i < LightMapCount)
                {
                    SelectedLightMapIndex = i++;
                    await Task.Delay(SelectedLightMap.HoldTime);
                }

            });
        }

        private void stop()
        {
            _canPlay = false;
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
