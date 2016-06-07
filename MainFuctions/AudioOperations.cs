using Accord.Audio;
using Accord.Audio.Formats;
using Recorder.MFCC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Recorder
{
    public static class AudioOperations
    {
        /// <summary>
        /// Open the given audio file and return an "AudioSignal" with the following info:
        ///     1. data[]: array of audio samples
        ///     2. sample rate
        ///     3. signal length in milli sec
        /// </summary>
        /// <param name="filePath">audio file path</param>
        /// <returns>AudioSignal containing its data, sample rate and length in ms</returns>
        public static AudioSignal OpenAudioFile(string filePath)
        {
            WaveDecoder waveDecoder = new WaveDecoder(filePath);

            AudioSignal signal = new AudioSignal();

            signal.sampleRate = waveDecoder.SampleRate;
            signal.signalLengthInMilliSec = waveDecoder.Duration;
            Signal tempSignal = waveDecoder.Decode(waveDecoder.Frames);
            signal.data = new double[waveDecoder.Frames];
            tempSignal.CopyTo(signal.data);
            return signal;
        }

        /// <summary>
        /// Remove the silent segment from the given audio signal
        /// </summary>
        /// <param name="signal">original signal</param>
        /// <returns>signal after removing the silent segment(s) from it</returns>
        public static AudioSignal RemoveSilence(AudioSignal signal)
        {
            AudioSignal filteredSignal = new AudioSignal();
            filteredSignal.sampleRate = signal.sampleRate;
            filteredSignal.signalLengthInMilliSec = signal.signalLengthInMilliSec;
            filteredSignal.data = MFCC.MFCC.RemoveSilence(signal.data, signal.sampleRate, signal.signalLengthInMilliSec, 20);
            return filteredSignal;
        }

        /// <summary>
        /// Extract MFCC coefficients of the sequence of frames for the given AudioSignal. 
        /// Each frame (feature) consists of 13 coefficients
        /// </summary>
        /// <param name="signal">Audio signal to extract its features</param>
        /// <returns>Sequence of features (13 x NumOfFrames)</returns>
        public static Sequence ExtractFeatures(AudioSignal signal)
        {
            return MFCC.MFCC.ExtractFeatures(signal.data, signal.sampleRate);
        }
    }
}
