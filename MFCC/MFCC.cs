using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Recorder.MFCC
{
    public class AudioSignal
    {
        public double[] data;
        public int sampleRate;
        public double signalLengthInMilliSec;
    }
    public class MFCCFrame
    {
        public double[] Features = new double[13];
    }
    public class SignalFrame
    {
        public double[] Data;
    }
    public class Sequence
    {
        public MFCCFrame[] Frames { get; set; }
    }
    static class MFCC
    {
        private static MATLABAudioFunctionsNative.AudioProcessing MATLABAudioObject = new MATLABAudioFunctionsNative.AudioProcessing();
        public static Sequence ExtractFeatures(double[] pSignal, int samplingRate)
        {
            Sequence sequence = new Sequence();
            double[,] mfcc= MATLABMFCCfunction(pSignal, samplingRate);
            int numOfFrames = mfcc.GetLength(1);
            int numOfCoefficients = mfcc.GetLength(0);
            Debug.Assert(numOfCoefficients == 13);
            sequence.Frames = new MFCCFrame[numOfFrames];
            for (int i = 0; i < numOfFrames; i++)
			{
                sequence.Frames[i] = new MFCCFrame();
                for (int j = 0; j < numOfCoefficients; j++)
                {
                    sequence.Frames[i].Features[j] = mfcc[j, i];
                }
			}
            return sequence;
        }
        public static SignalFrame[] DivideSignalToFrames(double[] pSignal,int pSamplingRate, double pSignalLengthInMilliSeconds, double pFrameLengthinMilliSeconds)
        {
            int numberOfFrames = (int)Math.Ceiling(pSignalLengthInMilliSeconds / pFrameLengthinMilliSeconds);
            //START FIX1
            int frameSize = (int)(pSamplingRate * pFrameLengthinMilliSeconds / 1000.0);
            //END FIX1
            //Start FIX2
            int remainingDataSize = pSignal.Length - frameSize * (numberOfFrames - 1);
            int compensation = (int)(remainingDataSize/frameSize);
            numberOfFrames += compensation;
            remainingDataSize -= compensation * frameSize;
            //End FIX2
            //initialize frames.
            SignalFrame[] frames = new SignalFrame[numberOfFrames];
            for (int i = 0; i < numberOfFrames; i++)
            {
                //START: FIX1
                frames[i] = new SignalFrame();
                //END: FIX1
                frames[i].Data = new double[frameSize];
            }
            //copy data from signal to frames.
            int signalIndex = 0;
            //START FIX1
            for (int i = 0; i < numberOfFrames-1; i++)
            {
                Array.Copy(pSignal, signalIndex, frames[i].Data, 0, frameSize);        
                signalIndex += frameSize;
            }
            Array.Copy(pSignal, signalIndex, frames[numberOfFrames - 1].Data, 0, remainingDataSize);
            //END FIX1 
            return frames;
        }
        //Voice Activation Detection (VAD)
        public static SignalFrame[] RemoveSilentSegments(SignalFrame[] pFrames)
        {
            double[] framesWeights = new double[pFrames.Length];
            int frameIndex = 0;
            foreach (SignalFrame frame in pFrames)
            {
                double squareMean = 0;
                double avgZeroCrossing = 0;
                for (int i = 0; i < frame.Data.Length-1; i++)
                {
                    //FIX1
                    squareMean += frame.Data[i] * frame.Data[i];
                   // avgZeroCrossing += Math.Abs(Math.Sign(frame.Data[i+1]) - Math.Sign(frame.Data[i])) / 2;
                    avgZeroCrossing += Math.Abs(Math.Abs(frame.Data[i + 1]) - Math.Abs(frame.Data[i]))/2.0;
                }
                squareMean /= frame.Data.Length;
                avgZeroCrossing /= frame.Data.Length;
                framesWeights[frameIndex++] = squareMean*(1-avgZeroCrossing)*1000;
            }
            double avgWeights = mean(framesWeights);
            double stdWeights = std(framesWeights);
            double gamma = 0.2*Math.Pow(stdWeights,-0.8);
            double activationThreshold = avgWeights + gamma*stdWeights;

            //threshold weights.
            threshold(framesWeights,activationThreshold);
            //smooth weights to remove short silences.
            smooth(framesWeights);
            //set anything more than 0 with 1.
            threshold(framesWeights,0);
            int numberOfActiveFrames = (int)framesWeights.Sum();
            SignalFrame[] activeFrames = new SignalFrame[numberOfActiveFrames];
            int activeFramesIndex =0;
            for (int i = 0; i < pFrames.Length; i++)
			{
                if(framesWeights[i] == 1)
                {
                    activeFrames[activeFramesIndex] = new SignalFrame();
                    activeFrames[activeFramesIndex].Data = new double[pFrames[i].Data.Length];
                    pFrames[i].Data.CopyTo(activeFrames[activeFramesIndex].Data,0);
                    activeFramesIndex++;
                }
			}
            return activeFrames;
        }

        public static double[] RemoveSilence(double[] pSignal, int pSamplingRate, double pSignalLengthInMilliSeconds, double pFrameLengthinMilliSeconds)
        {
            SignalFrame[] originalFrames = DivideSignalToFrames(pSignal,pSamplingRate, pSignalLengthInMilliSeconds, pFrameLengthinMilliSeconds);
            SignalFrame[] filteredFrames = RemoveSilentSegments(originalFrames);
            int signalLength = 0;
            foreach (SignalFrame frame in filteredFrames)
            {
                signalLength += frame.Data.Length;
            }
            double[] filteredSignal = new double[signalLength];
            int index = 0;
            foreach (SignalFrame frame in filteredFrames)
            {
                frame.Data.CopyTo(filteredSignal, index);
                index += frame.Data.Length;
            }
            return filteredSignal;
        }

        #region Private Methods.
        private static double mean(double[] arr)
        {
            return arr.Sum() / arr.Length;
        }
        private static double std (double[] arr)
       {
           double avg = mean(arr);
           double stdDev = 0;
           for (int i = 0; i < arr.Length; i++)
           {
               stdDev += (arr[i] - avg) * (arr[i] - avg);
           }
           stdDev /= arr.Length;
           stdDev = Math.Sqrt(stdDev);
           return stdDev;
       }
        
        //smooth a signal with an averging filter with window size = 5;
        private static void smooth (double[] inputArr) 
        {
            double[] arr = new double[inputArr.Length];
            inputArr.CopyTo(arr, 0);

            inputArr[1] = (arr[0] + arr[1] + arr[2]) / 3.0;
            for (int i = 2; i < arr.Length-2; i++)
            {
                inputArr[i] = (arr[i - 2] + arr[i - 1] + arr[i] + arr[i + 1] + arr[i + 2]) / 5.0;
            }
            inputArr[arr.Length - 2] = (arr[arr.Length - 3] + arr[arr.Length - 2] + arr[arr.Length - 1]) / 3.0;
        }
        private static void threshold  (double[] arr,double thr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] > thr)
                {
                    arr[i] = 1;
                }
                else
                {
                    arr[i] = 0;
                }
            }
        }

        //private static MFCCFrame CalculateMFCC(SignalFrame signal,int samplingRate)
        //{           
        //    MFCCFrame res = new MFCCFrame();
        //    res.Features = MATLABMFCCfunction(signal.Data,samplingRate);
        //    return res;
        //}

        public static double[,] MATLABMFCCfunction(double[] signal, int samplingRate)
        {
            double[,] mfcc = (double[,])MATLABAudioObject.MATLABMFCCfunction(signal, (double)samplingRate);
            return mfcc;
        }
        #endregion

    }
}
