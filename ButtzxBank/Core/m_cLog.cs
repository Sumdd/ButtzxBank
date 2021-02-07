using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using System.Reflection;

namespace ButtzxBank
{
    /// <summary>
    /// 日志
    /// </summary>
    public class Log
    {
        /// <summary>
        /// 日志级别
        /// </summary>
        private int m_Loglevel = 5;
        /// <summary>
        /// 日志构造函数
        /// </summary>
        private Log()
        {
            log4net.Config.XmlConfigurator.Configure();

            log4 = new List<ILog>();
            log4.Add(log4net.LogManager.GetLogger($"{LogTyper.LogLogger}"));
            log4.Add(log4net.LogManager.GetLogger($"{LogTyper.JobLogger}"));
            log4.Add(log4net.LogManager.GetLogger($"{LogTyper.ProLogger}"));
        }
        /// <summary>
        /// 私人静态实例
        /// </summary>
        private List<log4net.ILog> log4 = null;
        /// <summary>
        /// 私人静态实例
        /// </summary>
        private static Log _instance = null;
        /// <summary>
        /// 公共静态实例
        /// </summary>
        public static Log Instance
        {
            get
            {
                return _instance == null ? _instance = new Log() : _instance;
            }
        }
        /// <summary>
        /// 调试
        /// </summary>
        /// <param name="msg">错误信息对象</param>
        public void Debug(object msg, LogTyper lt = LogTyper.LogLogger)
        {
            if (m_Loglevel >= 5)
            {
                ILog log = log4.Find(x => x.Logger.Name == lt.ToString());
                if (log != null) log.Debug(msg);
            }
        }
        /// <summary>
        /// 信息
        /// </summary>
        /// <param name="msg">信息字符串</param>
        public void Success(string msg, LogTyper lt = LogTyper.LogLogger)
        {
            if (m_Loglevel >= 4)
            {
                ILog log = log4.Find(x => x.Logger.Name == lt.ToString());
                if (log != null) log.Info(msg);
            }
        }
        /// <summary>
        /// 警告
        /// </summary>
        /// <param name="msg">警告信息字符串</param>
        public void Warn(string msg, LogTyper lt = LogTyper.LogLogger)
        {
            if (m_Loglevel >= 3)
            {
                ILog log = log4.Find(x => x.Logger.Name == lt.ToString());
                if (log != null) log.Warn(msg);
            }
        }
        /// <summary>
        /// 错误
        /// </summary>
        /// <param name="msg">错误信息字符串</param>
        public void Error(string msg, LogTyper lt = LogTyper.LogLogger)
        {
            if (m_Loglevel >= 2)
            {
                ILog log = log4.Find(x => x.Logger.Name == lt.ToString());
                if (log != null) log.Error(msg);
            }
        }
        /// <summary>
        /// 失败
        /// </summary>
        /// <param name="msg">失败信息字符串</param>
        public void Fail(string msg, LogTyper lt = LogTyper.LogLogger)
        {
            if (m_Loglevel >= 1)
            {
                ILog log = log4.Find(x => x.Logger.Name == lt.ToString());
                if (log != null) log.Fatal(msg);
            }
        }
        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="Loglevel">日志级别数字</param>
        public void SetLogLevel(int Loglevel)
        {
            m_Loglevel = Loglevel;
            foreach (ILog log in log4)
            {
                log.Info($"[ButtzxBank][Log][SetLogLevel][设置日志级别为{Loglevel}[ALL(>=5)|DEBUG(5)|INFO(4)|WARN(3)|ERROR(2)|FATAL(1)|OFF(<=0)]]");
            }
        }
    }

    public enum LogTyper
    {
        /// <summary>
        /// 默认日志类型
        /// </summary>
        LogLogger,
        /// <summary>
        /// 任务计划日志
        /// </summary>
        JobLogger,
        /// <summary>
        /// 拓展方法日志
        /// </summary>
        ProLogger
    }
}