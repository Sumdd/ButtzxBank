using ExcelDataReader;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;

namespace ButtzxBank
{
    public class m_cExcel
    {
        public static DataSet m_fToDataSet(HttpPostedFileBase file)
        {
            ///直接返回
            if (file == null) return null;

            IExcelDataReader excelReader;

            if (Path.GetExtension(file.FileName).ToUpper() == ".XLS")
            {
                //1.1 Reading from a binary Excel file ('97-2003 format; *.xls)
                excelReader = ExcelReaderFactory.CreateBinaryReader(file.InputStream);
            }
            else
            {
                //1.2 Reading from a OpenXml Excel file (2007 format; *.xlsx)
                excelReader = ExcelReaderFactory.CreateOpenXmlReader(file.InputStream);
            }

            DataSet ds = excelReader.AsDataSet(new ExcelDataSetConfiguration
            {
                UseColumnDataType = true,
                ConfigureDataTable = x => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            foreach (DataTable m_pDataTable in ds.Tables)
            {
                foreach (DataColumn m_pDataColumn in m_pDataTable.Columns)
                {
                    if (m_pDataColumn.ReadOnly)
                        m_pDataColumn.ReadOnly = false;
                    ///默认去前后空格、制表符
                    if (m_pDataColumn.DataType == typeof(string))
                    {
                        foreach (DataRow m_pDataRow in m_pDataTable.Rows)
                        {
                            m_pDataRow[m_pDataColumn.ColumnName] = m_pDataRow[m_pDataColumn.ColumnName]?.ToString()?.Trim()?.Replace("\t", "");
                        }
                    }
                }
            }

            return ds;
        }

        public static DataTable m_fGetSheet1(DataSet ds)
        {
            try
            {
                DataTable dt = null;
                foreach (DataTable item in ds.Tables)
                {
                    if (new List<string>() { "Sheet1", "Sheet1$" }.Contains(item.TableName))
                    {
                        dt = item.Copy();
                        return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Debug(ex);
            }
            return null;
        }

        public static void m_fExport(string OutxlsPath, string WworkBookName, DataTable dt, string Format = null)
        {
            ///为了兼容下载,这里默认本目录下Output文件即可

            string m_sOutxlsPath;
            if (OutxlsPath.StartsWith("~")) m_sOutxlsPath = m_cCore.m_fAbsoluteURL(OutxlsPath);
            else m_sOutxlsPath = OutxlsPath;

            FileInfo newFile = new FileInfo(m_sOutxlsPath);
            if (!Directory.Exists(newFile.DirectoryName))
            {
                System.IO.Directory.CreateDirectory(newFile.DirectoryName);
            }
            using (ExcelPackage excelPackage = new ExcelPackage(newFile))
            {
                ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets.Add("Arun");
                excelWorksheet.Name = WworkBookName;
                excelWorksheet.Cells.Style.Font.Size = 11f;
                if (Format != null) excelWorksheet.Cells.Style.Numberformat.Format = Format;
                excelWorksheet.DefaultColWidth = 100;
                excelWorksheet.Cells["A1"].LoadFromDataTable(dt, true);
                excelPackage.Save();
            }
        }

        public static void m_fExport(string OutxlsPath, DataSet dt, string Format = null)
        {
            ///为了兼容下载,这里默认本目录下Output文件即可

            string m_sOutxlsPath;
            if (OutxlsPath.StartsWith("~")) m_sOutxlsPath = m_cCore.m_fAbsoluteURL(OutxlsPath);
            else m_sOutxlsPath = OutxlsPath;

            FileInfo newFile = new FileInfo(m_sOutxlsPath);
            if (!Directory.Exists(newFile.DirectoryName))
            {
                System.IO.Directory.CreateDirectory(newFile.DirectoryName);
            }
            using (ExcelPackage excelPackage = new ExcelPackage(newFile))
            {
                foreach (DataTable m_pDataTable in dt.Tables)
                {
                    ExcelWorksheet excelWorksheet = excelPackage.Workbook.Worksheets.Add("Arun");
                    excelWorksheet.Name = m_pDataTable.TableName;
                    excelWorksheet.Cells.Style.Font.Size = 11f;
                    if (Format != null) excelWorksheet.Cells.Style.Numberformat.Format = Format;
                    excelWorksheet.DefaultColWidth = 100;
                    excelWorksheet.Cells["A1"].LoadFromDataTable(m_pDataTable, true);
                }
                excelPackage.Save();
            }
        }
    }
}