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
    using System.Globalization;

    public class Program
    {
        static KinectSensor sensor;
        public static void Main(string[] args)
        {
            FindKinect();
            sensor.Start();

            KinectAudioSource source = AudioSetup();
            RecognizerInfo ri = FindRecognizer();
            RecognizerReady();
            RecognizerSetup(source, ri);

            sensor.Stop();
        }

        private static void RecognizerSetup(KinectAudioSource source, RecognizerInfo ri)
        {
            using (var sre = new SpeechRecognitionEngine(ri.Id))
            {
                GrammarSetup(ri, sre);
                EventRegister(sre);
                RecognitionStart(source, sre);
            }
        }

        private static void RecognitionStart(KinectAudioSource source, SpeechRecognitionEngine sre)
        {
            using (Stream s = source.Start())
            {
                sre.SetInputToAudioStream(
                    s, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

                Console.WriteLine("請說 你好嗎? 或 你幾歲?。  按下 ENTER 停止辨識");

                sre.RecognizeAsync(RecognizeMode.Multiple);
                Console.ReadLine();
                Console.WriteLine("停止辨識 ...");
                sre.RecognizeAsyncStop();
            }
        }

        private static void EventRegister(SpeechRecognitionEngine sre)
        {
            sre.SpeechRecognized += SreSpeechRecognized;
            sre.SpeechHypothesized += SreSpeechHypothesized;
            sre.SpeechRecognitionRejected += SreSpeechRecognitionRejected;
        }

        private static void GrammarSetup(RecognizerInfo ri, SpeechRecognitionEngine sre)
        {
            var gb = new GrammarBuilder { Culture = ri.Culture };

            gb.Append(new Choices("ni"));
            gb.Append(new Choices("how", "ji"));
            gb.Append(new Choices("ma", "suei"));

            var g = new Grammar(gb);
            sre.LoadGrammar(g);
        }

        private static void RecognizerReady()
        {
            int wait = 4;
            while (wait > 0)
            {
                Console.Write(" {0} 秒後裝置開始進行語音辨識\r", wait--);
                Thread.Sleep(1000);
            }
        }

        private static void FindKinect()
        {
            if (KinectSensor.KinectSensors.Count > 0)
            {
                sensor = KinectSensor.KinectSensors[0];
                if (sensor == null || sensor.IsRunning == true)
                {
                    Console.WriteLine("無法使用 Kinect感應器");
                    Environment.Exit(0);
                }
            }
            else          
            {
                Console.WriteLine("找不到 Kinect感應器");
                Environment.Exit(0);
            }
        }

        private static RecognizerInfo FindRecognizer()
        {
            RecognizerInfo ri = GetKinectRecognizer();

            if (ri == null)
            {
                Console.WriteLine("找不到內建的聲音辨識器");
                Environment.Exit(0);
            }
            Console.WriteLine("使用: {0}", ri.Name);
            return ri;
        }

        private static KinectAudioSource AudioSetup()
        {
            //設定音源
            KinectAudioSource source = sensor.AudioSource;
            source.EchoCancellationMode = EchoCancellationMode.None;
            source.AutomaticGainControlEnabled = false;
            return source;
        }

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) 
                    && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
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
            string response = "可以再說一次嗎?";
            if (input == "ni how ma")
                response = "我很好";
            else if (input == "ni ji suei")
                response = "我兩歲";

            SpeechSynthesizer synthesizer = new SpeechSynthesizer();

            string voicename = VoiceChooser(synthesizer);
            Console.WriteLine(voicename + "說 : " + response);  


            synthesizer.Rate = 0;
            synthesizer.Volume = 100;
            synthesizer.Speak(response);
        }
        static string VoiceChooser(SpeechSynthesizer synthesizer)
        {
            string voicename;
            var culture = CultureInfo.GetCultureInfo("zh-TW");
            var voices = synthesizer.GetInstalledVoices(culture);
            if (voices.Count > 0)
            {
                voicename = voices[0].VoiceInfo.Name;
                synthesizer.SelectVoice(voicename);
                Console.WriteLine("找到中文語音合成引擎 : " + voicename);
            }
            else
            {
                voicename = synthesizer.GetInstalledVoices()[0].VoiceInfo.Name;
                Console.WriteLine("沒有中文語音合成引擎，使用英文語音合成引擎 : " + voicename);

            }
            return voicename;
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
