using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SimpleLanguageSupport
{
    public class LanguageSupportErrorException : Exception
    {
        public LanguageSupportErrorException()
            : base()
        {

        }

        public LanguageSupportErrorException(string message)
            : base(message)
        {

        }

        public LanguageSupportErrorException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
    /// <summary>
    /// Record information of different languages.
    /// PLZ use 'Abbreviation' to identify different languages.
    /// </summary>
    public class LanguageInfo : ICloneable
    {
        /// <summary>
        /// Get the name of the language
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Get the abbreviation of the language.
        /// PLZ reset 'System.Globalization.CultureInfo' with this param.
        /// See: https://stackoverflow.com/questions/3279403/change-language-in-c-sharp
        /// </summary>
        public string Abbreviation { get; private set; }
        public LanguageInfo(string abbreviation, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(abbreviation))
            {
                throw new LanguageSupportErrorException(
                    "Initialize Error: param is null or white space");
            }

            Name = string.Copy(name);
            Abbreviation = string.Copy(abbreviation);
        }

        public object Clone()
        {
            return new LanguageInfo(Abbreviation, Name);
        }

        public static LanguageInfo Copy(LanguageInfo item)
        {
            return (LanguageInfo)item.Clone();
        }
    }
    internal class LanguageDic : Dictionary<string, LanguageInfo>
    {

    }
    public class ItemInfo : ICloneable
    {
        public string ID { get; private set; }

        public string Content { get; private set; }

        public ItemInfo(string id, string content)
        {
            if (id == null)
            {
                throw new LanguageSupportErrorException(
                    "Initialize Error: id is null");
            }

            ID = string.Copy(id);
            Content = string.Copy(content);
        }

        public object Clone()
        {
            return new ItemInfo(ID, Content);
        }

        public static ItemInfo Copy(ItemInfo item)
        {
            return (ItemInfo)item.Clone();
        }
    }
    internal class ItemDic : Dictionary<string, ItemInfo>
    {

    }
    internal class ItemDicDic : Dictionary<string, ItemDic>
    {

    }

    public class LanguageSupportHelper
    {
        #region static

        private static LanguageSupportHelper helper;
        private static object helperLocker;

        static LanguageSupportHelper()
        {
            helper = null;
            helperLocker = new object();
        }

        public static LanguageSupportHelper GetHelper()
        {
            if (helper == null)
            {
                lock (helperLocker)
                {
                    if (helper == null)
                    {
                        helper = new LanguageSupportHelper();
                    }
                }
            }

            return helper;
        }

        #endregion

        #region Fields & Properties

        private LanguageDic langSupportDic;
        private ItemDicDic itemDicCollection;

        private LanguageInfo langDefault;
        private ItemDic itemDicDefault;

        private LanguageInfo langSelected;
        private ItemDic itemDicSelected;
        private object selectLocker;

        public LanguageInfo DefaultLanguageInfo
        {
            get
            {
                return langDefault;
            }
        }

        public IReadOnlyDictionary<string, ItemInfo> DefaultItemDictionary
        {
            get
            {
                return itemDicDefault;
            }
        }

        public LanguageInfo Language
        {
            get
            {
                return langSelected;
            }
        }

        public IReadOnlyDictionary<string, ItemInfo> ItemDictionary
        {
            get
            {
                lock (selectLocker)
                {
                    return itemDicSelected;
                }
            }
        }

        #endregion
        private LanguageSupportHelper()
        {
            InitField();

            XmlElement rootElement = LoadLanguageXml();
            AnalysisLanguageSupport((XmlElement)rootElement.GetElementsByTagName("LanguageSupport")[0]);
            AnalysisItemDetails((XmlElement)rootElement.GetElementsByTagName("ItemDetails")[0]);
            CheckAbbreviationList();
            AnalysisSettings((XmlElement)rootElement.GetElementsByTagName("Settings")[0]);

            InitSelected();
        }

        private void InitField()
        {
            langSupportDic = new LanguageDic();
            itemDicCollection = new ItemDicDic();

            langDefault = null;
            itemDicDefault = new ItemDic();

            langSelected = null;
            itemDicSelected = new ItemDic();
            selectLocker = new object();
        }

        private void InitSelected()
        {
            langSelected = langDefault;
            itemDicSelected = itemDicDefault;
        }

        #region Xml Part

        private XmlElement LoadLanguageXml()
        {
            Assembly xmlFileAssembly = Assembly.GetExecutingAssembly();
            Stream xmlFileStream = xmlFileAssembly.GetManifestResourceStream(
                "SimpleLanguageSupport.Resources.Language.xml");

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFileStream);

            return xmlDoc.DocumentElement;
        }

        private void AnalysisLanguageSupport(XmlElement langSupportElement)
        {
            foreach (XmlElement item in langSupportElement)
            {
                string abbreviation = item.GetAttribute("abbreviation");
                string name = item.GetAttribute("name");

                langSupportDic.Add(abbreviation, new LanguageInfo(abbreviation, name));
            }
        }

        private void AnalysisItemDetails(XmlElement detailsElement)
        {
            foreach (XmlElement item_ItemList in detailsElement.GetElementsByTagName("ItemList"))
            {
                string abbreviation = item_ItemList.GetAttribute("language");
                ItemDic itemList = new ItemDic();

                foreach (XmlElement item_iteminfo in item_ItemList.GetElementsByTagName("Item"))
                {
                    string id = item_iteminfo.GetAttribute("id");
                    string content = item_iteminfo.GetAttribute("content");

                    itemList.Add(id, new ItemInfo(id, content));
                }

                itemDicCollection.Add(abbreviation, itemList);
            }

        }

        private void AnalysisSettings(XmlElement settingsElement)
        {
            string abbreviation = ((XmlElement)settingsElement.GetElementsByTagName("Default")[0]).GetAttribute("language");
            if (!langSupportDic.TryGetValue(abbreviation, out langDefault) ||
                !itemDicCollection.TryGetValue(abbreviation, out itemDicDefault))
            {
                throw new LanguageSupportErrorException(
                    "Set Default Language Error: No such language in the language support list.");
            }

        }

        private void CheckAbbreviationList()
        {
            ICollection<string> itemAbbList = itemDicCollection.Keys;
            List<string> langAbbList = new List<string>(langSupportDic.Keys);

            foreach (string item_itemAbb in itemAbbList)
            {
                for (int i = 0; i < langAbbList.Count; i++)
                {
                    if (item_itemAbb == langAbbList[i])
                    {
                        langAbbList.RemoveAt(i);
                        break;
                    }
                }
            }

            if (langAbbList.Count != 0)
            {
                throw new LanguageSupportErrorException(
                    "Match Error: There are at least one 'Language' which has no 'ItemList' to match.");
            }
        }

        #endregion

        public bool SetLanguage(string abbreviation)
        {
            lock (selectLocker)
            {
                LanguageInfo temp = langSelected;

                if (langSupportDic.TryGetValue(abbreviation, out langSelected))
                {
                    itemDicSelected = itemDicCollection[abbreviation];
                    return true;
                }
                else
                {
                    langSelected = temp;
                    return false;
                }
            }

        }
    }
}
