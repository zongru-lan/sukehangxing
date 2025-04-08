using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace UI.XRay.Business.Entities
{
    /// <summary>
    /// 表示一个文本翻译项
    /// </summary>
    [XmlRoot]
    public class TranslationElement
    {
        public TranslationElement()
        {

        }

        public TranslationElement(string source, string translation, string comment)
        {
            Source = source;
            Translation = translation;
            Comment = comment;
        }

        /// <summary>
        /// Source string
        /// </summary>
        [XmlAttribute]
        public string Source { get; set; }

        /// <summary>
        /// Translated string
        /// </summary>
        [XmlElement]
        public string Translation { get; set; }

        /// <summary>
        /// Comment for translator
        /// </summary>
        [XmlAttribute]
        public string Comment { get; set; }
    }

    [XmlRoot]
    public class TranslationSection
    {
        [XmlElement]
        public List<TranslationElement> TranslationElements { get; set; }

        [XmlAttribute]
        public string SectionName { get; set; }

        [XmlAttribute]
        public string Description { get; set; }

        public TranslationSection(string sectionName ,List<TranslationElement> elements)
        {
            TranslationElements = elements;
            SectionName = sectionName;
        }

        public TranslationSection()
        {
            
        }

        public TranslationSection(string sectionName)
        {
            TranslationElements = new List<TranslationElement>();
            SectionName = sectionName;
        }
    }
}
