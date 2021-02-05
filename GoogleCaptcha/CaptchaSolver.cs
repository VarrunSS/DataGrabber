using GoogleCaptcha.Models;
using NAudio.Wave;
using System;
using System.IO;
using System.Net;
using System.Speech.Recognition;

namespace GoogleCaptcha
{

    // Google Captcha 2
    public partial class CaptchaSolver : ICaptchaSolver
    {
        public CaptchaSolver(string GoogleCaptchaBasePath, string AudioFileUrl)
        {
            Result = new CaptchaResponse();
            FileName = $"GoogleCaptcha_{DateTime.Now.Ticks}";
            SrcInputFilePath = Path.Combine(GoogleCaptchaBasePath, "Input", $"{FileName}.mp3");
            SrcOutputFilePath = Path.Combine(GoogleCaptchaBasePath, "Output", $"Converted_{FileName}.wav");
            this.AudioFileUrl = AudioFileUrl;
        }

        public CaptchaResponse Result { get; set; }

        private string FileName { get; set; }

        private string SrcInputFilePath { get; set; }

        private string SrcOutputFilePath { get; set; }

        private string AudioFileUrl { get; set; }



    }


    public partial class CaptchaSolver : ICaptchaSolver
    {

        public void StartProcess()
        {
            DownloadFile();
            ConvertMp3ToWav();
            ConvertWavToText();
        }

        private void DownloadFile()
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(AudioFileUrl, SrcInputFilePath);
            }
        }


        private void ConvertMp3ToWav()
        {
            using (Mp3FileReader mp3 = new Mp3FileReader(SrcInputFilePath))
            {
                using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3))
                {
                    WaveFileWriter.CreateWaveFile(SrcOutputFilePath, pcm);
                }
            }
        }

        private void ConvertWavToText()
        {
            using (SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine())
            {
                // Create and load a grammar.  
                Grammar dictation = new DictationGrammar
                {
                    Name = "Dictation Grammar"
                };

                recognizer.LoadGrammar(dictation);

                // Configure the input to the recognizer.  
                recognizer.SetInputToWaveFile(SrcOutputFilePath);

                // Attach event handlers for the results of recognition.  
                recognizer.SpeechRecognized +=
                  new EventHandler<SpeechRecognizedEventArgs>(Recognizer_SpeechRecognized);
                recognizer.RecognizeCompleted +=
                  new EventHandler<RecognizeCompletedEventArgs>(Recognizer_RecognizeCompleted);

                // Perform recognition on the entire file.  
                //Console.WriteLine("Starting asynchronous recognition...");
                //completed = false;
                //recognizer.RecognizeAsync();

                // Keep the console window open.  
                //while (!completed)
                //{
                //    Console.ReadLine();
                //}
                //Console.WriteLine("Done.");

                Console.WriteLine("Starting ");
                recognizer.Recognize();
                Console.WriteLine("Done.");

            }

        }

        void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null && e.Result.Text != null)
            {
                Console.WriteLine("  Recognized text =  {0}", e.Result.Text);
                Result = new CaptchaResponse(true, e.Result.Text);
            }
            else
            {
                Console.WriteLine("  Recognized text not available.");
                Result = new CaptchaResponse(false, "Recognized text not available");
            }
        }

        // Handle the RecognizeCompleted event.  
        void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Console.WriteLine("  Error encountered, {0}: {1}", e.Error.GetType().Name, e.Error.Message);
                Result = new CaptchaResponse(false, string.Format("Error encountered, {0}: {1}", e.Error.GetType().Name, e.Error.Message));
            }
            if (e.Cancelled)
            {
                Console.WriteLine("  Operation cancelled.");
                Result = new CaptchaResponse(false, "Operation cancelled.");
            }
            if (e.InputStreamEnded)
            {
                Console.WriteLine("  End of stream encountered.");
                //Result = new CaptchaResponse(false, "End of stream encountered.");
            }
        }

    }



}
