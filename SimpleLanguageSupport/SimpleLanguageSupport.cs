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
        public LanguageSupportErrorException() : base()
        { 

        }

        public LanguageSupportErrorException(string message) : base(message)
        {

        }

        public LanguageSupportErrorException(string message, Exception innerException) : base(message,innerException)
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

    public static class ProgramLanguageHelp
    {
        private static Dictionary<string, LanguageInfo> SupportList;
        private static Dictionary<string, Dictionary<string, ItemInfo>> ItemListCollection;

        private static LanguageInfo LanguageDefault;
        private static Dictionary<string, ItemInfo> ItemDicDefault;

        private static LanguageInfo LanguageSelect;

        static ProgramLanguageHelp()
        {
            InitStaticFields();

            XmlElement xmlRootElement = LoadLanguageXml();
            ReadSupportList(
                (XmlElement)xmlRootElement.GetElementsByTagName("LanguageSupport")[0]);
            ReadItemDetails(
                (XmlElement)xmlRootElement.GetElementsByTagName("ItemDetails")[0]);
            CheckAbbreviationList();

            ReadSettings(
                (XmlElement)xmlRootElement.GetElementsByTagName("Settings")[0]);
            
        }

        private static void InitStaticFields()
        {
            SupportList =new Dictionary<string,LanguageInfo>();
            ItemListCollection = new Dictionary<string, Dictionary<string, ItemInfo>>();

            LanguageDefault = null;
            ItemDicDefault = new Dictionary<string, ItemInfo>();

            LanguageSelect = null;
        }

        private static XmlElement LoadLanguageXml()
        {
            Assembly xmlFileAssembly = Assembly.GetExecutingAssembly();
            Stream xmlFileStream = xmlFileAssembly.GetManifestResourceStream(
                "SimpleLanguageSupport.Resources.Language.xml");

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFileStream);

            return xmlDoc.DocumentElement;
        }

        private static void ReadSupportList(XmlElement supportListElement)
        {
            foreach(XmlElement item in supportListElement)
            {
                string abbreviation = item.GetAttribute("abbreviation");
                string name = item.GetAttribute("name");

                SupportList.Add(abbreviation, new LanguageInfo(abbreviation, name));
            }
        }

        private static void ReadSettings(XmlElement settingsElement)
        {
            string abbreviation = ((XmlElement)settingsElement.GetElementsByTagName("Default")[0]).GetAttribute("language");
            if(!SupportList.TryGetValue(abbreviation, out LanguageDefault) || 
                !ItemListCollection.TryGetValue(abbreviation,out ItemDicDefault))
            {
                throw new LanguageSupportErrorException(
                    "Set Default Language Error: No such language in the language support list.");
            }

            LanguageSelect = LanguageDefault;
        }

        private static void ReadItemDetails(XmlElement detailsElement)
        {
            foreach (XmlElement item_ItemList in detailsElement.GetElementsByTagName("ItemList"))
            {
                string abbreviation = item_ItemList.GetAttribute("language");
                Dictionary<string, ItemInfo> itemList = new Dictionary<string, ItemInfo>();

                foreach (XmlElement item_iteminfo in item_ItemList.GetElementsByTagName("Item"))
                {
                    string id = item_iteminfo.GetAttribute("id");
                    string content = item_iteminfo.GetAttribute("content");

                    itemList.Add(id, new ItemInfo(id, content));
                }

                ItemListCollection.Add(abbreviation, itemList);
            }

        }

        private static void CheckAbbreviationList()
        {
            ICollection<string> itemAbbList = ItemListCollection.Keys;
            List<string> langAbbList = new List<string>(SupportList.Keys);

            foreach(string item_itemAbb in itemAbbList)
            {
                for(int i=0;i<langAbbList.Count;i++)
                {
                    if(item_itemAbb==langAbbList[i])
                    {
                        langAbbList.RemoveAt(i);
                        break;
                    }
                }
            }

            if(langAbbList.Count!=0)
            {
                throw new LanguageSupportErrorException(
                    "Match Error: There are at least one 'Language' which has no 'ItemList' to match.");
            }
        }

        public static List<LanguageInfo> GetSupportLanguageList()
        {
            List<LanguageInfo> langList = new List<LanguageInfo>();
            foreach(LanguageInfo item in SupportList.Values)
            {
                langList.Add(LanguageInfo.Copy(item));
            }

            return langList;
        }

        public static Dictionary<string, ItemInfo> GetItemDictionary(string abbreviation)
        {
            Dictionary<string, ItemInfo> itemDic = new Dictionary<string, ItemInfo>();
            Dictionary<string, ItemInfo> itemDic2 = new Dictionary<string, ItemInfo>();
            if(!ItemListCollection.TryGetValue(abbreviation, out itemDic2))
            {
                return null;
            }

            foreach(ItemInfo item in itemDic2.Values)
            {
                itemDic.Add(item.ID, item);
            }

            return itemDic;
        }

        public static LanguageInfo GetDefaultLanguage()
        {
            return LanguageDefault;
        }

        public static Dictionary<string, ItemInfo> GetDefaultItemDictionary()
        {
            return GetItemDictionary(LanguageDefault.Abbreviation);
        }

        public static bool SetLanguage(string abbreviation)
        {
            if(SupportList.TryGetValue(abbreviation, out LanguageSelect))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static LanguageInfo GetLanguage()
        {
            return LanguageInfo.Copy(LanguageSelect);
        }
    }
}
