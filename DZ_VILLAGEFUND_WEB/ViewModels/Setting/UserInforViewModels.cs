using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.ViewModels.Settings
{
    public class UserInforViewModels
    {
        public string Fullname { get; set; }
        public string IDCard { get; set; }
        public decimal Balance { get; set; }
    }

    public class TranslateResult
    {
        public List<object> MyArray { get; set; }
    }
}
