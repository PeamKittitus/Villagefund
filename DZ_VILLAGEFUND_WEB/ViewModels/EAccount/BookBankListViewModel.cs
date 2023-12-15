using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.EAccount
{
    public class BookBankListViewModel
    {
        public Int64 Id { get; set; }
        public string AccountName { get; set; }
        public string BankName { get; set; }
        public decimal Amount { get; set; }
    }


    public class FormApproceAccountBudget
    {
        public Int64 AccountBudgetd { get; set; }
        public decimal Amount { get; set; }
        public Int64 ProjectBbId { get; set; }
    }

}
