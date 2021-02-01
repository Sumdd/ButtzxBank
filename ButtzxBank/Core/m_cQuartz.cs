using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Quartz;
using Quartz.Impl;
using System.Data;
using Newtonsoft.Json.Linq;
using SqlSugar;

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
        public const string JOB_RESQTOKEN = "resqToken";
        /// <summary>
        /// 计划：提交催记
        /// </summary>
        public const string JOB_SUBMITACTION = "submitAction";
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

                    Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobResqToken][Execute][请在此处书写“请求用户令牌”逻辑]");

                    ///

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
                Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobResqToken][Execute][任务执行中,跳出]");
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

                    Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobSubmitAction][Execute]请在此处书写“提交催记”逻辑]");

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
                Log.Instance.Debug($"[ButtzxBank][m_cQuartzJobSubmitAction][Execute][任务执行中,跳出]");
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
                  .WithSimpleSchedule(t =>
                    t.WithIntervalInSeconds(300)
                     .RepeatForever())
                     .Build();
                scheduler.ScheduleJob(job, trigger);
            }

            ///提交催记
            {
                IJobDetail job = JobBuilder.Create<m_cQuartzJobSubmitAction>().Build();
                ITrigger trigger = TriggerBuilder.Create()
                  .WithIdentity(m_cQuartzJobModel.JOB_SUBMITACTION, m_cQuartzJobModel.GROUP_NAME)
                  .WithSimpleSchedule(t =>
                    t.WithIntervalInSeconds(300)
                     .RepeatForever())
                     .Build();
                scheduler.ScheduleJob(job, trigger);
            }
        }

        /// <summary>
        /// 后续增加可动态更改计划任务模式
        /// </summary>
        public static void Set()
        {
            ///
        }
    }
    #endregion
}