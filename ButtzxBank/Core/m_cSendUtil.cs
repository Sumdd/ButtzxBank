using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace ButtzxBank
{
    public class m_cSendUtil
    {
        //封装发送
        public static Dictionary<string, object> send(Dictionary<string, object> bizData, string interfaceId, HttpRequestBase request, ref string retCode, ref string retMsg, bool m_bNullIsNoData = true, bool m_bUseNoData = false)
        {
            if (bizData == null)
                throw new ArgumentNullException("bizData");

            #region ***公共信息加载
            ///客户端IP
            bizData.Add(m_cConfigConstants.CLIENTIP, m_cCore.ToEncodingString(request.UserHostAddress));
            ///客户端浏览器信息
            string UserAgent = request.UserAgent;
            if (string.IsNullOrWhiteSpace(UserAgent)) UserAgent = "VS";
            bizData.Add(m_cConfigConstants.USERAGENT, m_cCore.ToEncodingString(UserAgent));
            ///客户端设备类型
            bizData.Add(m_cConfigConstants.DEVICETYPE, request.Browser.IsMobileDevice ? "1" : "3");
            ///设备唯一标识,N
            ///GPS位置信息,N
            #endregion

            //签名排序字典
            SortedDictionary<string, string> signData = new SortedDictionary<string, string>();

            string encryptStr = null;
            //加密域加密
            if (bizData.ContainsKey(m_cConfigConstants.DATA))
            {
                encryptStr = m_cSendUtil.encryptInfoProccess(bizData[m_cConfigConstants.DATA]);
                bizData.Remove(m_cConfigConstants.DATA);
                signData.Add(m_cConfigConstants.DATA, encryptStr);
            }

            ///厂商appId
            signData.Add(m_cConfigConstants.APPID, m_cConfigConstants.APP_ID);
            bizData.Add(m_cConfigConstants.APPID, m_cConfigConstants.APP_ID);
            ///时间戳
            string m_sDateTimeSp = m_cHmacAuthUtil.m_fDateTime();
            signData.Add(m_cConfigConstants.TIMESTAMP, m_sDateTimeSp);
            bizData.Add(m_cConfigConstants.TIMESTAMP, m_sDateTimeSp);

            //待签名字符串
            string signStr = m_cSendUtil.convertToSignData(signData);
            //公钥签名
            bizData.Add(m_cConfigConstants.SIGN, m_cRSA.getSign(signStr));
            //参数化
            string respEnBody = m_cSendUtil.sendPostReq(interfaceId, m_cSendUtil.convertToUrlParam(bizData), encryptStr);

            //解密
            string respBody = respEnBody;
            if (!(respEnBody.Contains("{") || respEnBody.Contains("[")))
            {
                respBody = m_cRSA.DecryptByPrivate(respEnBody);
            }
            else
            {
                ///如果有错误再显示报文原文
                Log.Instance.Debug($"最后的响应数据原文:{respEnBody}");
            }
            Log.Instance.Debug($"最后的响应数据:{respBody}");

            //验证
            JObject m_pJObject = JObject.Parse(respBody);

            ///优化空数据响应,去掉
            if (m_bUseNoData)
            {
                m_pJObject.Add("noData", false);
                if (m_pJObject.Property("data") != null)
                {
                    JToken m_pJToken = m_pJObject["data"];
                    switch (m_pJToken.Type)
                    {
                        case JTokenType.Array:
                            JArray m_pJArray = JArray.Parse(m_pJObject["data"].ToString());
                            if (m_pJArray.Count <= 0)
                            {
                                m_pJObject["noData"] = true;
                            }
                            break;
                        case JTokenType.Null:
                            if (m_bNullIsNoData) m_pJObject["noData"] = true;
                            break;
                        default:
                            m_pJObject["noData"] = false;
                            break;
                    }
                }
            }

            Dictionary<string, object> resultMap = m_pJObject.ToObject<Dictionary<string, object>>();
            respVerify(resultMap, ref retCode, ref retMsg, m_bUseNoData);

            return resultMap;
        }

        //签名
        private static string convertToSignData(SortedDictionary<string, string> signData)
        {
            if (signData == null)
                throw new ArgumentNullException("signData");

            if (signData.ContainsKey("sign"))
                signData.Remove("sign");

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in signData)
            {
                if (!string.IsNullOrEmpty(entry.Key) && !string.IsNullOrEmpty(entry.Value))
                    sb.Append("&").Append(entry.Key).Append("=").Append(entry.Value);
            }
            string data = sb.ToString();
            if (data.Length > 0) data = data.Substring(1);

            Log.Instance.Debug($"待签名字符串:{data}");
            return data;
        }

        private static string convertToUrlParam(Dictionary<string, object> signData)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, object> entry in signData)
            {
                sb.Append("&").Append(entry.Key).Append("=").Append(entry.Value);
            }
            string data = sb.ToString().Substring(1);
            return data;
        }

        //处理报文体
        public static string encryptInfoProccess(object encryptInfo)
        {
            return JsonConvert.SerializeObject(encryptInfo);
        }

        //验签
        public static void respVerify(Dictionary<string, object> resultMap, ref string retCode, ref string retMsg, bool m_bUseNoData)
        {
            //获取网关响应码
            retCode = resultMap["retCode"]?.ToString();
            //网关响应消息
            retMsg = resultMap["retMsg"]?.ToString();

            ///此处有个疑问,抛出异常前能否对ref参数赋值

            if (!"0".Equals(retCode))
            {
                if (string.IsNullOrWhiteSpace(retMsg)) retMsg = "非成功";
                throw new Exception(retMsg);
            }

            if (string.IsNullOrWhiteSpace(retMsg)) retMsg = "成功";

            ///优化空数据响应,去掉
            if (m_bUseNoData)
            {
                bool noData = Convert.ToBoolean(resultMap["noData"]);
                if (noData)
                {
                    throw new Exception("Err空数据");
                }
            }
        }

        //发送请求
        public static string sendPostReq(string interfaceId, string urlParam, string reqBody)
        {
            if (reqBody == null)
                throw new ArgumentNullException("reqBody");

            HttpWebRequest post = m_cHmacAuthUtil.hmacAuth(interfaceId, urlParam, reqBody);
            WebResponse response = null;
            string responseStr = null;

            try
            {
                //发送请求
                response = post.GetResponse();

                if (response != null)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING));
                    responseStr = reader.ReadToEnd();
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                throw ex;
            }

            return responseStr;
        }

        ///调用JAVA API 接口
        public static string sendGetReq(string reqHttp, string reqBody = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{reqHttp}{(reqBody == null ? null : $"?{reqBody}")}");
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

            WebResponse response = null;
            string responseStr = null;

            try
            {
                //发送请求
                response = request.GetResponse();

                if (response != null)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING));
                    responseStr = reader.ReadToEnd();
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                throw ex;
            }

            return responseStr;
        }
    }
}