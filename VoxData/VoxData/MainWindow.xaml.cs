using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace VoxData
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {



        // Declara los atributos
        WaveIn waveIn;
        SampleAggregator sampleAggregator;
        //UnsignedMixerControl volumeControl;
        double desiredVolume = 100;
        RecordingState recordingState;
        WaveFileWriter writer;
        WaveFormat recordingFormat;

        public event EventHandler Stopped = delegate { };

        //inicializa 
        public MainWindow()
        {
            InitializeComponent();
            sampleAggregator = new SampleAggregator();
            RecordingFormat = new WaveFormat(44100, 1);
        }


        //Get-Set RecordingFormat
        public WaveFormat RecordingFormat
        {
            get
            {
                return recordingFormat;
            }
            set
            {
                recordingFormat = value;
                sampleAggregator.NotificationCount = value.SampleRate / 10;
            }
        }



        public void BeginMonitoring(int recordingDevice)
        {
            if (recordingState != RecordingState.Stopped)
            {
                throw new InvalidOperationException("Can't begin monitoring while we are in this state: " + recordingState.ToString());
            }
            waveIn = new WaveIn();
            waveIn.DeviceNumber = recordingDevice;
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.RecordingStopped += new EventHandler<StoppedEventArgs>(waveIn_RecordingStopped);
            waveIn.WaveFormat = recordingFormat;
            waveIn.StartRecording();
            //TryGetVolumeControl();
            recordingState = RecordingState.Monitoring;
        }

        void waveIn_RecordingStopped(object sender, EventArgs e)
        {
            recordingState = RecordingState.Stopped;
            writer.Dispose();
            Stopped(this, EventArgs.Empty);
        }

        //Data Available
        void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int bytesRecorded = e.BytesRecorded;
            WriteToFile(buffer, bytesRecorded);

            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((buffer[index + 1] << 8) |
                                        buffer[index + 0]);
                float sample32 = sample / 32768f;
                sampleAggregator.Add(sample32);
            }
        }


        //Escribre el archivo
        private void WriteToFile(byte[] buffer, int bytesRecorded)
        {
            long maxFileLength = this.recordingFormat.AverageBytesPerSecond * 60;

            if (recordingState == RecordingState.Recording
                || recordingState == RecordingState.RequestedStop)
            {
                int toWrite = (int)Math.Min(maxFileLength - writer.Length, bytesRecorded);
                if (toWrite > 0)
                {
                    writer.WriteData(buffer, 0, bytesRecorded);
                }
                else
                {
                    Stop();
                }
            }
        }


        public void Stop()
        {
            if (recordingState == RecordingState.Recording)
            {
                recordingState = RecordingState.RequestedStop;
                waveIn.StopRecording();
            }
        }


        private void button_Click(object sender, RoutedEventArgs e)
        {
            BeginMonitoring(0);

            mainV.Background = new SolidColorBrush(Colors.Green);

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            AudioSaver
        }
    }





    public class SampleAggregator
    {
        // volume
        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;
        public event EventHandler Restart = delegate { };
        public float maxValue;
        public float minValue;
        public int NotificationCount { get; set; }
        int count;

        public SampleAggregator()
        {
        }

        public void RaiseRestart()
        {
            Restart(this, EventArgs.Empty);
        }

        private void Reset()
        {
            count = 0;
            maxValue = minValue = 0;
        }

        public void Add(float value)
        {
            maxValue = Math.Max(maxValue, value);
            minValue = Math.Min(minValue, value);
            count++;
            if (count >= NotificationCount && NotificationCount > 0)
            {
                if (MaximumCalculated != null)
                {
                    MaximumCalculated(this, new MaxSampleEventArgs(minValue, maxValue));
                }
                Reset();
            }
        }
    }

    public class MaxSampleEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        public MaxSampleEventArgs(float minValue, float maxValue)
        {
            this.MaxSample = maxValue;
            this.MinSample = minValue;
        }
        public float MaxSample { get; private set; }
        public float MinSample { get; private set; }
    }


    public class AudioSaver
    {
        private string inputFile;

        public TimeSpan TrimFromStart { get; set; }
        public TimeSpan TrimFromEnd { get; set; }
        //public AutoTuneSettings AutoTuneSettings { get; set; }
        public SaveFileFormat SaveFileFormat { get; set; }
        public string LameExePath { get; set; }

        public AudioSaver(string inputFile)
        {
            this.inputFile = inputFile;
            //this.AutoTuneSettings = new AutoTuneSettings(); // default settings
        }

        public bool IsTrimNeeded
        {
            get
            {
                return TrimFromStart != TimeSpan.Zero || TrimFromEnd != TimeSpan.Zero;
            }
        }

        public void SaveAudio(string outputFile)
        {
            List<string> tempFiles = new List<string>();
            string fileToProcess = inputFile;
            if (IsTrimNeeded)
            {
                string tempFile = WavFileUtils.GetTempWavFileName();
                tempFiles.Add(tempFile);
                WavFileUtils.TrimWavFile(inputFile, tempFile, TrimFromStart, TrimFromEnd);
                fileToProcess = tempFile;
            }
            //if (AutoTuneSettings.Enabled)
            //{
            //    string tempFile = WavFileUtils.GetTempWavFileName();
            //    tempFiles.Add(tempFile);
            //    AutoTuneUtils.ApplyAutoTune(fileToProcess, tempFile, AutoTuneSettings);
            //    fileToProcess = tempFile;
            //}
            if (SaveFileFormat == SaveFileFormat.Mp3)
            {
                ConvertToMp3(this.LameExePath, fileToProcess, outputFile);
            }
            else
            {
                File.Copy(fileToProcess, outputFile);
            }
            DeleteTempFiles(tempFiles);
        }

        private void DeleteTempFiles(IEnumerable<string> tempFiles)
        {
            foreach (string tempFile in tempFiles)
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        public static void ConvertToMp3(string lameExePath, string waveFile, string mp3File)
        {
            Process converter = Process.Start(lameExePath, "-V2 \"" + waveFile + "\" \"" + mp3File + "\"");
            converter.WaitForExit();
        }
    }

    public static class WavFileUtils
    {
        public static void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd)
        {
            using (WaveFileReader reader = new WaveFileReader(inPath))
            {
                using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat))
                {
                    int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                    int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                    startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

                    int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                    endBytes = endBytes - endBytes % reader.WaveFormat.BlockAlign;
                    int endPos = (int)reader.Length - endBytes;

                    TrimWavFile(reader, writer, startPos, endPos);
                }
            }
        }

        public static string GetTempWavFileName()
        {
            return Path.Combine(System.IO.Path.GetTempPath(), new Guid().ToString() + ".wav");
        }

        private static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.WriteData(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }

    public enum SaveFileFormat
    {
        Wav,
        Mp3
    }

    public enum RecordingState
    {
        Stopped,
        Monitoring,
        Recording,
        RequestedStop
    }


}
