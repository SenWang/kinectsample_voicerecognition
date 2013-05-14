namespace Speech
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.Kinect;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.Speech.Synthesis;

    public class Program
    {
        static KinectSensor sensor;
        public static void Main(string[] args)
        {
            sensor = KinectSensor.KinectSensors[0];
            if (sensor == null)
            {
                //�S��Sensor,�������}
                return;
            }

            sensor.Start();

            //�]�w����
            KinectAudioSource source = sensor.AudioSource;
            source.EchoCancellationMode = EchoCancellationMode.None; 
            source.AutomaticGainControlEnabled = false; 

            RecognizerInfo ri = GetKinectRecognizer();

            if (ri == null)
            {
                Console.WriteLine("�䤣�줺�ت��n�����Ѿ�");
                return;
            }

            Console.WriteLine("Using: {0}", ri.Name);

            int wait = 4;
            while (wait > 0)
            {
                Console.Write(" {0} ���˸m�}�l�i��y������\r", wait--);
                Thread.Sleep(1000);
            }
            
            using (var sre = new SpeechRecognitionEngine(ri.Id))
            {                
                var gb = new GrammarBuilder { Culture = ri.Culture };

                gb.Append(new Choices("ni", "ni"));
                gb.Append(new Choices("how", "ji"));
                gb.Append(new Choices("ma", "suei"));
                    
                var g = new Grammar(gb);                    

                sre.LoadGrammar(g);
                sre.SpeechRecognized += SreSpeechRecognized;
                sre.SpeechHypothesized += SreSpeechHypothesized;
                sre.SpeechRecognitionRejected += SreSpeechRecognitionRejected;

                using (Stream s = source.Start())
                {
                    sre.SetInputToAudioStream(
                        s, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

                    Console.WriteLine("�л� �A�n��? ��  �A�X��? . ���U ENTER �������");

                    sre.RecognizeAsync(RecognizeMode.Multiple);
                    Console.ReadLine();
                    Console.WriteLine("������� ...");
                    sre.RecognizeAsyncStop();                       
                }
            }
            sensor.Stop();
        }

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        private static void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Console.WriteLine("\n�L�k����");
            //if (e.Result != null)
            //{
            //    SaveRecordedAudio(e.Result.Audio);
            //}
        }

        private static void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Console.Write("\r�i��O: \t{0}", e.Result.Text);
            //if (e.Result != null)
            //{
            //    SaveRecordedAudio(e.Result.Audio);
            //}
        }

        private static void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence >= 0.5)
            {
                Console.WriteLine("\n���ӬO: \t{0}\tConfidence:\t{1}", e.Result.Text, e.Result.Confidence);
                VoiceResponse(e.Result.Text);
            }
            else
            {
                Console.WriteLine("\n���ӬO,���O���ӽT�w: \t{0}", e.Result.Confidence);
            }
        }


        static void VoiceResponse(string input)
        {
            string response = "i don\'t understand";
            if (input == "ni how ma")
                response = "o han how";

            SpeechSynthesizer synthesizer = new SpeechSynthesizer();

            foreach(var v in synthesizer.GetInstalledVoices())
                Console.WriteLine(v.VoiceInfo.Name + "��:" + response);

            synthesizer.Rate = 0;
            synthesizer.Volume = 100;
            synthesizer.Speak(response);
        }

        //�ϥΩ��U�{���X��,���ݭn�[�JSystem.Speech.dll�ե�PSystem.Speech.Synthesis�R�W�Ŷ��Y�i�����ϥ�
        //static void VoiceResponse(string input)
        //{
        //    string response = "i don\'t understand";
        //    if (input == "ni how ma")
        //        response = "o han how";

        //    Type type = Type.GetTypeFromProgID("SAPI.SpVoice");
        //    dynamic synthesizer = Activator.CreateInstance(type);

        //    synthesizer.Rate = 0;
        //    synthesizer.Volume = 100;
        //    synthesizer.Speak(response);
        //}

        static int count = 0; 
        static void SaveRecordedAudio(RecognizedAudio audio)
        {
            if (audio == null)
                return;

            string filename = "save_" + count + ".wav" ;
            while (File.Exists(filename))
            {
                count++;
                filename = "save_" + count + ".wav";
            }

            Console.WriteLine("�g�J�ɮ�: " +  filename);
            using (var file = new FileStream(filename, FileMode.CreateNew))
            {
                audio.WriteToWaveStream(file);
            }
        }
    }
}
