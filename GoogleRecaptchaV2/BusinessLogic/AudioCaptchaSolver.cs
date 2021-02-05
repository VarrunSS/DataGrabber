using GoogleRecaptchaV2.Models;
using NAudio.Wave;
using System;
using System.IO;
using System.Net;
using System.Speech.Recognition;
using GoogleRecaptchaV2.BusinessLogic.Contract;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using DataGrabberV2.Utility;
using DataGrabberV2.LogWriter;
using OpenQA.Selenium.Support.UI;
using GoogleRecaptchaV2.BusinessLogic;

namespace GoogleRecaptchaV2.BusinessLogic
{

    // Google Captcha 2
    public partial class AudioCaptchaSolver : CaptchaSolver, ICaptchaSolver
    {
        public AudioCaptchaSolver()
        {
        }


        public override CaptchaResponse Result { get; set; }

        private ChromeDriver Driver { get; set; }

        private string FileName { get; set; }

        private string SrcInputFilePath { get; set; }

        private string SrcOutputFilePath { get; set; }

        private string AudioFileUrl { get; set; }


    }


    public partial class AudioCaptchaSolver : CaptchaSolver, ICaptchaSolver
    {

        public override void Initialize(string GoogleCaptchaBasePath, ChromeDriver driver)
        {
            Driver = driver;
            Result = new CaptchaResponse();
            FileName = $"GoogleCaptcha_{DateTime.Now.Ticks}";
            SrcInputFilePath = Path.Combine(GoogleCaptchaBasePath, "Input", $"{FileName}.mp3");
            SrcOutputFilePath = Path.Combine(GoogleCaptchaBasePath, "Output", $"Converted_{FileName}.wav");
        }

        public override void StartProcess()
        {
            CheckIfGoogleCaptchaIsPresent(Driver);
            GetAudioURL();

            if (Result.IsCaptchaSolved)
            {
                DownloadFile();
                ConvertMp3ToWav();
                ConvertWavToText();
            }
        }

        private void GetAudioURL()
        {
            try
            {
                WebDriverWait wait = Driver.GetWebDriverWait();

                new WebDriverWait(Driver, TimeSpan.FromSeconds(10)).Until(SeleniumExtras.WaitHelpers.ExpectedConditions.FrameToBeAvailableAndSwitchToIt(By.XPath("//iframe[starts-with(@name, 'a-') and starts-with(@src, 'https://www.google.com/recaptcha')]")));
                new WebDriverWait(Driver, TimeSpan.FromSeconds(20)).Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(By.CssSelector("div.recaptcha-checkbox-checkmark"))).Click();


                By iframeElemPath = By.XPath("//*[contains(@class, 'g-recaptcha ')]//iframe[1]");
                if (!Driver.IsElementPresent(iframeElemPath))
                {
                    Logger.Write($"ERROR: Could not find iframe, but google Captcha is present-> GoogleRecaptchaV2.");
                    Result = new CaptchaResponse(false, "Could not find iframe, but google Captcha is present");
                    return;
                }
                else
                {
                    // Go into iframe content
                    Driver.SwitchTo().Frame(Driver.FindElement(iframeElemPath));
                }


                By checkboxElemPath = By.XPath("//*[@class='recaptcha-checkbox goog-inline-block recaptcha-checkbox-unchecked rc-anchor-checkbox']");
                if (!Driver.IsElementPresent(checkboxElemPath))
                {
                    Result = new CaptchaResponse(false, "Could not find checkbox, but google Captcha is present");
                    return;
                }

                IWebElement checkboxElem = Driver.FindElement(checkboxElemPath);
                checkboxElem.ClickIfDisplayed(Driver);


                // wait for content to be loaded through ajax 
                By productContainer = By.XPath("//*[@id='rc-imageselect']");
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(productContainer));

                // click audio button
                By audioElemPath = By.XPath("//*[@class='rc-button goog-inline-block rc-button-audio']");
                if (!Driver.IsElementPresent(audioElemPath))
                {
                    Result = new CaptchaResponse(false, "Could not find audio button, but google Captcha is present");
                    return;
                }

                IWebElement audioElem = Driver.FindElement(audioElemPath);
                audioElem.ClickIfDisplayed(Driver);

                By audioDownloadElemPath = By.XPath("//*[@class='rc-audiochallenge-tdownload-link']");
                // wait for content to be loaded through ajax 
                wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(audioDownloadElemPath));

                if (!Driver.IsElementPresent(audioDownloadElemPath))
                {
                    Result = new CaptchaResponse(false, "Could not find audio download button, but google Captcha is present");
                    return;
                }
                else
                {
                    AudioFileUrl = Driver.FindElement(audioElemPath).GetAttribute("href");
                }

                // Switch back to default frame
                Driver.SwitchTo().DefaultContent();
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in GetAudioURL -> GoogleRecaptchaV2. Message: " + ex.Message);
            }
        }

        private void DownloadFile()
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(AudioFileUrl, SrcInputFilePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in DownloadFile -> GoogleRecaptchaV2. Message: " + ex.Message);
            }
        }

        private void ConvertMp3ToWav()
        {
            try
            {
                using (Mp3FileReader mp3 = new Mp3FileReader(SrcInputFilePath))
                {
                    using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3))
                    {
                        WaveFileWriter.CreateWaveFile(SrcOutputFilePath, pcm);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ConvertMp3ToWav -> GoogleRecaptchaV2. Message: " + ex.Message);
            }
        }

        private void ConvertWavToText()
        {
            try
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

                    Console.WriteLine("Starting captcha solver..");
                    recognizer.Recognize();
                    Console.WriteLine("Done.");
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ConvertWavToText -> GoogleRecaptchaV2. Message: " + ex.Message);
            }

        }

        void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Logger.Write("Exception in ConvertWavToText - Recognizer_SpeechRecognized -> GoogleRecaptchaV2. Message: " + ex.Message);
            }
        }

        // Handle the RecognizeCompleted event.  
        void Recognizer_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            try
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
                    Result = new CaptchaResponse(false, "End of stream encountered.");
                }
            }
            catch (Exception ex)
            {
                Logger.Write("Exception in ConvertWavToText - Recognizer_RecognizeCompleted -> GoogleRecaptchaV2. Message: " + ex.Message);
            }
        }

    }



}
