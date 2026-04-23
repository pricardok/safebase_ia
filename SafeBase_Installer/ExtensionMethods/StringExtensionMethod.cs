using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafeBase_Installer.ExtensionMethods
{
    public static class StringExtensionMethod
    {
        public static string ClearCaracterEspecial(this string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                char cr = (char)13;
                char lf = (char)10;
                char tab = (char)9;

                value =
                    value.
                        Replace("\\r", cr.ToString()).
                        Replace("\\n", lf.ToString()).
                        Replace("\\t", tab.ToString());
            }

            return value;
        }
    }
}
