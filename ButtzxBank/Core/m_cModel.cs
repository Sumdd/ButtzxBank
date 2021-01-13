using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ButtzxBank
{
    public class m_mAccts
    {
        public string acctNbr
        {
            get; set;
        }
        public List<m_mPlanlist> planlist
        {
            get; set;
        }
    }
    public class m_mPlanlist
    {
        public string reducePeriods
        {
            get; set;
        }
        public string repayAmt
        {
            get; set;
        }
        public string repayDate
        {
            get; set;
        }
    }
}