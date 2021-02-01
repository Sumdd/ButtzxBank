using CsvHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace ButtzxBank
{
    public class m_cCSV
    {
        public static DataTable m_fGet(string m_sFile, string Code = "UTF-8", string Delimiter = ",", int ReadLine = 0)
        {
            using (Stream InputStream = System.IO.File.OpenRead(m_sFile))
            {
                return m_cCSV.m_fGet(InputStream, Code, Delimiter, ReadLine);
            }
        }

        public static DataTable m_fGet(Stream InputStream, string Code = "UTF-8", string Delimiter = ",", int ReadLine = 0)
        {
            using (StreamReader reader = new StreamReader(InputStream, Encoding.GetEncoding(Code)))
            {
                ///兼容无效行
                for (int i = 0; i < ReadLine; i++)
                {
                    reader.ReadLine();
                }
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Configuration.MissingFieldFound = null;
                    csv.Configuration.Delimiter = Delimiter;
                    csv.Configuration.TrimOptions = CsvHelper.Configuration.TrimOptions.Trim;
                    csv.Configuration.BadDataFound = (a) =>
                    {
                        Log.Instance.Debug(a);
                    };
                    // Do any configuration to `CsvReader` before creating CsvDataReader.
                    using (var dr = new CsvDataReader(csv))
                    {
                        var dt = new DataTable();
                        dt.Load(dr);

                        ///处理制表符
                        foreach (DataColumn m_pDataColumn in dt.Columns)
                        {
                            if (m_pDataColumn.ReadOnly)
                                m_pDataColumn.ReadOnly = false;
                            ///默认去前后空格、制表符
                            if (m_pDataColumn.DataType == typeof(string))
                            {
                                foreach (DataRow m_pDataRow in dt.Rows)
                                {
                                    m_pDataRow[m_pDataColumn.ColumnName] = m_pDataRow[m_pDataColumn.ColumnName]?.ToString()?.Replace("\t", "");
                                }
                            }
                        }

                        return dt;
                    }
                }
            }
        }

        public static DataTable m_fGetLine(string m_sFile, string Code = "UTF-8", string Delimiter = ",", int ReadLine = 0)
        {
            using (Stream InputStream = System.IO.File.OpenRead(m_sFile))
            {
                return m_cCSV.m_fGetLine(InputStream, Code, Delimiter, ReadLine);
            }
        }

        public static DataTable m_fGetLine(Stream InputStream, string Code = "UTF-8", string Delimiter = ",", int ReadLine = 0)
        {
            string m_sReadString = string.Empty;

            using (StreamReader reader = new StreamReader(InputStream, Encoding.GetEncoding(Code)))
            {
                ///兼容无效行
                for (int i = 0; i < ReadLine; i++)
                {
                    if (i == ReadLine - 1) m_sReadString = reader.ReadLine();
                    else reader.ReadLine();
                }
            }
            DataTable m_pDataTable = new DataTable();
            ///自动分割
            if (!string.IsNullOrWhiteSpace(m_sReadString))
            {
                string[] m_lHeaders = m_sReadString.Split(new string[] { Delimiter }, StringSplitOptions.None);
                ///做表
                int m_uOK = 0;
                int m_uNO = 0;
                List<object> m_lObject = new List<object>();
                foreach (string _item in m_lHeaders)
                {
                    string item = _item?.Replace("\t", "");
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        m_uOK++;
                        m_pDataTable.Columns.Add($"F{m_uOK}");
                        m_lObject.Add(item);
                    }
                    else
                    {
                        m_uNO++;
                        m_pDataTable.Columns.Add($"Column{m_uNO}");
                        m_lObject.Add(item);
                    }
                }
                m_pDataTable.Rows.Add(m_lObject.ToArray());
            }
            return m_pDataTable;
        }
    }
}