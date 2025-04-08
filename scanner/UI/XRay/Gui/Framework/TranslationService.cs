using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using UI.Common.Tracers;
using UI.XRay.Business.DataAccess.Config;
using UI.XRay.Business.Entities;
using UI.XRay.Business.Entities.Enums;
using UI.XRay.ImagePlant;

namespace UI.XRay.Gui.Framework
{
    /// <summary>
    /// 翻译服务
    /// </summary>
    public static class TranslationService
    {
        public static void LoadLanguageFile()
        {
            try
            {
                string languageName;
                if (!ScannerConfig.Read(ConfigPath.SystemLanguage, out languageName))
                {
                    languageName = "English";
                }

                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../", "Language", languageName + ".xml");
                var transDict = Load(path);
                if (transDict != null)
                {
                    // 同时更新语言资源扩展
                    LanguageResourceExtension.SetLangDictionary(transDict);
                }
            }
            catch (Exception e)
            {
                Tracer.TraceException(e, "Failed to language translation file from disk.");
            }
        }

        /// <summary>
        /// 将翻译项列表，以xml的方式，保存至指定的文件中
        /// </summary>
        /// <param name="items"></param>
        /// <param name="filePath"></param>
        public static void Save(List<TranslationSection> items, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                var xmlSerializer = new XmlSerializer(typeof(List<TranslationSection>));
                xmlSerializer.Serialize(fileStream, items);
            }
        }

        /// <summary>
        /// 从指定的xml文件中，读取本地化翻译字符串
        /// </summary>
        /// <param name="filePath">包含本地化翻译字符串的xml文件全路径</param>
        /// <returns>key-value对组成的翻译字符串词典</returns>
        private static Dictionary<string, Dictionary<string, string>> Load(string filePath)
        {
            var ci = new CultureInfo("en-US");
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1000000))
                {
                    var xmlSerializer = new XmlSerializer(typeof(List<TranslationSection>));

                    var list = xmlSerializer.Deserialize(fileStream) as List<TranslationSection>;

                    // 将list转换为Dictionary
                    if (list != null)
                    {
                        var dict = new Dictionary<string, Dictionary<string, string>>();

                        foreach (var section in list)
                        {
                            var translationDict = new Dictionary<string, string>();

                            foreach (var item in section.TranslationElements)
                            {
                                translationDict[item.Source.ToLower(ci)] = item.Translation;
                            }

                            dict[section.SectionName.ToLower(ci)] = translationDict;
                        }

                        return dict;
                    }
                }
            }
            catch (Exception exception)
            {
                Tracer.TraceException(exception, "Failed to read language pack from file " + filePath);
            }

            return null;
        }

        public static string FindTranslation(string source)
        {
            return LanguageResourceExtension.FindTranslation(source);
        }

        public static string FindTranslation(string viewSection, string source)
        {
            return LanguageResourceExtension.FindTranslation(viewSection, source);
        }

        public static string FindTranslation(AccountRole role)
        {
            return LanguageResourceExtension.FindTranslation("AccountRole", role.ToString());
        }

        public static string FindTranslation(StatisticalPeriod period)
        {
            return LanguageResourceExtension.FindTranslation("StatisticalPeriod", period.ToString());
        }

        public static string FindTranslation(TrainingImageLoopMode mode)
        {
            return LanguageResourceExtension.FindTranslation("TrainingImageLoopMode", mode.ToString());
        }
        
        
        public static string FindTranslation(DisplayColorMode mode)
        {
            return LanguageResourceExtension.FindTranslation("DisplayColorMode", mode.ToString());
        }

        public static string FindTranslation(PenetrationMode mode)
        {
            return LanguageResourceExtension.FindTranslation("PenetrationMode", mode.ToString());
        }
    }
}
