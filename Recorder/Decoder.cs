using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Audio;
using Accord.Audio.Formats;
using System.IO;
using Accord.DirectSound;

namespace Recorder.Recorder
{
    class Decoder : BaseRecorder
    {

        private WaveDecoder decoder;

        private IAudioOutput output;

        public double[] wholeSignal{ get; set;}

        public int Position { get; set; }

        public override bool IsRunning()
        {
            return this.output.IsRunning;
        }

        public Decoder(string path, IntPtr controlHandle, EventHandler<AudioOutputErrorEventArgs> AudioOutputError, EventHandler<PlayFrameEventArgs> FramePlayingStarted, EventHandler<NewFrameRequestedEventArgs> NewFrameRequested, EventHandler Stopped)
        {
            this.decoder = new WaveDecoder(path);
            init(controlHandle, AudioOutputError, FramePlayingStarted, NewFrameRequested, Stopped);
        }

        public Decoder(Stream stream, IntPtr controlHandle, EventHandler<AudioOutputErrorEventArgs> AudioOutputError, EventHandler<PlayFrameEventArgs> FramePlayingStarted, EventHandler<NewFrameRequestedEventArgs> NewFrameRequested, EventHandler Stopped)
        {
            this.decoder = new WaveDecoder(stream);
            init(controlHandle, AudioOutputError, FramePlayingStarted, NewFrameRequested, Stopped);
        }

        private void init(IntPtr controlHandle, EventHandler<AudioOutputErrorEventArgs> AudioOutputError, EventHandler<PlayFrameEventArgs> FramePlayingStarted, EventHandler<NewFrameRequestedEventArgs> NewFrameRequested, EventHandler Stopped)
        {
            AudioDeviceCollection audioDevices = new AudioDeviceCollection(AudioDeviceCategory.Output);
            AudioDeviceInfo INfo = null;
            foreach (var item in audioDevices)
            {
                INfo = item;
            }
            // Here we can create the output audio device that will be playing the recording
            this.output = new AudioOutputDevice(INfo.Guid, controlHandle, decoder.SampleRate, decoder.Channels);

            // Wire up some events
            output.FramePlayingStarted += FramePlayingStarted;
            output.NewFrameRequested += NewFrameRequested;
            output.Stopped += Stopped;
            output.AudioOutputError += AudioOutputError;

            this.frames = this.decoder.Frames;
            this.samples = this.decoder.Samples;

            this.wholeSignal = getWholeSignal();
        }

        public override void Start()
        {
            if (this.output != null)
            {
                this.output.Play();
            }
        }

        public override void Stop()
        {
            if (this.output != null)
            {
                // If we were playing
                this.output.SignalToStop();
                this.output.WaitForStop();
            }
        }

        private double[] getWholeSignal()
        {
            Signal tempSignal = this.decoder.Decode(this.frames);
            double[] wholeSignal = new double[this.frames];
            tempSignal.CopyTo(wholeSignal);
            return wholeSignal;
        }

        public void FillNewFrame(NewFrameRequestedEventArgs frameRequestArgs) 
        {
              // This is the next frame index
            frameRequestArgs.FrameIndex = this.decoder.Position;

            // Attempt to decode the requested number of frames from the stream
            Signal signal = this.decoder.Decode(frameRequestArgs.Frames);

            if (signal == null)
            {
                // We could not get the requested number of frames. When
                // this happens, this is an indication we need to stop.
                frameRequestArgs.Stop = true;
                return;
            }

            // Inform the number of frames
            // actually read from source
            frameRequestArgs.Frames = signal.Length;

            // Copy the signal to the buffer
            signal.CopyTo(frameRequestArgs.Buffer);
        }

        public void Seek(int value)
        {
            this.decoder.Seek(value);
        }


        public Signal Decode(int frames)
        {
           return this.decoder.Decode(frames);
        }
    }
}
