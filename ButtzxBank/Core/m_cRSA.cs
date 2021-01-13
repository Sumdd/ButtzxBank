using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using XC.RSAUtil;

namespace ButtzxBank
{
    public class m_cRSA
    {
        //SHA256
        private static string digest(string data)
        {
            ///无摘要
            return data.Replace("\n", "").Replace("\r", "").Replace("\t", "");

            using (SHA256Managed sha256 = new SHA256Managed())
            {
                byte[] SHA256Data = Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(data);
                byte[] by = sha256.ComputeHash(SHA256Data);
                return Hex.ToHexString(by).ToUpper();
            }
        }

        #region ***私钥分段加密,公钥分段解密,公钥分段加密,私钥分段解密

        /// <summary>
        /// 私钥分段加密
        /// </summary>
        [Obsolete]
        public static string PrivateKeyEncrypt(string data)
        {
            ///加载私钥
            RSACryptoServiceProvider privateRsa = new RSACryptoServiceProvider();
            RsaPkcs8Util m_pPrivateRsaPkcs8Util = new RsaPkcs8Util(Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING), null, m_cConfigConstants.PRIVATE_KEY);
            privateRsa.ImportParameters(m_pPrivateRsaPkcs8Util.PrivateRsa.ExportParameters(false));

            ///转换密钥
            AsymmetricCipherKeyPair keyPair = DotNetUtilities.GetKeyPair(privateRsa);
            ///使用RSA/ECB/PKCS1Padding格式
            ///第一个参数为true表示加密，为false表示解密；第二个参数表示密钥
            IBufferedCipher cipher = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");

            ///分段加密
            cipher.Init(true, keyPair.Private);
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(data);
            int bufferSize = (privateRsa.KeySize / 8) - 11;
            byte[] buffer = new byte[bufferSize];
            byte[] outBytes = null;
            using (MemoryStream input = new MemoryStream(dataToEncrypt))
            using (MemoryStream ouput = new MemoryStream())
            {
                while (true)
                {
                    int readLine = input.Read(buffer, 0, bufferSize);
                    if (readLine <= 0)
                    {
                        break;
                    }
                    byte[] temp = new byte[readLine];
                    Array.Copy(buffer, 0, temp, 0, readLine);
                    byte[] encrypt = cipher.DoFinal(temp);
                    ouput.Write(encrypt, 0, encrypt.Length);
                }
                outBytes = ouput.ToArray();
            }
            return Convert.ToBase64String(outBytes);
        }

        /// <summary>
        /// 公钥分段解密
        /// </summary>
        [Obsolete]
        public static string PublicKeyDecrypt(string xmlPublicKey, string data)
        {
            ///加载公钥
            RSACryptoServiceProvider publicRsa = new RSACryptoServiceProvider();
            RsaPkcs8Util m_pPublicRsaPkcs8Util = new RsaPkcs8Util(Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING), m_cConfigConstants.PUBLIC_KEY);
            publicRsa.ImportParameters(m_pPublicRsaPkcs8Util.PublicRsa.ExportParameters(false));
            RSAParameters rp = publicRsa.ExportParameters(false);

            ///转换密钥
            AsymmetricKeyParameter pbk = DotNetUtilities.GetRsaPublicKey(rp);
            ///第一个参数为true表示加密，为false表示解密；第二个参数表示密钥
            IBufferedCipher cipher = CipherUtilities.GetCipher("RSA/ECB/PKCS1Padding");
            cipher.Init(false, pbk);

            ///分段解密
            byte[] outBytes = null;
            byte[] dataToDecrypt = Convert.FromBase64String(data);
            int keySize = publicRsa.KeySize / 8;
            byte[] buffer = new byte[keySize];

            using (MemoryStream input = new MemoryStream(dataToDecrypt))
            using (MemoryStream output = new MemoryStream())
            {
                while (true)
                {
                    int readLine = input.Read(buffer, 0, keySize);
                    if (readLine <= 0)
                    {
                        break;
                    }
                    byte[] temp = new byte[readLine];
                    Array.Copy(buffer, 0, temp, 0, readLine);
                    byte[] decrypt = cipher.DoFinal(temp);
                    output.Write(decrypt, 0, decrypt.Length);
                }
                outBytes = output.ToArray();
            }
            return Encoding.UTF8.GetString(outBytes);
        }

        /// <summary>
        /// 公钥分段加密
        /// </summary>
        public static string EncrytByPublic(string data)
        {
            ///后续静态化
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            RsaPkcs8Util m_pPublicRsaPkcs8Util = new RsaPkcs8Util(Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING), m_cConfigConstants.PUBLIC_KEY);
            rsa.ImportParameters(m_pPublicRsaPkcs8Util.PublicRsa.ExportParameters(false));

            ///分段解密
            byte[] encryContent = null;
            int bufferSize = (rsa.KeySize / 8) - 11;
            byte[] buffer = new byte[bufferSize];
            using (MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            using (MemoryStream ouput = new MemoryStream())
            {
                while (true)
                {
                    int readLine = input.Read(buffer, 0, bufferSize);
                    if (readLine <= 0)
                    {
                        break;
                    }
                    byte[] temp = new byte[readLine];
                    Array.Copy(buffer, 0, temp, 0, readLine);
                    byte[] encrypt = rsa.Encrypt(temp, false);
                    ouput.Write(encrypt, 0, encrypt.Length);
                }
                encryContent = ouput.ToArray();
            }
            return Convert.ToBase64String(encryContent);
        }

        /// <summary>
        /// 私钥分段解密
        /// </summary>
        public static string DecryptByPrivate(string data)
        {
            ///后续静态化
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            RsaPkcs8Util m_pPrivateRsaPkcs8Util = new RsaPkcs8Util(Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING), null, m_cConfigConstants.PRIVATE_KEY);
            rsa.ImportParameters(m_pPrivateRsaPkcs8Util.PrivateRsa.ExportParameters(true));

            ///分段解密
            byte[] dencryContent = null;
            int keySize = rsa.KeySize / 8;
            byte[] buffer = new byte[keySize];
            using (MemoryStream input = new MemoryStream(Convert.FromBase64String(data)))
            using (MemoryStream output = new MemoryStream())
            {
                while (true)
                {
                    int readLine = input.Read(buffer, 0, keySize);
                    if (readLine <= 0)
                    {
                        break;
                    }
                    byte[] temp = new byte[readLine];
                    Array.Copy(buffer, 0, temp, 0, readLine);
                    byte[] decrypt = rsa.Decrypt(temp, false);
                    output.Write(decrypt, 0, decrypt.Length);
                }
                dencryContent = output.ToArray();
            }
            return Encoding.UTF8.GetString(dencryContent);
        }

        #endregion

        //签名
        public static string getSign(string data)
        {
            RsaPkcs8Util m_pPrivateRsaPkcs8Util = new RsaPkcs8Util(Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING), null, m_cConfigConstants.PRIVATE_KEY);
            string m_sPrivateSignBase64String = m_pPrivateRsaPkcs8Util.SignData(digest(data), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            Log.Instance.Debug($"字符串签名:{m_sPrivateSignBase64String}");
            return m_cCore.ToEncodingString(m_sPrivateSignBase64String);
        }

        //验签
        public static bool verifySign(string data, string sign)
        {
            try
            {
                RsaPkcs8Util m_pPublicRsaPkcs8Util = new RsaPkcs8Util(Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING), m_cConfigConstants.PUBLIC_KEY);
                return m_pPublicRsaPkcs8Util.VerifyData(digest(data), sign, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                return false;
            }
        }
    }
}