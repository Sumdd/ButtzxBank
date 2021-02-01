using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ButtzxBank.Controllers
{
    public class HomeController : Controller
    {
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
                Dictionary<string, object> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request, ref retCode, ref retMsg);

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