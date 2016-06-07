using Recorder.MFCC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Recorder
{
    struct User
    {
        public string UserName;
        public List<AudioSignal> UserTemplates;
    }
    static class TestcaseLoader
    {
        //11 users. each user has ~100 small training samples (with silent parts removed).

        static public List<User> LoadTestcase1Training(string trainingListFileName)
        {
            return LoadDataset(trainingListFileName);
        }

        
        static public List<User> LoadTestcase1Testing(string testingListFileName)
        {
            return LoadDataset(testingListFileName);
        }

        //WARNING: this function in particular is not tested!!!!!
        static public double CheckTestcaseAccuracy(List<User> testCase, List<string> testcaseResult)
        {
            int misclassifiedSamples = 0;
            int resultIndex = 0;
            for (int i = 0; i < testCase.Count; i++)
            {
                for (int j = 0; j < testCase[i].UserTemplates.Count; j++)
			    {
                    if (testCase[i].UserName != testcaseResult[resultIndex])
                        misclassifiedSamples++;
                    
                    resultIndex++;
			    }
            }

            return (double) misclassifiedSamples / testcaseResult.Count;
        }

        //11 users. each user has ~10 medium sized training samples (with silent parts removed).
        static public List<User> LoadTestcase2Training(string trainingListFileName)
        {
            var originalDataset = LoadDataset(trainingListFileName);

            //shrinkage factor should be larger than 1.
            return ConcatenateSamples(originalDataset, 10);
        }

        static public List<User> LoadTestcase2Testing(string testingListFileName)
        {
            var originalDataset = LoadDataset(testingListFileName);

            //shrinkage factor should be larger than 1.
            return ConcatenateSamples(originalDataset, 10);
        }

        //11 users. each user has ~2 large sized training samples (with silent parts removed).
        static public List<User> LoadTestcase3Training(string trainingListFileName)
        {
            var originalDataset = LoadDataset(trainingListFileName);

            //shrinkage factor should be larger than 1.
            return ConcatenateSamples(originalDataset, 40);
        }

        static public List<User> LoadTestcase3Testing(string testingListFileName)
        {
            var originalDataset = LoadDataset(testingListFileName);

            //shrinkage factor should be larger than 1.
            return ConcatenateSamples(originalDataset, 40);
        }

        static private List<User> LoadDataset(string datasetFileName)
        {
            //Get The dataset folder path.
            var splittedPath = datasetFileName.Split('\\');
            string folderPath = "";
            for (int i = 0; i < splittedPath.Length - 1; i++)
            {
                folderPath += splittedPath[i] + '\\';
            }
            folderPath += "audiofiles\\";


            //read the training samples files names
            Dictionary<string, User> users = new Dictionary<string, User>();
            StreamReader reader = new StreamReader(datasetFileName);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string userName = line.Split('/')[0];
                string fileName = line.Split('/')[1] + ".wav";
                //check if user already exists, if not add an entry in the dictionary.
                if (users.ContainsKey(userName) == false)
                {
                    User user = new User();
                    user.UserTemplates = new List<AudioSignal>();
                    user.UserName = userName;
                    users.Add(userName, user);
                }
                AudioSignal audio;
                string fullFileName = folderPath + userName + '\\' + fileName;
                try
                {
                    audio = openNISTWav(fullFileName);
                }
                catch (Exception)
                {
                    audio = AudioOperations.OpenAudioFile(folderPath + userName + '\\' + fileName);
                }
                audio = AudioOperations.RemoveSilence(audio);
                users[userName].UserTemplates.Add(audio);
            }
            reader.Close();

            //move the users to a list for convenience reasons only.
            List<User> usersList = new List<User>();
            foreach (var user in users)
            {
                usersList.Add(user.Value);
            }

            return usersList;
        }

        // convert two bytes to one double in the range -1 to 1
        static private double bytesToDouble(byte firstByte, byte secondByte)
        {
            // convert two bytes to one short (little endian)
            short s = (short)((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            return s / 32768.0;
        }

        static private AudioSignal openNISTWav(string filename)
        {
            int sample_rate = 0, sample_count =0, sample_n_bytes = 0;
            StreamReader reader = new StreamReader(filename);
              
              while(true)
              {
                  string line = reader.ReadLine();
                  var splittedLine = line.Split(' ');
                  if (splittedLine[0] == "sample_count")
                  {
                      sample_count = int.Parse(splittedLine[2]);
                  }
                  else if (splittedLine[0] == "sample_rate")
                  {
                      sample_rate = int.Parse(splittedLine[2]);
                  }
                  else if (splittedLine[0] == "sample_n_bytes")
                  {
                      sample_n_bytes = int.Parse(splittedLine[2]);
                  }
                  else if (splittedLine[0] == "end_head")
                      break;
              }
            reader.Close();
            byte[] wav = File.ReadAllBytes(filename);

            //header offset.
            int pos = 1024;

            int samples = (wav.Length - pos) / sample_n_bytes;     // 2 bytes per sample (16 bit sound mono)
            int altsamples = sample_count / sample_n_bytes;
            double[] data = new double[sample_count];

            // Write to double array:
            int i = 0;
            while (pos < wav.Length)
            {
                data[i] = bytesToDouble(wav[pos], wav[pos + 1]);
                pos += 2;
                i++;
            }

            AudioSignal signal = new AudioSignal();
            signal.sampleRate = sample_rate;
            signal.data = data;
            signal.signalLengthInMilliSec = (double) 1000.0 * sample_count / sample_rate ;
            return signal;
        }

        static private List<User> ConcatenateSamples(List<User> dataset, int shrinkagefactor)
        {
            List<User> newDataset = new List<User>();
            foreach (User user in dataset)
            {
                
                int numberOfSequences = user.UserTemplates.Count;
                //NOTE: i didn't handle the case if the number of sequences is not divisible by the shrinkage factor :)
                int newNumberOfSequences = numberOfSequences / shrinkagefactor;
                User concUser = new User();
                concUser.UserName = user.UserName;
                concUser.UserTemplates = new List<AudioSignal>(newNumberOfSequences);
                int startIndex = 0;
                for (int i = 0; i < newNumberOfSequences; i++)
                {
                    int currentConcSeqLength = 0;
                    double currentConcSeqDuration = 0;
                    for (int j = startIndex; j < startIndex + shrinkagefactor; j++)
                    {
                        currentConcSeqLength += user.UserTemplates[j].data.Length;
                        currentConcSeqDuration += user.UserTemplates[j].signalLengthInMilliSec;
                    }
                    concUser.UserTemplates.Add(new AudioSignal());
                    concUser.UserTemplates[i].sampleRate = user.UserTemplates[0].sampleRate;
                    concUser.UserTemplates[i].signalLengthInMilliSec = currentConcSeqDuration;
                    concUser.UserTemplates[i].data = new double[currentConcSeqLength];
                    int concIndex = 0;
                    for (int j = startIndex; j < startIndex + shrinkagefactor; j++)
                    {
                        user.UserTemplates[j].data.CopyTo(concUser.UserTemplates[i].data, concIndex);
                        concIndex += user.UserTemplates[j].data.Length;
                    }
                    
                    startIndex += shrinkagefactor;
                }

                newDataset.Add(concUser);
            }
            return newDataset;
        }
    }
}
