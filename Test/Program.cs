using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleLanguageSupport;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            LanguageSupportHelper helper = LanguageSupportHelper.GetHelper();
            helper.SetLanguage("en-us");
        }
    }
}
