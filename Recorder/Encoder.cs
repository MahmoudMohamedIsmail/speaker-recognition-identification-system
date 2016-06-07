using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.DirectSound;
using System.IO;
using Accord.Audio;
using Accord.Audio.Formats;

namespace Recorder.Recorder
{
    class Encoder : BaseRecorder
    {

        private WaveEncoder encoder;

        private MemoryStream _stream;
        public MemoryStream stream
        {
            get { return _stream; }
            set { _stream = value; }
        }

        private IAudioSource _source;
        public IAudioSource source
        {
            get { return _source; }
            set { _source = value; }
        }

        private float[] _current;
        public float[] current
        {
            get { return _current; }
            set { _current = value; }
        }

        private int _duration;
        public int duration
        {
            get { return _duration; }
            set { _duration = value; }
        }

        public override bool IsRunning()
        {
            return this.source.IsRunning;
        }

        public Encoder(EventHandler<NewFrameEventArgs> NewFrame, EventHandler<AudioSourceErrorEventArgs> AudioSourceError)
        {
            AudioDeviceCollection audioDevices = new AudioDeviceCollection(AudioDeviceCategory.Capture);
            AudioDeviceInfo INfo = null;
            foreach (var item in audioDevices)
            {
                INfo = item;
            }
            // Create capture device
            this.source = new AudioCaptureDevice(INfo.Guid)
            {
                // Listen on 22050 Hz
                DesiredFrameSize = FRAME_SIZE,
                SampleRate = SAMPLE_RATE,

                // We will be reading 16-bit PCM
                Format = SampleFormat.Format16Bit
            };


            // Wire up some events
            source.NewFrame += NewFrame;
            source.AudioSourceError += AudioSourceError;

            // Create buffer for wavechart control
            this.current = new float[source.DesiredFrameSize];

            // Create stream to store file
            this.stream = new MemoryStream();
            this.encoder = new WaveEncoder(stream);
        }

        public override void Start()
        {
            this.duration = 0;
            this.source.Start();
        }

        public override void Stop()
        {
            if (this.source != null)
            {
                // If we were recording
                this.source.SignalToStop();
                this.source.WaitForStop();
                Array.Clear(this.current, 0, current.Length);
            }
        }

        public void addNewFrame(Signal signal)
        {
            // Save current frame
            signal.CopyTo(current);

            // Save to memory
            this.encoder.Encode(signal);

            // Update counters
            this.duration += signal.Duration;
            this.samples += signal.Samples;
            this.frames += signal.Length;
        }

        public void Save(Stream fileStream)
        {
            if (this.stream != null)
            {
                this.stream.WriteTo(fileStream);
                fileStream.Close();
            }
        }
    }
}
