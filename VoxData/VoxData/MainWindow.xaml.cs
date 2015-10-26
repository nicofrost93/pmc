using NAudio.Wave;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VoxData
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private WaveIn sStream = null;
        private DirectSoundOut sOut = null;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            sStream = new WaveIn();
            sStream.DeviceNumber = 0;
            sStream.WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(0).Channels);

            WaveInProvider wip = new WaveInProvider(sStream);
            sStream.StartRecording();

            mainV.Background = new SolidColorBrush(Colors.Green);
        

        }
    }
}
