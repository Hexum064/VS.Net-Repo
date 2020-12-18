using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    /// <summary>
    /// Interaction logic for InputLightCountWindow.xaml
    /// </summary>
    public partial class InputLightCountWindow : Window
    {
        public InputLightCountWindow()
        {
            InitializeComponent();
        }

        public bool OK
        {
            get;
            private set;
        }

        public ushort LightCount
        {
            get;
            private set;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OK = true;
            LightCount = Convert.ToUInt16(lightCountTextBox.Text);
            Close();
        }

        private void CencelButton_Click(object sender, RoutedEventArgs e)
        {
            OK = false;
            Close();
        }
    }
}
