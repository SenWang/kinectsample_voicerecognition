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
                //沒有Sensor,直接離開
                return;
            }

            sensor.Start();

            //設定音源
            KinectAudioSource source = sensor.AudioSource;
            source.EchoCancellationMode = EchoCancellationMode.None; 
            source.AutomaticGainControlEnabled = false; 

            RecognizerInfo ri = GetKinectRecognizer();

            if (ri == null)
            {
                Console.WriteLine("找不到內建的聲音辨識器");
                return;
            }

            Console.WriteLine("Using: {0}", ri.Name);

            int wait = 4;
            while (wait > 0)
            {
                Console.Write(" {0} 秒後裝置開始進行語音辨識\r", wait--);
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

                    Console.WriteLine("請說 你好嗎? 或  你幾歲? . 按下 ENTER 停止辨識");

                    sre.RecognizeAsync(RecognizeMode.Multiple);
                    Console.ReadLine();
                    Console.WriteLine("停止辨識 ...");
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
            Console.WriteLine("\n無法辨識");
            //if (e.Result != null)
            //{
            //    SaveRecordedAudio(e.Result.Audio);
            //}
        }

        private static void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Console.Write("\r可能是: \t{0}", e.Result.Text);
            //if (e.Result != null)
            //{
            //    SaveRecordedAudio(e.Result.Audio);
            //}
        }

        private static void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence >= 0.5)
            {
                Console.WriteLine("\n應該是: \t{0}\tConfidence:\t{1}", e.Result.Text, e.Result.Confidence);
                VoiceResponse(e.Result.Text);
            }
            else
            {
                Console.WriteLine("\n應該是,但是不太確定: \t{0}", e.Result.Confidence);
            }
        }


        static void VoiceResponse(string input)
        {
            string response = "i don\'t understand";
            if (input == "ni how ma")
                response = "o han how";

            SpeechSynthesizer synthesizer = new SpeechSynthesizer();

            foreach(var v in synthesizer.GetInstalledVoices())
                Console.WriteLine(v.VoiceInfo.Name + "說:" + response);

            synthesizer.Rate = 0;
            synthesizer.Volume = 100;
            synthesizer.Speak(response);
        }

        //使用底下程式碼時,不需要加入System.Speech.dll組件與System.Speech.Synthesis命名空間即可直接使用
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

            Console.WriteLine("寫入檔案: " +  filename);
            using (var file = new FileStream(filename, FileMode.CreateNew))
            {
                audio.WriteToWaveStream(file);
            }
        }
    }
}
