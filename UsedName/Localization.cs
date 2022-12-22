﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace UsedName
{
    class Localization
    {
        internal string currentLanguage = "en";
        private Dictionary<string, string> languageDict = new() { };
        public Localization(string language = "en")
        {
            currentLanguage = language;
            this.LoadLanguage(language);
        }

        public string Localize(string message)
        {
            if (this.currentLanguage == "en") return message;
            if (languageDict.ContainsKey(message))
            {
                return languageDict[message];
            }
#if DEBUG
            languageDict.Add(message, message);
#endif
            return message;
        }
        internal void LoadLanguage(string language = "en")
        {
            this.currentLanguage = language;
            if (language == "en") return;
            Translations.Culture = new System.Globalization.CultureInfo(language);
            var str = language switch{
                _ => Translations.zh_CN
            };
            try
            {
                this.languageDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
            }
            catch (Exception)
            {
#if DEBUG
                this.StoreLanguage();
#endif
            }
           


        }

        internal string[] GetLanguages()
        {
            string[] languages = { "en", "zh_CN" };
            return languages;
        }

#if DEBUG
        public void StoreLanguage()
        {
            var str = JsonConvert.SerializeObject(this.languageDict);
            string TempPath = @"F:\ffxiv";
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(TempPath), this.currentLanguage + ".json"), str);
        }

#endif
    }
}