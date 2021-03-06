﻿using SqlSugar;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ButtzxBank
{
    /// <summary>
    /// 语法糖
    /// </summary>
    public class m_cSugar
    {
        /// <summary>
        /// 简单实例化
        /// </summary>
        /// <param name="sConNameStr">连接字符串或名称</param>
        /// <param name="IsAutoCloseConnection">是否自动关闭链接</param>
        public m_cSugar(string sConNameStr = null, bool IsAutoCloseConnection = true)
        {
            string sConStr = string.Empty;

            if (!string.IsNullOrWhiteSpace(sConNameStr))
            {
                try
                {
                    sConStr = ConfigurationManager.ConnectionStrings[sConNameStr].ToString();
                }
                catch (Exception ex) { }
            }

            if (string.IsNullOrWhiteSpace(sConStr))
            {
                sConStr = sConNameStr;
            }

            if (string.IsNullOrWhiteSpace(sConStr))
            {
                try
                {
                    sConStr = ConfigurationManager.ConnectionStrings[(ConfigurationManager.ConnectionStrings.Count - 1)].ToString();
                }
                catch (Exception ex) { }
            }

            if (string.IsNullOrWhiteSpace(sConStr))
            {
                throw new Exception("请配置正确的连接字符串");
            }

            this.EasyClient = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = sConStr,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = IsAutoCloseConnection,
            });
        }
        public m_cSugar(ConnectionConfig config)
        {
            this.EasyClient = new SqlSugarClient(config);
        }
        public SqlSugarClient EasyClient { get; set; }
    }
}