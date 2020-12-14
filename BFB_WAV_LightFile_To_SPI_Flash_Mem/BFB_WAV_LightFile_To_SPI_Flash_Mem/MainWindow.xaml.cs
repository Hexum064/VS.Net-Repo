using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //TEST
            SerialPort serialPort = new SerialPort("com3");
            serialPort.BaudRate = 115200;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.DataBits = 8;
            serialPort.Handshake = Handshake.None;
            IMemApi memApi = new BusPirateMemApi(serialPort);
            Closing += (o, e) => memApi.Dispose();
            memApi.StatusUpdateCallback((m) => Debug.WriteLine(m));
            memApi.InitMem();
            memApi.ReadSignature();
        }
    }
}
