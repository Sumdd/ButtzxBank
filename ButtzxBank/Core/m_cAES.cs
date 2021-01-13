using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ButtzxBank
{
    public class m_cAES
    {
        /// <summary>  
        /// AES加密
        /// </summary>  
        /// <param name="encryptStr">明文</param>  
        /// <param name="key">密钥</param>  
        /// <returns></returns>
        public static string Encrypt(string encryptStr, string key, string iv)
        {
            var _aes = new AesCryptoServiceProvider();
            _aes.BlockSize = 128;
            _aes.KeySize = 256;
            _aes.Key = Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(key);
            _aes.IV = Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(iv);
            _aes.Padding = PaddingMode.PKCS7;
            _aes.Mode = CipherMode.CBC;

            var _crypto = _aes.CreateEncryptor(_aes.Key, _aes.IV);
            byte[] encrypted = _crypto.TransformFinalBlock(Encoding.UTF8.GetBytes(encryptStr), 0, Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(encryptStr).Length);

            _crypto.Dispose();

            return System.Convert.ToBase64String(encrypted);
        }

        /// <summary>  
        /// AES解密
        /// </summary>  
        /// <param name="decryptStr">密文</param>  
        /// <param name="key">密钥</param>  
        /// <returns></returns>  
        public static string Decrypt(string decryptStr, string key, string iv)
        {
            var _aes = new AesCryptoServiceProvider();
            _aes.BlockSize = 128;
            _aes.KeySize = 256;
            _aes.Key = Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(key);
            _aes.IV = Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetBytes(iv);
            _aes.Padding = PaddingMode.PKCS7;
            _aes.Mode = CipherMode.CBC;

            var _crypto = _aes.CreateDecryptor(_aes.Key, _aes.IV);
            byte[] decrypted = _crypto.TransformFinalBlock(
                System.Convert.FromBase64String(decryptStr), 0, System.Convert.FromBase64String(decryptStr).Length);
            _crypto.Dispose();
            return Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING).GetString(decrypted);
        }
    }
}