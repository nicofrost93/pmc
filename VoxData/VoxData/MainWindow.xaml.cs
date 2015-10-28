using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
        private IAudioRecorder recorder;

        private RelayCommand beginRecordingCommand;
        private RelayCommand stopCommand;

        public event EventHandler Stopped = delegate { };

        //inicializa 
        public MainWindow()
        {
            InitializeComponent();
            sampleAggregator = new SampleAggregator();
            RecordingFormat = new WaveFormat(44100, 1);
            //this.recorder = recorder;
            //this.recorder.Stopped += new EventHandler(recorder_Stopped);
   
            this.stopCommand = new RelayCommand(() => Stop(),() => true);

        }
        //void recorder_Stopped(object sender, EventArgs e)
        //{
        //    Messenger.Default.Send(new NavigateMessage(SaveViewModel.ViewName, new VoiceRecorderState(waveFileName, null)));
        //}


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


        public void BeginRecording(string waveFileName)
        {


            if (recordingState != RecordingState.Monitoring)
            {
                throw new InvalidOperationException("Can't begin recording while we are in this state: " + recordingState.ToString());
            }
            writer = new WaveFileWriter(waveFileName, recordingFormat);
            recordingState = RecordingState.Recording;
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
            Console.WriteLine(bytesRecorded);
            WriteToFile(buffer, bytesRecorded);

            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((buffer[index + 1] << 8) |
                                        buffer[index + 0]);
                float sample32 = sample / 32768f;
                sampleAggregator.Add(sample32);
                Console.WriteLine(sample32);

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
            BeginRecording("C:\\Users\\Usuario\\Desktop\\pm\\nombre.wav");
            Timer t = new Timer(1000);
            t.Disposed += T_Disposed;

            mainV.Background = new SolidColorBrush(Colors.Green);

        }

        private void T_Disposed(object sender, EventArgs e)
        {
            Stop();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
           
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

    // FFT based pitch detector. seems to work best with block sizes of 4096
    public class FftPitchDetector 
    {
        private float sampleRate;

        public FftPitchDetector(float sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        private float HammingWindow(int n, int N)
        {
            return 0.54f - 0.46f * (float)Math.Cos((2 * Math.PI * n) / (N - 1));
        }

        private float[] fftBuffer;
        private float[] prevBuffer;
        public float DetectPitch(float[] buffer, int inFrames)
        {
            Func<int, int, float> window = HammingWindow;
            if (prevBuffer == null)
            {
                prevBuffer = new float[inFrames];
            }

            // double frames since we are combining present and previous buffers
            int frames = inFrames * 2;
            if (fftBuffer == null)
            {
                fftBuffer = new float[frames * 2]; // times 2 because it is complex input
            }

            for (int n = 0; n < frames; n++)
            {
                if (n < inFrames)
                {
                    fftBuffer[n * 2] = prevBuffer[n] * window(n, frames);
                    fftBuffer[n * 2 + 1] = 0; // need to clear out as fft modifies buffer
                }
                else
                {
                    fftBuffer[n * 2] = buffer[n - inFrames] * window(n, frames);
                    fftBuffer[n * 2 + 1] = 0; // need to clear out as fft modifies buffer
                }
            }

            // assuming frames is a power of 2
            SmbPitchShift.smbFft(fftBuffer, frames, -1);

            float binSize = sampleRate / frames;
            int minBin = (int)(85 / binSize);
            int maxBin = (int)(300 / binSize);
            float maxIntensity = 0f;
            int maxBinIndex = 0;
            for (int bin = minBin; bin <= maxBin; bin++)
            {
                float real = fftBuffer[bin * 2];
                float imaginary = fftBuffer[bin * 2 + 1];
                float intensity = real * real + imaginary * imaginary;
                if (intensity > maxIntensity)
                {
                    maxIntensity = intensity;
                    maxBinIndex = bin;
                }
            }
            return binSize * maxBinIndex;
        }
    }


    class SmbPitchShift
    {
        public const double M_PI_VAL = 3.14159265358979323846;
        public const int MAX_FRAME_LENGTH = 8192;

        static float[] gInFIFO = new float[MAX_FRAME_LENGTH];
        static float[] gOutFIFO = new float[MAX_FRAME_LENGTH];
        static float[] gFFTworksp = new float[2 * MAX_FRAME_LENGTH];
        static float[] gLastPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        static float[] gSumPhase = new float[MAX_FRAME_LENGTH / 2 + 1];
        static float[] gOutputAccum = new float[2 * MAX_FRAME_LENGTH];
        static float[] gAnaFreq = new float[MAX_FRAME_LENGTH];
        static float[] gAnaMagn = new float[MAX_FRAME_LENGTH];
        static float[] gSynFreq = new float[MAX_FRAME_LENGTH];
        static float[] gSynMagn = new float[MAX_FRAME_LENGTH];
        static int gRover = 0;

        ///<summary>
        /// Routine smbPitchShift(). See top of file for explanation
        /// Purpose: doing pitch shifting while maintaining duration using the Short
        /// Time Fourier Transform.
        /// Author: (c)1999-2009 Stephan M. Bernsee &lt;smb [AT] dspdimension [DOT] com&gt;
        ///</summary>
        public static void smbPitchShift(float pitchShift, int numSampsToProcess, int fftFrameSize, int osamp, float sampleRate, float[] indata, float[] outdata)
        {

            double magn, phase, tmp, window, real, imag;
            double freqPerBin, expct;
            int i, k, qpd, index, inFifoLatency, stepSize, fftFrameSize2;

            /* set up some handy variables */
            fftFrameSize2 = fftFrameSize / 2;
            stepSize = fftFrameSize / osamp;
            freqPerBin = sampleRate / (double)fftFrameSize;
            expct = 2.0 * M_PI_VAL * (double)stepSize / (double)fftFrameSize;
            inFifoLatency = fftFrameSize - stepSize;
            if (gRover == 0) gRover = inFifoLatency;

            /* main processing loop */
            for (i = 0; i < numSampsToProcess; i++)
            {

                /* As long as we have not yet collected enough data just read in */
                gInFIFO[gRover] = indata[i];
                outdata[i] = gOutFIFO[gRover - inFifoLatency];
                gRover++;

                /* now we have enough data for processing */
                if (gRover >= fftFrameSize)
                {
                    gRover = inFifoLatency;

                    /* do windowing and re,im interleave */
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * M_PI_VAL * (double)k / (double)fftFrameSize) + .5;
                        gFFTworksp[2 * k] = (float)(gInFIFO[k] * window);
                        gFFTworksp[2 * k + 1] = 0.0f;
                    }


                    /* ***************** ANALYSIS ******************* */
                    /* do transform */
                    smbFft(gFFTworksp, fftFrameSize, -1);

                    /* this is the analysis step */
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        /* de-interlace FFT buffer */
                        real = gFFTworksp[2 * k];
                        imag = gFFTworksp[2 * k + 1];

                        /* compute magnitude and phase */
                        magn = 2.0 * Math.Sqrt(real * real + imag * imag);
                        phase = Math.Atan2(imag, real);

                        /* compute phase difference */
                        tmp = phase - gLastPhase[k];
                        gLastPhase[k] = (float)phase;

                        /* subtract expected phase difference */
                        tmp -= (double)k * expct;

                        /* map delta phase into +/- Pi interval */
                        qpd = (int)(tmp / M_PI_VAL);
                        if (qpd >= 0) qpd += (qpd & 1);
                        else qpd -= (qpd & 1);
                        tmp -= M_PI_VAL * (double)qpd;

                        /* get deviation from bin frequency from the +/- Pi interval */
                        tmp = osamp * tmp / (2.0 * M_PI_VAL);

                        /* compute the k-th partials' true frequency */
                        tmp = (double)k * freqPerBin + tmp * freqPerBin;

                        /* store magnitude and true frequency in analysis arrays */
                        gAnaMagn[k] = (float)magn;
                        gAnaFreq[k] = (float)tmp;

                    }

                    /* ***************** PROCESSING ******************* */
                    /* this does the actual pitch shifting */
                    Array.Clear(gSynMagn, 0, fftFrameSize);
                    Array.Clear(gSynFreq, 0, fftFrameSize);
                    for (k = 0; k <= fftFrameSize2; k++)
                    {
                        index = (int)(k * pitchShift);
                        if (index <= fftFrameSize2)
                        {
                            gSynMagn[index] += gAnaMagn[k];
                            gSynFreq[index] = gAnaFreq[k] * pitchShift;
                        }
                    }

                    /* ***************** SYNTHESIS ******************* */
                    /* this is the synthesis step */
                    for (k = 0; k <= fftFrameSize2; k++)
                    {

                        /* get magnitude and true frequency from synthesis arrays */
                        magn = gSynMagn[k];
                        tmp = gSynFreq[k];

                        /* subtract bin mid frequency */
                        tmp -= (double)k * freqPerBin;

                        /* get bin deviation from freq deviation */
                        tmp /= freqPerBin;

                        /* take osamp into account */
                        tmp = 2.0 * M_PI_VAL * tmp / osamp;

                        /* add the overlap phase advance back in */
                        tmp += (double)k * expct;

                        /* accumulate delta phase to get bin phase */
                        gSumPhase[k] += (float)tmp;
                        phase = gSumPhase[k];

                        /* get real and imag part and re-interleave */
                        gFFTworksp[2 * k] = (float)(magn * Math.Cos(phase));
                        gFFTworksp[2 * k + 1] = (float)(magn * Math.Sin(phase));
                    }

                    /* zero negative frequencies */
                    for (k = fftFrameSize + 2; k < 2 * fftFrameSize; k++) gFFTworksp[k] = 0.0f;

                    /* do inverse transform */
                    smbFft(gFFTworksp, fftFrameSize, 1);

                    /* do windowing and add to output accumulator */
                    for (k = 0; k < fftFrameSize; k++)
                    {
                        window = -.5 * Math.Cos(2.0 * M_PI_VAL * (double)k / (double)fftFrameSize) + .5;
                        gOutputAccum[k] += (float)(2.0 * window * gFFTworksp[2 * k] / (fftFrameSize2 * osamp));
                    }
                    for (k = 0; k < stepSize; k++) gOutFIFO[k] = gOutputAccum[k];

                    /* shift accumulator */
                    int destOffset = 0;
                    int sourceOffset = stepSize;
                    Array.Copy(gOutputAccum, sourceOffset, gOutputAccum, destOffset, fftFrameSize);
                    //memmove(gOutputAccum, gOutputAccum + stepSize, fftFrameSize * sizeof(float));

                    /* move input FIFO */
                    for (k = 0; k < inFifoLatency; k++) gInFIFO[k] = gInFIFO[k + stepSize];
                }
            }
        }

        // -----------------------------------------------------------------------------------------------------------------


        /* 
            FFT routine, (C)1996 S.M.Bernsee. Sign = -1 is FFT, 1 is iFFT (inverse)
            Fills fftBuffer[0...2*fftFrameSize-1] with the Fourier transform of the
            time domain data in fftBuffer[0...2*fftFrameSize-1]. The FFT array takes
            and returns the cosine and sine parts in an interleaved manner, ie.
            fftBuffer[0] = cosPart[0], fftBuffer[1] = sinPart[0], asf. fftFrameSize
            must be a power of 2. It expects a complex input signal (see footnote 2),
            ie. when working with 'common' audio signals our input signal has to be
            passed as {in[0],0.,in[1],0.,in[2],0.,...} asf. In that case, the transform
            of the frequencies of interest is in fftBuffer[0...fftFrameSize].
        */
        public static void smbFft(float[] fftBuffer, int fftFrameSize, int sign)
        {
            float wr, wi, arg, temp;
            int p1, p2; // MRH: were float*
            float tr, ti, ur, ui;
            int p1r, p1i, p2r, p2i; // MRH: were float*
            int i, bitm, j, le, le2, k;
            int fftFrameSize2 = fftFrameSize * 2;

            for (i = 2; i < fftFrameSize2 - 2; i += 2)
            {
                for (bitm = 2, j = 0; bitm < fftFrameSize2; bitm <<= 1)
                {
                    if ((i & bitm) != 0) j++;
                    j <<= 1;
                }
                if (i < j)
                {
                    p1 = i; p2 = j;
                    temp = fftBuffer[p1];
                    fftBuffer[p1++] = fftBuffer[p2];
                    fftBuffer[p2++] = temp;
                    temp = fftBuffer[p1];
                    fftBuffer[p1] = fftBuffer[p2];
                    fftBuffer[p2] = temp;
                }
            }
            int kmax = (int)(Math.Log(fftFrameSize) / Math.Log(2.0) + 0.5);
            for (k = 0, le = 2; k < kmax; k++)
            {
                le <<= 1;
                le2 = le >> 1;
                ur = 1.0f;
                ui = 0.0f;
                arg = (float)(M_PI_VAL / (le2 >> 1));
                wr = (float)Math.Cos(arg);
                wi = (float)(sign * Math.Sin(arg));
                for (j = 0; j < le2; j += 2)
                {
                    p1r = j; p1i = p1r + 1;
                    p2r = p1r + le2; p2i = p2r + 1;
                    for (i = j; i < fftFrameSize2; i += le)
                    {
                        float p2rVal = fftBuffer[p2r];
                        float p2iVal = fftBuffer[p2i];
                        tr = p2rVal * ur - p2iVal * ui;
                        ti = p2rVal * ui + p2iVal * ur;
                        fftBuffer[p2r] = fftBuffer[p1r] - tr;
                        fftBuffer[p2i] = fftBuffer[p1i] - ti;
                        fftBuffer[p1r] += tr;
                        fftBuffer[p1i] += ti;
                        p1r += le;
                        p1i += le;
                        p2r += le;
                        p2i += le;
                    }
                    tr = ur * wr - ui * wi;
                    ui = ur * wi + ui * wr;
                    ur = tr;
                }
            }
        }

        /// <summary>
        ///    12/12/02, smb
        ///
        ///    PLEASE NOTE:
        ///
        ///    There have been some reports on domain errors when the atan2() function was used
        ///    as in the above code. Usually, a domain error should not interrupt the program flow
        ///    (maybe except in Debug mode) but rather be handled "silently" and a global variable
        ///    should be set according to this error. However, on some occasions people ran into
        ///    this kind of scenario, so a replacement atan2() function is provided here.
        ///    If you are experiencing domain errors and your program stops, simply replace all
        ///    instances of atan2() with calls to the smbAtan2() function below.
        /// </summary>
        double smbAtan2(double x, double y)
        {
            double signx;
            if (x > 0.0) signx = 1.0;
            else signx = -1.0;

            if (x == 0.0) return 0.0;
            if (y == 0.0) return signx * M_PI_VAL / 2.0;

            return Math.Atan2(x, y);
        }

    }

    public interface IAudioRecorder
    {
        void BeginMonitoring(int recordingDevice);
        void BeginRecording(string path);
        void Stop();
        double MicrophoneLevel { get; set; }
        RecordingState RecordingState { get; }
        SampleAggregator SampleAggregator { get; }
        event EventHandler Stopped;
        WaveFormat RecordingFormat { get; set; }
        TimeSpan RecordedTime { get; }
    }
    class NavigateMessage
    {
        public string TargetView { get; private set; }
        public object State { get; private set; }

        public NavigateMessage(string targetView, object state)
        {
            this.TargetView = targetView;
            this.State = state;
        }
    }

    class VoiceRecorderState
    {
        private string recordingFileName;
        private string effectedFileName;

        public VoiceRecorderState(string recordingFileName, string effectedFileName)
        {
            this.RecordingFileName = recordingFileName;
            this.EffectedFileName = effectedFileName;
        }

        public string RecordingFileName
        {
            get
            {
                return recordingFileName;
            }
            set
            {
                if ((recordingFileName != null) && (recordingFileName != value))
                {
                    DeleteFile(recordingFileName);
                }
                this.recordingFileName = value;
            }
        }

        public string EffectedFileName
        {
            get
            {
                return effectedFileName;
            }
            set
            {
                if ((effectedFileName != null) && (effectedFileName != value))
                {
                    DeleteFile(effectedFileName);
                }
                this.effectedFileName = value;
            }
        }

        public string ActiveFile
        {
            get
            {
                    return EffectedFileName;
            }
        }


        public void DeleteFiles()
        {
            this.RecordingFileName = null;
            this.EffectedFileName = null;
        }

        private void DeleteFile(string fileName)
        {
            if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }



}
