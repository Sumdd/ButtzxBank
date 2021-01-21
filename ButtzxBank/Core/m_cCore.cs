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

        public static object m_pFileLock = new object();
        public static bool m_fWriteBase64TokenToTxt(string m_sText)
        {
            lock (m_pFileLock)
            {
                ///创建文件夹
                string path = HttpContext.Current.Server.MapPath("~/Token");
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);

                ///写入文件
                DateTime m_dtDateTime = DateTime.Now;
                string file = $"{path}/{m_dtDateTime.ToString("yyyy-MM-dd")}.token";
                System.IO.File.WriteAllText(file, Convert.ToBase64String(System.Text.Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(m_sText)));

                return true;
            }
        }

        public static string m_fReadTxtToToken()
        {
            lock (m_pFileLock)
            {
                try
                {
                    string path = HttpContext.Current.Server.MapPath("~/Token");

                    ///读取
                    DateTime m_dtDateTime = DateTime.Now;
                    string file = $"{path}/{m_dtDateTime.ToString("yyyy-MM-dd")}.token";
                    if (System.IO.File.Exists(file))
                    {
                        string m_sToken = System.Text.Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetString(Convert.FromBase64String(System.IO.File.ReadAllText(file)));
                        Log.Instance.Debug($"从{file}缓存中读取Token:{m_sToken}");
                        return m_sToken;
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Debug(ex);
                }
                return null;
            }
        }
    }
}