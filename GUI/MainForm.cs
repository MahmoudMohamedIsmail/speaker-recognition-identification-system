using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Audio.Filters;
using Recorder.Recorder;
using Recorder.MFCC;
using System.Diagnostics;
using System.Collections.Generic;
namespace Recorder
{
    /// <summary>
    ///   Speaker Identification application.
    /// </summary>
    /// 
    public partial class MainForm : Form
    {
        /// <summary>
        /// Data of the opened audio file, contains:
        ///     1. signal data
        ///     2. sample rate
        ///     3. signal length in ms
        /// </summary>
        private AudioSignal signal = null;


        private string path;

        private Encoder encoder;
        private Decoder decoder;

        private bool isRecorded;

        public MainForm()
        {
            InitializeComponent();

            // Configure the wavechart
            chart.SimpleMode = true;
            chart.AddWaveform("wave", Color.Green, 1, false);
            updateButtons();
        }

        
        /// <summary>
        ///   Starts recording audio from the sound card
        /// </summary>
        /// 
        private void btnRecord_Click(object sender, EventArgs e)
        {
            isRecorded = true;
            this.encoder = new Encoder(source_NewFrame, source_AudioSourceError);
            this.encoder.Start();
            updateButtons();
        }

        /// <summary>
        ///   Plays the recorded audio stream.
        /// </summary>
        /// 
        private void btnPlay_Click(object sender, EventArgs e)
        {
            InitializeDecoder();
            // Configure the track bar so the cursor
            // can show the proper current position
            if (trackBar1.Value < this.decoder.frames)
                this.decoder.Seek(trackBar1.Value);
            trackBar1.Maximum = this.decoder.samples;
            this.decoder.Start();
            updateButtons();
        }

        private void InitializeDecoder()
        {
            if (isRecorded)
            {
                // First, we rewind the stream
                this.encoder.stream.Seek(0, SeekOrigin.Begin);
                this.decoder = new Decoder(this.encoder.stream, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
            else
            {
                this.decoder = new Decoder(this.path, this.Handle, output_AudioOutputError, output_FramePlayingStarted, output_NewFrameRequested, output_PlayingFinished);
            }
        }

        /// <summary>
        ///   Stops recording or playing a stream.
        /// </summary>
        /// 
        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This callback will be called when there is some error with the audio 
        ///   source. It can be used to route exceptions so they don't compromise 
        ///   the audio processing pipeline.
        /// </summary>
        /// 
        private void source_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   This method will be called whenever there is a new input audio frame 
        ///   to be processed. This would be the case for samples arriving at the 
        ///   computer's microphone
        /// </summary>
        /// 
        private void source_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            this.encoder.addNewFrame(eventArgs.Signal);
            updateWaveform(this.encoder.current, eventArgs.Signal.Length);
        }


        /// <summary>
        ///   This event will be triggered as soon as the audio starts playing in the 
        ///   computer speakers. It can be used to update the UI and to notify that soon
        ///   we will be requesting additional frames.
        /// </summary>
        /// 
        private void output_FramePlayingStarted(object sender, PlayFrameEventArgs e)
        {
            updateTrackbar(e.FrameIndex);

            if (e.FrameIndex + e.Count < this.decoder.frames)
            {
                int previous = this.decoder.Position;
                decoder.Seek(e.FrameIndex);

                Signal s = this.decoder.Decode(e.Count);
                decoder.Seek(previous);

                updateWaveform(s.ToFloat(), s.Length);
            }
        }

        /// <summary>
        ///   This event will be triggered when the output device finishes
        ///   playing the audio stream. Again we can use it to update the UI.
        /// </summary>
        /// 
        private void output_PlayingFinished(object sender, EventArgs e)
        {
            updateButtons();
            updateWaveform(new float[BaseRecorder.FRAME_SIZE], BaseRecorder.FRAME_SIZE);
        }

        /// <summary>
        ///   This event is triggered when the sound card needs more samples to be
        ///   played. When this happens, we have to feed it additional frames so it
        ///   can continue playing.
        /// </summary>
        /// 
        private void output_NewFrameRequested(object sender, NewFrameRequestedEventArgs e)
        {
            this.decoder.FillNewFrame(e);
        }


        void output_AudioOutputError(object sender, AudioOutputErrorEventArgs e)
        {
            throw new Exception(e.Description);
        }

        /// <summary>
        ///   Updates the audio display in the wave chart
        /// </summary>
        /// 
        private void updateWaveform(float[] samples, int length)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    chart.UpdateWaveform("wave", samples, length);
                }));
            }
            else
            {
                if (this.encoder != null) { chart.UpdateWaveform("wave", this.encoder.current, length); }
            }
        }

        /// <summary>
        ///   Updates the current position at the trackbar.
        /// </summary>
        /// 
        private void updateTrackbar(int value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
                }));
            }
            else
            {
                trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, value));
            }
        }

        private void updateButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(updateButtons));
                return;
            }

            if (this.encoder != null && this.encoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = false;
            }
            else if (this.decoder != null && this.decoder.IsRunning())
            {
                btnAdd.Enabled = false;
                btnIdentify.Enabled = false;
                btnPlay.Enabled = false;
                btnStop.Enabled = true;
                btnRecord.Enabled = false;
                trackBar1.Enabled = true;
            }
            else
            {
                btnAdd.Enabled = this.path != null || this.encoder != null;
                btnIdentify.Enabled = true;// to active == true
                btnPlay.Enabled = this.path != null || this.encoder != null;//stream != null;
                btnStop.Enabled = false;
                btnRecord.Enabled = true;
                trackBar1.Enabled = this.decoder != null;
                trackBar1.Value = 0;
            }
        }

        private void MainFormFormClosed(object sender, FormClosedEventArgs e)
        {
            Stop();
        }

        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.encoder != null)
            {
                Stream fileStream = saveFileDialog1.OpenFile();
                this.encoder.Save(fileStream);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog(this);
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (this.encoder != null) { lbLength.Text = String.Format("Length: {0:00.00} sec.", this.encoder.duration / 1000.0); }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        Sequence seq_input;
        int sz_input;

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == DialogResult.OK)
            {
                isRecorded = false;
                path = open.FileName;
                //Open the selected audio file
                signal = AudioOperations.OpenAudioFile(path);

                // remove sielnce
                 AudioOperations.RemoveSilence(signal);

                Sequence seq = AudioOperations.ExtractFeatures(signal);

                seq_input = seq; /////*** seq_input initialize by open audio/////////
                sz_input=seq_input.Frames.Length;

                textBox2.Text = "";
                textBox3.Text = "";
                updateButtons();
                

            }
        }

        private void Stop()
        {
            if (this.encoder != null) { this.encoder.Stop(); }
            if (this.decoder != null) { this.decoder.Stop(); }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string name_seq = textBox1.Text;
            textBox2.Text = "";
            textBox3.Text = "";
            if (name_seq == "")
            {

                MessageBox.Show("Enter Your Name  :( -_-  ", "Name Don't Enrollment O.o -_-   ");


            }

            else
            {
                ////////////// ** save input sequence of audio **/////////
                FileStream fs = new FileStream("sequences.txt", FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);

                int sz_seq = seq_input.Frames.Length;
                sw.Write(sz_seq);
                sw.Write('%');
                sw.WriteLine(name_seq);
                for (int i = 0; i < sz_seq; i++)
                    for (int j = 0; j < 13; j++)
                    {
                        sw.WriteLine(seq_input.Frames[i].Features[j]);
                    }

                sw.Close();
                fs.Close();
                textBox1.Text = "";
                ////////////////// ** end save  *****/////////////////////

            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        double[,] seq_out;
      
            
        private void btnIdentify_Click(object sender, EventArgs e)
        {
            textBox2.Text = "";
            double match = double.PositiveInfinity;
            string name = "";

            

            ///////////////////***** open file to find the name of audio *****///////////////

            FileStream fs = new FileStream("sequences.txt", FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            Stopwatch t = new Stopwatch();

            while (sr.Peek() != -1)
            {


                string line = sr.ReadLine();
                string[] fields = line.Split('%');
                int sz_out = int.Parse(fields[0]);
                string name_out = fields[1];
                 seq_out = new double[sz_out, 13];

                ////////**read feature from file ****////////
                for (int i = 0; i < sz_out; i++)
                    for (int j = 0; j < 13; j++)
                    {
                        seq_out[i, j] = double.Parse(sr.ReadLine());
                    }
                ////////** save values in seq_out and find cost **////

                /////////////* DTW**/////////////////////////////////////////
                ////////////**DTW***////////////////////////////////////////

                t.Start();

                double[] D = new double[sz_input + 1];
                for (int i = 0; i <= sz_input; i++)
                {
                    D[i] = double.PositiveInfinity;
                }
              
                for (int i = 1; i <= sz_out; i++)
                {
                        if (i - 1 == 0)
                        {
                            D[0] = 0;
                        }
                        else
                            D[0] = double.PositiveInfinity;
                    
                        int a2 = 0,a3=1;
                        double A1 = D[0], A2 = D[a2], A3 = D[a3];
              
                 
                    for (int j = 1; j <= sz_input; j++)
                    {

                        double cost = 0;
                        for (int f = 0; f < 13; f++)
                        {
                            double ss=(seq_input.Frames[j - 1].Features[f] - seq_out[i - 1, f]);
                            cost += ss*ss;
                        }
                        cost = Math.Sqrt(cost);
                      double c = cost + Math.Min(A1, Math.Min(A2,A3));


                      if (a3 < sz_input)
                      {
                          a3++;
                          a2++;
                          A1 = A2;
                          A2 = D[a2];
                          A3 = D[a3];
              
                      }
                            D[j] = c;
 
                       
                    }

                }


                if (match > D[sz_input])
                {
                    match = D[sz_input];
                    name = name_out;

                }
             
            }
            t.Stop();
            textBox2.Text = t.Elapsed.TotalMilliseconds.ToString();
            //////////////////////////*****end file****///////////////////////////////////////////
            sr.Close();
            fs.Close();
          
            //textBox2.Text = match.ToString();

            MessageBox.Show("[ "+name+" ] .. ^_^ :D   ", "name is : ");
            Console.WriteLine(match);

            

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        private void button1_Click(object sender, EventArgs e)
        {
            textBox3.Text = "";
            double match = double.PositiveInfinity;
            string name = "";
            ///////////////////***** open file to find the name of audio *****///////////////

            FileStream fs = new FileStream("sequences.txt", FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            Stopwatch t1 = new Stopwatch();
            while (sr.Peek() != -1)
            {

                string line = sr.ReadLine();
                string[] fields = line.Split('%');
                int sz_out = int.Parse(fields[0]);
                string name_out = fields[1];
                seq_out = new double[sz_out, 13];

                ////////**read feature from file ****////////
                for (int i = 0; i < sz_out; i++)
                    for (int j = 0; j < 13; j++)
                    {
                        seq_out[i, j] = double.Parse(sr.ReadLine());
                    }
                ////////** save values in seq_out and find cost **/////*

                /////////////* DTW**/////////////////////////////////////////
                ////////////**DTW***////////////////////////////////////////
                t1.Start();

               
               
               int w = Math.Abs(sz_input - sz_out); // adapt window size (*)

                double[] D = new double[sz_input + 1];
                for (int i = 0; i <= sz_input; i++)
                {
                    D[i] = double.PositiveInfinity;
                }


                for (int i = 1; i <= sz_out; i++)
                {
                    /////////////
                    {
                        if (i - 1 == 0)
                        {
                            D[0] = 0;
                        }
                        else
                            D[0] = double.PositiveInfinity;

                        int a2 = 0, a3 = 1;
                        double A1 = D[0], A2 = D[a2], A3 = D[a3];

                        ///////////////////



                        for (int j = Math.Max(1, i - w); j <= Math.Min(sz_input, i + w); j++)
                        {
                            ////////////////////
                            double cost = 0;
                            for (int f = 0; f < 13; f++)
                            {
                                double ss = (seq_input.Frames[j - 1].Features[f] - seq_out[i - 1, f]);
                                cost += ss*ss;

                            }
                            cost = Math.Sqrt(cost);
                             double c=0;
                            if (A1 == 0.0)
                            {
                                c = cost + Math.Min(A1,A2); 
                            }
                            else
                             c = cost + Math.Min(A1, Math.Min(A2, A3));
                            //////////////////////
                            if (a3 < sz_input)
                            {
                                a3++;
                                a2++;
                                A1 = A2;
                                A2 = D[a2];
                                A3 = D[a3];
                            }
                            D[j] = c;
                        }
                    }
                
                    if (match > D[sz_input])
                    {
                        match = D[sz_input];
                        name = name_out;

                    }
                }
           
        
        }
            t1.Stop();
            //////////////////////////*****end file****///////////////////////////////////////////
            sr.Close();
            fs.Close();
            t1.Stop();
            textBox3.Text = t1.Elapsed.TotalMilliseconds.ToString();
            
            MessageBox.Show("[ " + name + " ] .. ^_^ :D   ", "name is : ");


        }

        private void timer2_Tick(object sender, EventArgs e)
        {
          
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

            List<User> s = TestcaseLoader.LoadTestcase1Training(@"E:\programing education\ain shams stage 3\just year\algorithm\labs\PROJECT RELEASE (1)\Startup Codes\Speaker Identification Startup Code\Speaker Identification Startup Code\Complete SpeakerID Dataset\TrainingList.txt");
         int sz = s.Count;
         for (int i = 0; i < sz; i++)
         { 
         string a= s[i].UserName;
         int si = s[i].UserTemplates.Count;
             
             for(int j=0;i<si;j++)
             {
                 Sequence seqq=AudioOperations.ExtractFeatures(s[i].UserTemplates[j]);
                 seq_input = seqq;

                 ////////////// ** save input sequence of audio **/////////
                 FileStream fs = new FileStream("sequence.txt", FileMode.Append);
                 StreamWriter sw = new StreamWriter(fs);

                 int sz_seq = seqq.Frames.Length;
                 sw.Write(sz_seq);
                 sw.Write('%');
                 sw.WriteLine(a);
                 for (int b = 0; b < sz_seq; b++)
                     for (int r = 0; r < 13; r++)
                     {
                         sw.WriteLine(seq_input.Frames[b].Features[r]);
                     }

                 sw.Close();
                 fs.Close();
                 ////////////////// ** end save  *****/////////////////////

             }
         
        }
      
        }
        List<string> list_us;
        private void button3_Click(object sender, EventArgs e)
        {

            List<User> s = TestcaseLoader.LoadTestcase1Training(@"E:\programing education\ain shams stage 3\just year\algorithm\labs\PROJECT RELEASE (1)\Startup Codes\Speaker Identification Startup Code\Speaker Identification Startup Code\Complete SpeakerID Dataset\TestingList.txt");
            int sz = s.Count;

            //string[] list_name =new string [s];
            for (int i = 0; i < sz; i++)
            {
                string a = s[i].UserName;
                int si = s[i].UserTemplates.Count;

                for (int j = 0; i < si; j++)
                {
                    Sequence seqq = AudioOperations.ExtractFeatures(s[i].UserTemplates[j]);

                    sz_input = seqq.Frames.Length;
                    seq_input = seqq;

                    FileStream fs = new FileStream("sequence.txt", FileMode.Open);
                    StreamReader sr = new StreamReader(fs);
                    double match = double.PositiveInfinity;
                    string name = "";

                    


                    while (sr.Peek() != -1)
                    {

                        string line = sr.ReadLine();
                        string[] fields = line.Split('%');
                        int sz_out = int.Parse(fields[0]);
                        string name_out = fields[1];
                        seq_out = new double[sz_out, 13];

                        ////////**read feature from file ****////////
                        for (int u = 0; u < sz_out; u++)
                            for (int y = 0; y < 13; y++)
                            {
                                seq_out[u, y] = double.Parse(sr.ReadLine());
                            }
                        ////////** save values in seq_out and find cost **/////*

                        /////////////* DTW**/////////////////////////////////////////
                        ////////////**DTW***////////////////////////////////////////



                        int w = Math.Abs(sz_input - sz_out); // adapt window size (*)

                        double[] D = new double[sz_input + 1];
                        for (int u = 0; u <= sz_input; u++)
                        {
                            D[u] = double.PositiveInfinity;
                        }


                        for (int u = 1; u <= sz_out; u++)
                        {
                            /////////////
                            {
                                if (u - 1 == 0)
                                {
                                    D[0] = 0;
                                }
                                else
                                    D[0] = double.PositiveInfinity;

                                int a2 = 0, a3 = 1;
                                double A1 = D[0], A2 = D[a2], A3 = D[a3];

                                ///////////////////



                                for (int y = Math.Max(1, u - w); y <= Math.Min(sz_input, u + w); y++)
                                {
                                    ////////////////////
                                    double cost = 0;
                                    for (int f = 0; f < 13; f++)
                                    {
                                        cost += Math.Pow(seq_input.Frames[y - 1].Features[f] - seq_out[u - 1, f], 2);

                                    }
                                    cost = Math.Sqrt(cost);
                                    double c = 0;
                                    if (A1 == 0.0)
                                    {
                                        c = cost + Math.Min(A1, A2);
                                    }
                                    else
                                        c = cost + Math.Min(A1, Math.Min(A2, A3));
                                    //////////////////////
                                    if (a3 < sz_input)
                                    {
                                        a3++;
                                        a2++;
                                        A1 = A2;
                                        A2 = D[a2];
                                        A3 = D[a3];
                                    }
                                    D[y] = c;
                                }
                            }

                            if (match > D[sz_input])
                            {
                                match = D[sz_input];
                                name = name_out;

                                            

                            }
                        }


                    }

                    //////////////////////////*****end file****///////////////////////////////////////////
                    sr.Close();
                    fs.Close();
                   

                    //////// set list of string 

                    list_us.Insert(i * j, name); 
                    
                   ////////

                }
                

            }

            for (int i = 0; i < list_us.Count; i++)
                Console.Write(list_us[i]);
            Console.WriteLine();
        }
       

      





    }
}

