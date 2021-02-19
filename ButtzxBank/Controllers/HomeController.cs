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

                encryptInfo.Add("users", JsonConvert.DeserializeObject<List<object>>(usersJSONStr));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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

                switch (searchMode)
                {
                    case "1":
                        {
                            #region ***缓存
                            string m_sToken = m_cCore.m_fReadTxtToToken();

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
                            Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                encryptInfo.Add("rrn", rrn);
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request ?? this.m_pRequest, ref retCode, ref retMsg);

                count = Convert.ToInt32(resultMap["total"]?.ToString());

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
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
                ///用户令牌
                encryptInfo.Add("userToken", m_cQuery.m_fGetQueryString(m_lQueryList, "userToken"));
                ///案件号
                encryptInfo.Add("caseId", m_cQuery.m_fGetQueryString(m_lQueryList, "caseId"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

                List<Dictionary<string, object>> m_pData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(resultMap["data"]?.ToString());
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
                ///约会日期
                string appointTime = m_cQuery.m_fGetQueryString(m_lQueryList, "appointTime");
                if (!string.IsNullOrWhiteSpace(appointTime)) appointTime = appointTime.Replace(":", "");
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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false);

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
                ///状态
                encryptInfo.Add("status", m_cQuery.m_fGetQueryString(m_lQueryList, "status"));

                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg, false);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                ///状态
                encryptInfo.Add("status", m_cQuery.m_fGetQueryString(m_lQueryList, "status"));

                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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
                ///获取A、B、T各类Token
                string Token = null;
                int resultMode = 2;
                List<m_cQuery> m_lQueryList = this.m_fSetAll(queryString, ref Token, ref resultMode);

                ///以系统最后一条的rrn做开始,一页一条取得总条数,得到需要循环的页,下一页以该查询页的最后一个rrn做开始
                HomeController m_pHome = new HomeController();
                m_pHome.m_pRequest = this.Request;
                JsonResult m_pJsonResult = m_pHome.f_casepool_list(1, 1, null, null, null, queryString);
                JObject m_pJObject = JObject.FromObject(m_pJsonResult.Data);

                ///起始rrn
                int rrn = 0;

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
                        m_pCaseDT.Columns.Add("flag", typeof(string));
                        m_pCaseDT.Columns.Add("caseId", typeof(string));
                        m_pCaseDT.Columns.Add("rrn", typeof(string));
                        m_pCaseDT.Columns.Add("agentId", typeof(string));
                        m_pCaseDT.Columns.Add("branchId", typeof(string));
                        m_pCaseDT.Columns.Add("adjustAreaCode", typeof(string));
                        m_pCaseDT.Columns.Add("afterAreaCode", typeof(string));
                        m_pCaseDT.Columns.Add("batchNum", typeof(string));
                        m_pCaseDT.Columns.Add("custName", typeof(string));
                        m_pCaseDT.Columns.Add("cidDES", typeof(string));
                        m_pCaseDT.Columns.Add("age", typeof(string));
                        m_pCaseDT.Columns.Add("acctIdDES", typeof(string));
                        m_pCaseDT.Columns.Add("acctIdENC", typeof(string));
                        m_pCaseDT.Columns.Add("currency", typeof(string));
                        m_pCaseDT.Columns.Add("gender", typeof(string));
                        m_pCaseDT.Columns.Add("principalOpsAmt", typeof(string));
                        m_pCaseDT.Columns.Add("balanceOpsAmt", typeof(string));
                        m_pCaseDT.Columns.Add("lastBalanceOpsAmt", typeof(string));
                        m_pCaseDT.Columns.Add("monthBalanceAmt", typeof(string));
                        m_pCaseDT.Columns.Add("overPeriod", typeof(string));
                        m_pCaseDT.Columns.Add("targetPeriod", typeof(string));
                        m_pCaseDT.Columns.Add("caseType", typeof(string));
                        m_pCaseDT.Columns.Add("entrustStartDate", typeof(string));
                        m_pCaseDT.Columns.Add("entrustEndDate", typeof(string));
                        m_pCaseDT.Columns.Add("isSued", typeof(string));
                        DataTable[] m_lCaseDT = new DataTable[m_uResqPages];
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
                                        m_pHome0.m_pRequest = this.Request;
                                        JsonResult m_pPageIndexJsonResult = m_pHome0.f_casepool_list(m_uResqPageIndex, m_uPageSize, null, null, null, queryString);
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
                                                    ///放入各页数据
                                                    foreach (JToken caseJT in m_pPageIndexJArray)
                                                    {
                                                        DataRow m_pCaseDR = m_lCaseDT[m_uIndex].NewRow();
                                                        foreach (DataColumn caseDC in m_pCaseDT.Columns)
                                                        {
                                                            m_pCaseDR[caseDC.ColumnName] = caseJT[caseDC.ColumnName];
                                                        }
                                                        m_lCaseDT[m_uIndex].Rows.Add(m_pCaseDR);

                                                        #region ***委外案件基本信息

                                                        #endregion

                                                        #region ***委外案件账户信息

                                                        #endregion
                                                    }

                                                }, null);

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

                                ///唤醒等待
                                if (Interlocked.Decrement(ref m_uReset) == 0) m_lManualResetEvent[0].Set();

                            }
                        }

                        ///等待
                        WaitHandle.WaitAll(m_lManualResetEvent);
                        Log.Instance.Debug($"页码共{m_uResqPages},全部完成", LogTyper.ProLogger);

                        DataSet m_pDataSet = new DataSet();

                        DataTable m_pCaseSheet = m_pCaseDT.Clone();
                        m_pCaseSheet.TableName = "委外案件池信息";
                        m_pDataSet.Tables.Add(m_pCaseSheet);

                        ///不考虑大数据量的情况,导出缓存
                        DataTable m_pMsgData = new DataTable();
                        m_pMsgData.TableName = "委案案件导出详情";
                        m_pMsgData.Columns.Add("页码", typeof(string));
                        m_pMsgData.Columns.Add("结果", typeof(string));
                        m_pDataSet.Tables.Add(m_pMsgData);

                        for (int i = 0; i < m_uResqPages; i++)
                        {
                            m_pCaseSheet.Merge(m_lCaseDT[i]);

                            DataRow m_pDataRow = m_pMsgData.NewRow();
                            m_pDataRow["页码"] = m_lStatus[i];
                            m_pDataRow["结果"] = m_lErrMsg[i];
                            m_pMsgData.Rows.Add(m_pDataRow);
                        }

                        Log.Instance.Debug($"委外案件基本信息:{m_pCaseSheet.Rows.Count}条", LogTyper.ProLogger);

                        ///命名
                        DateTime m_dtNow = DateTime.Now;
                        string m_sUUID = Guid.NewGuid().ToString();

                        ///多数据库合并输出
                        string m_sFilePath = $"~/Output/{m_dtNow.ToString("yyyy/yyyyMM/yyyyMMdd/委外案件池信息_yyyyMMddHHmmssfffffff")}_{m_sUUID}.xlsx";

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
        private List<m_cQuery> m_fSetAll(string queryString, ref string Token, ref int resultMode)
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
                JsonResult m_pJsonResult;
                if (!string.IsNullOrWhiteSpace(m_cCore.m_fReadTxtToToken()))
                    m_pJsonResult = new HomeController().f_user_sync_token($"{{\"searchMode\":\"1\"}}");
                else
                    m_pJsonResult = new HomeController().f_user_sync_token($"{{\"searchMode\":\"2\"}}");
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