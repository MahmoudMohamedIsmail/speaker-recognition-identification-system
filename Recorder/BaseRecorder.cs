using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Recorder.Recorder
{
    abstract class BaseRecorder
    {
        public static int FRAME_SIZE = 4096;
        public static int SAMPLE_RATE = 22050;

        private int _frames;
        public int frames
        {
            get { return _frames; }
            set { _frames = value; }
        }

        private int _samples;
        public int samples
        {
            get { return _samples; }
            set { _samples = value; }
        }

        public abstract bool IsRunning();

        public abstract void Start();

        public abstract void Stop();
    }
}
