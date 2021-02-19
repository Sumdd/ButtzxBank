using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Data;
using System.Collections;

namespace ButtzxBank
{
    public class m_cJSON
    {
        public static IList m_fDataTableToIList(DataTable m_pDataTable)
        {
            if (m_pDataTable != null && m_pDataTable.Rows.Count > 0)

                return m_pDataTable.Rows.Cast<DataRow>().Select(x =>
                {
                    return m_pDataTable.Columns.Cast<DataColumn>().Select(y =>
                    {
                        return new KeyValuePair<string, object>(y.ColumnName, x[y.ColumnName]);

                    }).ToDictionary(z => z.Key, z => z.Value);

                }).ToList();

            else return null;
        }

        public static string Parse(object m_oObject)
        {
            if (m_oObject.GetType() == typeof(DataTable))
            {
                return JsonConvert.SerializeObject(m_cJSON.m_fDataTableToIList(m_oObject as DataTable));
            }

            return JsonConvert.SerializeObject(m_oObject);
        }
    }
}