using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ButtzxBank
{
    public class m_cCore
    {
        public static string ToEncodingString(string m_sStr)
        {
            return HttpUtility.UrlEncode(m_sStr, System.Text.Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING));
        }
    }
}