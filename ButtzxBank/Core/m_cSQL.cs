using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SqlSugar;
using System.Data;
using System.Collections;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace ButtzxBank
{
    /// <summary>
    /// 数据库操作类
    /// <![CDATA[
    /// 注意事项：如果语句执行时间可能会很久，需进行如下操作
    /// 1.对应数据库超时时间需设置为0无限刷
    /// 2.对应链接对象超时时间设置为0无限刷
    /// ]]>
    /// </summary>
    public class m_cSQL
    {
        #region ***示例
        public static void demo()
        {
            ///不自动关闭连接
            using (SqlSugarClient m_pEsyClient = new m_cSugar(null, false).EasyClient)
            {
                ///设置超时时间为0
                m_pEsyClient.Ado.CommandTimeOut = 0;

                ///SQL语句
                string m_sSQL = $@"
SELECT 1;
";
                ///执行语句
                m_pEsyClient.Ado.ExecuteCommand(m_sSQL);

                ///略
            }
        }
        #endregion

        #region ***创建临时表
        public static string m_fCreateTempTable(DataTable dtUserData, bool m_bString)
        {
            string createTempTablesql = @"CREATE TABLE #tUserData
                                            (
                                                {0}
                                            );";
            Dictionary<Type, string> dataDictionary = new Dictionary<Type, string>();
            dataDictionary[typeof(decimal)] = dataDictionary[typeof(double)] = " decimal(18,6) ";
            dataDictionary[typeof(int)] = dataDictionary[typeof(long)] = " bigint ";
            dataDictionary[typeof(string)] = " nvarchar(max) ";
            dataDictionary[typeof(DateTime)] = " DateTime ";
            dataDictionary[typeof(object)] = " nvarchar(max) ";

            List<string> colSqlList = new List<string>();
            foreach (DataColumn col in dtUserData.Columns)
            {
                string colDataType = dataDictionary[col.DataType];
                if (col.DataType != typeof(DateTime) && m_bString) colDataType = " nvarchar(max) ";

                string colSql = string.Format(@" [{0}] {1} ", col.ColumnName, colDataType);
                colSqlList.Add(colSql);
            }
            createTempTablesql = string.Format(createTempTablesql,
                string.Join(",", colSqlList));
            return createTempTablesql;
        }
        #endregion
    }
}