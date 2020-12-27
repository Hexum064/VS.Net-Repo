using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static BFB_WAV_LightFile_To_SPI_Flash_Mem.LightMap;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    public class LightMapSequence : INotifyCollectionChanged
    {
        //TODO: Make this an observable collection instead

        private List<LightMap> _lightMaps = new List<LightMap>();

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public LightMapSequence(ushort lightCount)
        {
            LightCount = lightCount;
            //Make sure we always have at least one map
            AddNew();
        }

        public LightMapSequence(ushort lightCount, IEnumerable<byte> bytes)
        {
            LightCount = lightCount;
            loadFromBytes(bytes.ToArray());
        }



        public ushort LightCount { get; private set; }

        public IEnumerable<LightMap> LightMaps
        {
            get
            {
                return _lightMaps.AsEnumerable();
            }
        }

        public LightMap this[int index]
        {
            get
            {
                return _lightMaps[index];
            }
        }

        public ushort MapCount
        {
            get
            {
                return (ushort)_lightMaps.Count;
            }
        }

        public int IndexOf(LightMap lightMap)
        {
            return _lightMaps.IndexOf(lightMap);
        }

        public LightMap InsertNew(int index)
        {
            LightMap lightMap = new LightMap(LightCount);
            _lightMaps.Insert(index, lightMap);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add);
            return lightMap;
        }

        public LightMap AddNew()
        {
            LightMap lightMap = new LightMap(LightCount);
            _lightMaps.Add(lightMap);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add);
            return lightMap;
        }

        public LightMap AddCopy(int sourceIndex)
        {
            LightMap lightMap = this[sourceIndex].Clone();
            _lightMaps.Add(lightMap);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add);
            return lightMap;
        }

        public LightMap InsertCopy(int sourceIndex, int destIndex)
        {
            LightMap lightMap = this[sourceIndex].Clone();
            _lightMaps.Insert(destIndex, lightMap);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add);
            return lightMap;
        }

        public void Remove(LightMap lightMap)
        {
            Remove(IndexOf(lightMap));
        }

        public LightMap Remove(int index)
        {
            //make sure we always have at least 1
            if (_lightMaps.Count < 2)
            {
                return null;
            }

            LightMap lightMap = this[index];
            _lightMaps.RemoveAt(index);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Remove);
            return lightMap;
        }



        public int ByteCount
        {
            get
            {
                int size = 4; //2 bytes for number of maps + 2 bytes for number of lights
                //number of maps * (2 bytes for hold time  + 3 * number of lights for 3 bytes per light)
                size += _lightMaps.Count * (2 + (LightCount * 3));

                return size;
            }
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();

            //first two bytes are number of maps
            bytes.AddRange(BitConverter.GetBytes((ushort)_lightMaps.Count));

            //next two bytes are number of lights per map
            bytes.AddRange(BitConverter.GetBytes(LightCount));

            //Then add each light map
            _lightMaps
                .ForEach((map) =>
                {
                    //first two bytes are the hold time (time before next map is loaded)
                    bytes.AddRange(BitConverter.GetBytes(map.HoldTime));

                    //Now add the RGB (actually GRB) bytes for each light

                    foreach(Color color in map.Lights)
                    {
                        bytes.Add(color.G);
                        bytes.Add(color.R);
                        bytes.Add(color.B);
                    }
                        
                });

            return bytes.ToArray();
        }

        public LightMapSequence Clone()
        {
            LightMapSequence newSequence = new LightMapSequence(LightCount);

            newSequence._lightMaps = _lightMaps
                .Select((map) => map.Clone())
                .ToList();

            return newSequence;
        }

        private void loadFromBytes(byte[] bytes)
        {
            ushort maps = BitConverter.ToUInt16(bytes, 0);
            int lightMapByteCount = LightCount * 3 + 2;//+2 for hold time
            for (ushort i = 0; i < maps; i++)
            {
                //4 bytes are included for the map count and light count, so we skip those 4 bytes.
                _lightMaps.Add(new LightMap(LightCount, bytes.Skip(i * lightMapByteCount + 4).Take(lightMapByteCount + 2)));
            }

        }

        private void RaiseCollectionChanged(NotifyCollectionChangedAction action)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs( NotifyCollectionChangedAction.Reset));
        }
    }
}
