using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButtzxBank
{
    public class m_cSettings
    {
        #region ***通用加载参数
        private static Dictionary<string, string> _m_dKeyValue = null;
        public static Dictionary<string, string> m_dKeyValue
        {
            get
            {
                if (_m_dKeyValue == null || (_m_dKeyValue != null && _m_dKeyValue.Count <= 0))
                {
                    _m_dKeyValue = new Dictionary<string, string>();
                    foreach (string item in System.Configuration.ConfigurationSettings.AppSettings.AllKeys)
                    {
                        _m_dKeyValue.Add(item, System.Configuration.ConfigurationSettings.AppSettings[item].Replace("\\r\\n", "\r\n"));
                    }
                }
                return _m_dKeyValue;
            }
        }
        #endregion

        #region ***获取配置
        private static string m_fGet(string m_sName)
        {
            try
            {
                return System.Configuration.ConfigurationSettings.AppSettings.Get(m_sName).ToString();
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[AutoxRecSetID][m_cSettings][m_fGet][Exception][{ex.Message}]");
            }
            return string.Empty;
        }
        #endregion

        #region ***设置配置
        private static void m_fSet(string m_sName, string m_sValue)
        {
            try
            {
                System.Configuration.ConfigurationSettings.AppSettings.Set(m_sName, m_sValue);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[AutoxRecSetID][m_cSettings][m_fSet][Exception][{ex.Message}]");
            }
        }
        #endregion
    }
}
