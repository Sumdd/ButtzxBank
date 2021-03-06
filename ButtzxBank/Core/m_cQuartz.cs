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

                    ///查询出所有未导入的内容
                    JsonResult m_pTokenJR = new HomeController().f_user_sync_token($"");
                    JObject m_pTokenJO = JObject.FromObject(m_pTokenJR.Data);
                    if (m_pTokenJO["status"].ToString().Equals("0"))
                    {
                        if (m_pTokenJO["data"].Type == JTokenType.Array)
                        {
                            JArray m_pTokenJA = JArray.FromObject(m_pTokenJO["data"]);
                            if (m_pTokenJA.Count > 0)
                            {
                                Log.Instance.Debug($"[Imp][m_cQuartzJobResqToken][Execute][用户令牌数据:{m_pTokenJA.Count}]", LogTyper.JobLogger);

                                foreach (JToken m_pJT in m_pTokenJA)
                                {

                                }

                            }
                            else Log.Instance.Debug($"[Imp][m_cQuartzJobResqToken][Execute][无任务对账单数据,完成]", LogTyper.JobLogger);
                        }
                        else Log.Instance.Debug($"[Imp][m_cQuartzJobResqToken][Execute][无任何导案数据,完成]", LogTyper.JobLogger);
                    }
                    else
                    {
                        //记录一下错误即可
                        Log.Instance.Debug($"[Imp][m_cQuartzJobResqAll][Execute][查询用户令牌数据错误:{m_pTokenJR.Data}]", LogTyper.JobLogger);
                    }

                }
                catch (Exception ex)
                {
                    Log.Instance.Debug(ex);
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
                    Log.Instance.Debug(ex);
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
        public void Execute(IJobExecutionContext context)
        {
            if (!m_bJobDoing)
            {
                try
                {
                    m_bJobDoing = true;

                    Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobSubmitAction][Execute]请在此处书写“提交催记”逻辑]", LogTyper.JobLogger);

                }
                catch (Exception ex)
                {
                    Log.Instance.Debug(ex);
                }
                finally
                {
                    m_bJobDoing = false;
                }
            }
            else
            {
                Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobSubmitAction][Execute][任务执行中,跳出]", LogTyper.JobLogger);
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
                IJobDetail job = JobBuilder.Create<m_cQuartzJobResqToken>().Build();
                ITrigger trigger = TriggerBuilder.Create()
                  .WithIdentity(m_cQuartzJobModel.JOB_RESQTOKEN, m_cQuartzJobModel.GROUP_NAME)
                  .WithCronSchedule(m_cSettings.m_dKeyValue[m_cQuartzJobModel.JOB_RESQTOKEN])
                  .Build();
                scheduler.ScheduleJob(job, trigger);
            }

            ///提交催记
            {
                IJobDetail job = JobBuilder.Create<m_cQuartzJobSubmitAction>().Build();
                ITrigger trigger = TriggerBuilder.Create()
                  .WithIdentity(m_cQuartzJobModel.JOB_SUBMITACTION, m_cQuartzJobModel.GROUP_NAME)
                  .WithCronSchedule(m_cSettings.m_dKeyValue[m_cQuartzJobModel.JOB_SUBMITACTION])
                  .Build();
                scheduler.ScheduleJob(job, trigger);
            }

            ///获取对账单
            {
                IJobDetail job = JobBuilder.Create<m_cQuartzJobResqDzd>().Build();
                ITrigger trigger = TriggerBuilder.Create()
                  .WithIdentity(m_cQuartzJobModel.JOB_RESQDZD, m_cQuartzJobModel.GROUP_NAME)
                  .WithCronSchedule(m_cSettings.m_dKeyValue[m_cQuartzJobModel.JOB_RESQDZD])
                  .Build();
                scheduler.ScheduleJob(job, trigger);
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