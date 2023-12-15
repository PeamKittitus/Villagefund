using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.Settings;
using DZ_VILLAGEFUND_WEB.ViewModels.Transactions;
using DZ_VILLAGEFUND_WEB.ViewModels.News;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Globalization;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Drawing;
using DZ_VILLAGEFUND_WEB.ViewModels.DateCollection;
using Microsoft.Extensions.Configuration;
using DZ_VILLAGEFUND_WEB.ViewModels.EAccount;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net.Mail;
using System.Net;
using static QRCoder.PayloadGenerator;
using System.Data;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    [Authorize]
    public class NewsLatterController : Controller
    {
        private readonly ILogger<NewsLatterController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private Microsoft.AspNetCore.Hosting.IHostingEnvironment _environment;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        public NewsLatterController(
             ILogger<NewsLatterController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext db,
            IConfiguration configuration,
            Microsoft.AspNetCore.Hosting.IHostingEnvironment environment)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string UserRole = Helper.GetRoleUser(CurrentUser.Id);
            //if (UserRole == "HeadQuarterAdmin" || UserRole == "officer" || UserRole == "administrator")
            //{
            //    return RedirectToAction("PublicNews");
            //}

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            var Gets = await DB.TransactionNews.Where(w => w.TransactionBy == CurrentUser.Id).OrderByDescending(o => o.TransactionNewsId).ToListAsync();

            var Models = new List<ListNewsViewModel>();
            foreach (var Get in Gets)
            {
                string UserId = string.Empty;
                if (Get.UserId != null)
                {
                    string[] ArrayName = Get.UserId.Split(',');
                    for (int i = 0; i < ArrayName.Length; i++)
                    {
                        if (string.IsNullOrEmpty(UserId))
                            UserId = DB.Users.Where(w => w.Id == ArrayName[i]).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                        else UserId += ", " + DB.Users.Where(w => w.Id == ArrayName[i]).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                    }
                }

                var Model = new ListNewsViewModel();
                Model.TransactionNewsId = Get.TransactionNewsId;
                Model.TransactionTitle = Get.TransactionTitle;
                Model.TransactionBy = Get.TransactionBy;
                Model.UserId = UserId;
                Model.UpdateDate = Helper.getShortDateThai(Get.UpdateDate);
                Model.TransactionType = Get.TransactionType;
                Model.IsActive = Get.IsActive;
                Model.IsApprove = Get.IsApprove;
                Models.Add(Model);
            }

            return PartialView("GetNews", Models);
        }

        [HttpGet]
        public async Task<IActionResult> IndexAll()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string UserRole = Helper.GetRoleUser(CurrentUser.Id);
            //if (UserRole == "HeadQuarterAdmin" || UserRole == "officer" || UserRole == "administrator")
            //{
            //    return RedirectToAction("PublicNews");
            //}

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            var GetsNews = await DB.TransactionReader.Where(w => w.UserId == CurrentUser.Id || w.UserId == "-").OrderByDescending(o => o.TransactionNewsId).ToListAsync();

            var Models = new List<ListNewsViewModel>();
            foreach (var GetReader in GetsNews)
            {
                var Gets = await DB.TransactionNews.Where(w => w.TransactionNewsId == GetReader.TransactionNewsId && w.IsApprove == true && w.TransactionBy != CurrentUser.Id).OrderByDescending(o => o.TransactionNewsId).ToListAsync();

                foreach (var Get in Gets)
                {
                    string UserId = string.Empty;
                    if (!string.IsNullOrEmpty(Get.UserId))
                    {
                        string[] ArrayName = Get.UserId.Split(',');
                        for (int i = 0; i < ArrayName.Length; i++)
                        {
                            if (string.IsNullOrEmpty(UserId))
                                UserId = DB.Users.Where(w => w.Id == ArrayName[i]).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                            else UserId += ", " + DB.Users.Where(w => w.Id == ArrayName[i]).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                        }
                    }
                    else UserId = "-";

                    var Model = new ListNewsViewModel();
                    Model.TransactionNewsId = Get.TransactionNewsId;
                    Model.TransactionTitle = Get.TransactionTitle;
                    Model.TransactionBy = Get.TransactionBy;
                    Model.UserId = UserId;
                    Model.UpdateDate = Helper.getShortDateThai(Get.UpdateDate);
                    Model.TransactionType = Get.TransactionType;
                    Model.IsActive = Get.IsActive;
                    Model.FileName = DB.TransactionFileNews.Where(w => w.TransactionNewsId == GetReader.TransactionNewsId).Select(s => s.GencodeFileName).FirstOrDefault();
                    Models.Add(Model);
                }
            }
            return PartialView("GetAllNews", Models);
        }


        [HttpGet]
        public async Task<IActionResult> FormEditNews(int NewsId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            //Gen Day Selection 
            ViewBag.Day = new SelectList(new List<DateCollection>(), "Day", "Day");
            //Gen month and Month selection
            string[] MonthName = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            var Months = new List<SelectListItem>();
            for (int m = 0; m < MonthName.Length; m++)
            {

                Months.Add(new SelectListItem
                {
                    Text = MonthName[m],
                    Value = m.ToString()
                });

            }
            ViewBag.Month = new SelectList(Months, "Value", "Text");
            //Gen Year and Year Selection
            var Years = new List<SelectListItem>();
            for (int y = DateTime.Now.Year; y < (DateTime.Now.Year + 7); y++)
            {
                Years.Add(new SelectListItem
                {
                    Text = (y + 543).ToString(),
                    Value = y.ToString()
                });
            }
            ViewBag.Year = new SelectList(Years, "Value", "Text");

            var GetNews = DB.TransactionNews.Where(w => w.TransactionNewsId == NewsId).FirstOrDefault();
            var GetFileNews = DB.TransactionFileNews.Where(w => w.TransactionNewsId == NewsId).ToList();
            var FileName = string.Empty;
            foreach (var Get in GetFileNews)
            {
                if (FileName != "")
                    FileName += "," + Get.FileName;
                else FileName = Get.FileName;
            }
            //var GetReader = await DB.TransactionReader.Where(w => w.TransactionNewsId == int.Parse(NewsId)).Select(s => s.UserId).ToListAsync();
            //ViewBag.UserId = new SelectList(DB.Users.Select(s => new { Id = s.Id, Name = s.FirstName + " " + s.LastName }).ToList(), "Id", "Name");
            var jsonData = new List<object>();
            var OrgStructures = DB.SystemOrgStructures.Where(w => w.ParentId == 0 || w.ParentId == 1).Select(s => new { OrgId = s.OrgId, OrgName = s.OrgName }).OrderBy(o => o.OrgId).ToList();
            foreach (var item in OrgStructures)
            {
                if (item.OrgId == 1)
                {
                    jsonData.Add(new
                    {
                        title = item.OrgName,
                        subs = DB.Users.Where(w => w.OrgId == item.OrgId).Select(s => new { id = s.Id, title = s.FirstName + " " + s.LastName }).ToList()
                    });
                }
                else
                {
                    jsonData.Add(new
                    {
                        title = item.OrgName,
                        subs = DB.SystemOrgStructures.Where(w => w.ParentId == item.OrgId && w.ParentId > 1).Select(s => new { title = s.OrgName, subs = DB.Users.Where(ws => ws.OrgId == s.OrgId).Select(ss => new { id = ss.Id, title = ss.FirstName + " " + ss.LastName }).ToList() }).ToList()
                    });
                }
            }
            ViewBag.UserIdSub = JsonConvert.SerializeObject(jsonData);

            List<SelectListItem> listTransactionType = new List<SelectListItem>();
            listTransactionType.Add(new SelectListItem
            {
                Text = "สมาชิก",
                Value = "true"
            });
            listTransactionType.Add(new SelectListItem
            {
                Text = "สาธารณะ",
                Value = "false"
            });
            ViewBag.TransactionType = new SelectList(listTransactionType, "Value", "Text", "false");

            List<SelectListItem> listIsActive = new List<SelectListItem>();
            listIsActive.Add(new SelectListItem
            {
                Text = "ใช้งาน",
                Value = "true"
            });
            listIsActive.Add(new SelectListItem
            {
                Text = "ไม่ใช้งาน",
                Value = "false"
            });
            ViewBag.IsActive = new SelectList(listIsActive, "Value", "Text", GetNews.IsActive);

            ViewBag.UserIdList = "";
            var UserIdList = DB.TransactionReader.Where(w => w.TransactionNewsId == NewsId).Select(s => s.UserId).ToList();

            foreach (var item in UserIdList)
            {
                if (item != UserIdList.Last())
                {
                    ViewBag.UserIdList = item + ",";
                }
                else
                {
                    ViewBag.UserIdList = item;
                }
            }

            var ModelNews = new TransactionNewsViewModel();
            ModelNews.UserId = GetNews.UserId;
            ModelNews.TransactionNewsId = NewsId;
            ModelNews.LookupId = GetNews.LookupId;
            ModelNews.TransactionTitle = GetNews.TransactionTitle;
            ModelNews.TransactionDetail = GetNews.TransactionDetail;
            ModelNews.NewsStartDate = Helper.getShortDate(GetNews.NewsStartDate);
            ModelNews.NewsEndDate = Helper.getShortDate(GetNews.NewsEndDate);
            ModelNews.StartDate = GetNews.NewsStartDate;
            ModelNews.EndDate = GetNews.NewsEndDate;
            ModelNews.FileNameEdit = FileName;
            ModelNews.IsActive = GetNews.IsActive;
            ModelNews.TransactionType = GetNews.TransactionType;
            ViewBag.TransactionType = new SelectList(listTransactionType, "Value", "Text", GetNews.TransactionType);
            ViewBag.LookupId = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 3).Select(s => new { Id = s.LookupId, Name = s.LookupText }).ToList(), "Id", "Name", GetNews.LookupId);
            return View("FormEditNews", ModelNews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditNews(TransactionNewsViewModel Model, DateCollection Date)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var ModelNews = DB.TransactionNews.Where(w => w.TransactionNewsId == Model.TransactionNewsId).FirstOrDefault();
                ModelNews.UserId = (Model.TransactionType == false ? "" : Model.UserId);
                ModelNews.TransactionType = Model.TransactionType;
                ModelNews.LookupId = Model.LookupId;
                ModelNews.TransactionYear = DateTime.Now.Year + 543;
                ModelNews.TransactionBy = CurrentUser.Id;
                ModelNews.TransactionTitle = Model.TransactionTitle;
                ModelNews.TransactionDetail = Model.TransactionDetail;
                ModelNews.NewsStartDate = new DateTime(Date.StartYear, Date.StartMonth, Date.StartDay);
                ModelNews.NewsEndDate = new DateTime(Date.EndYear, Date.EndMonth, Date.EndDay);
                ModelNews.IsActive = Model.IsActive;
                ModelNews.IsApprove = (Model.TransactionType == false ? false : true);
                ModelNews.UpdateDate = DateTime.Now;
                ModelNews.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                DB.TransactionNews.Update(ModelNews);
                await DB.SaveChangesAsync();

                Helper.AddUsedLog(CurrentUser.Id, "เพิ่มข้อมูลข่าวสาร", HttpContext, "ระบบจัดการข่าวสาร");

                var InModel = await DB.TransactionReader.Where(f => f.TransactionNewsId == Model.TransactionNewsId).ToListAsync();
                DB.TransactionReader.RemoveRange(InModel);
                await DB.SaveChangesAsync();

                if (!string.IsNullOrEmpty(Model.UserId))
                {
                    string[] ArrayName = Model.UserId.Split(',');
                    for (int i = 0; i < ArrayName.Length; i++)
                    {
                        var ModelReader = new TransactionReader();
                        ModelReader.TransactionNewsId = ModelNews.TransactionNewsId;
                        ModelReader.UserId = (Model.TransactionType == false ? "-" : ArrayName[i]);
                        ModelReader.TransactionYear = DateTime.Now.Year + 543;
                        ModelReader.IsRead = false;
                        ModelReader.UpdateDate = DateTime.Now;
                        ModelReader.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                        DB.TransactionReader.Update(ModelReader);
                        await DB.SaveChangesAsync();

                        if (Model.IsEmail)
                        {
                            string Email = DB.Users.Where(w => w.Id == ArrayName[i]).Select(s => s.Email).FirstOrDefault();
                            string UserName = DB.Users.Where(w => w.Id == ArrayName[i]).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                            string Title = Model.TransactionTitle;
                            SendMail(Email, UserName, Title);
                        }
                    }
                }
                else
                {
                    var ModelReader = new TransactionReader();
                    ModelReader.TransactionNewsId = ModelNews.TransactionNewsId;
                    ModelReader.UserId = "-";
                    ModelReader.TransactionYear = DateTime.Now.Year + 543;
                    ModelReader.IsRead = false;
                    ModelReader.UpdateDate = DateTime.Now;
                    ModelReader.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                    DB.TransactionReader.Update(ModelReader);
                    await DB.SaveChangesAsync();
                }

                try
                {

                    if (Model.FileName != null)
                    {
                        var Uploads = Path.Combine(_environment.WebRootPath.ToString(), "uploads/news/");
                        var ThisModel = DB.TransactionFileNews.Where(w => w.TransactionNewsId == ModelNews.TransactionNewsId).ToList();

                        //Delete File
                        foreach (var item in ThisModel)
                        {
                            if (item.GencodeFileName != null)
                            {
                                string oldfilepath = Path.Combine(Uploads, item.GencodeFileName);
                                if (System.IO.File.Exists(oldfilepath))
                                {
                                    System.IO.File.Delete(oldfilepath);
                                }
                            }
                        }

                        DB.TransactionFileNews.RemoveRange(ThisModel);
                        DB.SaveChanges();

                        foreach (var FileAtt in Model.FileName)
                        {
                            if (FileAtt.ContentType == "application/pdf" || FileAtt.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || FileAtt.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                            {

                                string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(FileAtt.ContentDisposition).FileName.Trim('"');
                                string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                                using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                                {
                                    await FileAtt.CopyToAsync(fileStream);
                                }

                                var ModelFile = new TransactionFileNews();
                                ModelFile.TransactionNewsId = ModelNews.TransactionNewsId;
                                ModelFile.GencodeFileName = UniqueFileName;
                                ModelFile.TransactionYear = DateTime.Now.Year + 543;
                                ModelFile.UpdateDate = DateTime.Now;
                                ModelFile.FileName = FileAtt.FileName;
                                ModelFile.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                                DB.TransactionFileNews.Add(ModelFile);
                                await DB.SaveChangesAsync();

                                Helper.AddUsedLog(CurrentUser.Id, "แก้ไขไฟล์แนบข่าวสาร", HttpContext, "ระบบจัดการข่าวสาร");
                            }
                            else
                            {
                                throw new Exception("รองรับนามสกุลไฟล์ (xlsx,docx,pdf) เท่านั้น");
                            }
                        }
                    }
                }
                catch (Exception Error)
                {
                    return Json(new { valid = false, message = Error.Message });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> FormAddNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            //ViewBag.UserId = new SelectList(DB.Users.Select(s => new { Id = s.Id, Name = s.FirstName + " " + s.LastName }).ToList(), "Id", "Name");
            ViewBag.LookupId = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 3).Select(s => new { Id = s.LookupId, Name = s.LookupText }).ToList(), "Id", "Name");

            var jsonData = new List<object>();
            var OrgStructures = DB.SystemOrgStructures.Where(w => w.ParentId == 0 || w.ParentId == 1).Select(s => new { OrgId = s.OrgId, OrgName = s.OrgName }).OrderBy(o => o.OrgId).ToList();
            foreach (var item in OrgStructures)
            {
                if (item.OrgId == 1)
                {
                    jsonData.Add(new
                    {
                        title = item.OrgName,
                        subs = DB.Users.Where(w => w.OrgId == item.OrgId).Select(s => new { id = s.Id, title = s.FirstName + " " + s.LastName }).ToList()
                    });
                }
                else
                {
                    jsonData.Add(new
                    {
                        title = item.OrgName,
                        subs = DB.SystemOrgStructures.Where(w => w.ParentId == item.OrgId && w.ParentId > 1).Select(s => new {  title = s.OrgName, subs = DB.Users.Where(ws => ws.OrgId == s.OrgId).Select(ss => new { id = ss.Id, title = ss.FirstName + " " + ss.LastName }).ToList() }).ToList()
                    });
                }
            }
            ViewBag.UserIdSub = JsonConvert.SerializeObject(jsonData);

            //Gen Day Selection 
            ViewBag.Day = new SelectList(new List<DateCollection>(), "Day", "Day");
            //Gen month and Month selection
            string[] MonthName = { "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };
            var Months = new List<SelectListItem>();
            for (int m = 0; m < MonthName.Length; m++)
            {

                Months.Add(new SelectListItem
                {
                    Text = MonthName[m],
                    Value = m.ToString()
                });

            }
            ViewBag.Month = new SelectList(Months, "Value", "Text");
            //Gen Year and Year Selection
            var Years = new List<SelectListItem>();
            for (int y = DateTime.Now.Year; y < (DateTime.Now.Year + 7); y++)
            {
                Years.Add(new SelectListItem
                {
                    Text = (y + 543).ToString(),
                    Value = y.ToString()
                });
            }
            ViewBag.Year = new SelectList(Years, "Value", "Text");

            List<SelectListItem> listTransactionType = new List<SelectListItem>();
            listTransactionType.Add(new SelectListItem
            {
                Text = "สมาชิก",
                Value = "true"
            });
            listTransactionType.Add(new SelectListItem
            {
                Text = "สาธารณะ",
                Value = "false",
                Selected = true
            });
            ViewBag.TransactionType = listTransactionType;

            List<SelectListItem> listIsActive = new List<SelectListItem>();
            listIsActive.Add(new SelectListItem
            {
                Text = "ใช้งาน",
                Value = "true",
                Selected = true
            });
            listIsActive.Add(new SelectListItem
            {
                Text = "ไม่ใช้งาน",
                Value = "false"
            });
            ViewBag.IsActive = listIsActive;
            return View("FormAddNews");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddNews(TransactionNewsViewModel Model, DateCollection Date)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var ModelNews = new TransactionNews();
                ModelNews.UserId = (Model.TransactionType == false ? "" : Model.UserId);
                ModelNews.TransactionType = Model.TransactionType;
                ModelNews.LookupId = Model.LookupId;
                ModelNews.TransactionYear = DateTime.Now.Year + 543;
                ModelNews.TransactionBy = CurrentUser.Id;
                ModelNews.TransactionTitle = Model.TransactionTitle;
                ModelNews.TransactionDetail = Model.TransactionDetail;
                ModelNews.NewsStartDate = new DateTime(Date.StartYear, Date.StartMonth, Date.StartDay);
                ModelNews.NewsEndDate = new DateTime(Date.EndYear, Date.EndMonth, Date.EndDay);
                ModelNews.IsActive = Model.IsActive;
                ModelNews.IsApprove = (Model.TransactionType == false ? false : true);
                ModelNews.UpdateDate = DateTime.Now;
                ModelNews.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                DB.TransactionNews.Add(ModelNews);
                await DB.SaveChangesAsync();

                Helper.AddUsedLog(CurrentUser.Id, "เพิ่มข้อมูลข่าวสาร", HttpContext, "ระบบจัดการข่าวสาร");

                if (!string.IsNullOrEmpty(Model.UserId))
                {
                    string[] ArrayName = Model.UserId.Split(',');
                    for (int i = 0; i < ArrayName.Length; i++)
                    {
                        var ModelReader = new TransactionReader();
                        ModelReader.TransactionNewsId = ModelNews.TransactionNewsId;
                        ModelReader.UserId = (Model.TransactionType == false ? "-" : ArrayName[i]);
                        ModelReader.TransactionYear = DateTime.Now.Year + 543;
                        ModelReader.IsRead = false;
                        ModelReader.UpdateDate = DateTime.Now;
                        ModelReader.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                        DB.TransactionReader.Add(ModelReader);
                        await DB.SaveChangesAsync();

                        if (Model.IsEmail)
                        {
                            string Email = DB.Users.Where(w => w.Id == ArrayName[i]).Select(s => s.Email).FirstOrDefault();
                            string UserName = DB.Users.Where(w => w.Id == ArrayName[i]).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                            string Title = Model.TransactionTitle;
                            SendMail(Email, UserName, Title);
                        }
                    }
                }
                else
                {
                    var ModelReader = new TransactionReader();
                    ModelReader.TransactionNewsId = ModelNews.TransactionNewsId;
                    ModelReader.UserId = "-";
                    ModelReader.TransactionYear = DateTime.Now.Year + 543;
                    ModelReader.IsRead = false;
                    ModelReader.UpdateDate = DateTime.Now;
                    ModelReader.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                    DB.TransactionReader.Add(ModelReader);
                    await DB.SaveChangesAsync();
                }

                try
                {
                    if (Model.FileName != null)
                    {
                        var Uploads = Path.Combine(_environment.WebRootPath.ToString(), "uploads/news/");
                        var ThisModel = DB.TransactionFileNews.Where(w => w.TransactionNewsId == ModelNews.TransactionNewsId).ToList();
                        DB.TransactionFileNews.RemoveRange(ThisModel);
                        DB.SaveChanges();

                        foreach (var FileAtt in Model.FileName)
                        {
                            if (FileAtt.ContentType == "application/pdf" || FileAtt.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || FileAtt.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                            {
                                string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(FileAtt.ContentDisposition).FileName.Trim('"');
                                string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                                using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                                {
                                    await FileAtt.CopyToAsync(fileStream);
                                }

                                var ModelFile = new TransactionFileNews();
                                ModelFile.TransactionNewsId = ModelNews.TransactionNewsId;
                                ModelFile.GencodeFileName = UniqueFileName;
                                ModelFile.TransactionYear = DateTime.Now.Year + 543;
                                ModelFile.UpdateDate = DateTime.Now;
                                ModelFile.FileName = FileAtt.FileName;
                                ModelFile.UpdateBy = DB.Users.Where(w => w.Id == CurrentUser.Id).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                                DB.TransactionFileNews.Add(ModelFile);
                                await DB.SaveChangesAsync();

                                Helper.AddUsedLog(CurrentUser.Id, "แก้ไขไฟล์แนบข่าวสาร", HttpContext, "ระบบจัดการข่าวสาร");
                            }
                            else
                            {
                                throw new Exception("รองรับนามสกุลไฟล์ (xlsx,docx,pdf) เท่านั้น");
                            }
                        }
                    }
                }
                catch (Exception Error)
                {
                    return Json(new { valid = false, message = Error.Message });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteNews(string TransactionNewsId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var TransactionNews = DB.TransactionNews.Where(w => w.TransactionNewsId == int.Parse(TransactionNewsId)).FirstOrDefault();


                //Delete File
                var ThisModel = DB.TransactionFileNews.Where(w => w.TransactionNewsId == TransactionNews.TransactionNewsId).ToList();
                var Uploads = Path.Combine(_environment.WebRootPath.ToString(), "uploads/news/");
                foreach (var item in ThisModel)
                {
                    if (item.GencodeFileName != null)
                    {
                        string oldfilepath = Path.Combine(Uploads, item.GencodeFileName);
                        if (System.IO.File.Exists(oldfilepath))
                        {
                            System.IO.File.Delete(oldfilepath);
                        }
                    }
                }

                DB.TransactionNews.Remove(TransactionNews);
                DB.SaveChanges();

                Helper.AddUsedLog(CurrentUser.Id, "ลบข้อมูลข่าวสาร", HttpContext, "ระบบจัดการข่าวสาร");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> NewsReader(int NewsId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {


            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult ViewsUser(int NewsId)
        {

            ViewBag.NewsId = NewsId;

            return View();
        }

        [HttpGet]
        public IActionResult GetViewsUser(int NewsId)
        {

            var Models = new List<NewsReader>();
            var GetReader = DB.TransactionReader.Where(w => w.TransactionNewsId == NewsId).OrderBy(o => o.TransactionNewsId).ToList();
            foreach (var item in GetReader)
            {
                var Model = new NewsReader();
                Model.UserName = DB.Users.Where(w => w.Id == item.UserId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Model.UpdateDate = Helper.getDateThai(item.UpdateDate);
                Model.IsRead = item.IsRead;
                Models.Add(Model);
            }

            return PartialView(Models);
        }

        #region Public News
        [HttpGet]
        public async Task<IActionResult> PublicNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPublicNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            var Gets = await DB.TransactionNews.Where(w => w.TransactionType == false).ToListAsync();

            var Models = new List<ListNewsViewModel>();
            foreach (var Get in Gets)
            {

                var Model = new ListNewsViewModel();
                Model.TransactionNewsId = Get.TransactionNewsId;
                Model.TransactionTitle = Get.TransactionTitle;
                Model.TransactionBy = Get.UpdateBy;
                Model.UserId = Get.UserId;
                Model.IsActive = Get.IsApprove;
                Model.UpdateDate = Helper.getShortDateThai(Get.UpdateDate);
                Model.TransactionType = Get.TransactionType;
                Model.IsActive = Get.IsActive;
                Model.IsApprove = Get.IsApprove;
                Model._UpdateDate = Get.UpdateDate;
                Models.Add(Model);
            }

            return PartialView("GetPublicNews", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ApproveNews(int NewsId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            try
            {
                var GetData = DB.TransactionNews.Where(w => w.TransactionNewsId == NewsId).FirstOrDefault();
                GetData.IsApprove = true;
                GetData.IsActive = true;
                DB.TransactionNews.Update(GetData);
                await DB.SaveChangesAsync();

                Helper.AddUsedLog(CurrentUser.Id, "อนุมัติข่าวสาร", HttpContext, "ระบบจัดการข่าวสาร");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });


        }
        #endregion


        #region Report
        [HttpGet]
        public async Task<IActionResult> ReportTypeNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();

            var Gets = DB.TransactionNews.ToList().GroupBy(g => g.UpdateDate.Year);
            foreach (var Get in Gets)
            {
                var Model = new ReportTypeNewsViewModel();
                var Years = Get.Select(s => s.UpdateDate.Year).FirstOrDefault();
                Model.TransactionYear = (Years < 2500 ? Years + 543 : Years).ToString();
                Model.AmountMember = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == true && w.IsActive == true).Count();
                Model.AmountPublic = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == false && w.IsActive == true).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.IsActive == true).Count();
                Models.Add(Model);
            }

            return View("ReportTypeNews", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportApproveNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();

            var Gets = DB.TransactionNews.ToList().GroupBy(g => g.UpdateDate.Year);
            foreach (var Get in Gets)
            {
                var Model = new ReportTypeNewsViewModel();
                var Years = Get.Select(s => s.UpdateDate.Year).FirstOrDefault();
                Model.TransactionYear = (Years < 2500 ? Years + 543 : Years).ToString();
                Model.AmountWait = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == false && w.IsApprove == false && w.IsActive == true).Count();
                Model.AmountApprove = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == false && w.IsApprove == true && w.IsActive == true).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == false && w.IsActive == true).Count();
                Models.Add(Model);
            }

            return View("ReportApproveNews", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportTypeNewsByMonth()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.CurrentBudgetYear = (DateTime.Now.Year < 2500 ? DateTime.Now.Year + 543 : DateTime.Now.Year);
            return View("ReportTypeNewsByMonth");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportType(int Years)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();
            String[] Months = { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };

            for (int i = 1; i <= 12; i++)
            {
                var Model = new ReportTypeNewsViewModel();
                int Y = (Years > 2500 ? Years - 543 : Years);
                Model.TransactionYear = Years.ToString();
                Model.Month = Months[i - 1];
                Model.AmountMember = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.TransactionType == true && w.IsActive == true && w.UpdateDate.Month == i).Count();
                Model.AmountPublic = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.TransactionType == false && w.IsActive == true && w.UpdateDate.Month == i).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.IsActive == true && w.UpdateDate.Month == i).Count();
                Models.Add(Model);
            }

            return PartialView("GetReportType", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportApproveNewsByMonth()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.CurrentBudgetYear = (DateTime.Now.Year < 2500 ? DateTime.Now.Year + 543 : DateTime.Now.Year);

            return View("ReportApproveNewsByMonth");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportApprove(int Years)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();
            String[] Months = { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };

            for (int i = 1; i <= 12; i++)
            {
                var Model = new ReportTypeNewsViewModel();
                int Y = (Years > 2500 ? Years - 543 : Years);
                Model.TransactionYear = Years.ToString();
                Model.Month = Months[i - 1];
                Model.AmountWait = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.TransactionType == false && w.IsApprove == false && w.IsActive == true && w.UpdateDate.Month == i).Count();
                Model.AmountApprove = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.TransactionType == false && w.IsApprove == true && w.IsActive == true && w.UpdateDate.Month == i).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.TransactionType == false && w.IsActive == true && w.UpdateDate.Month == i).Count();
                Models.Add(Model);
            }
            return PartialView("GetReportApprove", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportCreatorNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.CurrentBudgetYear = (DateTime.Now.Year < 2500 ? DateTime.Now.Year + 543 : DateTime.Now.Year);

            return View("ReportCreatorNews");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportCreator(int Years)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();
            int Y = (Years > 2500 ? Years - 543 : Years);
            var Gets = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.IsActive == true).ToList().GroupBy(g => g.TransactionBy);
            int i = 0;
            foreach (var Get in Gets)
            {
                var Model = new ReportTypeNewsViewModel();
                var Year = Get.Select(s => s.UpdateDate.Year).FirstOrDefault();
                var Transaction = Get.Select(s => s.TransactionBy).FirstOrDefault();
                Model.TransactionYear = (Year < 2500 ? Year + 543 : Year).ToString();
                Model.fullName = DB.Users.Where(w => w.Id == Transaction).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Model.AmountMember = DB.TransactionNews.Where(w => w.UpdateDate.Year == Year && w.TransactionType == true && w.IsActive == true && w.TransactionBy == Transaction).Count();
                Model.AmountPublic = DB.TransactionNews.Where(w => w.UpdateDate.Year == Year && w.TransactionType == false && w.IsActive == true && w.TransactionBy == Transaction).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Year && w.IsActive == true && w.TransactionBy == Transaction).Count();
                Models.Add(Model);
                i++;
            }
            return PartialView("GetReportCreator", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportCreatorMostNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();

            var Gets = DB.TransactionNews.ToList().GroupBy(g => g.UpdateDate.Year);
            foreach (var Get in Gets)
            {
                var Model = new ReportTypeNewsViewModel();
                var Years = Get.Select(s => s.UpdateDate.Year).FirstOrDefault();
                var Tran = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.IsActive == true).ToList().GroupBy(g => g.TransactionBy).Select(s => new { Count = s.Count(), TransactionBy = s.Max(m => m.TransactionBy) });
                var TranMax = Tran.OrderByDescending(o => o.Count).Select(s => s.TransactionBy).FirstOrDefault();
                Model.TransactionYear = (Years < 2500 ? Years + 543 : Years).ToString();
                Model.fullName = DB.Users.Where(w => w.Id == TranMax).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Model.AmountMember = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == true && w.IsActive == true && w.TransactionBy == TranMax).Count();
                Model.AmountPublic = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == false && w.IsActive == true && w.TransactionBy == TranMax).Count();
                Model.AmountCreate = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.IsActive == true && w.TransactionBy == TranMax).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.IsActive == true).Count();
                Models.Add(Model);
            }

            return View("ReportCreatorMostNews", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportCreatorMostNewsByMonth()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.CurrentBudgetYear = (DateTime.Now.Year < 2500 ? DateTime.Now.Year + 543 : DateTime.Now.Year);

            return View("ReportCreatorMostNewsByMonth");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportCreatorMost(int Years)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();
            String[] Months = { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };

            for (int i = 1; i <= 12; i++)
            {
                var Model = new ReportTypeNewsViewModel();
                int Y = (Years > 2500 ? Years - 543 : Years);
                Model.TransactionYear = Years.ToString();
                var Tran = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.IsActive == true).ToList().GroupBy(g => g.TransactionBy).Select(s => new { Count = s.Count(), TransactionBy = s.Max(m => m.TransactionBy) });
                var TranMax = Tran.OrderByDescending(o => o.Count).Select(s => s.TransactionBy).FirstOrDefault();
                Model.TransactionYear = (Years < 2500 ? Years + 543 : Years).ToString();
                Model.fullName = DB.Users.Where(w => w.Id == TranMax).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Model.Month = Months[i - 1];
                Model.AmountMember = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.TransactionType == true && w.IsActive == true && w.TransactionBy == TranMax && w.UpdateDate.Month == i).Count();
                Model.AmountPublic = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.TransactionType == false && w.IsActive == true && w.TransactionBy == TranMax && w.UpdateDate.Month == i).Count();
                Model.AmountCreate = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.IsActive == true && w.TransactionBy == TranMax && w.UpdateDate.Month == i).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Y && w.IsActive == true && w.UpdateDate.Month == i).Count();
                Models.Add(Model);
            }

            return PartialView("GetReportCreatorMost", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportMonthMostNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();
            String[] Months = { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };

            var Gets = DB.TransactionNews.ToList().GroupBy(g => g.UpdateDate.Year);
            foreach (var Get in Gets)
            {
                var Model = new ReportTypeNewsViewModel();
                var Years = Get.Select(s => s.UpdateDate.Year).FirstOrDefault();
                var Month = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.IsActive == true).ToList().GroupBy(g => g.UpdateDate.Month).Select(s => new { Count = s.Count(), Month = s.Max(m => m.UpdateDate.Month) });
                var MonthMax = Month.OrderByDescending(o => o.Count).Select(s => s.Month).FirstOrDefault();
                Model.Month = Months[MonthMax - 1];
                Model.TransactionYear = (Years < 2500 ? Years + 543 : Years).ToString();
                Model.AmountMember = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == true && w.IsActive == true && w.UpdateDate.Month == MonthMax).Count();
                Model.AmountPublic = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == false && w.IsActive == true && w.UpdateDate.Month == MonthMax).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.IsActive == true && w.UpdateDate.Month == MonthMax).Count();
                Models.Add(Model);
            }

            return View("ReportMonthMostNews", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportOnNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();

            var Gets = DB.TransactionNews.ToList().GroupBy(g => g.UpdateDate.Year);
            foreach (var Get in Gets)
            {
                var Model = new ReportTypeNewsViewModel();
                var Years = Get.Select(s => s.UpdateDate.Year).FirstOrDefault();
                Model.TransactionYear = (Years < 2500 ? Years + 543 : Years).ToString();
                string _Date = (DateTime.Now.Year < 2500 ? DateTime.Now.ToString("dd/MM/yyyy") : DateTime.Now.AddYears(-543).ToString("dd/MM/yyyy"));
                Model.AmountMember = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == true && w.IsActive == true && w.NewsStartDate < DateTime.Parse(_Date) && w.NewsEndDate > DateTime.Parse(_Date)).Count();
                Model.AmountPublic = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == false && w.IsActive == true && w.NewsStartDate < DateTime.Parse(_Date) && w.NewsEndDate > DateTime.Parse(_Date)).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.IsActive == true && w.NewsStartDate < DateTime.Parse(_Date) && w.NewsEndDate > DateTime.Parse(_Date)).Count();
                if (Model.AmountNews > 0)
                    Models.Add(Model);
            }

            return View("ReportOnNews", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ReportLateNews()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ReportTypeNewsViewModel>();

            var Gets = DB.TransactionNews.ToList().GroupBy(g => g.UpdateDate.Year);
            foreach (var Get in Gets)
            {
                var Model = new ReportTypeNewsViewModel();
                var Years = Get.Select(s => s.UpdateDate.Year).FirstOrDefault();
                Model.TransactionYear = (Years < 2500 ? Years + 543 : Years).ToString();
                string _Date = (DateTime.Now.Year < 2500 ? DateTime.Now.ToString("dd/MM/yyyy") : DateTime.Now.AddYears(-543).ToString("dd/MM/yyyy"));
                Model.AmountMember = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == true && w.IsActive == true && w.NewsEndDate < DateTime.Parse(_Date)).Count();
                Model.AmountPublic = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.TransactionType == false && w.IsActive == true && w.NewsEndDate < DateTime.Parse(_Date)).Count();
                Model.AmountNews = DB.TransactionNews.Where(w => w.UpdateDate.Year == Years && w.IsActive == true && w.NewsEndDate < DateTime.Parse(_Date)).Count();
                if (Model.AmountNews > 0)
                    Models.Add(Model);
            }

            return View("ReportLateNews", Models);
        }



        #endregion
        #region Send Email
        public async void SendMail(string Email, string UserId, string Title)
        {

            string Subject = "ระบบสื่อสารสมาชิก :" + Email;
            string Body = "<div style='margin-bottom:10px'><h1><b>สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ</b><sup style='font-size:10px'>สทบ.</sup></h1></div>"
                   + "\n <br/><div style='background-color:#f26822 ;height:8px; margin-bottom:20px'>&nbsp;</div>"
                   + "\n <br/> เรียนคุณ " + UserId + " "
                   + "\n <br/> มีประกาศ/ข่าวสารเรื่อง " + Title + " ส่งถึงคุณ"
                   + "\n <br/> "
                   + "\n <br/> - ที่ตั้งสำนักงานส่วนกลาง สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.)"
                   + "\n <br/> - เบอร์โทรศัพท์: 02-100-4209"
                   + "\n <br/> - เบอร์แฟกซ์: 02-100-4203";

            await Helper.SendMailWithSubject(Email, Subject, Body);
        }
        #endregion
        #region Helper
        public static bool IsValidDate(string Value, string DateFormats)
        {
            DateTime TempDate;
            bool validDate = DateTime.TryParseExact(Value, DateFormats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out TempDate);
            if (validDate)
                return true;
            else
                return false;
        }
        public static DateTime StringToDateTime(string StringDate)
        {

            //Helper For Insert DateThaiPicker into DataBase
            //StringDate Parameter Can Use Only dd/mm/yyyy Date Formats

            return new DateTime(Convert.ToInt32(StringDate.Split("/")[2]), Convert.ToInt32(StringDate.Split("/")[1]), Convert.ToInt32(StringDate.Split("/")[0]));
        }
        #endregion
    }
}
