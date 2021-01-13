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
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();
                encryptInfo.Add(m_cConfigConstants.DATA, m_cQuery.m_fGetQueryString(m_lQueryList, "usersJSONStr"));
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                SortedDictionary<string, string> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request);

                JObject m_pData = JObject.Parse(resultMap["data"]);
                data = m_pData["userToken"].ToString();
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

                //1、接口编号
                string interfaceId = "/user/sync/token";
                //2、报文体内容
                Dictionary<string, string> encryptInfo = new Dictionary<string, string>();
                //3、其它可以直接获取的内容
                Dictionary<string, object> bizData = new Dictionary<string, object>();
                //4、引入报文体
                bizData.Add(m_cConfigConstants.DATA, encryptInfo);
                //5、发送处理对应处理请求
                SortedDictionary<string, string> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request);

                JObject m_pData = JObject.Parse(resultMap["data"]);
                data = m_pData["userToken"].ToString();
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
                SortedDictionary<string, string> resultMap = m_cSendUtil.send(bizData, interfaceId, this.Request);

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
                data = data
            });
        }

        private JsonResult eJson()
        {
            return Json(new
            {
                status = 1,
                msg = msg,
                count = 0,
                data = new { }
            });
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

                //删除一个session
                //Session["UserName"] = null;
                //Session.Remove("UserName");
            }
            catch (Exception)
            {
                throw;
            }
            return View("V_EXG0200001");

        }
        #endregion
    }
}