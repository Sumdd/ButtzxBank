using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ButtzxBank
{
    /// <summary>
    /// 中信对接
    /// </summary>
    public class m_cConfigConstants
    {
        /**
         * 系统编码: UTF-8
         */
        public static string SYSTEM_ENCODING = m_cSettings.m_dKeyValue["[SYSTEM_ENCODING]"];
        /**
         * 公钥
         */
        public static string PUBLIC_KEY = m_cSettings.m_dKeyValue["[PUBLIC_KEY]"];
        /**
         * 私钥
         */
        public static string PRIVATE_KEY = m_cSettings.m_dKeyValue["[PRIVATE_KEY]"];
        /**
        * appId
        */
        public static string APP_ID = m_cSettings.m_dKeyValue["[APP_ID]"];
        /**
        * 请求URL
        */
        public static string APP_URL = m_cSettings.m_dKeyValue["[APP_URL]"];
        /**
        * 厂商appId键名
        */
        public const string APPID = "appId";
        /**
        * 客户端IP
        */
        public const string CLIENTIP = "clientIp";
        /**
        * 客户端浏览器信息
        */
        public const string USERAGENT = "userAgent";
        /**
        * 设备类型
        * 默认3即可
        */
        public const string DEVICETYPE = "deviceType";
        /**
        * 设备唯一标识
        * N
        */
        public const string DEVICEID = "deviceId";
        /**
        * GPS位置信息
        * N
        */
        public const string GPSLOCATION = "gpsLocation";
        /**
        * 签名
        */
        public const string SIGN = "sign";
        /**
        * 请求时间戳
        */
        public const string TIMESTAMP = "timestamp";
        /**
        * 报文体
        */
        public const string DATA = "data";
    }
}