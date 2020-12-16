using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Xceed.Wpf.Toolkit;

namespace BFB_WAV_LightFile_To_SPI_Flash_Mem
{
    /// <summary>
    /// Interaction logic for LightMapSequencer.xaml
    /// </summary>
    public partial class LightMapSequencer : Window
    {
        public LightMapSequencer()
        {
            InitializeComponent();
            Loaded += LightMapSequencer_Loaded;
        }

        private void LightMapSequencer_Loaded(object sender, RoutedEventArgs e)
        {
            LightMapSequenceViewModel viewModel = (DataContext as LightMapSequenceViewModel);
            viewModel.PropertyChanged += LightMapSequencer_PropertyChanged;

            for(int i = 0; i < viewModel.LightCount; i++)
            {

                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                sp.Children.Add(new Label() { Margin = new Thickness(1) });
                sp.Children.Add(new ColorPicker() { Width = 40, Margin = new Thickness(1) });
                LightsWrapPanel.Children.Add(sp);

            }
            LightMapSequencer_PropertyChanged(viewModel, null);
        }
        

        private void LightMapSequencer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LightMapSequenceViewModel viewModel = (sender as LightMapSequenceViewModel);
            int i = 0;
            foreach(LightToRefColor light in viewModel.SelectedLightMapLights)
            {


                ((LightsWrapPanel.Children[i] as StackPanel).Children[0] as Label).Content = i.ToString();
                ((LightsWrapPanel.Children[i] as StackPanel).Children[1] as ColorPicker).DataContext = light;
                ((LightsWrapPanel.Children[i] as StackPanel).Children[1] as ColorPicker).SetBinding(ColorPicker.SelectedColorProperty, new Binding("Color"));
                i++;

            }
        }
    }
}
