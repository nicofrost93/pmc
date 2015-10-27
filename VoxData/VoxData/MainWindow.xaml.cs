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
        private BufferedWaveProvider buffer;
        private AsioOut input;
        private AsioOut output;
        
        private void button_Click(object sender, RoutedEventArgs e)
        {
            //input = new AsioOut(RecordInCbox.SelectedIndex);
            //WaveFormat format = new WaveFormat();
            //buffer = new BufferedWaveProvider(format);
            //buffer.DiscardOnBufferOverflow = true;
            //input.InitRecordAndPlayback(buffer, 1, 44100);
            //input.AudioAvailable += new EventHandler<AsioAudioAvailableEventArgs>(AudioAvailable);

            ////output = new AsioOut(RecordInCbox.SelectedIndex);
            ////output.Init(buffer);

            //input.Play();
            ////output.Play();

            if (sStream == null)
            {
                sStream = new WaveIn();
                sStream.BufferMilliseconds = 50;
                sStream.DeviceNumber = 0;
                sStream.WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(0).Channels);
                sStream.DataAvailable += SStream_DataAvailable;

                WaveInProvider wip = new WaveInProvider(sStream);

                sStream.StartRecording();


                mainV.Background = new SolidColorBrush(Colors.Green);
            }
            else 
            {
                sStream.StopRecording();
                mainV.Background = new SolidColorBrush(Colors.Aqua);
            }

        }

        private void SStream_DataAvailable(object sender, WaveInEventArgs e)
        {

        }
    }
}
