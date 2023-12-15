using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.Transactions
{
    public class TransactionViewModel
    {
        public string TypeName { get; set; }
        public string ActiveDate { get; set; }
        public bool InOut { get; set; }
        public string Amount { get; set; }
        public string TransactionTypeId { get; set; }
    }

    public class TransactionBillResult
    {
        public string ActiveDate { get; set; }
        public string FromAcc { get; set; }
        public string ToAcc { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string ReferentNumber { get; set; }
        public string TransactionTypeId { get; set; }
    }
}
