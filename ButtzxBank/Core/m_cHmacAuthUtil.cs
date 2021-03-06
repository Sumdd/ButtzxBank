using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ButtzxBank
{
    public class m_cHmacAuthUtil
    {
        //构造请求
        public static HttpWebRequest hmacAuth(string url, string body)
        {
            HttpWebRequest post;
            if (m_cConfigConstants.APP_URL.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((a, b, c, d) => { return true; });
                post = WebRequest.Create(url) as HttpWebRequest;
                post.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                post = WebRequest.Create(url) as HttpWebRequest;
            }

            post.Method = "POST"; //提交方式
            post.ContentType = "application/json"; //内容类型
            post.Accept = "*/*";
            post.Timeout = 15000;
            post.AllowAutoRedirect = false;

            StreamWriter requestStream = null;

            try
            {
                requestStream = new StreamWriter(post.GetRequestStream());
                requestStream.Write(body);
                requestStream.Close();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                throw ex;
            }

            return post;
        }

        //生成唯一ID
        public static string m_fUUID()
        {
            string uuid = Guid.NewGuid().ToString().Replace("-", "");
            long timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
            return $"{uuid}{timestamp}";
        }

        //GMT时间
        private static string m_fDateGMT()
        {
            return DateTime.Now.AddHours(-8).ToString("r");
        }

        public static string m_fDateTime()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        //生成SHA256摘要
        private static string sha256Digest(string body)
        {
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                byte[] SHA256Data = Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(body);
                byte[] by = sha256.ComputeHash(SHA256Data);
                return Convert.ToBase64String(by);
            }
        }

        //生成hmacSHA256摘要
        private static string hmacSHA256(string message, string secret)
        {
            using (var hmacsha256 = new HMACSHA256(Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(secret)))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(message));
                return Convert.ToBase64String(hashmessage);
            }
        }
    }
}