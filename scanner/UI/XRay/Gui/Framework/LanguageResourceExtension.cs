using System;
using System.Collections.Generic;
using System.Windows.Markup;
using UI.Common.Tracers;

namespace UI.XRay.Gui.Framework
{
    public class LanguageItem
    {
        public string Section { get; set; }

        public string Source { get; set; }

        public string Translation { get; set; }

        public string Comment { get; set; }
    }

    /// <summary>
    /// 表示基于多语言的字符串资源扩展，用于xaml中绑定字符串资源
    /// </summary>
    [MarkupExtensionReturnType(typeof(Object))]
    public class LanguageResourceExtension : MarkupExtension
    {
        /// <summary>
        /// 通用语言分部。为避免重复翻译以及保证界面文字的一致性，将一些通用的语言资源，放置在这里。
        /// </summary>
        public static readonly string CommonSection = "Common";

        /// <summary>
        /// 语言字典：字典的第一层为按视图分类的Section；第二层为该Section下的所有语言翻译，使用Source进行查询
        /// </summary>
        public static Dictionary<string, Dictionary<string, string>> LanguageDictionary { get; private set; }

        public static object _syncRoot = new object();

        /// <summary>
        /// 设置语言字典。软件默认语言为英文，即设计时语言，如果需要翻译为其它语言，需要在程序启动时加载语言包，并调用
        /// 此静态函数初始化程序的语言
        /// </summary>
        /// <param name="langStringsDictionary"></param>
        public static void SetLangDictionary(Dictionary<string, Dictionary<string, string>> langStringsDictionary)
        {
            lock (_syncRoot)
            {
                LanguageDictionary = langStringsDictionary;
            }
        }

        public LanguageResourceExtension(){}

        /// <summary>
        /// 获取Common下的语言翻译
        /// </summary>
        /// <param name="source">要翻译的语言</param>
        public LanguageResourceExtension(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            // 移除字符串前及字符串后可能存在的空白字符串
            _source = source.Trim();
        }

        /// <summary>
        /// 按视图分部，获取语言翻译
        /// </summary>
        /// <param name="viewSection"></param>
        /// <param name="langSource"></param>
        public LanguageResourceExtension(string viewSection, string langSource)
        {
            if (langSource == null)
            {
                throw new ArgumentNullException("langSource");
            }

            ViewSection = viewSection;

            // 移除字符串前及字符串后可能存在的空白字符串
            _source = langSource.Trim();
        }

        /// <summary>
        /// resource key
        /// </summary>
        private string _source;

        /// <summary>
        /// 要翻译的字符串的英文Key，不能为空
        /// </summary>
        public string Source
        {
            get
            {
                return _source;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this._source = value;
            }
        }

        /// <summary>
        /// 要翻译的字符串所在的视图部分。
        /// </summary>
        public string ViewSection { get; set; }

        /// <summary>
        /// 重写基类的ProvideValue函数，根据ResourceKey，获取当前基于当前配置语言的本地化字符串
        /// 。如果找不到翻译的字符串，则返回原字符串
        /// </summary>
        /// <param name="serviceProvider">在此未使用</param>
        /// <returns>与当前配置语言有关的本地化字符串</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            lock (_syncRoot)
            {
                // 如果从未加载任何语言字典，则返回默认的设计时语言，即英语
                if (LanguageDictionary == null)
                {
                    return Source;
                }

                return FindTranslation(ViewSection, Source);
            }
        }

        public static string FindTranslation(string source)
        {
            if (LanguageDictionary == null)
            {
                return source;
            }

            Dictionary<string, string> dict;

            if (LanguageDictionary.TryGetValue(CommonSection.ToLower(), out dict))
            {
                string translation;
                if (dict.TryGetValue(source.ToLower(), out translation))
                {
                    return translation;
                }

                //Tracer.TraceWarning("Could not find translation for '" + source + "'in section " + LanguageResourceExtension.CommonSection);
            }
            else
            {
                //Tracer.TraceWarning("Language translation does not have section: " + LanguageResourceExtension.CommonSection);
            }

            return source;
        }

        public static string FindTranslation(string viewSection, string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;
            if (LanguageDictionary == null)
            {
                return source;
            }

            if (!string.IsNullOrEmpty(viewSection))
            {
                Dictionary<string, string> dict;
                if (LanguageDictionary.TryGetValue(viewSection.ToLower(), out dict))
                {
                    string translation;
                    if (dict.TryGetValue(source.ToLower(), out translation))
                    {
                        return translation;
                    }

                    //Tracer.TraceWarning("Could not find translation for '" + source + "'in section " + LanguageResourceExtension.CommonSection);
                }
                else
                {
                    //Tracer.TraceWarning("Language translation does not have section: " + viewSection);
                }
            }

            // 如果在指定的 section 中未找到翻译项，则尝试在common中寻找翻译项
            if (!string.Equals(viewSection, LanguageResourceExtension.CommonSection, StringComparison.OrdinalIgnoreCase))
            {
                return FindTranslation(source);
            }

            return source;
        }

    }
}
