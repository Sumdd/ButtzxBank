using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace ButtzxBank.Controllers
{
    public class HomeController : Controller
    {
        ///每页最大条数
        private const int m_uPageSize = 50;

        ///缓存
        private HttpRequestBase m_pRequest;

        private int status;
        private string msg;
        private int count;
        private object data;
        /// <summary>
        /// 续传标识,可能用不到,但这里还是进行兼容
        /// </summary>
        private string moreInd = "N";
        private string retCode;
        private string retMsg;
        private string retTip;

        public ActionResult Index()
        {
            return View();
        }

        #region ***/user/sync/info 用户基本数据同步
        public ActionResult v_user_sync_info(string queryString)
        {
            ViewBag.Title = "/user/sync/info 用户基本数据同步";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_user_sync_info(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/user/sync/info";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                string usersJSONStr = m_cQuery.m_fGetQueryString(m_lQueryList, "usersJSONStr");
                if (string.IsNullOrWhiteSpace(usersJSONStr))
                    throw new ArgumentNullException("users");

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                encryptInfo.Add("users", JsonConvert.DeserializeObject<List<object>>(usersJSONStr));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                JObject m_pData = JObject.Parse(resultMap["data"]?.ToString());
                msg = $"用户令牌数据同步成功,更新数据量{m_pData["count"]}";
                data = m_pData["count"];

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/user/sync/token 用户令牌数据同步
        public ActionResult v_user_sync_token(string queryString)
        {
            ViewBag.Title = "/user/sync/token 用户令牌数据同步";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_user_sync_token(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);

                ///appId
                string appId = m_cQuery.m_fGetQueryString(m_lQueryList, "appId");
                if (!string.IsNullOrWhiteSpace(appId) && appId != m_cConfigConstants.APP_ID)
                    throw new Exception("appId不正确");
                ///查询原因
                string reason = m_cQuery.m_fGetQueryString(m_lQueryList, "reason");
                if (!string.IsNullOrWhiteSpace(reason))
                    Log.Instance.Debug($"用户令牌数据同步:{reason}");

                ///查询模式,空则为接口查询
                string searchMode = m_cQuery.m_fGetQueryString(m_lQueryList, "searchMode");
                if (string.IsNullOrWhiteSpace(searchMode)) searchMode = "2";

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                switch (searchMode)
                {
                    case "1":
                        {
                            #region ***缓存
                            string m_sToken = m_cCore.m_fReadTxtToToken(writeLog);

                            if (m_sToken == null)
                                throw new Exception("Err无缓存");

                            List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(m_sToken);
                            msg = "从缓存获取Token成功";
                            count = m_pData.Count;
                            data = m_pData;
                            retCode = "0";
                            retMsg = msg;

                            return rJson();
                            #endregion
                        }
                    default:
                        {
                            #region ***接口
                            //1、接口编号
                            string interfaceId = "/user/sync/token";
                            //2、报文体内容
                            Dictionary<string, string> encryptInfo = new Dictionary<string, string>();
                            //3、其它可以直接获取的内容
                            Dictionary<string, object> bizData = new Dictionary<string, object>();
                            //4、引入报文体
                            bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                            //5、发送处理对应处理请求
                            Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg, true, false, writeLog);

                            string m_sToken = resultMap["data"]?.ToString();

                            ///写入文件
                            m_cCore.m_fWriteBase64TokenToTxt(m_sToken);

                            List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(m_sToken);
                            msg = resultMap["retMsg"]?.ToString();
                            count = m_pData.Count;
                            data = m_pData;

                            return rJson();
                            #endregion
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/casepool/list 委外案件案池信息
        public ActionResult v_casepool_list(string queryString)
        {
            ViewBag.Title = "/casepool/list 委外案件案池信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }

        public JsonResult f_casepool_list(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/casepool/list";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();
                string rrn = m_cQuery.m_fGetQueryString(m_lQueryList, "rrn");
                if (string.IsNullOrWhiteSpace(rrn)) rrn = "1";

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                encryptInfo.Add("rrn", rrn);
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg, true, false, writeLog);

                count = Convert.ToInt32(resultMap["total"]?.ToString());

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = m_pData.Count;
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/case/info 委外案件基本信息
        public ActionResult v_case_info(string queryString)
        {
            ViewBag.Title = "/case/info 委外案件基本信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_case_info(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/case/info";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///访问标志
                encryptInfo.Add("visitFlag", m_cQuery.m_fGetQueryString(m_lQueryList, "visitFlag"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg, true, false, writeLog);

                ///Object转List
                List<Dictionary<string, object>> m_pData = new List<Dictionary<string, object>>();
                m_pData.Add(JsonConvert.DeserializeObject<Dictionary<string, object>>(resultMap["data"]?.ToString()));
                msg = resultMap["retMsg"]?.ToString();
                count = m_pData.Count;
                data = m_pData;

                ///提示信息
                retTip = "Object转List";

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/acct/list 委外案件账户信息
        public ActionResult v_acct_list(string queryString)
        {
            ViewBag.Title = "/acct/list 委外案件账户信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_acct_list(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/acct/list";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///账号
                encryptInfo.Add("acctId", m_cQuery.m_fGetQueryString(m_lQueryList, "acctId"));
                ///起始页码
                encryptInfo.Add("start", m_cQuery.m_fGetQueryString(m_lQueryList, "start"));
                ///每页条数
                encryptInfo.Add("limit", m_cQuery.m_fGetQueryString(m_lQueryList, "limit"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = Convert.ToInt32(resultMap["total"]?.ToString());
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/cust/maininfo 委外案件客户信息
        public ActionResult v_cust_maininfo(string queryString)
        {
            ViewBag.Title = "/cust/maininfo 委外案件客户信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_cust_maininfo(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/cust/maininfo";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg, true, false, writeLog);

                ///Object转List
                List<Dictionary<string, object>> m_pData = new List<Dictionary<string, object>>();
                Dictionary<string, object> m_pDataDef0 = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultMap["data"]?.ToString());

                ///有些字段需解密,需在此处解密字段
                Dictionary<string, object> m_pData0 = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> item in m_pDataDef0)
                {
                    if (item.Key.EndsWith("RSA"))
                    {
                        string m_sRSAStr = item.Value?.ToString();
                        m_pData0.Add(item.Key, m_sRSAStr);
                        m_pData0.Add($"{item.Key}D", m_cRSA.DecryptByPrivate(item.Value?.ToString()));
                    }
                    else
                        m_pData0.Add(item.Key, item.Value);
                }
                m_pData.Add(m_pData0);

                msg = resultMap["retMsg"]?.ToString();
                count = m_pData.Count;
                data = m_pData;

                ///提示信息
                retTip = "Object转List,RSA转RSAD";

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/cust/maininfo/addition 委外案件客户邮寄积分信息
        public ActionResult v_cust_maininfo_addition(string queryString)
        {
            ViewBag.Title = "/cust/maininfo/addition 委外案件客户邮寄积分信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_cust_maininfo_addition(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/cust/maininfo/addition";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///起始页码
                encryptInfo.Add("start", m_cQuery.m_fGetQueryString(m_lQueryList, "start"));
                ///每页条数
                encryptInfo.Add("limit", m_cQuery.m_fGetQueryString(m_lQueryList, "limit"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg, true, false, writeLog);

                ///有些字段需解密,需在此处解密字段
                List<Dictionary<string, object>> m_pData = new List<Dictionary<string, object>>();
                List<Dictionary<string, object>> m_pDataDef = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
                foreach (Dictionary<string, object> item1 in m_pDataDef)
                {
                    Dictionary<string, object> m_pDataN = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, object> item2 in item1)
                    {
                        if (item2.Key.EndsWith("RSA"))
                        {
                            string m_sRSAStr = item2.Value?.ToString();
                            m_pDataN.Add(item2.Key, m_sRSAStr);
                            m_pDataN.Add($"{item2.Key}D", m_cRSA.DecryptByPrivate(item2.Value?.ToString()));
                        }
                        else
                            m_pDataN.Add(item2.Key, item2.Value);
                    }
                    m_pData.Add(m_pDataN);
                }

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = Convert.ToInt32(resultMap["total"]?.ToString());
                data = m_pData;

                ///提示信息
                retTip = "RSA转RSAD";

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/cust/maininfo/phone/list 委外案件客户联系方式
        public ActionResult v_cust_maininfo_phone_list(string queryString)
        {
            ViewBag.Title = "/cust/maininfo/phone/list 委外案件客户联系方式";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_cust_maininfo_phone_list(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/cust/maininfo/phone/list";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///起始页码
                encryptInfo.Add("start", m_cQuery.m_fGetQueryString(m_lQueryList, "start"));
                ///每页条数
                encryptInfo.Add("limit", m_cQuery.m_fGetQueryString(m_lQueryList, "limit"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg, true, false, writeLog);

                ///有些字段需解密,需在此处解密字段
                List<Dictionary<string, object>> m_pData = new List<Dictionary<string, object>>();
                List<Dictionary<string, object>> m_pDataDef = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
                foreach (Dictionary<string, object> item1 in m_pDataDef)
                {
                    Dictionary<string, object> m_pDataN = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, object> item2 in item1)
                    {
                        if (item2.Key.EndsWith("RSA"))
                        {
                            string m_sRSAStr = item2.Value?.ToString();
                            m_pDataN.Add(item2.Key, m_sRSAStr);
                            m_pDataN.Add($"{item2.Key}D", m_cRSA.DecryptByPrivate(item2.Value?.ToString()));
                        }
                        else
                            m_pDataN.Add(item2.Key, item2.Value);
                    }
                    m_pData.Add(m_pDataN);
                }

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = Convert.ToInt32(resultMap["total"]?.ToString());
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/cust/maininfo/address/list 委外案件客户联系地址
        public ActionResult v_cust_maininfo_address_list(string queryString)
        {
            ViewBag.Title = "/cust/maininfo/address/list 委外案件客户联系地址";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_cust_maininfo_address_list(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/cust/maininfo/address/list";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///起始页码
                encryptInfo.Add("start", m_cQuery.m_fGetQueryString(m_lQueryList, "start"));
                ///每页条数
                encryptInfo.Add("limit", m_cQuery.m_fGetQueryString(m_lQueryList, "limit"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = Convert.ToInt32(resultMap["total"]?.ToString());
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/case/aipa 委外案件客户实时交易信息
        public ActionResult v_case_aipa(string queryString)
        {
            ViewBag.Title = "/case/aipa 委外案件客户实时交易信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_case_aipa(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/case/aipa";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///卡号
                encryptInfo.Add("cardId", m_cQuery.m_fGetQueryString(m_lQueryList, "cardId"));
                ///起始页码
                encryptInfo.Add("start", m_cQuery.m_fGetQueryString(m_lQueryList, "start"));
                ///每页条数
                encryptInfo.Add("limit", m_cQuery.m_fGetQueryString(m_lQueryList, "limit"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = Convert.ToInt32(resultMap["total"]?.ToString());
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/case/realtime/repay 委外案件客户实时还款信息
        public ActionResult v_case_realtime_repay(string queryString)
        {
            ViewBag.Title = "/case/realtime/repay 委外案件客户实时还款信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_case_realtime_repay(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/case/realtime/repay";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///账户号
                encryptInfo.Add("acctId", m_cQuery.m_fGetQueryString(m_lQueryList, "acctId"));
                ///起始页码
                encryptInfo.Add("start", m_cQuery.m_fGetQueryString(m_lQueryList, "start"));
                ///每页条数
                encryptInfo.Add("limit", m_cQuery.m_fGetQueryString(m_lQueryList, "limit"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = Convert.ToInt32(resultMap["total"]?.ToString());
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/case/etp 委外案件客户账单信息
        public ActionResult v_case_etp(string queryString)
        {
            ViewBag.Title = "/case/etp 委外案件客户账单信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_case_etp(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/case/etp";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///卡号
                encryptInfo.Add("cardId", m_cQuery.m_fGetQueryString(m_lQueryList, "cardId"));
                ///币种
                encryptInfo.Add("currency", m_cQuery.m_fGetQueryString(m_lQueryList, "currency"));
                ///交易起始日期
                string startDate = m_cQuery.m_fGetQueryString(m_lQueryList, "startDate");
                encryptInfo.Add("startDate", startDate.Replace("-", ""));
                ///交易结束日期
                string endDate = m_cQuery.m_fGetQueryString(m_lQueryList, "endDate");
                encryptInfo.Add("endDate", endDate.Replace("-", ""));
                ///起始页码
                encryptInfo.Add("start", m_cQuery.m_fGetQueryString(m_lQueryList, "start"));
                ///每页条数
                encryptInfo.Add("limit", m_cQuery.m_fGetQueryString(m_lQueryList, "limit"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = Convert.ToInt32(resultMap["total"]?.ToString());
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/coll/record/history 委外案件催记历史信息
        public ActionResult v_coll_record_history(string queryString)
        {
            ViewBag.Title = "/coll/record/history 委外案件催记历史信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_coll_record_history(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/coll/record/history";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///催记类型
                encryptInfo.Add("collType", m_cQuery.m_fGetQueryString(m_lQueryList, "collType"));
                ///起始页码
                encryptInfo.Add("start", m_cQuery.m_fGetQueryString(m_lQueryList, "start"));
                ///每页条数
                encryptInfo.Add("limit", m_cQuery.m_fGetQueryString(m_lQueryList, "limit"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///催记开始时间
                string startDate = m_cQuery.m_fGetQueryString(m_lQueryList, "startDate");
                encryptInfo.Add("startDate", startDate.Replace("-", ""));
                ///催记结束时间
                string endDate = m_cQuery.m_fGetQueryString(m_lQueryList, "endDate");
                encryptInfo.Add("endDate", endDate.Replace("-", ""));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());

                ///Y:有下一页 N:无下一页
                moreInd = resultMap["moreInd"]?.ToString();
                msg = resultMap["retMsg"]?.ToString();
                count = Convert.ToInt32(resultMap["total"]?.ToString());
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/action/submit 委外机构催记录入
        public ActionResult v_action_submit(string queryString)
        {
            ViewBag.Title = "/action/submit 委外机构催记录入";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_action_submit(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/action/submit";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///行动日期
                string actionDate = m_cQuery.m_fGetQueryString(m_lQueryList, "actionDate");
                actionDate = actionDate.Replace("-", "").Replace(" ", "").Replace(":", "");
                encryptInfo.Add("actionDate", actionDate);
                ///约会日期
                string appointDate = m_cQuery.m_fGetQueryString(m_lQueryList, "appointDate");
                if (!string.IsNullOrWhiteSpace(appointDate)) appointDate = appointDate.Replace("-", "").Replace(" ", "");
                encryptInfo.Add("appointDate", appointDate);
                ///约会时间
                string appointTime = m_cQuery.m_fGetQueryString(m_lQueryList, "appointTime");
                if (!string.IsNullOrWhiteSpace(appointTime))
                {
                    appointTime = appointTime.Replace(":", "");
                    if (appointTime.Length > 4) appointTime = appointTime.Substring(0, 4);
                }
                encryptInfo.Add("appointTime", appointTime);
                ///行动代码
                encryptInfo.Add("actionCode", m_cQuery.m_fGetQueryString(m_lQueryList, "actionCode"));
                ///账户号
                encryptInfo.Add("acctId", m_cQuery.m_fGetQueryString(m_lQueryList, "acctId"));
                ///承诺金额
                encryptInfo.Add("dueAmt", m_cQuery.m_fGetQueryString(m_lQueryList, "dueAmt"));
                ///备注
                encryptInfo.Add("comment", m_cQuery.m_fGetQueryString(m_lQueryList, "comment"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false, false, writeLog);

                msg = retMsg;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/cust/phone/update 委外案件客户联系方式修正
        public ActionResult v_cust_phone_update(string queryString)
        {
            ViewBag.Title = "/cust/phone/update 委外案件客户联系方式修正";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_cust_phone_update(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/cust/phone/update";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///电话类型
                encryptInfo.Add("phoneType", m_cQuery.m_fGetQueryString(m_lQueryList, "phoneType"));
                ///电话号码
                encryptInfo.Add("phone", m_cQuery.m_fGetQueryString(m_lQueryList, "phone"));
                ///关系类型
                encryptInfo.Add("relation", m_cQuery.m_fGetQueryString(m_lQueryList, "relation"));
                ///姓名
                encryptInfo.Add("name", m_cQuery.m_fGetQueryString(m_lQueryList, "name"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false, false, writeLog);

                msg = retMsg;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/cust/address/update 委外案件客户联系地址修正
        public ActionResult v_cust_address_update(string queryString)
        {
            ViewBag.Title = "/cust/address/update 委外案件客户联系地址修正";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_cust_address_update(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/cust/address/update";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///姓名
                encryptInfo.Add("name", m_cQuery.m_fGetQueryString(m_lQueryList, "name"));
                ///关系类型
                encryptInfo.Add("relation", m_cQuery.m_fGetQueryString(m_lQueryList, "relation"));
                ///地址类型
                encryptInfo.Add("addressType", m_cQuery.m_fGetQueryString(m_lQueryList, "addressType"));
                ///地址
                encryptInfo.Add("address", m_cQuery.m_fGetQueryString(m_lQueryList, "address"));

                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false, false, writeLog);

                msg = retMsg;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/visit/apply/decode 委外案件信息外访申请
        public ActionResult v_visit_apply_decode(string queryString)
        {
            ViewBag.Title = "/visit/apply/decode 委外案件信息外访申请";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_visit_apply_decode(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/visit/apply/decode";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///委外机构网点代码
                encryptInfo.Add("agentId", m_cQuery.m_fGetQueryString(m_lQueryList, "agentId"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///外访时间
                encryptInfo.Add("outTime", m_cQuery.m_fGetQueryString(m_lQueryList, "outTime"));
                ///地址类型
                encryptInfo.Add("decodeType", m_cQuery.m_fGetQueryString(m_lQueryList, "decodeType"));
                ///脱敏值
                encryptInfo.Add("desValue", m_cQuery.m_fGetQueryString(m_lQueryList, "desValue"));
                ///申请明文值ID
                encryptInfo.Add("applyId", m_cQuery.m_fGetQueryString(m_lQueryList, "applyId"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                ///Object转List
                List<Dictionary<string, object>> m_pData = new List<Dictionary<string, object>>();
                m_pData.Add(JsonConvert.DeserializeObject<Dictionary<string, object>>(resultMap["data"]?.ToString()));
                msg = retMsg;
                count = m_pData.Count;
                data = m_pData;

                ///提示信息
                retTip = "Object转List";

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/visit/apply/decode/delay 委外案件信息外访申请延期
        public ActionResult v_visit_apply_decode_delay(string queryString)
        {
            ViewBag.Title = "/visit/apply/decode/delay 委外案件信息外访申请延期";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_visit_apply_decode_delay(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/visit/apply/decode/delay";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///外访记录ID
                encryptInfo.Add("visitId", m_cQuery.m_fGetQueryString(m_lQueryList, "visitId"));
                ///外访时间
                encryptInfo.Add("outTime", m_cQuery.m_fGetQueryString(m_lQueryList, "outTime"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false, false, writeLog);

                msg = retMsg;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/visit/apply/list 委外案件外访申请审核信息
        public ActionResult v_visit_apply_list(string queryString)
        {
            ViewBag.Title = "/visit/apply/list 委外案件外访申请审核信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_visit_apply_list(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/visit/apply/list";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///状态
                encryptInfo.Add("status", m_cQuery.m_fGetQueryString(m_lQueryList, "status"));

                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
                msg = resultMap["retMsg"]?.ToString();
                count = m_pData.Count;
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/visit/apply/review 委外案件外访审核
        public ActionResult v_visit_apply_review(string queryString)
        {
            ViewBag.Title = "/visit/apply/review 委外案件外访审核";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_visit_apply_review(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/visit/apply/review";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///外访记录ID
                encryptInfo.Add("visitId", m_cQuery.m_fGetQueryString(m_lQueryList, "visitId"));
                ///审核结果
                encryptInfo.Add("result", m_cQuery.m_fGetQueryString(m_lQueryList, "result"));
                ///审核备注
                encryptInfo.Add("comment", m_cQuery.m_fGetQueryString(m_lQueryList, "comment"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false, false, writeLog);

                msg = retMsg;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/apply/decode 委外数据明文申请
        public ActionResult v_apply_decode(string queryString)
        {
            ViewBag.Title = "/apply/decode 委外数据明文申请";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_apply_decode(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/apply/decode";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///脱敏值
                encryptInfo.Add("desValue", m_cQuery.m_fGetQueryString(m_lQueryList, "desValue"));
                ///明文类型
                encryptInfo.Add("decodeType", m_cQuery.m_fGetQueryString(m_lQueryList, "decodeType"));
                ///申请明文值ID
                encryptInfo.Add("applyId", m_cQuery.m_fGetQueryString(m_lQueryList, "applyId"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                ///Object转List
                List<Dictionary<string, object>> m_pData = new List<Dictionary<string, object>>();
                m_pData.Add(JsonConvert.DeserializeObject<Dictionary<string, object>>(resultMap["data"]?.ToString()));
                msg = retMsg;
                count = m_pData.Count;
                data = m_pData;

                ///提示信息
                retTip = "Object转List";

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/apply/list 委外数据明文申请审核信息
        public ActionResult v_apply_list(string queryString)
        {
            ViewBag.Title = "/apply/list 委外数据明文申请审核信息";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_apply_list(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/apply/list";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///状态
                encryptInfo.Add("status", m_cQuery.m_fGetQueryString(m_lQueryList, "status"));

                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
                msg = resultMap["retMsg"]?.ToString();
                count = m_pData.Count;
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/apply/review 委外数据明文审核
        public ActionResult v_apply_review(string queryString)
        {
            ViewBag.Title = "/apply/review 委外数据明文审核";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_apply_review(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/apply/review";
                //2、报文体内容下
                Dictionary<string, object> encryptInfo = new Dictionary<string, object>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///待审核ID
                encryptInfo.Add("reviewId", m_cQuery.m_fGetQueryString(m_lQueryList, "reviewId"));
                ///审核结果
                encryptInfo.Add("result", m_cQuery.m_fGetQueryString(m_lQueryList, "result"));
                ///审核备注
                encryptInfo.Add("comment", m_cQuery.m_fGetQueryString(m_lQueryList, "comment"));

                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                msg = retMsg;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region ***/visit/record/list 委外外访信息查询
        public ActionResult v_visit_record_list(string queryString)
        {
            ViewBag.Title = "/visit/record/list 委外外访信息查询";
            ViewBag.queryString = m_cCore.ToEncodingString(queryString);
            return View();
        }
        public JsonResult f_visit_record_list(int page, int limit, string type, string eqli, string field, string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                //1、接口编号
                string interfaceId = "/visit/record/list";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();

                ///是否写非错误日志
                string writeLog = m_cQuery.m_fGetQueryString(m_lQueryList, "writeLog");
                if (string.IsNullOrWhiteSpace(writeLog)) writeLog = "1";

                ///外访开始时间
                encryptInfo.Add("outBeginTime", m_cQuery.m_fGetQueryString(m_lQueryList, "outBeginTime"));
                ///外访结束时间
                encryptInfo.Add("outEndTime", m_cQuery.m_fGetQueryString(m_lQueryList, "outEndTime"));
                ///委外机构网点代码
                encryptInfo.Add("agentId", m_cQuery.m_fGetQueryString(m_lQueryList, "agentId"));
                ///卡号
                encryptInfo.Add("cardNo", m_cQuery.m_fGetQueryString(m_lQueryList, "cardNo"));
                ///客户姓名
                encryptInfo.Add("custName", m_cQuery.m_fGetQueryString(m_lQueryList, "custName"));
                ///证件号码
                encryptInfo.Add("cid", m_cQuery.m_fGetQueryString(m_lQueryList, "cid"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                ///手机号码
                encryptInfo.Add("phone", m_cQuery.m_fGetQueryString(m_lQueryList, "phone"));
                ///外访人
                encryptInfo.Add("userId", m_cQuery.m_fGetQueryString(m_lQueryList, "userId"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, true, false, writeLog);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
                msg = resultMap["retMsg"]?.ToString();
                count = m_pData.Count;
                data = m_pData;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region +++casepool_list_t 委外案件案池信息拓展
        /// <summary>
        /// casepool_list_t接口锁
        /// </summary>
        private static object casepool_list_t_lock = new object();

        public ActionResult v_casepool_list_t(string queryString)
        {
            ViewBag.Title = "casepool_list_t 委外案件案池信息拓展";
            ViewBag.queryString = HttpUtility.UrlEncode(queryString);
            this.m_fGetThreadPoolState();
            return View();
        }

        public JsonResult f_casepool_list_t(string queryString)
        {
            try
            {
                HttpRequestBase thisRequest = this.Request;
                ///获取A、B、T各类Token
                string Token = null;
                int resultMode = 2;
                List<m_cQuery> m_lQueryList = this.m_fSetAll(thisRequest, queryString, ref Token, ref resultMode);

                ///以系统最后一条的rrn做开始,一页一条取得总条数,得到需要循环的页,下一页以该查询页的最后一个rrn做开始
                HomeController m_pHC = new HomeController();
                m_pHC.m_pRequest = thisRequest;
                JsonResult m_pJsonResult = m_pHC.f_casepool_list(1, 1, null, null, null, queryString);
                JObject m_pJObject = JObject.FromObject(m_pJsonResult.Data);

                #region ***起始rrn
                int m_uRrn = 0;
                string rrn = m_cQuery.m_fGetQueryString(m_lQueryList, "rrn");
                if (string.IsNullOrWhiteSpace(rrn))
                {
                    rrn = "1";
                    if (!int.TryParse(rrn, out m_uRrn)) Log.Instance.Debug($"查询起始RRN NO值有误,默认使用{m_uRrn}", LogTyper.ProLogger);
                }
                #endregion

                ///判断结果
                if (m_pJObject["status"].ToString().Equals("0"))
                {
                    ///总请求次数
                    int m_uCount = Convert.ToInt32(m_pJObject["count"]);
                    ///根据总条数计算线程请求总次数
                    int m_uLastPageSize = m_uCount % m_uPageSize;
                    int m_uResqPages = (m_uCount / m_uPageSize) + (m_uLastPageSize > 0 ? 1 : 0);

                    if (m_uResqPages > 0)
                    {
                        #region ***委外案件池信息
                        DataTable m_pCaseDT = new DataTable();
                        m_pCaseDT.Columns.Add("case_flag", typeof(string));
                        m_pCaseDT.Columns.Add("case_caseId", typeof(string));
                        m_pCaseDT.Columns.Add("case_rrn", typeof(string));
                        m_pCaseDT.Columns.Add("case_agentId", typeof(string));
                        m_pCaseDT.Columns.Add("case_branchId", typeof(string));
                        m_pCaseDT.Columns.Add("case_adjustAreaCode", typeof(string));
                        m_pCaseDT.Columns.Add("case_afterAreaCode", typeof(string));
                        m_pCaseDT.Columns.Add("case_batchNum", typeof(string));
                        m_pCaseDT.Columns.Add("case_custName", typeof(string));
                        m_pCaseDT.Columns.Add("case_cidDES", typeof(string));
                        m_pCaseDT.Columns.Add("case_age", typeof(string));
                        m_pCaseDT.Columns.Add("case_acctIdDES", typeof(string));
                        m_pCaseDT.Columns.Add("case_acctIdENC", typeof(string));
                        m_pCaseDT.Columns.Add("case_currency", typeof(string));
                        m_pCaseDT.Columns.Add("case_gender", typeof(string));
                        m_pCaseDT.Columns.Add("case_principalOpsAmt", typeof(string));
                        m_pCaseDT.Columns.Add("case_balanceOpsAmt", typeof(string));
                        m_pCaseDT.Columns.Add("case_lastBalanceOpsAmt", typeof(string));
                        m_pCaseDT.Columns.Add("case_monthBalanceAmt", typeof(string));
                        m_pCaseDT.Columns.Add("case_overPeriod", typeof(string));
                        m_pCaseDT.Columns.Add("case_targetPeriod", typeof(string));
                        m_pCaseDT.Columns.Add("case_caseType", typeof(string));
                        m_pCaseDT.Columns.Add("case_entrustStartDate", typeof(string));
                        m_pCaseDT.Columns.Add("case_entrustEndDate", typeof(string));
                        m_pCaseDT.Columns.Add("case_isSued", typeof(string));
                        DataTable[] m_lCaseDT = new DataTable[m_uResqPages];
                        #endregion

                        #region ***委外案件基本信息
                        DataTable m_pBaseDT = new DataTable();
                        m_pBaseDT.Columns.Add("base_caseId", typeof(string));
                        m_pBaseDT.Columns.Add("base_lstActionId", typeof(string));
                        m_pBaseDT.Columns.Add("base_lstActionName", typeof(string));
                        m_pBaseDT.Columns.Add("base_currUserId", typeof(string));
                        m_pBaseDT.Columns.Add("base_lstActionTime", typeof(string));
                        m_pBaseDT.Columns.Add("base_actToWorkDate", typeof(string));
                        m_pBaseDT.Columns.Add("base_actAppointTime", typeof(string));
                        m_pBaseDT.Columns.Add("base_balanceAmt", typeof(string));
                        DataTable[] m_lBaseDT = new DataTable[m_uResqPages];
                        #endregion

                        #region ***委外案件账户信息
                        DataTable m_pAcctDT = new DataTable();
                        m_pAcctDT.Columns.Add("acct_acctIdENC", typeof(string));
                        m_pAcctDT.Columns.Add("acct_acctIdDES", typeof(string));
                        m_pAcctDT.Columns.Add("acct_acctPdt", typeof(string));
                        m_pAcctDT.Columns.Add("acct_currency", typeof(string));
                        m_pAcctDT.Columns.Add("acct_caseId", typeof(string));
                        m_pAcctDT.Columns.Add("acct_rdCorCustNbr", typeof(string));
                        m_pAcctDT.Columns.Add("acct_rdCustNbr", typeof(string));
                        m_pAcctDT.Columns.Add("acct_lastPayMonth", typeof(string));
                        m_pAcctDT.Columns.Add("acct_entrustStartDate", typeof(string));
                        m_pAcctDT.Columns.Add("acct_entrustEndDate", typeof(string));
                        m_pAcctDT.Columns.Add("acct_cardId", typeof(string));
                        m_pAcctDT.Columns.Add("acct_balanceOpsAmt", typeof(string));
                        m_pAcctDT.Columns.Add("acct_principalOpsAmt", typeof(string));
                        m_pAcctDT.Columns.Add("acct_accAmt", typeof(string));
                        m_pAcctDT.Columns.Add("acct_overPeriod", typeof(string));
                        m_pAcctDT.Columns.Add("acct_outsourceTimes", typeof(string));
                        DataTable[] m_lAcctDT = new DataTable[m_uResqPages];
                        #endregion

                        #region ***委外案件客户信息
                        DataTable m_pCustDT = new DataTable();
                        m_pCustDT.Columns.Add("cust_caseId", typeof(string));
                        m_pCustDT.Columns.Add("cust_currUserId", typeof(string));
                        m_pCustDT.Columns.Add("cust_custName", typeof(string));
                        m_pCustDT.Columns.Add("cust_custename", typeof(string));
                        m_pCustDT.Columns.Add("cust_cidType", typeof(string));
                        m_pCustDT.Columns.Add("cust_cidDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_gender", typeof(string));
                        m_pCustDT.Columns.Add("cust_nation", typeof(string));
                        m_pCustDT.Columns.Add("cust_custmprov", typeof(string));
                        m_pCustDT.Columns.Add("cust_custmcity", typeof(string));
                        m_pCustDT.Columns.Add("cust_married", typeof(string));
                        m_pCustDT.Columns.Add("cust_companyName", typeof(string));
                        m_pCustDT.Columns.Add("cust_position", typeof(string));
                        m_pCustDT.Columns.Add("cust_workId", typeof(string));
                        m_pCustDT.Columns.Add("cust_mail", typeof(string));
                        m_pCustDT.Columns.Add("cust_cellphoneDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_cellphoneRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_cellphoneRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custmaddrDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custmzip", typeof(string));
                        m_pCustDT.Columns.Add("cust_custphoneDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custphoneRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custphoneRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custaddrDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custcity", typeof(string));
                        m_pCustDT.Columns.Add("cust_custprov", typeof(string));
                        m_pCustDT.Columns.Add("cust_custzip", typeof(string));
                        m_pCustDT.Columns.Add("cust_custemptelDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custemptelRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custemptelRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custempaDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custempaz", typeof(string));
                        m_pCustDT.Columns.Add("cust_custempctc", typeof(string));
                        m_pCustDT.Columns.Add("cust_custglnam", typeof(string));
                        m_pCustDT.Columns.Add("cust_custglrln", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgsex", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgemp", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgwrkidDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgwrkidRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgwrkidRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgphoneDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgphoneRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgphoneRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgemptlDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgemptlRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgemptlRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgoccDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgoccRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgoccRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgempaDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgcity", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgprov", typeof(string));
                        m_pCustDT.Columns.Add("cust_custgempaz", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfname", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfrln", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfmblpDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfmblpRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfmblpRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfphnoDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfphnoRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfphnoRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfofpnDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfofpnRSA", typeof(string));
                        m_pCustDT.Columns.Add("cust_custrfofpnRSAD", typeof(string));
                        m_pCustDT.Columns.Add("cust_custcname", typeof(string));
                        m_pCustDT.Columns.Add("cust_policeregAddrDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_serveAddrDES", typeof(string));
                        m_pCustDT.Columns.Add("cust_policestaAddrDES", typeof(string));
                        DataTable[] m_lCustDT = new DataTable[m_uResqPages];
                        #endregion

                        #region ***委外案件客户邮寄积分信息
                        DataTable m_pAddiDT = new DataTable();
                        m_pAddiDT.Columns.Add("addi_caseId", typeof(string));
                        m_pAddiDT.Columns.Add("addi_addressId", typeof(string));
                        m_pAddiDT.Columns.Add("addi_mailName", typeof(string));
                        m_pAddiDT.Columns.Add("addi_mailAddressDES", typeof(string));
                        m_pAddiDT.Columns.Add("addi_mailPhoneDES", typeof(string));
                        m_pAddiDT.Columns.Add("addi_mailPhoneRSA", typeof(string));
                        m_pAddiDT.Columns.Add("addi_mailPhoneRSAD", typeof(string));
                        m_pAddiDT.Columns.Add("addi_mailHomePhoneDES", typeof(string));
                        m_pAddiDT.Columns.Add("addi_mailHomePhoneRSA", typeof(string));
                        m_pAddiDT.Columns.Add("addi_mailHomePhoneRSAD", typeof(string));
                        m_pAddiDT.Columns.Add("addi_csgName", typeof(string));
                        m_pAddiDT.Columns.Add("addi_csgAddressDES", typeof(string));
                        m_pAddiDT.Columns.Add("addi_csgPhone1DES", typeof(string));
                        m_pAddiDT.Columns.Add("addi_csgPhone1RSA", typeof(string));
                        m_pAddiDT.Columns.Add("addi_csgPhone1RSAD", typeof(string));
                        m_pAddiDT.Columns.Add("addi_csgPhone2DES", typeof(string));
                        m_pAddiDT.Columns.Add("addi_csgPhone2RSA", typeof(string));
                        m_pAddiDT.Columns.Add("addi_csgPhone2RSAD", typeof(string));
                        DataTable[] m_lAddiDT = new DataTable[m_uResqPages];
                        #endregion

                        #region ***委外案件客户联系方式
                        DataTable m_pCntaDT = new DataTable();
                        m_pCntaDT.Columns.Add("cnta_caseId", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_phoneId", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_phoneType", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_relation", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_name", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_phoneDES", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_phoneRSA", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_phoneRSAD", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_status", typeof(string));
                        m_pCntaDT.Columns.Add("cnta_origin", typeof(string));
                        DataTable[] m_lCntaDT = new DataTable[m_uResqPages];
                        #endregion

                        #region ***委外案件客户联系地址
                        DataTable m_pAddrDT = new DataTable();
                        m_pAddrDT.Columns.Add("addr_caseId", typeof(string));
                        m_pAddrDT.Columns.Add("addr_addressId", typeof(string));
                        m_pAddrDT.Columns.Add("addr_name", typeof(string));
                        m_pAddrDT.Columns.Add("addr_relation", typeof(string));
                        m_pAddrDT.Columns.Add("addr_addressType", typeof(string));
                        m_pAddrDT.Columns.Add("addr_addressDES", typeof(string));
                        m_pAddrDT.Columns.Add("addr_origin", typeof(string));
                        DataTable[] m_lAddrDT = new DataTable[m_uResqPages];
                        #endregion

                        bool[] m_lStatus = new bool[m_uResqPages];
                        string[] m_lErrMsg = new string[m_uResqPages];

                        ///仅使用1个
                        ManualResetEvent[] m_lManualResetEvent = new ManualResetEvent[1];
                        m_lManualResetEvent[0] = new ManualResetEvent(false);
                        int m_uReset = m_uResqPages;

                        for (int i = 1; i <= m_uResqPages; i++)
                        {
                            ///请求页码缓存
                            int m_uResqPageIndex = i;
                            ///缓存下标
                            int m_uIndex = i - 1;

                            m_lCaseDT[m_uIndex] = m_pCaseDT.Clone();
                            m_lBaseDT[m_uIndex] = m_pBaseDT.Clone();
                            m_lAcctDT[m_uIndex] = m_pAcctDT.Clone();
                            m_lCustDT[m_uIndex] = m_pCustDT.Clone();
                            m_lAddiDT[m_uIndex] = m_pAddiDT.Clone();
                            m_lCntaDT[m_uIndex] = m_pCntaDT.Clone();
                            m_lAddrDT[m_uIndex] = m_pAddrDT.Clone();

                            ///此处不能使用线程池取数据,要求为单线程
                            {
                                ///打印下日志吧
                                Log.Instance.Debug($"第{m_uResqPageIndex}页开始", LogTyper.ProLogger);

                                ///设置遇到错误时最大请求次数
                                int m_uResqCountWhenErr = 3;
                                int m_uResqCount = 0;
                                bool m_bResqState = false;
                                ///兼容一下请求时错误
                                while (true)
                                {
                                    try
                                    {
                                        ///记录请求次数
                                        m_uResqCount++;

                                        ///请求对应页码的数据
                                        HomeController m_pHome0 = new HomeController();
                                        m_pHome0.m_pRequest = thisRequest;
                                        JsonResult m_pPageIndexJsonResult = m_pHome0.f_casepool_list(m_uResqPageIndex, m_uPageSize, null, null, null, $"{{\"rrn\":\"{m_uRrn}\"}}");
                                        JObject m_pPageIndexJObject = JObject.FromObject(m_pPageIndexJsonResult.Data);

                                        ///判断页码请求结果
                                        if (m_pPageIndexJObject["status"].ToString().Equals("0") && m_pPageIndexJObject["data"].Type == JTokenType.Array)
                                        {
                                            JArray m_pPageIndexJArray = JArray.FromObject(m_pPageIndexJObject["data"]);
                                            if (m_pPageIndexJArray != null && ((m_pPageIndexJArray.Count == m_uPageSize && m_uResqPageIndex < m_uResqPages) || (m_pPageIndexJArray.Count == m_uLastPageSize && m_uResqPageIndex == m_uResqPages)))
                                            {
                                                ///每页的其它数据使用线程池查询
                                                ThreadPool.QueueUserWorkItem((o) =>
                                                {
                                                    ///放入各页数据,将最后一条数据的rrn取出
                                                    foreach (JToken caseJT in m_pPageIndexJArray)
                                                    {
                                                        ///案件号
                                                        string caseId = caseJT["caseId"].ToString();

                                                        DataRow m_pCaseDR = m_lCaseDT[m_uIndex].NewRow();
                                                        foreach (DataColumn caseDC in m_pCaseDT.Columns)
                                                        {
                                                            m_pCaseDR[caseDC.ColumnName] = caseJT[caseDC.ColumnName.Replace("case_", "")]?.ToString();
                                                        }
                                                        m_lCaseDT[m_uIndex].Rows.Add(m_pCaseDR);

                                                        #region ***委外案件基本信息
                                                        {
                                                            #region ***局部变量
                                                            int _m_uResqCountWhenErr = 3;
                                                            int _m_uResqCount = 0;
                                                            bool _m_bResqState = false;
                                                            int _m_uPageIndex = 1;
                                                            bool _m_uMoreInd = false;
                                                            #endregion

                                                            while (false)
                                                            {
                                                                ///请求对应页码的数据
                                                                HomeController m_pHome = new HomeController();
                                                                m_pHome.m_pRequest = thisRequest;
                                                                JsonResult m_pJR = m_pHome.f_case_info(_m_uPageIndex, m_uPageSize, null, null, null, $"{{\"userToken\":\"{Token}\",\"caseId\":\"{caseId}\",\"visitFlag\":\"1\"}}");
                                                                JObject m_pJO = JObject.FromObject(m_pJR.Data);
                                                                if (m_pJO["status"].ToString().Equals("0") && m_pJO["data"].Type == JTokenType.Array)
                                                                {
                                                                    JArray m_pJA = JArray.FromObject(m_pJO["data"]);
                                                                    foreach (JToken jT in m_pJA)
                                                                    {
                                                                        DataRow m_pDR = m_lBaseDT[m_uIndex].NewRow();
                                                                        m_pDR["base_caseId"] = caseId;
                                                                        foreach (DataColumn dC in m_pBaseDT.Columns)
                                                                        {
                                                                            if (!dC.ColumnName.Equals("base_caseId")) m_pDR[dC.ColumnName] = jT[dC.ColumnName.Replace("base_", "")]?.ToString();
                                                                        }
                                                                        m_lBaseDT[m_uIndex].Rows.Add(m_pDR);
                                                                    }

                                                                    _m_uMoreInd = m_pJO["moreInd"]?.ToString() == "Y";

                                                                    _m_bResqState = true;
                                                                    _m_uResqCount = 0;

                                                                    ///继续下一页
                                                                    if (_m_uMoreInd)
                                                                    {
                                                                        _m_uPageIndex++;
                                                                        continue;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Log.Instance.Debug($"委外案件基本信息错误记录:{m_pJR.Data};案件号:{caseId}", LogTyper.ProLogger);
                                                                    _m_bResqState = false;
                                                                    _m_uResqCount++;
                                                                }

                                                                ///如果请求状态成功或者请求次数超过最大错误次数,退出
                                                                if (_m_bResqState || _m_uResqCount > _m_uResqCountWhenErr) break;
                                                            }
                                                        }
                                                        #endregion

                                                        #region ***委外案件账户信息
                                                        {
                                                            #region ***局部变量
                                                            int _m_uResqCountWhenErr = 3;
                                                            int _m_uResqCount = 0;
                                                            bool _m_bResqState = false;
                                                            int _m_uPageIndex = 1;
                                                            bool _m_uMoreInd = false;
                                                            #endregion

                                                            while (true)
                                                            {
                                                                ///请求对应页码的数据
                                                                HomeController m_pHome = new HomeController();
                                                                m_pHome.m_pRequest = thisRequest;
                                                                JsonResult m_pJR = m_pHome.f_acct_list(1, m_uPageSize, null, null, null, $"{{\"userToken\":\"{Token}\",\"caseId\":\"{caseId}\"}}");
                                                                JObject m_pJO = JObject.FromObject(m_pJR.Data);
                                                                if (m_pJO["status"].ToString().Equals("0") && m_pJO["data"].Type == JTokenType.Array)
                                                                {
                                                                    JArray m_pJA = JArray.FromObject(m_pJO["data"]);
                                                                    foreach (JToken jT in m_pJA)
                                                                    {
                                                                        DataRow m_pDR = m_lAcctDT[m_uIndex].NewRow();
                                                                        foreach (DataColumn dC in m_pAcctDT.Columns)
                                                                        {
                                                                            m_pDR[dC.ColumnName] = jT[dC.ColumnName.Replace("acct_", "")]?.ToString();
                                                                        }
                                                                        m_lAcctDT[m_uIndex].Rows.Add(m_pDR);
                                                                    }

                                                                    _m_uMoreInd = m_pJO["moreInd"]?.ToString() == "Y";

                                                                    _m_bResqState = true;
                                                                    _m_uResqCount = 0;

                                                                    ///继续下一页
                                                                    if (_m_uMoreInd)
                                                                    {
                                                                        _m_uPageIndex++;
                                                                        continue;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Log.Instance.Debug($"委外案件账户信息错误记录:{m_pJR.Data};案件号:{caseId}", LogTyper.ProLogger);
                                                                    _m_bResqState = false;
                                                                    _m_uResqCount++;
                                                                }

                                                                ///如果请求状态成功或者请求次数超过最大错误次数,退出
                                                                if (_m_bResqState || _m_uResqCount > _m_uResqCountWhenErr) break;
                                                            }
                                                        }
                                                        #endregion

                                                        #region ***委外案件客户信息
                                                        {
                                                            #region ***局部变量
                                                            int _m_uResqCountWhenErr = 3;
                                                            int _m_uResqCount = 0;
                                                            bool _m_bResqState = false;
                                                            int _m_uPageIndex = 1;
                                                            bool _m_uMoreInd = false;
                                                            #endregion

                                                            while (true)
                                                            {
                                                                ///请求对应页码的数据
                                                                HomeController m_pHome = new HomeController();
                                                                m_pHome.m_pRequest = thisRequest;
                                                                JsonResult m_pJR = m_pHome.f_cust_maininfo(_m_uPageIndex, m_uPageSize, null, null, null, $"{{\"userToken\":\"{Token}\",\"caseId\":\"{caseId}\"}}");
                                                                JObject m_pJO = JObject.FromObject(m_pJR.Data);
                                                                if (m_pJO["status"].ToString().Equals("0") && m_pJO["data"].Type == JTokenType.Array)
                                                                {
                                                                    JArray m_pJA = JArray.FromObject(m_pJO["data"]);
                                                                    foreach (JToken jT in m_pJA)
                                                                    {
                                                                        DataRow m_pDR = m_lCustDT[m_uIndex].NewRow();
                                                                        m_pDR["cust_caseId"] = caseId;
                                                                        foreach (DataColumn dC in m_pCustDT.Columns)
                                                                        {
                                                                            if (!dC.ColumnName.Equals("cust_caseId")) m_pDR[dC.ColumnName] = jT[dC.ColumnName.Replace("cust_", "")]?.ToString();
                                                                        }
                                                                        m_lCustDT[m_uIndex].Rows.Add(m_pDR);
                                                                    }

                                                                    _m_uMoreInd = m_pJO["moreInd"]?.ToString() == "Y";

                                                                    _m_bResqState = true;
                                                                    _m_uResqCount = 0;

                                                                    ///继续下一页
                                                                    if (_m_uMoreInd)
                                                                    {
                                                                        _m_uPageIndex++;
                                                                        continue;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Log.Instance.Debug($"委外案件客户信息错误记录:{m_pJR.Data};案件号:{caseId}", LogTyper.ProLogger);
                                                                    _m_bResqState = false;
                                                                    _m_uResqCount++;
                                                                }

                                                                ///如果请求状态成功或者请求次数超过最大错误次数,退出
                                                                if (_m_bResqState || _m_uResqCount > _m_uResqCountWhenErr) break;
                                                            }
                                                        }
                                                        #endregion

                                                        #region ***委外案件客户邮寄积分信息
                                                        {
                                                            #region ***局部变量
                                                            int _m_uResqCountWhenErr = 3;
                                                            int _m_uResqCount = 0;
                                                            bool _m_bResqState = false;
                                                            int _m_uPageIndex = 1;
                                                            bool _m_uMoreInd = false;
                                                            #endregion

                                                            while (true)
                                                            {
                                                                ///请求对应页码的数据
                                                                HomeController m_pHome = new HomeController();
                                                                m_pHome.m_pRequest = thisRequest;
                                                                JsonResult m_pJR = m_pHome.f_cust_maininfo_addition(_m_uPageIndex, m_uPageSize, null, null, null, $"{{\"userToken\":\"{Token}\",\"caseId\":\"{caseId}\"}}");
                                                                JObject m_pJO = JObject.FromObject(m_pJR.Data);
                                                                if (m_pJO["status"].ToString().Equals("0") && m_pJO["data"].Type == JTokenType.Array)
                                                                {
                                                                    JArray m_pJA = JArray.FromObject(m_pJO["data"]);
                                                                    foreach (JToken jT in m_pJA)
                                                                    {
                                                                        DataRow m_pDR = m_lAddiDT[m_uIndex].NewRow();
                                                                        m_pDR["addi_caseId"] = caseId;
                                                                        foreach (DataColumn dC in m_pAddiDT.Columns)
                                                                        {
                                                                            if (!dC.ColumnName.Equals("addi_caseId")) m_pDR[dC.ColumnName] = jT[dC.ColumnName.Replace("addi_", "")]?.ToString();
                                                                        }
                                                                        m_lAddiDT[m_uIndex].Rows.Add(m_pDR);
                                                                    }

                                                                    _m_uMoreInd = m_pJO["moreInd"]?.ToString() == "Y";

                                                                    _m_bResqState = true;
                                                                    _m_uResqCount = 0;

                                                                    ///继续下一页
                                                                    if (_m_uMoreInd)
                                                                    {
                                                                        _m_uPageIndex++;
                                                                        continue;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Log.Instance.Debug($"委外案件客户邮寄积分信息错误记录:{m_pJR.Data};案件号:{caseId}", LogTyper.ProLogger);
                                                                    _m_bResqState = false;
                                                                    _m_uResqCount++;
                                                                }

                                                                ///如果请求状态成功或者请求次数超过最大错误次数,退出
                                                                if (_m_bResqState || _m_uResqCount > _m_uResqCountWhenErr) break;
                                                            }
                                                        }
                                                        #endregion

                                                        #region ***委外案件客户联系方式
                                                        {
                                                            #region ***局部变量
                                                            int _m_uResqCountWhenErr = 3;
                                                            int _m_uResqCount = 0;
                                                            bool _m_bResqState = false;
                                                            int _m_uPageIndex = 1;
                                                            bool _m_uMoreInd = false;
                                                            #endregion

                                                            while (true)
                                                            {
                                                                ///请求对应页码的数据
                                                                HomeController m_pHome = new HomeController();
                                                                m_pHome.m_pRequest = thisRequest;
                                                                JsonResult m_pJR = m_pHome.f_cust_maininfo_phone_list(_m_uPageIndex, m_uPageSize, null, null, null, $"{{\"userToken\":\"{Token}\",\"caseId\":\"{caseId}\"}}");
                                                                JObject m_pJO = JObject.FromObject(m_pJR.Data);
                                                                if (m_pJO["status"].ToString().Equals("0") && m_pJO["data"].Type == JTokenType.Array)
                                                                {
                                                                    JArray m_pJA = JArray.FromObject(m_pJO["data"]);
                                                                    foreach (JToken jT in m_pJA)
                                                                    {
                                                                        DataRow m_pDR = m_lCntaDT[m_uIndex].NewRow();
                                                                        m_pDR["cnta_caseId"] = caseId;
                                                                        foreach (DataColumn dC in m_pCntaDT.Columns)
                                                                        {
                                                                            if (!dC.ColumnName.Equals("cnta_caseId")) m_pDR[dC.ColumnName] = jT[dC.ColumnName.Replace("cnta_", "")]?.ToString();
                                                                        }
                                                                        m_lCntaDT[m_uIndex].Rows.Add(m_pDR);
                                                                    }

                                                                    _m_uMoreInd = m_pJO["moreInd"]?.ToString() == "Y";

                                                                    _m_bResqState = true;
                                                                    _m_uResqCount = 0;

                                                                    ///继续下一页
                                                                    if (_m_uMoreInd)
                                                                    {
                                                                        _m_uPageIndex++;
                                                                        continue;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Log.Instance.Debug($"委外案件客户联系方式错误记录:{m_pJR.Data};案件号:{caseId}", LogTyper.ProLogger);
                                                                    _m_bResqState = false;
                                                                    _m_uResqCount++;
                                                                }

                                                                ///如果请求状态成功或者请求次数超过最大错误次数,退出
                                                                if (_m_bResqState || _m_uResqCount > _m_uResqCountWhenErr) break;
                                                            }
                                                        }
                                                        #endregion

                                                        #region ***委外案件客户联系地址
                                                        {
                                                            #region ***局部变量
                                                            int _m_uResqCountWhenErr = 3;
                                                            int _m_uResqCount = 0;
                                                            bool _m_bResqState = false;
                                                            int _m_uPageIndex = 1;
                                                            bool _m_uMoreInd = false;
                                                            #endregion

                                                            while (true)
                                                            {
                                                                ///请求对应页码的数据
                                                                HomeController m_pHome = new HomeController();
                                                                m_pHome.m_pRequest = thisRequest;
                                                                JsonResult m_pJR = m_pHome.f_cust_maininfo_address_list(_m_uPageIndex, m_uPageSize, null, null, null, $"{{\"userToken\":\"{Token}\",\"caseId\":\"{caseId}\"}}");
                                                                JObject m_pJO = JObject.FromObject(m_pJR.Data);
                                                                if (m_pJO["status"].ToString().Equals("0") && m_pJO["data"].Type == JTokenType.Array)
                                                                {
                                                                    JArray m_pJA = JArray.FromObject(m_pJO["data"]);
                                                                    foreach (JToken jT in m_pJA)
                                                                    {
                                                                        DataRow m_pDR = m_lAddrDT[m_uIndex].NewRow();
                                                                        m_pDR["addr_caseId"] = caseId;
                                                                        foreach (DataColumn dC in m_pAddrDT.Columns)
                                                                        {
                                                                            if (!dC.ColumnName.Equals("addr_caseId")) m_pDR[dC.ColumnName] = jT[dC.ColumnName.Replace("addr_", "")]?.ToString();
                                                                        }
                                                                        m_lAddrDT[m_uIndex].Rows.Add(m_pDR);
                                                                    }

                                                                    _m_uMoreInd = m_pJO["moreInd"]?.ToString() == "Y";

                                                                    _m_bResqState = true;
                                                                    _m_uResqCount = 0;

                                                                    ///继续下一页
                                                                    if (_m_uMoreInd)
                                                                    {
                                                                        _m_uPageIndex++;
                                                                        continue;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    Log.Instance.Debug($"委外案件客户联系地址错误记录:{m_pJR.Data};案件号:{caseId}", LogTyper.ProLogger);
                                                                    _m_bResqState = false;
                                                                    _m_uResqCount++;
                                                                }

                                                                ///如果请求状态成功或者请求次数超过最大错误次数,退出
                                                                if (_m_bResqState || _m_uResqCount > _m_uResqCountWhenErr) break;
                                                            }
                                                        }
                                                        #endregion
                                                    }

                                                    ///唤醒等待
                                                    if (Interlocked.Decrement(ref m_uReset) == 0) m_lManualResetEvent[0].Set();

                                                }, null);

                                                ///直接取得,但文档说D类型无此值,如果无值如何处理
                                                m_uRrn = Convert.ToInt32(m_pPageIndexJArray[m_pPageIndexJArray.Count - 1]["rrn"].ToString());

                                                m_lStatus[m_uIndex] = true;
                                                m_lErrMsg[m_uIndex] += $"请求次数:{m_uResqCount};成功";
                                                m_bResqState = true;
                                            }
                                            else
                                            {
                                                m_lStatus[m_uIndex] = false;
                                                string m_sErrMsg = $"请求次数:{m_uResqCount};第{m_uResqPageIndex}页的数据发生变更,应为{(m_uResqPageIndex == m_uResqPages ? m_uLastPageSize : m_uPageSize)}条";
                                                m_lErrMsg[m_uIndex] += m_sErrMsg;
                                                Log.Instance.Debug(m_sErrMsg, LogTyper.ProLogger);
                                            }
                                        }
                                        else
                                        {
                                            m_lStatus[m_uIndex] = false;
                                            string m_sErrMsg = $"请求次数:{m_uResqCount};第{m_uResqPageIndex}页的数据非~JTokenType.Array~类型";
                                            m_lErrMsg[m_uIndex] += m_sErrMsg;
                                            Log.Instance.Debug(m_sErrMsg, LogTyper.ProLogger);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        m_lStatus[m_uIndex] = false;
                                        string m_sErrMsg = $"请求次数:{m_uResqCount};第{m_uResqPageIndex}页的数据处理有误:{ex.Message}";
                                        m_lErrMsg[m_uIndex] += m_sErrMsg;
                                        Log.Instance.Debug(m_sErrMsg, LogTyper.ProLogger);
                                        Log.Instance.Debug(ex, LogTyper.ProLogger);
                                    }

                                    if (m_bResqState || m_uResqCount > m_uResqCountWhenErr) break;
                                }

                                ///打印下日志吧
                                Log.Instance.Debug($"第{m_uResqPageIndex}页结束", LogTyper.ProLogger);
                            }
                        }

                        ///等待
                        WaitHandle.WaitAll(m_lManualResetEvent);
                        Log.Instance.Debug($"页码共{m_uResqPages},全部完成", LogTyper.ProLogger);

                        DataSet m_pDataSet = new DataSet();

                        DataTable m_pCaseSheet = m_pCaseDT.Clone();
                        m_pCaseSheet.TableName = "委外案件池信息";
                        m_pDataSet.Tables.Add(m_pCaseSheet);
                        DataTable m_pBaseSheet = m_pBaseDT.Clone();
                        m_pBaseSheet.TableName = "委外案件基本信息";
                        m_pDataSet.Tables.Add(m_pBaseSheet);
                        DataTable m_pAcctSheet = m_pAcctDT.Clone();
                        m_pAcctSheet.TableName = "委外案件账户信息";
                        m_pDataSet.Tables.Add(m_pAcctSheet);
                        DataTable m_pCustSheet = m_pCustDT.Clone();
                        m_pCustSheet.TableName = "委外案件客户信息";
                        m_pDataSet.Tables.Add(m_pCustSheet);
                        DataTable m_pAddiSheet = m_pAddiDT.Clone();
                        m_pAddiSheet.TableName = "委外案件客户邮寄积分信息";
                        m_pDataSet.Tables.Add(m_pAddiSheet);
                        DataTable m_pCntaSheet = m_pCntaDT.Clone();
                        m_pCntaSheet.TableName = "委外案件客户联系方式";
                        m_pDataSet.Tables.Add(m_pCntaSheet);
                        DataTable m_pAddrSheet = m_pAddrDT.Clone();
                        m_pAddrSheet.TableName = "委外案件客户联系地址";
                        m_pDataSet.Tables.Add(m_pAddrSheet);

                        ///不考虑大数据量的情况,导出缓存
                        DataTable m_pMsgData = new DataTable();
                        m_pMsgData.TableName = "委案案件导出详情";
                        m_pMsgData.Columns.Add("页码", typeof(string));
                        m_pMsgData.Columns.Add("结果", typeof(string));
                        m_pDataSet.Tables.Add(m_pMsgData);

                        for (int i = 0; i < m_uResqPages; i++)
                        {
                            m_pCaseSheet.Merge(m_lCaseDT[i]);
                            m_pBaseSheet.Merge(m_lBaseDT[i]);
                            m_pAcctSheet.Merge(m_lAcctDT[i]);
                            m_pCustSheet.Merge(m_lCustDT[i]);
                            m_pAddiSheet.Merge(m_lAddiDT[i]);
                            m_pCntaSheet.Merge(m_lCntaDT[i]);
                            m_pAddrSheet.Merge(m_lAddrDT[i]);

                            DataRow m_pDataRow = m_pMsgData.NewRow();
                            m_pDataRow["页码"] = m_lStatus[i];
                            m_pDataRow["结果"] = m_lErrMsg[i];
                            m_pMsgData.Rows.Add(m_pDataRow);
                        }

                        Log.Instance.Debug($"委外案件池信息:{m_pCaseSheet.Rows.Count}条", LogTyper.ProLogger);

                        ///命名
                        DateTime m_dtNow = DateTime.Now;
                        string m_sUUID = Guid.NewGuid().ToString();

                        ///多数据库合并输出
                        string m_sFilePath = $"~/Output/{m_dtNow.ToString("yyyy/yyyyMM/yyyyMMdd/委外案件信息_yyyyMMddHHmmssfffffff")}_{m_sUUID}.xlsx";

                        ///入系统测试
                        bool m_bImport = m_cSQL.m_fImportCase60(m_pDataSet);

                        ///结果模式
                        switch (resultMode)
                        {
                            case 1:
                                data = m_cJSON.m_fDataTableToIList(m_pCaseSheet);
                                break;
                            case 2:
                                m_cExcel.m_fExport(m_sFilePath, m_pDataSet);
                                break;
                            case 3:
                                data = m_cJSON.m_fDataTableToIList(m_pCaseSheet);
                                m_cExcel.m_fExport(m_sFilePath, m_pDataSet);
                                break;
                            default:
                                break;
                        }

                        msg = "成功";
                    }
                    return rJson();
                }
                else msg = m_pJObject["msg"].ToString();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex, LogTyper.ProLogger);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region +++设置Token,设置线程池配置
        private List<m_cQuery> m_fSetAll(HttpRequestBase thisRequest, string queryString, ref string Token, ref int resultMode)
        {
            List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);

            ///工作线程数区间
            int MinWorkthread = int.Parse(m_cQuery.m_fGetQueryString(m_lQueryList, "MinWorkthread"));
            int MaxWorkthread = int.Parse(m_cQuery.m_fGetQueryString(m_lQueryList, "MaxWorkthread"));
            int TheWorkthread = int.Parse(m_cQuery.m_fGetQueryString(m_lQueryList, "TheWorkthread"));
            int workthread = 0;
            string m_sWorkthread = m_cQuery.m_fGetQueryString(m_lQueryList, "workthread");
            if (!string.IsNullOrWhiteSpace(m_sWorkthread) && int.TryParse(m_sWorkthread, out workthread))
            {
                if (workthread < MinWorkthread || workthread > MaxWorkthread) throw new ArgumentException($"工作线程数区间[{MinWorkthread},{MaxWorkthread}]");
                ///I/O线程数区间
                int MinIOthread = int.Parse(m_cQuery.m_fGetQueryString(m_lQueryList, "MinIOthread"));
                int MaxIOthread = int.Parse(m_cQuery.m_fGetQueryString(m_lQueryList, "MaxIOthread"));
                int TheIOthread = int.Parse(m_cQuery.m_fGetQueryString(m_lQueryList, "TheIOthread"));
                int iothread = 0;
                string m_sIothread = m_cQuery.m_fGetQueryString(m_lQueryList, "iothread");
                if (!string.IsNullOrWhiteSpace(m_sIothread) && int.TryParse(m_sIothread, out iothread))
                {
                    if (iothread < MinIOthread || iothread > MaxIOthread) throw new ArgumentException($"I/O线程数区间[{MinIOthread},{MaxIOthread}]");
                    if (workthread != MaxWorkthread || iothread != MaxIOthread)
                    {
                        ///设置线程数目
                        bool set = ThreadPool.SetMaxThreads(workthread, iothread);
                        Log.Instance.Debug($"设置线程池最大线程[{workthread},{iothread}]{(set ? "成功" : "失败")}");
                    }
                }
            }

            ///结果模式
            string _resultMode = m_cQuery.m_fGetQueryString(m_lQueryList, "resultMode");
            if (!string.IsNullOrWhiteSpace(_resultMode))
            {
                if (!int.TryParse(_resultMode, out resultMode))
                {
                    Log.Instance.Debug($"结果模式转换错误,默认Excel模式");
                }
            }

            ///可替换值
            string _Token = m_cQuery.m_fGetQueryString(m_lQueryList, "Token");
            if (!string.IsNullOrWhiteSpace(_Token)) Token = _Token;

            ///如果Token为空,先查询缓存,无缓存直接走接口即可
            if (string.IsNullOrWhiteSpace(Token))
            {
                HomeController m_pHome = new HomeController();
                m_pHome.m_pRequest = thisRequest;
                JsonResult m_pJsonResult;
                if (!string.IsNullOrWhiteSpace(m_cCore.m_fReadTxtToToken()))
                    m_pJsonResult = m_pHome.f_user_sync_token($"{{\"searchMode\":\"1\"}}");
                else
                    m_pJsonResult = m_pHome.f_user_sync_token($"{{\"searchMode\":\"2\"}}");
                JObject m_pJObject = JObject.FromObject(m_pJsonResult.Data);

                ///判断结果
                if (m_pJObject["status"].ToString().Equals("0") && m_pJObject["data"].Type == JTokenType.Array)
                {
                    Token = m_pJObject["data"][0]["userToken"].ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(Token))
                throw new Exception("无可用Token");

            Log.Instance.Debug($"Token:{Token}", LogTyper.ProLogger);

            return m_lQueryList;
        }
        #endregion

        #region +++默认显示线程池状态
        private void m_fGetThreadPoolState()
        {
            ///返回最大最小线程池
            string m_sThreadMsg = string.Empty;
            int workthread;
            int iothread;
            ThreadPool.GetMinThreads(out workthread, out iothread);
            m_sThreadMsg += $"最小工作线程:{workthread};最小I/O线程:{iothread};";
            ViewBag.MinWorkthread = workthread;
            ViewBag.MinIOthread = iothread;
            ThreadPool.GetMaxThreads(out workthread, out iothread);
            m_sThreadMsg += $"最大工作线程:{workthread};最大I/O线程:{iothread};";
            ViewBag.MaxWorkthread = workthread;
            ViewBag.MaxIOthread = iothread;
            ThreadPool.GetAvailableThreads(out workthread, out iothread);
            m_sThreadMsg += $"剩余工作线程:{workthread};剩余I/O线程:{iothread};";
            ViewBag.TheWorkthread = workthread;
            ViewBag.TheIOthread = iothread;
            ViewBag.ThreadMsg = m_sThreadMsg;
        }
        #endregion

        #region +++定时器激活网站接口
        public void BT_1REQ(string m_sRefresh)
        {
            Log.Instance.Debug($"[ButtBank][HomeController][BT_1REQ][激活]");

            if (!string.IsNullOrWhiteSpace(m_sRefresh))
            {
                Log.Instance.Debug($"[ButtBank][HomeController][BT_1REQ][缓存重载,优化中...]");
            }

            m_fResponse("+OK");
        }
        #endregion

        #region ***JSON字符串封装返回
        private JsonResult rJson()
        {
            return Json(new
            {
                status = status,
                msg = msg,
                count = count,
                data = data,
                moreInd = moreInd,
                retCode = retCode,
                retMsg = retMsg,
                retTip = retTip
            });
        }

        private JsonResult eJson()
        {
            string m_sRetCode = retCode;
            string m_sRetMsg = retMsg;

            ///延用999999
            if (string.IsNullOrWhiteSpace(m_sRetCode))
            {
                m_sRetCode = "Err999999";
            }
            else if ("0".Equals(m_sRetCode))
            {
                m_sRetCode = "Err000000";
            }

            ///提示文字
            if (string.IsNullOrWhiteSpace(m_sRetMsg))
            {
                m_sRetMsg = $"{msg}";
            }

            return Json(new
            {
                status = 1,
                msg = msg,
                count = 0,
                data = new { },
                retCode = m_sRetCode,
                retMsg = m_sRetMsg,
                retTip = retTip
            });
        }
        #endregion

        #region ***返回流转格式
        private void m_fResponse(string m_sResponse)
        {
            Response.Charset = m_cConfigConstants.SYSTEM_ENCODING;
            Response.ContentType = $"application/json;charset={m_cConfigConstants.SYSTEM_ENCODING}";
            Response.ContentEncoding = System.Text.Encoding.GetEncoding(m_cConfigConstants.SYSTEM_ENCODING);
            Response.Write(m_sResponse);
        }
        #endregion

        #region 添加session
        public JsonResult Add(string queryString)
        {
            try
            {
                List<m_cQuery> m_lQueryList = m_cQuery.m_fSetQueryList(queryString);
                string userToken = m_cQuery.m_fGetQueryString(m_lQueryList, "userToken");
                if (string.IsNullOrWhiteSpace(userToken))
                    throw new ArgumentNullException("userToken");

                data = userToken;
                msg = "添加session成功";
                Session["userToken"] = data;

                return rJson();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                msg = ex.Message;
            }
            return eJson();
        }
        #endregion

        #region 注销session
        public ViewResult Del()
        {
            try
            {
                //删除全部session
                Session.Clear();
                Session.Abandon();
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
                throw ex;
            }

            ViewBag.Title = "/user/sync/info 用户基本数据同步";
            return View("v_user_sync_token");
        }
        #endregion
    }
}