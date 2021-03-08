using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Quartz;
using Quartz.Impl;
using System.Data;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Web.Mvc;
using ButtzxBank.Controllers;
using System.Threading;

namespace ButtzxBank
{
    #region ***模型
    public class m_cQuartzJobModel
    {
        /// <summary>
        /// 默认组
        /// </summary>
        public const string GROUP_NAME = "gq";
        /// <summary>
        /// 计划：请求用户令牌
        /// </summary>
        public const string JOB_RESQTOKEN = "JOB_RESQTOKEN";
        /// <summary>
        /// 计划：获取对账单
        /// </summary>
        public const string JOB_RESQDZD = "JOB_RESQDZD";
        /// <summary>
        /// 计划：提交催记
        /// </summary>
        public const string JOB_SUBMITACTION = "JOB_SUBMITACTION";
    }
    #endregion

    #region ***请求用户令牌
    [DisallowConcurrentExecution]
    public class m_cQuartzJobResqToken : IJob
    {
        private static bool m_bJobDoing = false;
        public void Execute(IJobExecutionContext context)
        {
            if (!m_bJobDoing)
            {
                try
                {
                    m_bJobDoing = true;

                    Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobResqToken][Execute][开始用户令牌数据同步]", LogTyper.JobLogger);

                    ///是否写非错误日志
                    string writeLog = "0";

                    ///查询用户令牌数据
                    HomeController m_pHome = new HomeController();
                    JsonResult m_pJsonResult;
                    if (!string.IsNullOrWhiteSpace(m_cCore.m_fReadTxtToToken(writeLog)))
                        m_pJsonResult = m_pHome.f_user_sync_token($"{{\"searchMode\":\"1\",\"writeLog\":\"{writeLog}\"}}");
                    else
                        m_pJsonResult = m_pHome.f_user_sync_token($"{{\"searchMode\":\"2\",\"writeLog\":\"{writeLog}\"}}");
                    JObject m_pTokenJO = JObject.FromObject(m_pJsonResult.Data);
                    if (m_pTokenJO["status"].ToString().Equals("0"))
                    {
                        if (m_pTokenJO["data"].Type == JTokenType.Array)
                        {
                            JArray m_pTokenJA = JArray.FromObject(m_pTokenJO["data"]);
                            if (m_pTokenJA.Count > 0)
                            {
                                Log.Instance.Debug($"[Imp][m_cQuartzJobResqToken][Execute][用户令牌数据:{m_pTokenJA.Count}]", LogTyper.JobLogger);

                                ///更新语句
                                List<string> m_lSQL = new List<string>();
                                foreach (JToken m_pJT in m_pTokenJA)
                                {
                                    m_lSQL.Add($" SELECT '{m_pJT["agentId"]}' AS agentId, '{m_pJT["userId"]}' AS userId, '{m_pJT["userToken"]}' AS userToken ");
                                }

                                string m_sSQL = $@"
UPDATE Users 
SET Users.zxToken = T0.userToken,
Users.zxTokenDate = GETDATE() 
FROM
	( {string.Join("\r\nUNION\r\n", m_lSQL)} ) AS T0 
WHERE
	T0.userId = Users.autoId;
";
                                int m_uCount = m_cSQL.m_fExecuteCommand(m_sSQL, m_cConnStr.CenoSystem60);
                                if (m_uCount > 0)
                                    Log.Instance.Success($"[Imp][m_cQuartzJobResqToken][Execute][用户令牌数据同步成功,响应{m_uCount}行]", LogTyper.JobLogger);
                                else Log.Instance.Warn($"[Imp][m_cQuartzJobResqToken][Execute][用户令牌数据同步完成,响应0行]", LogTyper.JobLogger);
                            }
                            else Log.Instance.Debug($"[Imp][m_cQuartzJobResqToken][Execute][无任何用户令牌数据,完成]", LogTyper.JobLogger);
                        }
                        else Log.Instance.Debug($"[Imp][m_cQuartzJobResqToken][Execute][无任何用户令牌数据,完成]", LogTyper.JobLogger);
                    }
                    else
                    {
                        //记录一下错误即可
                        Log.Instance.Debug($"[Imp][m_cQuartzJobResqAll][Execute][用户令牌数据查询时错误:{m_pJsonResult.Data}]", LogTyper.JobLogger);
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Debug(ex, LogTyper.JobLogger);
                }
                finally
                {
                    m_bJobDoing = false;
                }
            }
            else
            {
                Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobResqToken][Execute][任务执行中,跳出]", LogTyper.JobLogger);
            }
        }
    }
    #endregion

    #region ***获取对账单
    [DisallowConcurrentExecution]
    public class m_cQuartzJobResqDzd : IJob
    {
        private static bool m_bJobDoing = false;
        public void Execute(IJobExecutionContext context)
        {
            if (!m_bJobDoing)
            {
                try
                {
                    m_bJobDoing = true;

                    Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobResqDzd][Execute][请在此处书写“获取对账单”逻辑]", LogTyper.JobLogger);
                }
                catch (Exception ex)
                {
                    Log.Instance.Debug(ex, LogTyper.JobLogger);
                }
                finally
                {
                    m_bJobDoing = false;
                }
            }
            else
            {
                Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobResqDzd][Execute][任务执行中,跳出]", LogTyper.JobLogger);
            }
        }
    }
    #endregion

    #region ***提交催记
    [DisallowConcurrentExecution]
    public class m_cQuartzJobSubmitAction : IJob
    {
        private static bool m_bJobDoing = false;
        /// <summary>
        /// 读锁
        /// </summary>
        public static object m_oSQLLock = new object();
        public void Execute(IJobExecutionContext context)
        {
            if (!m_bJobDoing)
            {
                try
                {
                    m_bJobDoing = true;

                    Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobSubmitAction][Execute]请在此处书写“提交催记”逻辑]", LogTyper.JobLogger);

                    ///提交催记至少一个线程
                    int m_uThreadCount = 0;
                    int.TryParse(m_cSettings.m_dKeyValue["m_uThreadCount"], out m_uThreadCount);
                    if (m_uThreadCount <= 0) m_uThreadCount = 1;

                    ///开启线程
                    for (int i = 0; i < 10; i++)
                    {
                        ThreadPool.QueueUserWorkItem((o) =>
                        {
                            try
                            {

                            }
                            catch (Exception ex)
                            {
                                Log.Instance.Debug(ex, LogTyper.JobLogger);
                            }

                        }, null);
                    }

                }
                catch (Exception ex)
                {
                    Log.Instance.Debug(ex, LogTyper.JobLogger);
                }
                finally
                {
                    m_bJobDoing = false;
                }
            }
            else
            {
                Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobSubmitAction][Execute][催记提交任务执行中,跳出]", LogTyper.JobLogger);
            }
        }
    }
    #endregion

    #region ***任务计划设置
    public class m_cQuartzJobScheduler
    {
        public static void Start()
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();

            ///请求用户令牌
            {
                string cron = m_cSettings.m_dKeyValue[m_cQuartzJobModel.JOB_RESQTOKEN];
                if (!string.IsNullOrWhiteSpace(cron))
                {
                    IJobDetail job = JobBuilder.Create<m_cQuartzJobResqToken>().Build();
                    ITrigger trigger = TriggerBuilder.Create()
                      .WithIdentity(m_cQuartzJobModel.JOB_RESQTOKEN, m_cQuartzJobModel.GROUP_NAME)
                      .WithCronSchedule(cron)
                      .Build();
                    scheduler.ScheduleJob(job, trigger);
                }
            }

            ///提交催记
            {
                string cron = m_cSettings.m_dKeyValue[m_cQuartzJobModel.JOB_SUBMITACTION];
                if (!string.IsNullOrWhiteSpace(cron))
                {
                    IJobDetail job = JobBuilder.Create<m_cQuartzJobSubmitAction>().Build();
                    ITrigger trigger = TriggerBuilder.Create()
                      .WithIdentity(m_cQuartzJobModel.JOB_SUBMITACTION, m_cQuartzJobModel.GROUP_NAME)
                      .WithCronSchedule(cron)
                      .Build();
                    scheduler.ScheduleJob(job, trigger);
                }
            }

            ///获取对账单
            {
                string cron = m_cSettings.m_dKeyValue[m_cQuartzJobModel.JOB_RESQDZD];
                if (!string.IsNullOrWhiteSpace(cron))
                {
                    IJobDetail job = JobBuilder.Create<m_cQuartzJobResqDzd>().Build();
                    ITrigger trigger = TriggerBuilder.Create()
                      .WithIdentity(m_cQuartzJobModel.JOB_RESQDZD, m_cQuartzJobModel.GROUP_NAME)
                      .WithCronSchedule(cron)
                      .Build();
                    scheduler.ScheduleJob(job, trigger);
                }
            }
        }

        /// <summary>
        /// 后续增加可动态更改计划任务模式
        /// </summary>
        public static void Set()
        {
            ///略
        }
    }
    #endregion
}