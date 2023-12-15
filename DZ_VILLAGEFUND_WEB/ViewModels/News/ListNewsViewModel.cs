using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using DZ_VILLAGEFUND_WEB.Models;

namespace DZ_VILLAGEFUND_WEB.ViewModels.News
{
    public class ListNewsViewModel
    {
        public int TransactionNewsId { get; set; }
        public string TransactionTitle { get; set; }
        public string TransactionBy { get; set; }
        public int LookupId { get; set; }
        public string UpdateDate { get; set; }
        public DateTime _UpdateDate { get; set; }
        public string UserId { get; set; }
        public bool TransactionType { get; set; }
        public bool IsActive { get; set; }
        public bool IsApprove { get; set; }
        public string FileName { get; set; }
        public int CountReader { get; set; }
        public string NewsStartDate { get; set; }
        public string NewsEndDate { get; set; }
        public string FullName { get; set; }
    }

    public class ListNewsItemModel
    {
        public string GencodeFileName { get; set; }
        public string FileName { get; set; }
    }

    public class TransactionNewsViewModel
    {
        public int TransactionNewsId { get; set; }
        public string UserId { get; set; }
        public int LookupId { get; set; }
        public bool TransactionType { get; set; }
        public int TransactionYear { get; set; }
        public string TransactionBy { get; set; }
        public string TransactionTitle { get; set; }
        public string TransactionDetail { get; set; }
        public string NewsStartDate { get; set; }
        public string NewsEndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmail { get; set; }
        public bool IsApprove { get; set; }
        public string UpdateDate { get; set; }
        public string UpdateBy { get; set; }
        public IFormFile[] FileName { get; set; }
        public string FileNameView { get; set; }
        public string FileNameEdit { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<ListNewsItemModel> FileNames { get; set; }

        public virtual ICollection<DateCollection.DateCollection> DateCollectioin { get; set; }
    }

    public class NewsReader
    {
        public int TransactionFileId { get; set; }
        public int TransactionNewsId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int TransactionYear { get; set; }
        public bool IsRead { get; set; }
        public string UpdateDate { get; set; }
        public string UpdateBy { get; set; }
    }


    public class Notification
    {
        public Int64 Id { get; set; }
        public string Title { get; set; }
        public string TimeAgo { get; set; }
    }

}
