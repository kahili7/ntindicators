using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

using NinjaTrader.Data;

namespace NinjaTrader.Data
{
    public class GTranslate
    {
        private static Dictionary<string, string> _languageModeMap;

        #region Private functions
        private static string LanguageEnumToIdentifier(string language)
        {
            string mode = string.Empty;

            EnsureInitialized();

            _languageModeMap.TryGetValue(language, out mode);
            return mode;
        }

        private static void EnsureInitialized()
        {
            if (_languageModeMap == null)
            {
                _languageModeMap = new Dictionary<string, string>();
                _languageModeMap.Add("Afrikaans", "af");
                _languageModeMap.Add("Albanian", "sq");
                _languageModeMap.Add("Arabic", "ar");
                _languageModeMap.Add("Armenian", "hy");
                _languageModeMap.Add("Azerbaijani", "az");
                _languageModeMap.Add("Basque", "eu");
                _languageModeMap.Add("Belarusian", "be");
                _languageModeMap.Add("Bengali", "bn");
                _languageModeMap.Add("Bulgarian", "bg");
                _languageModeMap.Add("Catalan", "ca");
                _languageModeMap.Add("Chinese", "zh-CN");
                _languageModeMap.Add("Croatian", "hr");
                _languageModeMap.Add("Czech", "cs");
                _languageModeMap.Add("Danish", "da");
                _languageModeMap.Add("Dutch", "nl");
                _languageModeMap.Add("English", "en");
                _languageModeMap.Add("Esperanto", "eo");
                _languageModeMap.Add("Estonian", "et");
                _languageModeMap.Add("Filipino", "tl");
                _languageModeMap.Add("Finnish", "fi");
                _languageModeMap.Add("French", "fr");
                _languageModeMap.Add("Galician", "gl");
                _languageModeMap.Add("German", "de");
                _languageModeMap.Add("Georgian", "ka");
                _languageModeMap.Add("Greek", "el");
                _languageModeMap.Add("Haitian Creole", "ht");
                _languageModeMap.Add("Hebrew", "iw");
                _languageModeMap.Add("Hindi", "hi");
                _languageModeMap.Add("Hungarian", "hu");
                _languageModeMap.Add("Icelandic", "is");
                _languageModeMap.Add("Indonesian", "id");
                _languageModeMap.Add("Irish", "ga");
                _languageModeMap.Add("Italian", "it");
                _languageModeMap.Add("Japanese", "ja");
                _languageModeMap.Add("Korean", "ko");
                _languageModeMap.Add("Lao", "lo");
                _languageModeMap.Add("Latin", "la");
                _languageModeMap.Add("Latvian", "lv");
                _languageModeMap.Add("Lithuanian", "lt");
                _languageModeMap.Add("Macedonian", "mk");
                _languageModeMap.Add("Malay", "ms");
                _languageModeMap.Add("Maltese", "mt");
                _languageModeMap.Add("Norwegian", "no");
                _languageModeMap.Add("Persian", "fa");
                _languageModeMap.Add("Polish", "pl");
                _languageModeMap.Add("Portuguese", "pt");
                _languageModeMap.Add("Romanian", "ro");
                _languageModeMap.Add("Russian", "ru");
                _languageModeMap.Add("Serbian", "sr");
                _languageModeMap.Add("Slovak", "sk");
                _languageModeMap.Add("Slovenian", "sl");
                _languageModeMap.Add("Spanish", "es");
                _languageModeMap.Add("Swahili", "sw");
                _languageModeMap.Add("Swedish", "sv");
                _languageModeMap.Add("Tamil", "ta");
                _languageModeMap.Add("Telugu", "te");
                _languageModeMap.Add("Thai", "th");
                _languageModeMap.Add("Turkish", "tr");
                _languageModeMap.Add("Ukrainian", "uk");
                _languageModeMap.Add("Urdu", "ur");
                _languageModeMap.Add("Vietnamese", "vi");
                _languageModeMap.Add("Welsh", "cy");
                _languageModeMap.Add("Yiddish", "yi");
            }
        }
        #endregion

        public string Translate(string sourceText, string sourceLanguage, string targetLanguage)
        {
            // Initialize
            Error = null;
            TranslationSpeechUrl = null;
            TranslationTime = TimeSpan.Zero;
            DateTime tmStart = DateTime.Now;
            string translation = string.Empty;

            try
            {
                // Download translation
                string url = string.Format("https://translate.google.com/translate_a/single?client=t&sl={0}&tl={1}&hl=en&dt=bd&dt=ex&dt=ld&dt=md&dt=qc&dt=rw&dt=rm&dt=ss&dt=t&dt=at&ie=UTF-8&oe=UTF-8&source=btn&ssel=0&tsel=0&kc=0&q={2}",
                                            LanguageEnumToIdentifier(sourceLanguage), LanguageEnumToIdentifier(targetLanguage), HttpUtility.UrlEncode(sourceText));
                string outputFile = Path.GetTempFileName();

                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
                    wc.DownloadFile(url, outputFile);
                }

                if(File.Exists(outputFile))
                {
                    // Get phrase collection
                    string text = File.ReadAllText(outputFile);
                    int index = text.IndexOf(string.Format(",,\"{0}\"", LanguageEnumToIdentifier(sourceLanguage)));

                    if(index == -1)
                    {
                        // Translation of single word
                        int startQuote = text.IndexOf('\"');

                        if(startQuote != -1)
                        {
                            int endQuote = text.IndexOf('\"', startQuote + 1);

                            if(endQuote != -1)
                            {
                                translation = text.Substring(startQuote + 1, endQuote - startQuote - 1);
                            }
                        }
                    }
                    else
                    {
                        // Translation of phrase
                        text = text.Substring(0, index);
                        text = text.Replace("],[", ",");
                        text = text.Replace("]", string.Empty);
                        text = text.Replace("[", string.Empty);
                        text = text.Replace("\",\"", "\"");

                        // Get translated phrases
                        string[] phrases = text.Split(new[] { '\"' }, StringSplitOptions.RemoveEmptyEntries);

                        for(int i = 0; (i < phrases.Count()); i += 2)
                        {
                            string translatedPhrase = phrases[i];

                            if(translatedPhrase.StartsWith(",,"))
                            {
                                i--;
                                continue;
                            }

                            translation += translatedPhrase + "  ";
                        }
                    }

                    // Fix up translation
                    translation = translation.Trim();
                    translation = translation.Replace(" ?", "?");
                    translation = translation.Replace(" !", "!");
                    translation = translation.Replace(" ,", ",");
                    translation = translation.Replace(" .", ".");
                    translation = translation.Replace(" ;", ";");

                    // And translation speech URL
                    this.TranslationSpeechUrl = string.Format("https://translate.google.com/translate_tts?ie=UTF-8&q={0}&tl={1}&total=1&idx=0&textlen={2}&client=t", HttpUtility.UrlEncode (translation), LanguageEnumToIdentifier (targetLanguage), translation.Length);                 
                }
            }
            catch (Exception ex)
            {
                this.Error = ex;
            }

            TranslationTime = DateTime.Now - tmStart;
            return translation;
        }

        #region Properties
        public static IEnumerable<string> Languages
        {
            get
            {
                EnsureInitialized();
                return _languageModeMap.Keys.OrderBy(p => p);
            }
        }

        public TimeSpan TranslationTime
        {
            get;
            private set;
        }

        public string TranslationSpeechUrl
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }
        #endregion
    }
}
