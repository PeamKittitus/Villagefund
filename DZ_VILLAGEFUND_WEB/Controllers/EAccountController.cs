using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.DateCollection;
using DZ_VILLAGEFUND_WEB.ViewModels.EAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Engineering;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Threading.Tasks;
using static DZ_VILLAGEFUND_WEB.ViewModels.EAccount.ReportProjectType;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.Internal.AsyncLock;
using static System.Net.WebRequestMethods;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    [Authorize]
    public class EAccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IHostingEnvironment _environment;
        private string SystemName = "e-Account";
        private readonly IConfiguration _configuration;


        public EAccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext db,
            IConfiguration configuration,
            IHostingEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
            _environment = environment;
        }

        #region transaction account

        [HttpGet]
        public async Task<IActionResult> Index(int BudgetYear)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            string RoleUser = Helper.GetUserRoleName(CurrentUser.Id);
            if (RoleUser == "SuperUser")
            {
                return RedirectToAction("ActIndex");
            }

            //Project
            var GetAccountBudget = DB.AccountBudget.Where(w => w.ParentId != 0 && w.IsActive == true && w.AccEndDate > DateTime.Now && (BudgetYear != 0 ? w.BudgetYear == BudgetYear : true)).ToList();
            ViewBag.AccountBudget = new SelectList(GetAccountBudget, "AccountBudgetd", "AccName");

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.TransactionType = (RoleUser == "BranchAdmin" ? 1 : 0);

            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> LedgerReportIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id).ToLower();
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            var GetAccountBudget = DB.AccountBudgetCenter.Where(w => w.IsActive == true).ToList();

            var SelectAccountAct = new List<SelectListItem>();

            foreach (var Get in GetAccountBudget.Where(w => w.ParentId == 0).OrderBy(w => w.AccountBudgetCenterId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccName,
                    Value = Get.AccountBudgetCenterId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetAccountBudget.Where(w => w.ParentId == Get.AccountBudgetCenterId).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccName,
                        Value = Get2.AccountBudgetCenterId.ToString()
                    });
                }
            }
            ViewBag.AccountBudget = SelectAccountAct;

            //กลุ่ม
            ViewBag.UserLookupValueDivision = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 1).ToList(), "LookupText", "LookupText");
            //ฝ่าย
            ViewBag.UserLookupValueDepartment = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 2).ToList(), "LookupText", "LookupText");

            return View("LedgerReportIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions(int BudgetYear, int AccountBudgetId, int TransactionType)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string RoleUser = Helper.GetUserRoleName(CurrentUser.Id);
            var GetVillage = DB.Village.Where(w => w.UserId == CurrentUser.Id).FirstOrDefault();

            // get data 
            var Models = new List<TransactionAccBudgetViewModel>();
            if (TransactionType == 1) // รับ
            {
                var Gets = await DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear
                            && w.AccountBudgetd == AccountBudgetId
                            && (RoleUser == "SuperUser" ? w.ReceiverOrgId == GetVillage.VillageId : w.ReceiverOrgId == CurrentUser.OrgId)).ToListAsync();
                foreach (var Get in Gets)
                {
                    var Model = new TransactionAccBudgetViewModel();
                    Model.Amount = Get.Amount;
                    Model.TransactionType = Get.TransactionType;
                    Model.Title = Get.Title;
                    Model.TransactionAccBudgetId = Get.TransactionAccBudgeId;
                    Model.SendDate = Helper.getDateThai(Get.SenderDate);
                    Model.ToOrgAccount = (DB.Village.Where(w => w.VillageId == Get.SenderOrgId).Select(s => s.VillageName).FirstOrDefault() == null ? DB.SystemOrgStructures.Where(w => w.OrgId == Get.SenderOrgId).Select(s => s.OrgName).FirstOrDefault() : DB.Village.Where(w => w.VillageId == Get.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault());
                    Models.Add(Model);
                }
            }
            else if (TransactionType == 2) // ส่ง
            {
                var Gets = await DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear
                                            && w.AccountBudgetd == AccountBudgetId
                                            && (RoleUser == "SuperUser" ? w.SenderOrgId == GetVillage.VillageId : w.SenderOrgId == CurrentUser.OrgId)).ToListAsync();
                foreach (var Get in Gets)
                {
                    var Model = new TransactionAccBudgetViewModel();
                    Model.Amount = Get.Amount;
                    Model.TransactionType = Get.TransactionType;
                    Model.Title = Get.Title;
                    Model.TransactionAccBudgetId = Get.TransactionAccBudgeId;
                    Model.SendDate = Helper.getDateThai(Get.SenderDate);
                    Model.ToOrgAccount = (DB.Village.Where(w => w.VillageId == Get.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault() == null ? DB.SystemOrgStructures.Where(w => w.OrgId == Get.ReceiverOrgId).Select(s => s.OrgName).FirstOrDefault() : DB.Village.Where(w => w.VillageId == Get.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault());
                    Models.Add(Model);
                }
            }
            else
            {
                var Gets = await DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear
                                            && w.AccountBudgetd == AccountBudgetId
                                            && (RoleUser == "SuperUser" ? w.SenderOrgId == GetVillage.VillageId : w.SenderOrgId == CurrentUser.OrgId)).ToListAsync();
                foreach (var Get in Gets)
                {
                    var Model = new TransactionAccBudgetViewModel();
                    Model.Amount = Get.Amount;
                    Model.TransactionType = Get.TransactionType;
                    Model.Title = Get.Title;
                    Model.TransactionAccBudgetId = Get.TransactionAccBudgeId;
                    Model.SendDate = Helper.getDateThai(Get.SenderDate);
                    Model.ToOrgAccount = (DB.Village.Where(w => w.VillageId == Get.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault() == null ? DB.SystemOrgStructures.Where(w => w.OrgId == Get.ReceiverOrgId).Select(s => s.OrgName).FirstOrDefault() : DB.Village.Where(w => w.VillageId == Get.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault());
                    Models.Add(Model);
                }
            }

            ViewBag.TransactionType = TransactionType;


            return PartialView("GetTransactions", Models);
        }

        [HttpGet]
        public async Task<IActionResult> GetLedgerReport(int BudgetYear, int AccountBudgetId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id).ToLower();
            var Models = new List<LedgerReportViewModel>();


            var Gets = DB.TransactionAccountActivityCenter.Where(w => w.AccChartId == AccountBudgetId).ToList();
            foreach (var Get in Gets)
            {

                var Model = new LedgerReportViewModel();
                Model.Amount = Get.Amount;
                Model.Receiver = DB.SystemLookupMaster.Where(w => w.LookupGroupId == 1 && w.LookupValue == Get.LookupValueDivision).Select(s => s.LookupText).FirstOrDefault() + "/"
                    + DB.SystemLookupMaster.Where(w => w.LookupGroupId == 2 && w.LookupValue == Get.LookupValueDepartment).Select(s => s.LookupText).FirstOrDefault();
                Model.ReceiverDate = Helper.getDateThai(Get.ReceiverDate);

                Models.Add(Model);
            }

            return PartialView("GetLedgerReport", Models);
        }

        [HttpGet]
        public async Task<IActionResult> FormAddTransaction()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string RoleUser = Helper.GetUserRoleName(CurrentUser.Id);
            if (RoleUser == "HeadQuarterAdmin" || RoleUser == "administrator")
            {
                ViewBag.Village = new SelectList(DB.Village.OrderBy(e => e.OrgId).ToList(), "VillageId", "VillageName");
                ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.UpdateBy == CurrentUser.Id).ToList(), "ProjectBbId", "BookBankName");
                ViewBag.BranceOrg = new SelectList(DB.SystemOrgStructures.Where(w => w.ParentId != 0).ToList(), "OrgId", "OrgName");
            }
            if (RoleUser == "BranchAdmin")
            {
                ViewBag.Village = new SelectList(DB.Village.Where(w => DB.SystemOrgStructures.Where(w => w.ParentId == CurrentUser.OrgId).Select(s => s.OrgId).ToArray().Contains(w.OrgId)).OrderBy(e => e.OrgId).ToList(), "VillageId", "VillageName");
                ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.OrgId == CurrentUser.OrgId && w.IsBrance == false).ToList(), "ProjectBbId", "BookBankName");
                ViewBag.BranceOrg = new SelectList(DB.SystemOrgStructures.Where(w => w.ParentId == CurrentUser.OrgId).ToList(), "OrgId", "OrgName");
            }
            if (RoleUser == "ProvinceAdmin")
            {
                ViewBag.Village = new SelectList(DB.Village.Where(w => w.OrgId == CurrentUser.OrgId).OrderBy(e => e.OrgId).ToList(), "VillageId", "VillageName");
                ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.OrgId == CurrentUser.OrgId && w.IsBrance == false).ToList(), "ProjectBbId", "BookBankName");
                ViewBag.BranceOrg = new SelectList(DB.SystemOrgStructures.Where(w => w.ParentId == CurrentUser.OrgId).ToList(), "OrgId", "OrgName");
            }

            var AccountBudget = new List<SelectListItem>();
            var GetAccBudgets = await DB.AccountBudget.Where(w => w.BudgetYear == Helper.CurrentBudgetYear() && DB.ProjectBudget.Where(w => w.StatusId == 17).Any(s => s.AccountBudgetd == w.AccountBudgetd)).ToListAsync();
            var Gets = await DB.AccountBudget.Where(w => w.ParentId == 0 && w.BudgetYear == Helper.CurrentBudgetYear()).OrderBy(w => w.AccountBudgetd).ToListAsync();
            foreach (var Get in Gets)
            {
                AccountBudget.Add(new SelectListItem()
                {
                    Text = Get.AccName,
                    Value = Get.AccountBudgetd.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetAccBudgets.Where(w => w.ParentId == Get.AccountBudgetd).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    AccountBudget.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccName,
                        Value = Get2.AccountBudgetd.ToString()
                    });
                }
            }

            ViewBag.AccountBudgetd = AccountBudget;
            ViewBag.RoleUser = RoleUser;


            return View("FormAddTransaction");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddTransaction(TransacionAccountBudget Model, IFormFile[] FileUpload, int Village, int VillageBrance, int OrgType)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetVillage = DB.Village.Where(w => w.VillageId == Village).FirstOrDefault();

            try
            {
                var GetProjectBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefault() != null ? DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefault() : new AccountBudget();
                if (GetProjectBudget.SubAmount == 0)
                {

                    if (Model.Amount > GetProjectBudget.Amount)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบยอดเงินของท่าน" });
                    }
                }

                if (Model.Amount > GetProjectBudget.SubAmount)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบยอดเงินของท่าน" });
                }

                //// check amount on account
                //var InAmount = DB.TransacionAccountBudget.Where(w => w.ReceiverBookBankId == Model.SenderBookBankId).Select(s => s.Amount).Sum();
                //var OutAmount = DB.TransacionAccountBudget.Where(w => w.SenderBookBankId == Model.SenderBookBankId).Select(s => s.Amount).Sum();
                //decimal Total = (InAmount - OutAmount);
                //if (Model.Amount > Total)
                //{
                //    return Json(new { valid = false, message = "ยอดเงินในบัญชีไม่เพียงพอ กรุณาตรวจสอบ ยอดเงินคงเหลือ : " + Total.ToString("N") + " บาท" });
                //}

                // add transaction acc budget 
                Model.TransactionYear = Helper.CurrentBudgetYear();
                Model.TransactionType = false;
                Model.SenderOrgId = CurrentUser.OrgId;
                Model.SenderDate = DateTime.Now;
                Model.ReceiverDate = DateTime.Now;
                Model.UpdateDate = DateTime.Now;
                Model.UpdateBy = CurrentUser.Id;
                Model.ReceiverOrgId = (OrgType == 1 ? VillageBrance : Village);
                Model.Title = "โอนเงินจากโครงการ";
                DB.TransacionAccountBudget.Add(Model);
                await DB.SaveChangesAsync();

                // update file 
                if (FileUpload.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                    foreach (var File in FileUpload)
                    {
                        if (File.ContentType == "application/pdf" || File.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || File.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                        {
                            string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await File.CopyToAsync(fileStream);
                            }

                            // insert data
                            var FileData = new TransactionFileBudget();
                            FileData.TransactionAccBudgeId = Model.TransactionAccBudgeId;
                            FileData.TransactionYear = Helper.CurrentBudgetYear();
                            FileData.FileName = File.FileName;
                            FileData.GencodeFileName = UniqueFileName;
                            FileData.UpdateBy = CurrentUser.Id;
                            FileData.UpdateDate = DateTime.Now;
                            DB.TransactionFileBudget.Add(FileData);

                        }
                    }

                    await DB.SaveChangesAsync();

                    // add log
                    Helper.AddUsedLog(CurrentUser.Id, "บันทึกจ่ายเงินงบประมาณ", HttpContext, SystemName);
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> FormEditTransaction(Int64 TransactionAccBudgetId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            // set data 
            var GetTransactionBudget = await DB.TransacionAccountBudget.Where(w => w.TransactionAccBudgeId == TransactionAccBudgetId).FirstOrDefaultAsync();

            if (GetTransactionBudget == null)
            {
                GetTransactionBudget = new TransacionAccountBudget();
            }

            string RoleUser = Helper.GetUserRoleName(CurrentUser.Id);
            if (RoleUser == "HeadQuarterAdmin" || RoleUser == "administrator")
            {
                ViewBag.Village = new SelectList(DB.Village.OrderBy(e => e.OrgId).ToList(), "VillageId", "VillageName", GetTransactionBudget.ReceiverOrgId);
                ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.OrgId == CurrentUser.OrgId).ToList(), "ProjectBbId", "BookBankName");
                ViewBag.BranceOrg = new SelectList(DB.SystemOrgStructures.Where(w => w.ParentId != 0).ToList(), "OrgId", "OrgName", GetTransactionBudget.ReceiverOrgId);
            }
            if (RoleUser == "BranchAdmin")
            {
                ViewBag.Village = new SelectList(DB.Village.Where(w => DB.SystemOrgStructures.Where(w => w.ParentId == CurrentUser.OrgId).Select(s => s.OrgId).ToArray().Contains(w.OrgId)).OrderBy(e => e.OrgId).ToList(), "VillageId", "VillageName", GetTransactionBudget.ReceiverOrgId);
                ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.OrgId == CurrentUser.OrgId && w.IsBrance == false).ToList(), "ProjectBbId", "BookBankName");
                ViewBag.BranceOrg = new SelectList(DB.SystemOrgStructures.Where(w => w.ParentId == CurrentUser.OrgId).ToList(), "OrgId", "OrgName", GetTransactionBudget.ReceiverOrgId);
            }
            if (RoleUser == "ProvinceAdmin")
            {
                ViewBag.Village = new SelectList(DB.Village.Where(w => w.OrgId == CurrentUser.OrgId).OrderBy(e => e.OrgId).ToList(), "VillageId", "VillageName", GetTransactionBudget.ReceiverOrgId);
                ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.OrgId == CurrentUser.OrgId && w.IsBrance == false).ToList(), "ProjectBbId", "BookBankName");
                ViewBag.BranceOrg = new SelectList(DB.SystemOrgStructures.Where(w => w.ParentId == CurrentUser.OrgId).ToList(), "OrgId", "OrgName", GetTransactionBudget.ReceiverOrgId);
            }

            var AccountBudget = new List<SelectListItem>();
            var GetAccBudgets = await DB.AccountBudget.Where(w => w.BudgetYear == Helper.CurrentBudgetYear()).ToListAsync();
            var Gets = await DB.AccountBudget.Where(w => w.ParentId == 0 && w.BudgetYear == Helper.CurrentBudgetYear()).OrderBy(w => w.AccountBudgetd).ToListAsync();
            foreach (var Get in Gets)
            {
                AccountBudget.Add(new SelectListItem()
                {
                    Text = Get.AccName,
                    Value = Get.AccountBudgetd.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetAccBudgets.Where(w => w.ParentId == Get.AccountBudgetd).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    AccountBudget.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccName,
                        Value = Get2.AccountBudgetd.ToString(),
                        Selected = (Get2.AccountBudgetd == GetTransactionBudget.AccountBudgetd ? true : false)
                    });
                }
            }

            ViewBag.AccountBudgetd = AccountBudget;
            ViewBag.RoleUser = RoleUser;
            ViewBag.OrgType = (DB.Village.Where(w => w.VillageId == GetTransactionBudget.ReceiverOrgId).FirstOrDefault() == null ? 1 : 0);

            return View("FormEditTransaction", GetTransactionBudget);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditTransaction(TransacionAccountBudget Model, IFormFile[] FileUpload, int Village, int VillageBrance, int OrgType)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetVillage = DB.Village.Where(w => w.VillageId == Village).FirstOrDefault();

            try
            {
                // check amount on account
                //if (Model.Amount > DB.AccountBookBank.Where(w => w.ProjectBbId == Model.SenderBookBankId).Select(s => s.Amount).FirstOrDefault())
                //{
                //    return Json(new { valid = false, message = "ยอดเงินในบัญชีไม่เพียงพอ กรุณาตรวจสอบ" });
                //}

                var GetProjectBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefault();
                if (GetProjectBudget.SubAmount == 0)
                {

                    if (Model.Amount > GetProjectBudget.Amount)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบยอดเงินของท่าน" });
                    }
                }

                if (Model.Amount > GetProjectBudget.SubAmount)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบยอดเงินของท่าน" });
                }

                // get data
                var GetData = await DB.TransacionAccountBudget.Where(w => w.TransactionAccBudgeId == Model.TransactionAccBudgeId).FirstOrDefaultAsync();
                GetData.AccountBudgetd = Model.AccountBudgetd;
                GetData.SenderBookBankId = Model.SenderBookBankId;
                GetData.ReceiverOrgId = (OrgType == 1 ? VillageBrance : Village);
                GetData.ReceiverBookBankId = Model.ReceiverBookBankId;
                GetData.Amount = Model.Amount;
                GetData.UpdateBy = CurrentUser.Id;
                GetData.UpdateDate = DateTime.Now;
                DB.TransacionAccountBudget.Update(GetData);
                await DB.SaveChangesAsync();

                // update file 
                if (FileUpload.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                    foreach (var File in FileUpload)
                    {
                        if (File.ContentType == "application/pdf" || File.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || File.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
                        {
                            string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await File.CopyToAsync(fileStream);
                            }

                            // insert data
                            var FileData = new TransactionFileBudget();
                            FileData.TransactionAccBudgeId = Model.TransactionAccBudgeId;
                            FileData.TransactionYear = Helper.CurrentBudgetYear();
                            FileData.FileName = File.FileName;
                            FileData.GencodeFileName = UniqueFileName;
                            FileData.UpdateBy = CurrentUser.Id;
                            FileData.UpdateDate = DateTime.Now;
                            DB.TransactionFileBudget.Add(FileData);
                        }
                    }

                    await DB.SaveChangesAsync();

                    // add log
                    Helper.AddUsedLog(CurrentUser.Id, "แก้ไขจ่ายเงินงบประมาณ", HttpContext, SystemName);
                }

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteTransaction(int TransactionAccBudgetId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // check file document
                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                var GetFiles = await DB.TransactionFileBudget.Where(w => w.TransactionAccBudgeId == TransactionAccBudgetId).ToListAsync();
                if (GetFiles.Count() > 0)
                {
                    foreach (var File in GetFiles)
                    {
                        string Oldfilepath = Path.Combine(Uploads, File.GencodeFileName);
                        if (System.IO.File.Exists(Oldfilepath))
                        {
                            System.IO.File.Delete(Oldfilepath);
                        }
                    }
                }

                // find data
                var Gets = await DB.TransacionAccountBudget.FirstOrDefaultAsync(f => f.TransactionAccBudgeId == TransactionAccBudgetId);
                DB.TransacionAccountBudget.Remove(Gets);
                await DB.SaveChangesAsync();

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "ลบรายการจ่ายเงินงบประมาณ", HttpContext, SystemName);
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> ViewDocuments(Int64 TransactionAccBudgetId)
        {
            // get data
            var Gets = await DB.TransactionFileBudget.Where(w => w.TransactionAccBudgeId == TransactionAccBudgetId).ToListAsync();
            return PartialView("ViewDocuments", Gets);
        }

        [HttpGet]
        public async Task<IActionResult> ViewDocumentsNotApprove(Int64 TransactionAccBudgetId)
        {
            // get data
            var Gets = await DB.TransactionFileBudget.Where(w => w.TransactionAccBudgeId == TransactionAccBudgetId).ToListAsync();
            return PartialView("ViewDocumentsNotApprove", Gets);
        }

        [HttpGet]
        public async Task<IActionResult> ViewBookbankDocuments(Int64 BookbankId)
        {
            // get data
            var Gets = await DB.TransactionFileBookbank.Where(w => w.BookBankId == BookbankId).ToListAsync();
            return PartialView("ViewBookbankDocuments", Gets);
        }

        [HttpGet]
        public IActionResult UpdateApproveFile(Int64 TransactionFileBudgetId)
        {
            try
            {
                // update is approve is true
                var Get = DB.TransactionFileBudget.Where(w => w.TransactionFileBudgetId == TransactionFileBudgetId).FirstOrDefault();
                Get.IsApprove = (Get.IsApprove == true ? false : true);
                DB.TransactionFileBudget.Update(Get);
                DB.SaveChanges();

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> VillageReceive()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            List<SelectListItem> SelectMyProject = new List<SelectListItem>();
            foreach (var item in DB.TransacionAccountBudget.Where(w => w.ReceiverOrgId == CurrentUser.VillageId).ToList().GroupBy(g => g.AccountBudgetd))
            {
                SelectMyProject.Add(
                   new SelectListItem()
                   {
                       Text = DB.AccountBudget.Where(w => w.AccountBudgetd == item.Key).Select(s => s.AccName).FirstOrDefault(),
                       Value = item.Key.ToString()
                   });
            }

            ViewBag.SelectMyProject = SelectMyProject;
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return View("VillageReceive");
        }

        [HttpGet]
        public async Task<IActionResult> GetVillageReceive(int AccountBudgetId, int BudgetYear)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var Models = new List<TransactionAccBudgetViewModel>();
            var Gets = DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear && w.ReceiverOrgId == CurrentUser.VillageId && w.AccountBudgetd == AccountBudgetId).ToList();
            foreach (var Get in Gets)
            {
                var Model = new TransactionAccBudgetViewModel();
                Model.Amount = Get.Amount;
                Model.TransactionType = Get.TransactionType;
                Model.Title = Get.Title;
                Model.TransactionAccBudgetId = Get.TransactionAccBudgeId;
                Model.SendDate = Helper.getDateThai(Get.SenderDate);
                Model.ToOrgAccount = (DB.Village.Where(w => w.VillageId == Get.SenderOrgId).Select(s => s.VillageName).FirstOrDefault() == null ? DB.SystemOrgStructures.Where(w => w.OrgId == Get.SenderOrgId).Select(s => s.OrgName).FirstOrDefault() : DB.Village.Where(w => w.VillageId == Get.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault());
                Models.Add(Model);
            }

            return PartialView("GetVillageReceive", Models);
        }


        #endregion

        #region transaction Activity
        public async Task<IActionResult> ActIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            ViewBag.SelectMyProject = SelectMyProject(CurrentUser.VillageId);
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return View("ActIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetActTransactions(int BudgetYear, Int64 ProjectId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            // master data 
            var DataAccountChart = DB.AccountActivity;
            string[] TransType = { "เงินสด", "โอนเข้าบัญชี", "เช็ค" };
            // get data 
            var Models = new List<TransactionActivityViewModel>();

            foreach (var item in DB.TransactionAccountActivity.Where(w => w.TransactionYear == BudgetYear && w.ProjectId == ProjectId).ToList())
            {
                var Model = new TransactionActivityViewModel()
                {
                    TransactionAccActivityId = item.TransactionAccActivityId,
                    AccChartName = DataAccountChart.Where(w => w.AccountChartId == item.AccChartId).Select(s => s.AccountName).FirstOrDefault(),
                    AccountType = DataAccountChart.Where(w => w.AccountChartId == item.AccChartId).Select(s => s.AccountType).FirstOrDefault(),
                    TransactionType = TransType[item.TransactionType],
                    Amount = item.Amount,
                    Receiver = item.Receiver,
                    ReceiverDate = Helper.getDateThai(item.ReceiverDate)
                };
                Models.Add(Model);
            }
            return PartialView("GetActTransactions", Models);
        }

        [HttpGet]
        public async Task<IActionResult> FormAddActTransaction()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id);

            var SelectAccountAct = new List<SelectListItem>();
            var GetActBudgets = new List<AccountActivity>();

            //Check IsChartCenter
            if (RoleName == "administrator")
            {
                GetActBudgets = DB.AccountActivity.ToList();
            }
            else if (RoleName == "HeadQuarterAdmin")
            {
                GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == true).ToList();
            }
            else
            {
                GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == false).ToList();
            }

            foreach (var Get in GetActBudgets.Where(w => w.AccountParentId == 0 && w.AccountType == true).OrderBy(w => w.AccountChartId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccountName,
                    Value = Get.AccountChartId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetActBudgets.Where(w => w.AccountParentId == Get.AccountChartId && w.AccountType == true).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccountName,
                        Value = Get2.AccountChartId.ToString()
                    });
                }
            }

            ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.UpdateBy == CurrentUser.Id).ToList(), "ProjectBbId", "BookBankName");
            ViewBag.AccountBudget = SelectMyProject(CurrentUser.VillageId);
            ViewBag.SelectAccount = SelectAccountAct;
            ViewBag.VillageId = CurrentUser.VillageId;
            return View("FormAddActTransaction");
        }

        [HttpGet]
        public async Task<IActionResult> FormAddActTransactionPay()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id);

            var SelectAccountAct = new List<SelectListItem>();
            var GetActBudgets = new List<AccountActivity>();

            //Check IsChartCenter
            if (RoleName == "administrator")
            {
                GetActBudgets = DB.AccountActivity.ToList();
            }
            else if (RoleName == "HeadQuarterAdmin")
            {
                GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == true).ToList();
            }
            else
            {
                GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == false).ToList();
            }

            foreach (var Get in GetActBudgets.Where(w => w.AccountParentId == 0 && w.AccountType == false).OrderBy(w => w.AccountChartId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccountName,
                    Value = Get.AccountChartId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetActBudgets.Where(w => w.AccountParentId == Get.AccountChartId && w.AccountType == false).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccountName,
                        Value = Get2.AccountChartId.ToString()
                    });
                }
            }

            ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.UpdateBy == CurrentUser.Id).ToList(), "ProjectBbId", "BookBankName");
            ViewBag.AccountBudget = SelectMyProject(CurrentUser.VillageId);
            ViewBag.SelectAccount = SelectAccountAct;
            ViewBag.VillageId = CurrentUser.VillageId;
            return View("FormAddActTransactionPay");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddActTransaction(TransactionAccountActivity Model, IFormFile[] FileUpload, DateCollection Date)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            try
            {
                // check amount on account
                //if (Model.Amount > DB.AccountBookBank.Where(w => w.ProjectBbId == Model.SenderBookBankId).Select(s => s.Amount).FirstOrDefault())
                //{
                //    return Json(new { valid = false, message = "ยอดเงินในบัญชีไม่เพียงพอ กรุณาตรวจสอบ" });
                //}

                Model.TransactionYear = Helper.CurrentBudgetYear();
                Model.UpdateDate = DateTime.Now;
                Model.UpdateBy = CurrentUser.Id;
                Model.ReceiverDate = new DateTime(Date.StartYear, Date.StartMonth, Date.StartDay);
                DB.TransactionAccountActivity.Add(Model);
                await DB.SaveChangesAsync();

                // update file 
                if (FileUpload.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account-activity/");
                    foreach (var File in FileUpload)
                    {
                        if (File.ContentType == "application/pdf" ||
                            File.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                            File.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                            File.ContentType == "image/png" || File.ContentType == "image/jpg" || File.ContentType == "image/jpeg")
                        {
                            string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await File.CopyToAsync(fileStream);
                            }

                            // insert data
                            var FileData = new TransactionFileAccActivity();
                            FileData.TransactionAccAcsId = Model.TransactionAccActivityId;
                            FileData.TransactionYear = Helper.CurrentBudgetYear();
                            FileData.FileName = File.FileName;
                            FileData.GencodeFileName = UniqueFileName;
                            FileData.UpdateBy = CurrentUser.Id;
                            FileData.UpdateDate = DateTime.Now;
                            FileData.Approvemark = false;
                            DB.TransactionFileAccActivity.Add(FileData);
                        }
                    }
                    await DB.SaveChangesAsync();
                }

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "บันทึกรับ/จ่ายเงินกิจกรรมโครงการ", HttpContext, SystemName);

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> ActCenterIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            return View("ActCenterIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetActTransactionsCenter(int BudgetYear)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            // master data 
            var DataAccountChart = DB.AccountBudgetCenter.ToList();
            string[] TransType = { "เงินสด", "โอนเข้าบัญชี", "เช็ค" };
            // get data 
            var Models = new List<TransactionActivityViewModel>();

            foreach (var item in DB.TransactionAccountActivityCenter.Where(w => w.TransactionYear == BudgetYear).ToList())
            {
                var Model = new TransactionActivityViewModel()
                {
                    TransactionAccActivityId = item.TransactionAccActivityCenterId,
                    AccChartName = DataAccountChart.Where(w => w.AccountBudgetCenterId == item.AccChartId).Select(s => s.AccName).FirstOrDefault(),
                    TransactionType = TransType[item.TransactionType],
                    Amount = item.Amount,
                    Receiver = item.Receiver,
                    ReceiverDate = Helper.getDateThai(item.ReceiverDate),
                    IsFinish = item.IsFinish == true ? "เสร็จสิ้น" : "กำลังดำเนินการ",
                    RealAmount = item.RealAmount,
                    TotalAmout = item.TotalAmout
                };
                Models.Add(Model);
            }
            return PartialView("GetActTransactionsCenter", Models);
        }

        [HttpGet]
        public async Task<IActionResult> FormAddActTransactionNotAccountBudget(bool AccountType)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id);

            var SelectAccountAct = new List<SelectListItem>();
            var GetActBudgets = new List<AccountActivity>();

            //Check IsChartCenter
            GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == true).ToList();

            foreach (var Get in GetActBudgets.Where(w => w.AccountParentId == 0).OrderBy(w => w.AccountChartId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccountName,
                    Value = Get.AccountChartId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetActBudgets.Where(w => w.AccountParentId == Get.AccountChartId).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccountName,
                        Value = Get2.AccountChartId.ToString()
                    });
                }
            }

            ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.UpdateBy == CurrentUser.Id).ToList(), "ProjectBbId", "BookBankName");
            ViewBag.SelectAccount = SelectAccountAct;
            ViewBag.VillageId = CurrentUser.VillageId;
            ViewBag.AccountType = AccountType;

            ViewBag.Group = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 1).ToList(), "LookupValue", "LookupText", CurrentUser.LookupValueDivision);
            ViewBag.Department = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 2).ToList(), "LookupValue", "LookupText", CurrentUser.LookupValueDepartment);

            return View("FormAddActTransactionNotAccountBudget");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddActTransactionNotAccountBudget(TransactionAccountActivityCenter Model, IFormFile[] FileUpload, DateCollection Date)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            try
            {
                //check AcctName
                if (Model.AccChartId == 0)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบชื่อแผนงาน/โครงการ" });
                }

                Model.TransactionYear = Helper.CurrentBudgetYear();
                Model.UpdateDate = DateTime.Now;
                Model.UpdateBy = CurrentUser.Id;
                Model.ReceiverDate = DateTime.Now;
                DB.TransactionAccountActivityCenter.Add(Model);
                await DB.SaveChangesAsync();

                // update file 
                if (FileUpload.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account-activity/");
                    foreach (var File in FileUpload)
                    {
                        if (File.ContentType == "application/pdf" ||
                            File.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                            File.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                            File.ContentType == "image/png" || File.ContentType == "image/jpg" || File.ContentType == "image/jpeg")
                        {
                            string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await File.CopyToAsync(fileStream);
                            }

                            // insert data
                            var FileData = new TransactionFileAccActivityCenter();
                            FileData.TransactionAccAcsId = Model.TransactionAccActivityCenterId;
                            FileData.TransactionYear = Helper.CurrentBudgetYear();
                            FileData.FileName = File.FileName;
                            FileData.GencodeFileName = UniqueFileName;
                            FileData.UpdateBy = CurrentUser.Id;
                            FileData.UpdateDate = DateTime.Now;
                            FileData.Approvemark = false;
                            DB.TransactionFileAccActivityCenter.Add(FileData);
                        }
                    }
                    await DB.SaveChangesAsync();
                }

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "บันทึกรับ/จ่ายเงิน", HttpContext, SystemName);

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult GetPersonByGroupId(int Group, int Department)
        {

            var User = DB.Users.Where(w => w.LookupValueDivision == Group && w.LookupValueDepartment == Department);

            var SelectUser = new List<SelectListItem>();


            foreach (var item in User)
            {
                SelectUser.Add(new SelectListItem()
                {
                    Text = item.FirstName + " " + item.LastName,
                    Value = item.FirstName + " " + item.LastName
                });
            }

            return Json(SelectUser);
        }

        [HttpGet]
        public IActionResult GetAccBudgetCenterByGroupId(int Group, int Department, Int64 AcctId)
        {

            var GetAccountBudgetCenter = DB.AccountBudgetCenter.Where(w => w.LookupValueDivision == Group && w.LookupValueDepartment == Department).ToList();

            var SelectAccountAct = new List<SelectListItem>();


            foreach (var Get in GetAccountBudgetCenter.Where(w => w.ParentId == 0).OrderBy(w => w.AccountBudgetCenterId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccName,
                    Value = Get.AccountBudgetCenterId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetAccountBudgetCenter.Where(w => w.ParentId == Get.AccountBudgetCenterId).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccName,
                        Value = Get2.AccountBudgetCenterId.ToString(),
                        Selected = Get2.AccountBudgetCenterId == AcctId ? true : false
                    });
                }
            }

            return Json(SelectAccountAct);
        }

        [HttpGet]
        public async Task<IActionResult> FormEditActTransaction(Int64 AccActivityId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id);

            var SelectAccountAct = new List<SelectListItem>();
            var GetData = DB.TransactionAccountActivity.Where(w => w.TransactionAccActivityId == AccActivityId).FirstOrDefault();
            var GetActBudgets = new List<AccountActivity>();

            //Check IsChartCenter
            if (RoleName == "administrator")
            {
                GetActBudgets = DB.AccountActivity.ToList();
            }
            else if (RoleName == "HeadQuarterAdmin")
            {
                GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == true).ToList();
            }
            else
            {
                GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == false).ToList();
            }

            foreach (var Get in GetActBudgets.Where(w => w.AccountParentId == 0 && w.AccountType == true).OrderBy(w => w.AccountChartId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccountName,
                    Value = Get.AccountChartId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetActBudgets.Where(w => w.AccountParentId == Get.AccountChartId && w.AccountType == true).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccountName,
                        Value = Get2.AccountChartId.ToString(),
                        Selected = Get2.AccountChartId == GetData.AccChartId ? true : false
                    });
                }
            }

            ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.UpdateBy == CurrentUser.Id).ToList(), "ProjectBbId", "BookBankName", GetData.BookBankId);
            ViewBag.AccountBudget = SelectMyProject(GetData.VillageId);
            ViewBag.Activity = SelectMyActivity(GetData.ProjectId, GetData.VillageId);
            ViewBag.SelectAccount = SelectAccountAct;
            ViewBag.VillageId = CurrentUser.VillageId;

            ViewBag.Group = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 1).ToList(), "LookupId", "LookupText");
            ViewBag.Department = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 2).ToList(), "LookupId", "LookupText");

            return View("FormEditActTransaction", GetData);
        }

        [HttpGet]
        public async Task<IActionResult> FormEditActTransactionPay(Int64 AccActivityId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id);

            var SelectAccountAct = new List<SelectListItem>();
            var GetData = DB.TransactionAccountActivity.Where(w => w.TransactionAccActivityId == AccActivityId).FirstOrDefault();
            var GetActBudgets = new List<AccountActivity>();

            //Check IsChartCenter
            if (RoleName == "administrator")
            {
                GetActBudgets = DB.AccountActivity.ToList();
            }
            else if (RoleName == "HeadQuarterAdmin")
            {
                GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == true).ToList();
            }
            else
            {
                GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == false).ToList();
            }

            foreach (var Get in GetActBudgets.Where(w => w.AccountParentId == 0 && w.AccountType == false).OrderBy(w => w.AccountChartId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccountName,
                    Value = Get.AccountChartId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetActBudgets.Where(w => w.AccountParentId == Get.AccountChartId && w.AccountType == false).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccountName,
                        Value = Get2.AccountChartId.ToString(),
                        Selected = Get2.AccountChartId == GetData.AccChartId ? true : false
                    });
                }
            }

            ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.UpdateBy == CurrentUser.Id).ToList(), "ProjectBbId", "BookBankName", GetData.BookBankId);
            ViewBag.AccountBudget = SelectMyProject(GetData.VillageId);
            ViewBag.Activity = SelectMyActivity(GetData.ProjectId, GetData.VillageId);
            ViewBag.SelectAccount = SelectAccountAct;
            ViewBag.VillageId = CurrentUser.VillageId;
            return View("FormEditActTransactionPay", GetData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditActTransaction(TransactionAccountActivity Model, IFormFile[] FileUpload, DateCollection Date)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            try
            {
                // check amount on account
                //if (Model.Amount > DB.AccountBookBank.Where(w => w.ProjectBbId == Model.SenderBookBankId).Select(s => s.Amount).FirstOrDefault())
                //{
                //    return Json(new { valid = false, message = "ยอดเงินในบัญชีไม่เพียงพอ กรุณาตรวจสอบ" });
                //}

                var GetData = await DB.TransactionAccountActivity.Where(w => w.TransactionAccActivityId == Model.TransactionAccActivityId).FirstOrDefaultAsync();

                GetData.ProjectId = Model.ProjectId;
                GetData.ActivityId = Model.ActivityId;
                GetData.AccChartId = Model.AccChartId;
                GetData.Receiver = Model.Receiver;
                GetData.TransactionYear = Model.TransactionYear;
                GetData.BookBankId = Model.BookBankId;
                GetData.Amount = Model.Amount;
                GetData.Detail = Model.Detail;
                GetData.TransactionType = Model.TransactionType;
                GetData.ReceiverDate = new DateTime(Date.StartYear, Date.StartMonth, Date.StartDay);
                GetData.UpdateDate = DateTime.Now;
                GetData.UpdateBy = CurrentUser.Id;
                GetData.Tax = Model.Tax;

                DB.TransactionAccountActivity.Update(GetData);
                await DB.SaveChangesAsync();

                // update file 
                if (FileUpload.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account-activity/");
                    foreach (var File in FileUpload)
                    {
                        if (File.ContentType == "application/pdf" ||
                            File.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                            File.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                            File.ContentType == "image/png" || File.ContentType == "image/jpg" || File.ContentType == "image/jpeg")
                        {
                            string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await File.CopyToAsync(fileStream);
                            }

                            // insert data
                            var FileData = new TransactionFileAccActivity();
                            FileData.TransactionAccAcsId = Model.TransactionAccActivityId;
                            FileData.TransactionYear = Helper.CurrentBudgetYear();
                            FileData.FileName = File.FileName;
                            FileData.GencodeFileName = UniqueFileName;
                            FileData.UpdateBy = CurrentUser.Id;
                            FileData.UpdateDate = DateTime.Now;
                            FileData.Approvemark = false;
                            DB.TransactionFileAccActivity.Add(FileData);
                        }
                    }
                    await DB.SaveChangesAsync();
                }

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "แก้ไขรับ/จ่ายเงินกิจกรรมโครงการ", HttpContext, SystemName);

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> FormEditActTransactionNotAccountBudget(Int64 AccActivityId, bool AccountType)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id);

            var SelectAccountAct = new List<SelectListItem>();
            var GetData = DB.TransactionAccountActivityCenter.Where(w => w.TransactionAccActivityCenterId == AccActivityId).FirstOrDefault();
            var GetActBudgets = new List<AccountActivity>();

            //Check IsChartCenter
            GetActBudgets = DB.AccountActivity.Where(w => w.IsChartCenter == true && w.AccountType == AccountType).ToList();

            foreach (var Get in GetActBudgets.Where(w => w.AccountParentId == 0).OrderBy(w => w.AccountChartId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccountName,
                    Value = Get.AccountChartId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetActBudgets.Where(w => w.AccountParentId == Get.AccountChartId).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccountName,
                        Value = Get2.AccountChartId.ToString(),
                        Selected = Get2.AccountChartId == GetData.AccChartId ? true : false
                    });
                }
            }

            ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.UpdateBy == CurrentUser.Id).ToList(), "ProjectBbId", "BookBankName", GetData.BookBankId);
            ViewBag.SelectAccount = SelectAccountAct;
            ViewBag.VillageId = CurrentUser.VillageId;
            ViewBag.AccountType = AccountType;

            ViewBag.Group = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 1).ToList(), "LookupValue", "LookupText", GetData.LookupValueDivision);
            ViewBag.Department = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 2).ToList(), "LookupValue", "LookupText", GetData.LookupValueDepartment);

            return View("FormEditActTransactionNotAccountBudget", GetData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditActTransactionNotAccountBudget(TransactionAccountActivityCenter Model, IFormFile[] FileUpload, DateCollection Date)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            try
            {
                // check amount on account
                //if (Model.Amount > DB.AccountBookBank.Where(w => w.ProjectBbId == Model.SenderBookBankId).Select(s => s.Amount).FirstOrDefault())
                //{
                //    return Json(new { valid = false, message = "ยอดเงินในบัญชีไม่เพียงพอ กรุณาตรวจสอบ" });
                //}

                var GetData = await DB.TransactionAccountActivityCenter.Where(w => w.TransactionAccActivityCenterId == Model.TransactionAccActivityCenterId).FirstOrDefaultAsync();

                GetData.ProjectId = Model.ProjectId;
                GetData.ActivityId = Model.ActivityId;
                GetData.AccChartId = Model.AccChartId;
                GetData.Receiver = Model.Receiver;
                GetData.TransactionYear = Model.TransactionYear;
                GetData.BookBankId = Model.BookBankId;
                GetData.Amount = Model.Amount;
                GetData.Detail = Model.Detail;
                GetData.TransactionType = Model.TransactionType;
                GetData.ReceiverDate = DateTime.Now;
                GetData.UpdateDate = DateTime.Now;
                GetData.UpdateBy = CurrentUser.Id;
                GetData.IsFinish = Model.IsFinish;
                GetData.RealAmount = Model.RealAmount;
                GetData.TotalAmout = Model.TotalAmout;

                DB.TransactionAccountActivityCenter.Update(GetData);
                await DB.SaveChangesAsync();

                // update file 
                if (FileUpload.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account-activity/");
                    foreach (var File in FileUpload)
                    {
                        if (File.ContentType == "application/pdf" ||
                            File.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                            File.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                            File.ContentType == "image/png" || File.ContentType == "image/jpg" || File.ContentType == "image/jpeg")
                        {
                            string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await File.CopyToAsync(fileStream);
                            }

                            // insert data
                            var FileData = new TransactionFileAccActivityCenter();
                            FileData.TransactionAccAcsId = Model.TransactionAccActivityCenterId;
                            FileData.TransactionYear = Helper.CurrentBudgetYear();
                            FileData.FileName = File.FileName;
                            FileData.GencodeFileName = UniqueFileName;
                            FileData.UpdateBy = CurrentUser.Id;
                            FileData.UpdateDate = DateTime.Now;
                            FileData.Approvemark = false;
                            DB.TransactionFileAccActivityCenter.Add(FileData);
                        }
                    }
                    await DB.SaveChangesAsync();
                }

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "แก้ไขรับ/จ่ายเงินกิจกรรมโครงการ", HttpContext, SystemName);

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult GetActivityFiles(Int64 AccActivityId)
        {
            var GetProjectFiles = DB.TransactionFileAccActivity.Where(w => w.TransactionAccAcsId == AccActivityId).ToList();
            return Json(new { GetProjectFiles });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteActivityFiles(Int64 TransactionFileId)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                // delete file
                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account-activity/");
                var GetFiles = await DB.TransactionFileAccActivity.Where(w => w.TransactionFileId == TransactionFileId).ToListAsync();
                if (GetFiles.Count() > 0)
                {
                    foreach (var GetFile in GetFiles)
                    {
                        string Oldfilepath = Path.Combine(Uploads, GetFile.GencodeFileName);
                        if (System.IO.File.Exists(Oldfilepath))
                        {
                            System.IO.File.Delete(Oldfilepath);
                        }

                        DB.TransactionFileAccActivity.Remove(GetFile);
                        await DB.SaveChangesAsync();

                    }
                }

                // add log
                // Helper.AddUsedLog(CurrentUser.Id, "ลบเอกสารแนบ แก้ไขรับ/จ่ายเงินกิจกรรมโครงการ", HttpContext, SystemName);
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult GetActivityFilesCenter(Int64 AccActivityId)
        {
            var GetProjectFiles = DB.TransactionFileAccActivityCenter.Where(w => w.TransactionAccAcsId == AccActivityId).ToList();
            return Json(new { GetProjectFiles });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteActivityFilesCenter(Int64 TransactionFileId)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                // delete file
                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account-activity/");
                var GetFiles = await DB.TransactionFileAccActivityCenter.Where(w => w.TransactionFileId == TransactionFileId).ToListAsync();
                if (GetFiles.Count() > 0)
                {
                    foreach (var GetFile in GetFiles)
                    {
                        string Oldfilepath = Path.Combine(Uploads, GetFile.GencodeFileName);
                        if (System.IO.File.Exists(Oldfilepath))
                        {
                            System.IO.File.Delete(Oldfilepath);
                        }

                        DB.TransactionFileAccActivityCenter.Remove(GetFile);
                        await DB.SaveChangesAsync();

                    }
                }

                // add log
                // Helper.AddUsedLog(CurrentUser.Id, "ลบเอกสารแนบ แก้ไขรับ/จ่ายเงินกิจกรรมโครงการ", HttpContext, SystemName);
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> ViewActDocuments(Int64 AccActivityId)
        {
            // get data
            var Gets = await DB.TransactionFileAccActivity.Where(w => w.TransactionAccAcsId == AccActivityId).ToListAsync();
            return PartialView("ViewActDocuments", Gets);
        }

        [HttpGet]
        public async Task<IActionResult> ViewActDocumentsCenter(Int64 AccActivityId)
        {
            // get data
            var Gets = await DB.TransactionFileAccActivityCenter.Where(w => w.TransactionAccAcsId == AccActivityId).ToListAsync();
            return PartialView("ViewActDocumentsCenter", Gets);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteActTransaction(Int64 TransactionAccActivityId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // check file document
                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                var GetFiles = await DB.TransactionFileAccActivity.Where(w => w.TransactionAccAcsId == TransactionAccActivityId).ToListAsync();
                if (GetFiles.Count() > 0)
                {
                    foreach (var File in GetFiles)
                    {
                        string Oldfilepath = Path.Combine(Uploads, File.GencodeFileName);
                        if (System.IO.File.Exists(Oldfilepath))
                        {
                            System.IO.File.Delete(Oldfilepath);
                        }
                    }
                }

                // find data
                var Gets = await DB.TransactionAccountActivity.FirstOrDefaultAsync(f => f.TransactionAccActivityId == TransactionAccActivityId);
                DB.TransactionAccountActivity.Remove(Gets);
                await DB.SaveChangesAsync();

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "ลบรายการรับ/จ่ายเงินกิจกรรม", HttpContext, SystemName);
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteActTransactionAccountBudgetCenter(Int64 TransactionAccActivityId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // check file document
                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                var GetFiles = await DB.TransactionFileAccActivityCenter.Where(w => w.TransactionAccAcsId == TransactionAccActivityId).ToListAsync();
                if (GetFiles.Count() > 0)
                {
                    foreach (var File in GetFiles)
                    {
                        string Oldfilepath = Path.Combine(Uploads, File.GencodeFileName);
                        if (System.IO.File.Exists(Oldfilepath))
                        {
                            System.IO.File.Delete(Oldfilepath);
                        }
                    }
                }

                // find data
                var Gets = await DB.TransactionAccountActivityCenter.FirstOrDefaultAsync(f => f.TransactionAccActivityCenterId == TransactionAccActivityId);
                DB.TransactionAccountActivityCenter.Remove(Gets);
                await DB.SaveChangesAsync();

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "ลบรายการเบิกรับ/จ่าย", HttpContext, SystemName);
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        #endregion

        #region mile stone

        [HttpGet]
        public async Task<IActionResult> MileStoneIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id).ToLower();
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //Project
            if (RoleName == "superuser")
            {
                var GetAccountBudget = DB.ProjectBudget.Where(w => w.VillageId == CurrentUser.VillageId && w.StatusId == 5);
                ViewBag.AccountBudget = new SelectList(GetAccountBudget, "ProjectId", "ProjectName");
            }
            else
            {
                var GetAccountBudget = DB.AccountBudget.Where(w => w.ParentId != 0 && w.IsActive == true);
                ViewBag.AccountBudget = new SelectList(GetAccountBudget, "AccountBudgetd", "AccName");
            }

            var SelectAccountAct = new List<SelectListItem>();
            var GetActBudgets = DB.AccountActivity;

            foreach (var Get in GetActBudgets.Where(w => w.AccountParentId == 0 && w.AccountType == false).OrderBy(w => w.AccountChartId))
            {
                SelectAccountAct.Add(new SelectListItem()
                {
                    Text = Get.AccountName,
                    Value = Get.AccountChartId.ToString(),
                    Disabled = true
                });

                var GetLevel2 = GetActBudgets.Where(w => w.AccountParentId == Get.AccountChartId).ToList();
                foreach (var Get2 in GetLevel2)
                {
                    SelectAccountAct.Add(new SelectListItem()
                    {
                        Text = SpaceString(10) + Get2.AccountName,
                        Value = Get2.AccountChartId.ToString()
                    });
                }
            }
            ViewBag.AccountChart = SelectAccountAct;

            return View("MileStoneIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetMileStoneIndex(int BudgetYear, int AccountBudgetId, int AccChartId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var RoleName = Helper.GetUserRoleName(CurrentUser.Id).ToLower();
            var Models = new List<LedgerReportViewModel>();
            var AccountActivity = DB.AccountActivity.Where(w => w.AccountChartId == AccChartId).Select(s => s.AccountType).FirstOrDefault();


            if (RoleName == "superuser")
            {
                var Gets = DB.TransactionAccountActivity.Where(w => w.VillageId == CurrentUser.VillageId && w.ProjectId == AccountBudgetId && w.TransactionYear == BudgetYear && w.AccChartId == AccChartId);
                foreach (var Get in Gets)
                {

                    var Model = new LedgerReportViewModel();
                    Model.Id = Get.ProjectId;
                    Model.Amount = Get.Amount;
                    Model.TransactionType = AccountActivity;
                    Model.Receiver = Get.Receiver;
                    Model.ReceiverDate = Helper.getDateThai(Get.ReceiverDate);
                    Models.Add(Model);
                }
            }
            else
            {
                var ProjectBudget = DB.ProjectBudget.Where(w => w.AccountBudgetd == AccountBudgetId).ToList();
                foreach (var GetsBudget in ProjectBudget)
                {
                    var Gets = DB.TransactionAccountActivity.Where(w => w.ProjectId == GetsBudget.ProjectId && w.TransactionYear == BudgetYear && w.AccChartId == AccChartId).ToList();
                    foreach (var Get in Gets)
                    {

                        var Model = new LedgerReportViewModel();
                        Model.Id = Get.ProjectId;
                        Model.Amount = Get.Amount;
                        Model.TransactionType = AccountActivity;
                        Model.Receiver = Get.Receiver;
                        Model.ReceiverDate = Helper.getDateThai(Get.ReceiverDate);
                        Models.Add(Model);
                    }
                }
            }
            ViewBag.AccountType = AccountActivity;
            return PartialView("GetMileStoneIndex", Models);
        }

        [HttpGet]
        public IActionResult GetProjectByBudget(int BudgetYear)
        {
            List<SelectListItem> Project = new List<SelectListItem>();
            var GetProjects = DB.AccountBudget.Where(w => w.BudgetYear == BudgetYear && w.ParentId != 0).ToList();
            foreach (var GetProject in GetProjects)
            {
                Project.Add(new SelectListItem() { Text = GetProject.AccName, Value = GetProject.AccountBudgetd.ToString() });
            }

            return Json(Project);
        }

        [HttpGet]
        public IActionResult ViewMileStone(string Id, string BudgetYear)
        {
            var ProjectBudget = DB.ProjectBudget.Where(w => w.ProjectId == int.Parse(Id)).Select(s => s.AccountBudgetd).FirstOrDefault();
            var Models = new List<TransacionAccountBudgetViewModel>();
            string[] _Id = Id.Split(",");
            foreach (var Ids in _Id)
            {
                var Gets = DB.TransacionAccountBudget.Where(w => w.AccountBudgetd == ProjectBudget && w.TransactionYear == int.Parse(BudgetYear)).ToList();
                foreach (var Get in Gets)
                {
                    var Model = new TransacionAccountBudgetViewModel();
                    Model.TransactionAccBudgeId = Get.TransactionAccBudgeId;
                    Model.Title = Get.Title;
                    Model.SenderOrgId = DB.SystemOrgStructures.Where(w => w.OrgId == Get.SenderOrgId).Select(s => s.OrgName).FirstOrDefault();
                    Model.SenderBookBankId = DB.AccountBookBank.Where(w => w.ProjectBbId == Get.SenderBookBankId).Select(s => s.BookBankName).FirstOrDefault();
                    Model.ReceiverOrgId = (DB.SystemOrgStructures.Where(w => w.OrgId == Get.ReceiverOrgId).Count() > 0 ? DB.SystemOrgStructures.Where(w => w.OrgId == Get.ReceiverOrgId).Select(s => s.OrgName).FirstOrDefault() : DB.Village.Where(w => w.VillageId == Get.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault());
                    Model.ReceiverBookBankId = DB.AccountBookBank.Where(w => w.ProjectBbId == Get.ReceiverBookBankId).Select(s => s.BookBankName).FirstOrDefault();

                    Models.Add(Model);
                }
            }

            return View("ViewMileStone", Models);
        }

        [HttpGet]
        public IActionResult GetActivityDetail(Int64 Id)
        {
            var Get = DB.TransacionAccountBudget.Where(w => w.TransactionAccBudgeId == Id).FirstOrDefault();
            var Model = new TransacionAccountBudgetViewModel();
            Model.Title = Get.Title;
            Model.SenderOrgId = DB.SystemOrgStructures.Where(w => w.OrgId == Get.SenderOrgId).Select(s => s.OrgName).FirstOrDefault();
            Model.SenderBookBankId = DB.AccountBookBank.Where(w => w.ProjectBbId == Get.SenderBookBankId).Select(s => "(" + s.BookBankId + ") " + s.BookBankName).FirstOrDefault();
            Model.ReceiverOrgId = (DB.SystemOrgStructures.Where(w => w.OrgId == Get.ReceiverOrgId).Count() > 0 ? DB.SystemOrgStructures.Where(w => w.OrgId == Get.ReceiverOrgId).Select(s => s.OrgName).FirstOrDefault() : DB.Village.Where(w => w.VillageId == Get.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault());
            Model.ReceiverBookBankId = DB.AccountBookBank.Where(w => w.ProjectBbId == Get.ReceiverBookBankId).Select(s => "(" + s.BookBankId + ") " + s.BookBankName).FirstOrDefault();
            Model.Amount = Get.Amount;
            Model.SenderDate = Helper.getDateThai(Get.SenderDate);
            Model.ReceiverDate = Helper.getDateThai(Get.ReceiverDate);
            return PartialView("GetActivityDetail", Model);
        }

        #endregion

        #region master 

        [HttpGet]
        public async Task<IActionResult> BookBankIndex()
        {
            /* get data master */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);


            return View("BookBankIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetBookBanks()
        {
            /* get data master */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string CurrentRole = Helper.GetUserRoleName(CurrentUser.Id);
            var Banks = await DB.AccountBankMaster.ToListAsync();
            var Models = new List<BookBankListViewModel>();

            if (CurrentRole == "administrator")
            {
                var GetDatas = await DB.AccountBookBank.Where(w => w.OrgId == CurrentUser.OrgId).ToListAsync();
                foreach (var Get in GetDatas)
                {
                    decimal Income = DB.TransacionAccountBudget.Where(w => w.SenderBookBankId == Get.ProjectBbId && w.TransactionType == true).Select(s => s.Amount).Sum();
                    decimal Expense = DB.TransacionAccountBudget.Where(w => w.SenderBookBankId == Get.ProjectBbId && w.TransactionType == false).Select(s => s.Amount).Sum();
                    decimal Amount = Income - Expense;

                    var Model = new BookBankListViewModel();
                    Model.AccountName = Get.BookBankName + " (" + Get.BookBankId + ")";
                    Model.Amount = Amount;
                    Model.BankName = Banks.Where(w => w.BankCode == Get.BankCode).Select(s => s.BankName + " (" + s.BankShotName + ")").FirstOrDefault();
                    Model.Id = Get.ProjectBbId;
                    Models.Add(Model);
                }
            }
            else
            {
                var GetDatas = await DB.AccountBookBank.Where(w => w.UpdateBy == CurrentUser.Id).ToListAsync();
                foreach (var Get in GetDatas)
                {
                    decimal Income = DB.TransacionAccountBudget.Where(w => w.SenderBookBankId == Get.ProjectBbId && w.TransactionType == true).Select(s => s.Amount).Sum();
                    decimal Expense = DB.TransacionAccountBudget.Where(w => w.SenderBookBankId == Get.ProjectBbId && w.TransactionType == false).Select(s => s.Amount).Sum();
                    decimal Amount = Income - Expense;

                    var Model = new BookBankListViewModel();
                    Model.AccountName = Get.BookBankName + " (" + Get.BookBankId + ")";
                    Model.Amount = Amount;
                    Model.BankName = Banks.Where(w => w.BankCode == Get.BankCode).Select(s => s.BankName + " (" + s.BankShotName + ")").FirstOrDefault();
                    Model.Id = Get.ProjectBbId;
                    Models.Add(Model);
                }
            }

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return PartialView("GetBookBanks", Models);
        }

        [HttpGet]
        public async Task<IActionResult> FormAddBookBank()
        {
            /* get data master */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            /* set dropdown */
            ViewBag.Banks = new SelectList(DB.AccountBankMaster.ToList(), "BankCode", "BankName");
            ViewBag.IsBranch = (Helper.GetUserRoleName(CurrentUser.Id) == "SuperUser" ? true : false);
            return PartialView("FormAddBookBank");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddBookBank(AccountBookBank Model, IFormFile BookbankFile)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                Model.UpdateBy = CurrentUser.Id;
                Model.UpdateDate = DateTime.Now;
                Model.OrgId = CurrentUser.OrgId == 0 ? 1 : CurrentUser.OrgId;
                DB.AccountBookBank.Add(Model);
                await DB.SaveChangesAsync();

                //Upload Bookbank File 
                if (BookbankFile != null)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                    if (BookbankFile.ContentType != "application/x-msdownload" && Path.GetExtension(BookbankFile.FileName) != ".msi" && Path.GetExtension(BookbankFile.FileName) != ".bat")
                    {
                        string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(BookbankFile.ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                        using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                        {
                            BookbankFile.CopyTo(fileStream);
                        }
                        var FileModel = new TransactionFileBookbank();
                        FileModel.TransactionYear = DateTime.Now.Year + 543;
                        FileModel.FileName = BookbankFile.FileName;
                        FileModel.GencodeFileName = UniqueFileName;
                        FileModel.UpdateDate = DateTime.Now;
                        FileModel.UpdateBy = CurrentUser.Id;
                        FileModel.BookBankId = Model.ProjectBbId;
                        DB.TransactionFileBookbank.Add(FileModel);
                        DB.SaveChanges();
                    }
                }

            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> FormEditBookBank(Int64 Id)
        {
            /* get data */
            var GetData = await DB.AccountBookBank.Where(w => w.ProjectBbId == Id).FirstOrDefaultAsync();
            ViewBag.Banks = new SelectList(DB.AccountBankMaster.ToList(), "BankCode", "BankName", GetData.BankCode);

            return PartialView("FormEditBookBank", GetData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditBookBank(AccountBookBank Model, IFormFile BookbankFile)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                Model.UpdateBy = CurrentUser.Id;
                Model.UpdateDate = DateTime.Now;
                DB.AccountBookBank.Update(Model);
                await DB.SaveChangesAsync();

                //Upload Bookbank File 
                if (BookbankFile != null)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                    if (BookbankFile.ContentType != "application/x-msdownload" && Path.GetExtension(BookbankFile.FileName) != ".msi" && Path.GetExtension(BookbankFile.FileName) != ".bat")
                    {
                        string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(BookbankFile.ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                        using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                        {
                            BookbankFile.CopyTo(fileStream);
                        }
                        var FileModel = new TransactionFileBookbank();
                        FileModel.TransactionYear = DateTime.Now.Year + 543;
                        FileModel.FileName = BookbankFile.FileName;
                        FileModel.GencodeFileName = UniqueFileName;
                        FileModel.UpdateDate = DateTime.Now;
                        FileModel.UpdateBy = CurrentUser.Id;
                        FileModel.BookBankId = Model.ProjectBbId;
                        DB.TransactionFileBookbank.Add(FileModel);
                        DB.SaveChanges();
                    }

                    //Delete Old File
                    var GetFile = DB.TransactionFileBookbank.Where(w => w.BookBankId == Model.ProjectBbId).FirstOrDefault();
                    if (GetFile != null)
                    {
                        string oldfilepath = Path.Combine(Uploads, GetFile.GencodeFileName);
                        if (System.IO.File.Exists(oldfilepath))
                        {
                            System.IO.File.Delete(oldfilepath);
                        }
                        DB.TransactionFileBookbank.Remove(GetFile);
                        DB.SaveChanges();
                    }
                }

            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteBookBank(Int64 Id)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var GetData = await DB.AccountBookBank.Where(w => w.ProjectBbId == Id).FirstOrDefaultAsync();
                DB.AccountBookBank.Remove(GetData);
                await DB.SaveChangesAsync();

                var GetFile = DB.TransactionFileBookbank.Where(w => w.BookBankId == Id).FirstOrDefault();

                if (GetFile != null)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                    string oldfilepath = Path.Combine(Uploads, GetFile.GencodeFileName);
                    if (System.IO.File.Exists(oldfilepath))
                    {
                        System.IO.File.Delete(oldfilepath);
                    }
                    DB.TransactionFileBookbank.Remove(GetFile);
                    DB.SaveChanges();
                }

            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }
        #endregion

        #region structure account

        [HttpGet]
        public async Task<IActionResult> StructureIndex()
        {

            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return View("StructureIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetStructures(int BudgetYear)
        {

            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            /* get data account budget */
            var Models = new List<ViewStructures>();
            var GetDatas = await DB.AccountBudget.Where(w => w.BudgetYear == BudgetYear).ToListAsync();
            foreach (var Get in GetDatas)
            {
                var Model = new ViewStructures();
                Model.Amount = Get.Amount;
                Model.ProjectCode = Get.AccCode;
                Model.ProjectName = Get.AccName;
                Model.StartEndDate = Helper.getDateThai(Get.AccStart) + " ถึง " + Helper.getDateThai(Get.AccEndDate);
                Model.Status = Get.IsActive;
                Model.SubAmount = Get.SubAmount;
                Model.AccountBudgetd = Get.AccountBudgetd;
                Model.ParentId = Get.ParentId;
                Models.Add(Model);
            }

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return PartialView("GetStructures", Models);
        }

        [HttpGet]
        public IActionResult FormAddStructure()
        {
            /* get master data */
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            return View("FormAddStructure");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddStructure(AccountBudget Model, int StartDay, int StartMonth, int StartYear, int EndDay, int EndMonth, int EndYear)
        {
            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // set data
                Model.UpdateBy = CurrentUser.Id;
                Model.UpdateDate = DateTime.Now;
                Model.ParentId = 0;
                Model.AccCode = Model.BudgetYear + "-" + (DB.AccountBudget.Where(w => w.ParentId == 0).Count() + 1).ToString().PadLeft(3, '0');
                Model.AccStart = new DateTime(StartYear, StartMonth, StartDay);
                Model.AccEndDate = new DateTime(EndYear, EndMonth, EndDay);
                Model.OpenDate = DateTime.Now;
                Model.CloseDate = DateTime.Now;
                Model.IsActive = true;
                DB.AccountBudget.Add(Model);
                await DB.SaveChangesAsync();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult FormAddSubStructure(Int64 AccountBudgetd)
        {
            ViewBag.ParentId = AccountBudgetd;

            return View("FormAddSubStructure");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddSubStructure(AccountBudgetViewModel Model, int StartDay, int StartMonth, int StartYear,
            int EndDay, int EndMonth, int EndYear,
            int OpenDay, int OpenMonth, int OpenYear,
            int CloseDay, int CloseMonth, int CloseYear,
            IFormFile[] DocumentFile, string[] PeriodName, decimal[] PeriodPercent)
        {
            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {

                // Check Danger File
                if (DocumentFile.Count() > 0)
                {
                    foreach (var _File in DocumentFile)
                    {
                        if (_File.ContentType == "application/x-msdownload" || Path.GetExtension(_File.FileName) == ".msi" || Path.GetExtension(_File.FileName) == ".bat")
                        {
                            return Json(new { valid = false, message = " อัปโหลดไฟล์ Document, Sheet, Image, Multimedia, Zip หรือ Rar เท่านั้น" });
                        }
                    }
                }

                // get data 
                var GetData = await DB.AccountBudget.Where(w => w.AccountBudgetd == Model.ParentId).FirstOrDefaultAsync();
                decimal TotalSub = (DB.AccountBudget.Where(w => w.ParentId == Model.ParentId).Sum(s => s.Amount) + Model.Amount);
                if (TotalSub > GetData.Amount)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบข้อมูลวงเงินในโครงการ" });
                }

                if (Model.Amount < Model.SubAmount)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบข้อมูล" });
                }

                DateTime _StartDate = new DateTime(StartYear, StartMonth, StartDay);
                DateTime _EndDate = new DateTime(EndYear, EndMonth, EndDay);
                if (_EndDate < _StartDate)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบวันที่เริ่มต้นโครงการ" });
                }

                DateTime __OpenDate = new DateTime(OpenYear, OpenMonth, OpenDay);
                DateTime _CloseDate = new DateTime(CloseYear, CloseMonth, CloseDay);
                if (_CloseDate < __OpenDate)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบวันที่วันที่เปิด/ปิด รับโครงการ" });
                }

                // check PeriodPercent
                decimal Checked = 0;
                foreach (var _PeriodPercent in PeriodPercent)
                {
                    Checked += _PeriodPercent;
                }

                if (Checked > 100)
                {
                    return Json(new { valid = false, message = "จ่ายอัตราร้อยละไม่ถูกต้อง" });
                }

                int CountParent = DB.AccountBudget.Where(w => w.ParentId == Model.ParentId).Count();

                // set data
                var ThisAccountBudget = new AccountBudget();
                ThisAccountBudget.AccName = Model.AccName;
                ThisAccountBudget.Amount = Model.Amount;
                ThisAccountBudget.ParentId = Model.ParentId;
                ThisAccountBudget.UpdateBy = CurrentUser.Id;
                ThisAccountBudget.UpdateDate = DateTime.Now;
                ThisAccountBudget.BudgetYear = GetData.BudgetYear;
                ThisAccountBudget.AccCode = GetData.AccCode + "-" + (CountParent == 0 ? 1 : CountParent + 1).ToString().PadLeft(3, '0');
                ThisAccountBudget.IsActive = Model.IsActive;
                ThisAccountBudget.AccStart = _StartDate;
                ThisAccountBudget.AccEndDate = _EndDate;
                ThisAccountBudget.OpenDate = __OpenDate;
                ThisAccountBudget.CloseDate = _CloseDate;
                ThisAccountBudget.SubAmount = Model.SubAmount;
                ThisAccountBudget.Qualification = Model.Qualification;
                ThisAccountBudget.IsApproveProvince = Model.IsApproveProvince;
                ThisAccountBudget.IsApproveBranch = Model.IsApproveBranch;
                ThisAccountBudget.IsApproveCenter = Model.IsApproveCenter;
                DB.AccountBudget.Add(ThisAccountBudget);
                await DB.SaveChangesAsync();

                // upload file
                if (DocumentFile.Count() > 0)
                {
                    foreach (var _File in DocumentFile)
                    {
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                        string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(_File.ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                        using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                        {
                            await _File.CopyToAsync(fileStream);
                        }

                        var NewFile = new ProjectBudgetDocument();
                        NewFile.ProjectId = ThisAccountBudget.AccountBudgetd;
                        NewFile.ProjectPeriodId = 0;
                        NewFile.TransactionYear = ThisAccountBudget.BudgetYear;
                        NewFile.FileName = _File.FileName;
                        NewFile.GencodeFileName = UniqueFileName;
                        NewFile.FileType = 1;
                        NewFile.UpdateDate = DateTime.Now;
                        NewFile.UpdateBy = CurrentUser.Id;
                        DB.ProjectBudgetDocument.Add(NewFile);
                        DB.SaveChanges();
                    }
                }

                // add period project
                int RowPeriod = 0;
                foreach (var Period in PeriodName)
                {
                    Double Percent = (Convert.ToDouble(PeriodPercent[RowPeriod]) / 100.00);

                    var AddPeriod = new ProjectPeriod();
                    AddPeriod.AccBudgetId = ThisAccountBudget.AccountBudgetd;
                    AddPeriod.TransactionYear = ThisAccountBudget.BudgetYear;
                    AddPeriod.PeriodName = Period;
                    AddPeriod.PeriodPercent = PeriodPercent[RowPeriod];
                    AddPeriod.PeriodAmount = (ThisAccountBudget.SubAmount * Convert.ToDecimal(Percent));
                    AddPeriod.UpdateDate = DateTime.Now;
                    AddPeriod.UpdateBy = CurrentUser.Id;
                    AddPeriod.PeriodDate = DateTime.Now;
                    AddPeriod.PeriodComment = null;
                    DB.ProjectPeriod.Add(AddPeriod);
                    DB.SaveChanges();

                    RowPeriod++;
                }

                // risk project
                var AddRisk = new ProjectRisk();
                AddRisk.AccountBudgetId = ThisAccountBudget.AccountBudgetd;
                AddRisk.BudgetYear = Helper.CurrentBudgetYear();
                AddRisk.LowActivity = Model.LowActivity;
                AddRisk.MidActivity = Model.MidActivity;
                AddRisk.HiActivity = Model.HiActivity;
                AddRisk.LowTiming = Model.LowTiming;
                AddRisk.MidTiming = Model.MidTiming;
                AddRisk.HiTiming = Model.HiTiming;
                AddRisk.UpdateDate = DateTime.Now;
                AddRisk.UpdateBy = CurrentUser.Id;
                DB.ProjectRisk.Add(AddRisk);
                DB.SaveChanges();

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> FormEditStructure(Int64 AccountBudgetd, string IsParent)
        {
            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            // get data
            var GetData = await DB.AccountBudget.Where(w => w.AccountBudgetd == AccountBudgetd).FirstOrDefaultAsync();
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.IsParent = IsParent;

            // get period 
            ViewBag.GetPeriods = DB.ProjectPeriod.Where(w => w.AccBudgetId == AccountBudgetd).ToList();
            ViewBag.GetProjectRisk = DB.ProjectRisk.Where(w => w.AccountBudgetId == AccountBudgetd).FirstOrDefault();

            ViewBag.Helper = Helper;

            return View("FormEditStructure", GetData);
        }

        [HttpGet]
        public async Task<IActionResult> GetFileEditStructure(int AccountBudgetd)
        {
            //get File
            //Issue 156
            var File = new List<ProjectBudgetDocument>();
            var HeadQuarterFile = await DB.ProjectBudgetDocument.Where(w => w.ProjectId == AccountBudgetd).ToListAsync();
            foreach (var item in HeadQuarterFile)
            {
                if (Helper.GetRoleUser(item.UpdateBy) == "HeadQuarterAdmin" || Helper.GetRoleUser(item.UpdateBy) == "administrator")
                {
                    File.Add(item);
                }
            }

            ViewBag.Helper = Helper;

            return PartialView("GetFileEditStructure", File);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditStructure(AccountBudget Model, string PARENT, int StartDay, int StartMonth, int StartYear, int EndDay, int EndMonth, int EndYear,
            int OpenDay, int OpenMonth, int OpenYear,
            int CloseDay, int CloseMonth, int CloseYear,
            IFormFile[] DocumentFile, string[] PeriodName, decimal[] PeriodPercent,
            int LowActivity, int MidActivity, int HiActivity,
            int LowTiming, int MidTiming, int HiTiming)
        {
            // Check Danger File
            if (DocumentFile.Count() > 0)
            {
                foreach (var _File in DocumentFile)
                {
                    if (_File.ContentType == "application/x-msdownload" || Path.GetExtension(_File.FileName) == ".msi" || Path.GetExtension(_File.FileName) == ".bat")
                    {
                        return Json(new { valid = false, message = " อัปโหลดไฟล์ Document, Sheet, Image, Multimedia, Zip หรือ Rar เท่านั้น" });
                    }
                }
            }

            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var GetData = await DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefaultAsync();
                if (PARENT == "MAIN")
                {
                    // get data 
                    decimal TotalSub = DB.AccountBudget.Where(w => w.ParentId == Model.AccountBudgetd).Sum(s => s.Amount);
                    if (Model.Amount < TotalSub)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบข้อมูล" });
                    }

                    if (Model.Amount < Model.SubAmount)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบข้อมูล" });
                    }

                    GetData.AccName = Model.AccName;
                    GetData.BudgetYear = Model.BudgetYear;
                    GetData.Amount = Model.Amount;
                    GetData.SubAmount = Model.SubAmount;
                    GetData.IsActive = Model.IsActive;
                    GetData.UpdateBy = CurrentUser.Id;
                    GetData.UpdateDate = DateTime.Now;
                    //GetData.AccStart = _StartDate;
                    //GetData.AccEndDate = _EndDate;
                    GetData.Qualification = Model.Qualification;
                    DB.AccountBudget.Update(GetData);
                    await DB.SaveChangesAsync();

                    // update parent budget year
                    var SubAccounts = await DB.AccountBudget.Where(w => w.ParentId == Model.AccountBudgetd).ToListAsync();
                    if (SubAccounts.Count() > 0)
                    {
                        foreach (var SubAccount in SubAccounts)
                        {
                            var UpdateSubAccount = await DB.AccountBudget.Where(w => w.AccountBudgetd == SubAccount.AccountBudgetd).FirstOrDefaultAsync();
                            UpdateSubAccount.BudgetYear = Model.BudgetYear;
                            UpdateSubAccount.IsActive = Model.IsActive;
                            UpdateSubAccount.UpdateBy = CurrentUser.Id;
                            UpdateSubAccount.UpdateDate = DateTime.Now;
                            DB.AccountBudget.Update(UpdateSubAccount);
                        }

                        await DB.SaveChangesAsync();
                    }
                }
                else
                {
                    // get data 
                    var GeSubtData = await DB.AccountBudget.Where(w => w.AccountBudgetd == GetData.ParentId).FirstOrDefaultAsync();
                    decimal TotalSub = DB.AccountBudget.Where(w => w.AccountBudgetd != Model.AccountBudgetd && w.ParentId == GetData.ParentId).Sum(s => s.Amount) + Model.Amount;
                    if (TotalSub > GeSubtData.Amount)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบข้อมูล" });
                    }
                    if (Model.Amount < Model.SubAmount)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบข้อมูล" });
                    }

                    DateTime _StartDate = new DateTime(StartYear, StartMonth, StartDay);
                    DateTime _EndDate = new DateTime(EndYear, EndMonth, EndDay);
                    if (_EndDate < _StartDate)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบวันที่เริ่มต้นโครงการ" });
                    }

                    DateTime __OpenDate = new DateTime(OpenYear, OpenMonth, OpenDay);
                    DateTime _CloseDate = new DateTime(CloseYear, CloseMonth, CloseDay);
                    if (_CloseDate < __OpenDate)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบวันที่วันที่เปิด/ปิด รับโครงการ" });
                    }

                    // check PeriodPercent
                    decimal Checked = 0;
                    foreach (var _PeriodPercent in PeriodPercent)
                    {
                        Checked += _PeriodPercent;
                    }

                    if (Checked > 100)
                    {
                        return Json(new { valid = false, message = "จ่ายอัตราร้อยละไม่ถูกต้อง" });
                    }

                    // update data
                    GetData.AccName = Model.AccName;
                    GetData.Amount = Model.Amount;
                    GetData.SubAmount = Model.SubAmount;
                    GetData.IsActive = Model.IsActive;
                    GetData.UpdateBy = CurrentUser.Id;
                    GetData.UpdateDate = DateTime.Now;
                    GetData.AccStart = _StartDate;
                    GetData.AccEndDate = _EndDate;
                    GetData.OpenDate = __OpenDate;
                    GetData.CloseDate = _CloseDate;
                    GetData.Qualification = Model.Qualification;
                    GetData.IsApproveProvince = Model.IsApproveProvince;
                    GetData.IsApproveBranch = Model.IsApproveBranch;
                    DB.AccountBudget.Update(GetData);
                    await DB.SaveChangesAsync();

                    // upload file
                    if (DocumentFile.Count() > 0)
                    {
                        foreach (var _File in DocumentFile)
                        {
                            var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                            string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(_File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await _File.CopyToAsync(fileStream);
                            }

                            var NewFile = new ProjectBudgetDocument();
                            NewFile.ProjectId = GetData.AccountBudgetd;
                            NewFile.ProjectPeriodId = 0;
                            NewFile.TransactionYear = GetData.BudgetYear;
                            NewFile.FileName = _File.FileName;
                            NewFile.GencodeFileName = UniqueFileName;
                            NewFile.FileType = 1;
                            NewFile.UpdateDate = DateTime.Now;
                            NewFile.UpdateBy = CurrentUser.Id;
                            DB.ProjectBudgetDocument.Add(NewFile);
                            DB.SaveChanges();
                        }
                    }

                    // add period project
                    var GetPeriods = DB.ProjectPeriod.Where(w => w.AccBudgetId == GetData.AccountBudgetd);
                    if (GetPeriods.Count() > 0)
                    {
                        DB.ProjectPeriod.RemoveRange(GetPeriods);
                        DB.SaveChanges();
                    }

                    int RowPeriod = 0;
                    foreach (var Period in PeriodName)
                    {
                        Double Percent = (Convert.ToDouble(PeriodPercent[RowPeriod]) / 100.00);

                        var AddPeriod = new ProjectPeriod();
                        AddPeriod.AccBudgetId = GetData.AccountBudgetd;
                        AddPeriod.TransactionYear = GetData.BudgetYear;
                        AddPeriod.PeriodName = Period;
                        AddPeriod.PeriodPercent = PeriodPercent[RowPeriod];
                        AddPeriod.PeriodAmount = (GetData.SubAmount * Convert.ToDecimal(Percent));
                        AddPeriod.UpdateDate = DateTime.Now;
                        AddPeriod.UpdateBy = CurrentUser.Id;
                        AddPeriod.PeriodDate = DateTime.Now;
                        AddPeriod.PeriodComment = null;
                        DB.ProjectPeriod.Add(AddPeriod);
                        DB.SaveChanges();

                        RowPeriod++;
                    }

                    // update peoject risk
                    var GetProjectRisk = DB.ProjectRisk.Where(w => w.AccountBudgetId == GetData.AccountBudgetd).FirstOrDefault();
                    if (GetProjectRisk != null)
                    {
                        GetProjectRisk.LowActivity = LowActivity;
                        GetProjectRisk.MidActivity = MidActivity;
                        GetProjectRisk.HiActivity = HiActivity;
                        GetProjectRisk.LowTiming = LowTiming;
                        GetProjectRisk.MidTiming = MidTiming;
                        GetProjectRisk.HiTiming = HiTiming;
                        GetProjectRisk.UpdateBy = CurrentUser.Id;
                        GetProjectRisk.UpdateDate = DateTime.Now;
                        DB.ProjectRisk.Update(GetProjectRisk);
                        DB.SaveChanges();
                    }
                    else
                    {
                        var AddRisk = new ProjectRisk();
                        AddRisk.AccountBudgetId = GetData.AccountBudgetd;
                        AddRisk.BudgetYear = GetData.BudgetYear;
                        AddRisk.LowActivity = LowActivity;
                        AddRisk.MidActivity = MidActivity;
                        AddRisk.HiActivity = HiActivity;
                        AddRisk.LowTiming = LowTiming;
                        AddRisk.MidTiming = MidTiming;
                        AddRisk.HiTiming = HiTiming;
                        AddRisk.UpdateDate = DateTime.Now;
                        AddRisk.UpdateBy = CurrentUser.Id;
                        DB.ProjectRisk.Add(AddRisk);
                        DB.SaveChanges();
                    }
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditStructureFileUpload(AccountBudget Model, IFormFile[] DocumentFile)
        {
            try
            {

                /* get  current user */
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                var GetData = await DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefaultAsync();

                // upload file
                if (DocumentFile.Count() > 0)
                {
                    foreach (var _File in DocumentFile)
                    {
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                        string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(_File.ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                        using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                        {
                            await _File.CopyToAsync(fileStream);
                        }

                        var NewFile = new ProjectBudgetDocument();
                        NewFile.ProjectId = GetData.AccountBudgetd;
                        NewFile.ProjectPeriodId = 0;
                        NewFile.TransactionYear = GetData.BudgetYear;
                        NewFile.FileName = _File.FileName;
                        NewFile.GencodeFileName = UniqueFileName;
                        NewFile.FileType = 1;
                        NewFile.UpdateDate = DateTime.Now;
                        NewFile.UpdateBy = CurrentUser.Id;
                        DB.ProjectBudgetDocument.Add(NewFile);
                        DB.SaveChanges();
                    }
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAcctFile(Int64 AccountBudgetd)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var Get = DB.ProjectBudgetDocument.Where(w => w.TransactionDocId == AccountBudgetd).FirstOrDefault();
                DB.ProjectBudgetDocument.Remove(Get);
                DB.SaveChanges();

                // delete file
                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                string oldfilepath = Path.Combine(Uploads, Get.GencodeFileName);
                if (System.IO.File.Exists(oldfilepath))
                {
                    System.IO.File.Delete(oldfilepath);
                }

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "ลบเอกสารแนบในโครงการ", HttpContext, SystemName);
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpPost]
        public async Task<IActionResult> PostAcctFile(AccountBudget Model, IFormFile[] DocumentFile)
        {
            try
            {
                /* get  current user */
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                // upload file
                if (DocumentFile.Count() > 0)
                {
                    // remove old file 
                    var GetOldFiles = DB.ProjectBudgetDocument.Where(w => w.ProjectId == Model.AccountBudgetd).ToList();
                    foreach (var GetOldFile in GetOldFiles)
                    {
                        // delete old file
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                        string oldfilepath = Path.Combine(Uploads, GetOldFile.GencodeFileName);
                        if (System.IO.File.Exists(oldfilepath))
                        {
                            System.IO.File.Delete(oldfilepath);
                        }

                        DB.ProjectBudgetDocument.Remove(GetOldFile);
                        DB.SaveChanges();
                    }

                    foreach (var _File in DocumentFile)
                    {
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                        string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(_File.ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                        using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                        {
                            await _File.CopyToAsync(fileStream);
                        }

                        var NewFile = new ProjectBudgetDocument();
                        NewFile.ProjectId = Model.AccountBudgetd;
                        NewFile.ProjectPeriodId = 0;
                        NewFile.TransactionYear = Model.BudgetYear;
                        NewFile.FileName = _File.FileName;
                        NewFile.GencodeFileName = UniqueFileName;
                        NewFile.FileType = 1;
                        NewFile.UpdateDate = DateTime.Now;
                        NewFile.UpdateBy = CurrentUser.Id;
                        DB.ProjectBudgetDocument.Add(NewFile);
                        DB.SaveChanges();
                    }
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteStructure(Int64 AccountBudgetd)
        {
            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // check data 

                if (DB.AccountBudget.Where(w => w.ParentId == AccountBudgetd).Count() > 0)
                {
                    return Json(new { valid = false, message = "ไม่สามารถลบข้อมูลได้กรุณาตรวจสอบ" });
                }

                // delete data
                var GetData = await DB.AccountBudget.Where(w => w.AccountBudgetd == AccountBudgetd).FirstOrDefaultAsync();
                if (GetData.DocumentFile != null)
                {
                    // delete old file
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-account/");
                    string oldfilepath = Path.Combine(Uploads, GetData.DocumentFile);
                    if (System.IO.File.Exists(oldfilepath))
                    {
                        System.IO.File.Delete(oldfilepath);
                    }
                }


                DB.AccountBudget.Remove(GetData);
                await DB.SaveChangesAsync();

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult FormAddQualification(int AccountBudgetd)
        {
            var GetAcctBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == AccountBudgetd).FirstOrDefault();
            return View(GetAcctBudget);
        }

        [HttpPost]
        public IActionResult PostFormAddQualification(AccountBudget Model)
        {
            try
            {
                var GetAcctBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefault();
                GetAcctBudget.Qualification = Model.Qualification.Replace(Environment.NewLine, "");
                DB.Update(GetAcctBudget);
                DB.SaveChanges();
                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> FormUpdateStatusProject(Int64 AccountBudgetd, bool Status)
        {
            // get data master
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.BookBank = new SelectList(DB.AccountBookBank.Where(w => w.OrgId == CurrentUser.OrgId).ToList(), "ProjectBbId", "BookBankName");
            var GetAccountBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == AccountBudgetd).FirstOrDefault();

            var Model = new FormApproceAccountBudget();
            Model.Amount = GetAccountBudget.Amount;
            Model.AccountBudgetd = GetAccountBudget.AccountBudgetd;
            Model.ProjectBbId = 0;

            return PartialView("FormUpdateStatusProject", Model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormUpdateStatusProject(FormApproceAccountBudget Model)
        {
            // get data master
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            try
            {
                // update status 
                var Get = DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefault();
                Get.IsActive = true;
                Get.UpdateBy = CurrentUser.Id;
                Get.UpdateDate = DateTime.Now;
                DB.AccountBudget.Update(Get);
                DB.SaveChanges();

                // transfer data
                var Accbudget = new TransacionAccountBudget();
                Accbudget.AccountBudgetd = Get.AccountBudgetd;
                Accbudget.Title = "เงินโอนเข้าจากโครงการ";
                Accbudget.TransactionType = true;
                Accbudget.SenderOrgId = CurrentUser.OrgId;
                Accbudget.SenderDate = DateTime.Now;
                Accbudget.SenderBookBankId = Model.ProjectBbId;
                Accbudget.ReceiverBookBankId = Model.ProjectBbId;
                Accbudget.ReceiverDate = DateTime.Now;
                Accbudget.ReceiverOrgId = CurrentUser.OrgId;
                Accbudget.Amount = Model.Amount;
                Accbudget.UpdateBy = CurrentUser.Id;
                Accbudget.UpdateDate = DateTime.Now;
                DB.TransacionAccountBudget.Add(Accbudget);
                DB.SaveChanges();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error });
            }

            return Json(new { valid = true, message = "บันทึกรายการสำเร็จ" });
        }

        [HttpGet]
        public IActionResult UpdateStatusProject(Int64 AccountBudgetd)
        {
            try
            {
                var GetProject = DB.AccountBudget.Where(w => w.AccountBudgetd == AccountBudgetd).FirstOrDefault();
                if (GetProject != null)
                {

                    //เช็คโครงการย่อยไม่ให้เปิดการใช้งานถ้าโครงการใหญ่ปิดอยู่
                    var GetParent = DB.AccountBudget.Where(w => w.AccountBudgetd == GetProject.ParentId).FirstOrDefault();
                    if (GetParent != null && GetParent.IsActive == false && GetProject.IsActive == false)
                    {
                        return Json(new { valid = false, message = "สถานะโครงการใหญ่ปิดการใช้งานอยู่" });
                    }

                    // update main status
                    GetProject.IsActive = (GetProject.IsActive == false ? true : false);
                    DB.AccountBudget.Update(GetProject);
                    DB.SaveChanges();


                    // check parent
                    var GetProjectPrents = DB.AccountBudget.Where(w => w.ParentId == AccountBudgetd).ToList();
                    if (GetProjectPrents.Count > 0)
                    {
                        foreach (var GetProjectPrent in GetProjectPrents)
                        {
                            GetProjectPrent.IsActive = GetProject.IsActive;
                            DB.AccountBudget.UpdateRange(GetProjectPrents);
                            DB.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }
        #endregion

        #region Export Import

        //For Head Quater
        [HttpGet]
        public async Task<IActionResult> ExportExcelStructureProject(int BudgetYear)
        {
            try
            {
                //Database
                var GetDatas = await DB.AccountBudget.Where(w => w.BudgetYear == BudgetYear).ToListAsync();

                //Export Excel
                var templateFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/e-account/template/StructureIndexTemplate.xlsx");
                FileInfo templateFile = new FileInfo(templateFilePath);
                var newFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/e-account/export/StructureIndexReport.xlsx");
                FileInfo newFile = new FileInfo(newFilePath);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage package = new ExcelPackage(newFile, templateFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                    int RowStart = 5;
                    foreach (var item in GetDatas)
                    {
                        worksheet.Cells["A" + RowStart].Value = item.ParentId == 0 ? item.AccName : "          " + item.AccName;
                        worksheet.Cells["A" + RowStart].Style.Font.Size = 14;

                        worksheet.Cells["B" + RowStart].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells["B" + RowStart].Value = item.Amount.ToString("N");
                        worksheet.Cells["B" + RowStart].Style.Font.Size = 14;

                        worksheet.Cells["C" + RowStart].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells["C" + RowStart].Value = item.ParentId == 0 ? "" : item.SubAmount.ToString("N");
                        worksheet.Cells["C" + RowStart].Style.Font.Size = 14;

                        RowStart++;
                    }

                    package.Save();

                    var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return new FileStreamResult(new FileStream(newFilePath, FileMode.Open), mimeType);
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public IActionResult FormImportExcelStructureProject()
        {
            return PartialView("FormImportExcelStructureProject");
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcelStructureProject(IFormFile FileUpload)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
                if (FileUpload.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var Excel = new ExcelPackage(FileUpload.OpenReadStream()))
                    {
                        string[] fileNames = FileUpload.FileName.Split('_');
                        ExcelWorksheet worksheet = Excel.Workbook.Worksheets.First();
                        if (worksheet.Dimension.End.Row == 0)
                        {
                            return Json(new { valid = false, message = "ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                        }


                        for (int Row = 5; Row <= worksheet.Dimension.End.Row; Row++)
                        {

                            if (worksheet.Cells["B" + Row].Value.ToString() == "0")
                            {
                                var ParentProject = new AccountBudget();

                                ParentProject.UpdateBy = CurrentUser.Id;
                                ParentProject.UpdateDate = DateTime.Now;
                                ParentProject.ParentId = 0;
                                ParentProject.AccName = worksheet.Cells["C" + Row].Value.ToString();
                                ParentProject.Amount = Convert.ToDecimal(worksheet.Cells["E" + Row].Value.ToString());
                                ParentProject.SubAmount = 0;
                                ParentProject.AccCode = Helper.CurrentBudgetYear() + "-" + (DB.AccountBudget.Where(w => w.ParentId == 0).Count() + 1).ToString().PadLeft(3, '0');
                                ParentProject.AccStart = StringToDateTime(worksheet.Cells["H" + Row].Value.ToString());
                                ParentProject.AccEndDate = StringToDateTime(worksheet.Cells["I" + Row].Value.ToString());
                                ParentProject.BudgetYear = Convert.ToInt32(worksheet.Cells["D" + Row].Value.ToString());
                                ParentProject.OpenDate = DateTime.Now;
                                ParentProject.CloseDate = DateTime.Now;
                                ParentProject.IsActive = worksheet.Cells["F" + Row].Value.ToString() == "1" ? true : false;

                                DB.AccountBudget.Add(ParentProject);
                                DB.SaveChanges();

                            }
                            else
                            {
                                var ChildProject = new AccountBudget();
                                var GetAcctBudget = DB.AccountBudget.Where(w => w.ParentId == 0).OrderByDescending(o => o.AccountBudgetd).FirstOrDefault();

                                int CountParent = DB.AccountBudget.Where(w => w.ParentId == GetAcctBudget.AccountBudgetd).Count();

                                ChildProject.UpdateBy = CurrentUser.Id;
                                ChildProject.UpdateDate = DateTime.Now;
                                ChildProject.ParentId = Convert.ToInt32(GetAcctBudget.AccountBudgetd);
                                ChildProject.AccName = worksheet.Cells["C" + Row].Value.ToString();
                                ChildProject.Amount = Convert.ToDecimal(worksheet.Cells["E" + Row].Value);
                                ChildProject.SubAmount = Convert.ToDecimal(worksheet.Cells["L" + Row].Value);
                                ChildProject.AccCode = GetAcctBudget.AccCode + "-" + (CountParent == 0 ? 1 : CountParent + 1).ToString().PadLeft(3, '0');
                                ChildProject.AccStart = StringToDateTime(worksheet.Cells["H" + Row].Value.ToString());
                                ChildProject.AccEndDate = StringToDateTime(worksheet.Cells["I" + Row].Value.ToString());
                                ChildProject.BudgetYear = Convert.ToInt32(worksheet.Cells["D" + Row].Value);
                                ChildProject.OpenDate = StringToDateTime(worksheet.Cells["J" + Row].Value.ToString());
                                ChildProject.CloseDate = StringToDateTime(worksheet.Cells["K" + Row].Value.ToString());
                                ChildProject.IsActive = Convert.ToInt32(worksheet.Cells["F" + Row].Value) == 1 ? true : false;

                                DB.AccountBudget.Add(ChildProject);
                                DB.SaveChanges();
                            }


                        }
                    }
                }
                else
                {
                    return Json(new { valid = false, message = " ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                }
                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public IActionResult FormImportExcelActTransaction()
        {
            return PartialView("FormImportExcelActTransaction");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcelActTransaction(IFormFile FileUpload)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                if (FileUpload.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var Excel = new ExcelPackage(FileUpload.OpenReadStream()))
                    {
                        string[] fileNames = FileUpload.FileName.Split('_');
                        ExcelWorksheet worksheet = Excel.Workbook.Worksheets.First();
                        if (worksheet.Dimension.End.Row == 0)
                        {
                            return Json(new { valid = false, message = "ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                        }


                        for (int Row = 3; Row <= worksheet.Dimension.End.Row; Row++)
                        {

                            if (worksheet.Cells["A" + Row].Value == null)
                            {
                                break;
                            }

                            var TransactionAccBudget = new TransacionAccountBudget();
                            TransactionAccBudget.AccountBudgetd = Convert.ToInt64(GetSubstringByString("[", "]", worksheet.Cells["A" + Row].Value.ToString()));
                            TransactionAccBudget.Title = worksheet.Cells["B" + Row].Value.ToString();
                            TransactionAccBudget.TransactionYear = Convert.ToInt32(worksheet.Cells["C" + Row].Value);
                            TransactionAccBudget.TransactionType = Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["D" + Row].Value.ToString())) == 1 ? true : Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["D" + Row].Value.ToString())) == 0 ? false : throw new Exception("กรุณานำเข้าข้อมูลให้ถูกต้อง");
                            TransactionAccBudget.SenderOrgId = Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["E" + Row].Value.ToString()));
                            TransactionAccBudget.SenderBookBankId = DB.AccountBookBank.Where(w => w.BookBankId == worksheet.Cells["F" + Row].Value.ToString()).FirstOrDefault().ProjectBbId;
                            TransactionAccBudget.ReceiverOrgId = Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["G" + Row].Value.ToString()));
                            TransactionAccBudget.ReceiverBookBankId = DB.AccountBookBank.Where(w => w.BookBankId == worksheet.Cells["H" + Row].Value.ToString()).FirstOrDefault().ProjectBbId;
                            TransactionAccBudget.Amount = Convert.ToDecimal(worksheet.Cells["I" + Row].Value);
                            TransactionAccBudget.SenderDate = DateTime.Parse(worksheet.Cells["J" + Row].Value.ToString());
                            TransactionAccBudget.ReceiverDate = DateTime.Parse(worksheet.Cells["J" + Row].Value.ToString());
                            TransactionAccBudget.UpdateDate = DateTime.Now;
                            TransactionAccBudget.UpdateBy = CurrentUser.Id;

                            DB.TransacionAccountBudget.Add(TransactionAccBudget);
                            DB.SaveChanges();

                        }
                    }
                }
                else
                {
                    return Json(new { valid = false, message = " ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                }

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcelActtransactionTemplate()
        {
            try
            {
                //Master
                var GetAccountBudget = await DB.AccountBudget.Where(w => w.ParentId != 0).ToListAsync();
                var GetOrg = DB.SystemOrgStructures.Where(w => w.OrgId < 32).ToList();
                var GetBookBank = await DB.AccountBookBank.ToListAsync();
                var GetVillage = DB.Village.ToList();

                //Export Excel
                var templateFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/e-account/template/ActTransaction.xlsx");
                FileInfo templateFile = new FileInfo(templateFilePath);
                var newFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/e-account/export/ActTransaction.xlsx");
                FileInfo newFile = new FileInfo(newFilePath);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage package = new ExcelPackage(newFile, templateFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];

                    int RowStart = 3;

                    //
                    foreach (var item in GetAccountBudget)
                    {
                        worksheet.Cells["A" + RowStart].Value = "[" + item.AccountBudgetd + "]" + item.AccName;
                        worksheet.Cells["A" + RowStart].Style.Font.Size = 12;

                        RowStart++;
                    }
                    RowStart = 3;


                    //
                    foreach (var item in GetOrg)
                    {

                        worksheet.Cells["C" + RowStart].Value = "[" + item.OrgId + "]" + item.OrgName;
                        worksheet.Cells["C" + RowStart].Style.Font.Size = 12;

                        RowStart++;
                    }

                    foreach (var item in GetVillage)
                    {

                        worksheet.Cells["C" + RowStart].Value = "[" + item.VillageId + "]" + item.VillageName;
                        worksheet.Cells["C" + RowStart].Style.Font.Size = 12;

                        RowStart++;
                    }
                    RowStart = 3;

                    //
                    foreach (var item in GetBookBank)
                    {

                        worksheet.Cells["E" + RowStart].Value = item.BookBankId;
                        worksheet.Cells["E" + RowStart].Style.Font.Size = 12;

                        worksheet.Cells["F" + RowStart].Value = item.BookBankName;
                        worksheet.Cells["F" + RowStart].Style.Font.Size = 12;

                        RowStart++;
                    }
                    RowStart = 3;

                    package.Save();

                    var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return new FileStreamResult(new FileStream(newFilePath, FileMode.Open), mimeType);
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportChartAccount(IFormFile FileUpload)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                if (FileUpload.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var Excel = new ExcelPackage(FileUpload.OpenReadStream()))
                    {
                        string[] fileNames = FileUpload.FileName.Split('_');
                        ExcelWorksheet worksheet = Excel.Workbook.Worksheets.First();
                        if (worksheet.Dimension.End.Row == 0)
                        {
                            return Json(new { valid = false, message = "ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                        }


                        for (int Row = 3; Row <= worksheet.Dimension.End.Row; Row++)
                        {

                            if (worksheet.Cells["A" + Row].Value == null)
                            {
                                break;
                            }

                            var AccountActivity = new AccountActivity();
                            AccountActivity.AccountParentId = Convert.ToInt64(GetSubstringByString("[", "]", worksheet.Cells["C" + Row].Value.ToString()));
                            AccountActivity.AccountName = worksheet.Cells["A" + Row].Value.ToString();
                            AccountActivity.BudgetYear = Convert.ToInt32(worksheet.Cells["B" + Row].Value.ToString());
                            AccountActivity.AccountType = DB.AccountActivity.Where(w => w.AccountChartId == Convert.ToInt32(GetSubstringByString("[", "]", worksheet.Cells["C" + Row].Value.ToString()))).Select(s => s.AccountType).FirstOrDefault();
                            AccountActivity.IsChartCenter = false;
                            AccountActivity.UpdateDate = DateTime.Now;
                            AccountActivity.UpdateBy = CurrentUser.Id;

                            DB.AccountActivity.Add(AccountActivity);
                            DB.SaveChanges();

                        }
                    }
                }
                else
                {
                    return Json(new { valid = false, message = " ไม่มีข้อมูล/รูปแบบไม่ถูกต้อง" });
                }

                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public IActionResult FormImportChartAccount()
        {
            return PartialView("FormImportChartAccount");
        }

        [HttpGet]
        public IActionResult ExportExcelChartAccount()
        {
            try
            {
                //Master

                //Export Excel
                var templateFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/e-account/template/ChartAccount.xlsx");
                FileInfo templateFile = new FileInfo(templateFilePath);
                var newFilePath = Path.Combine(_environment.ContentRootPath.ToString(), "wwwroot/uploads/e-account/export/ChartAccount.xlsx");
                FileInfo newFile = new FileInfo(newFilePath);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage package = new ExcelPackage(newFile, templateFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];

                    int RowStart = 3;

                    //
                    foreach (var item in DB.AccountActivity.Where(w => w.AccountParentId == 0).ToList())
                    {
                        worksheet.Cells["B" + RowStart].Value = "[" + item.AccountChartId + "]" + item.AccountName;
                        worksheet.Cells["B" + RowStart].Style.Font.Size = 12;

                        RowStart++;
                    }
                    package.Save();
                    var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    return new FileStreamResult(new FileStream(newFilePath, FileMode.Open), mimeType);
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        #endregion

        #region ผังบัญชี

        [HttpGet]
        public async Task<IActionResult> ChartAccountIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            // create main
            if (DB.AccountActivity.Count() == 0)
            {
                bool[] AccountTypes = { true, false };
                string[] AccountName = { "หมวดบัญชีรายรับ", "หมวดบัญชีรายจ่าย" };
                int Rows = 0;
                foreach (var AccountType in AccountTypes)
                {
                    var ChartAcc = new AccountActivity();
                    ChartAcc.AccountCode = null;
                    ChartAcc.AccountName = AccountName[Rows];
                    ChartAcc.AccountParentId = 0;
                    ChartAcc.AccountType = AccountType;
                    ChartAcc.BudgetYear = Helper.CurrentBudgetYear();
                    ChartAcc.UpdateBy = CurrentUser.Id;
                    ChartAcc.UpdateDate = DateTime.Now;
                    DB.AccountActivity.Add(ChartAcc);
                    DB.SaveChanges();

                    Rows++;
                }
            }


            return View("ChartAccountIndex");
        }

        [HttpGet]
        public async Task<IActionResult> ChartAccountProjectIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            // create main
            if (DB.AccountActivity.Count() == 0)
            {
                bool[] AccountTypes = { true, false };
                string[] AccountName = { "หมวดบัญชีรายรับ", "หมวดบัญชีรายจ่าย" };
                int Rows = 0;
                foreach (var AccountType in AccountTypes)
                {
                    var ChartAcc = new AccountActivity();
                    ChartAcc.AccountCode = null;
                    ChartAcc.AccountName = AccountName[Rows];
                    ChartAcc.AccountParentId = 0;
                    ChartAcc.AccountType = AccountType;
                    ChartAcc.BudgetYear = Helper.CurrentBudgetYear();
                    ChartAcc.UpdateBy = CurrentUser.Id;
                    ChartAcc.UpdateDate = DateTime.Now;
                    DB.AccountActivity.Add(ChartAcc);
                    DB.SaveChanges();

                    Rows++;
                }
            }


            return View("ChartAccountProjectIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetChartAccount(bool IsChartCenter, int BudgetYear)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            var Gets = DB.AccountActivity.Where(w => w.IsChartCenter == IsChartCenter && w.BudgetYear == BudgetYear).ToList();
            return PartialView("GetChartAccount", Gets);
        }

        [HttpGet]
        public async Task<IActionResult> FromAddChartAccount(bool FormType, Int64 AccountChartId, bool IsChartCenter)
        {
            // current user and role
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.FormType = FormType;
            ViewBag.AccountChartId = AccountChartId;
            ViewBag.AccountName = DB.AccountActivity.Where(w => w.AccountChartId == AccountChartId).Select(s => s.AccountName).FirstOrDefault();
            ViewBag.IsChartCenter = IsChartCenter;
            return PartialView("FromAddChartAccount");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FromAddChartAccount(AccountActivity Model, bool FormType, bool IsChartCenter)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                if (FormType == true)
                {
                    Model.AccountParentId = 0;
                    Model.BudgetYear = Helper.CurrentBudgetYear();
                    Model.UpdateBy = CurrentUser.Id;
                    Model.UpdateDate = DateTime.Now;
                    Model.IsChartCenter = IsChartCenter;
                    DB.AccountActivity.Add(Model);
                    DB.SaveChanges();

                    // add log 
                    Helper.AddUsedLog(CurrentUser.Id, "เพิ่มข้อมูลผังบัญชี", HttpContext, SystemName);
                }
                else
                {
                    Model.BudgetYear = Helper.CurrentBudgetYear();
                    Model.UpdateBy = CurrentUser.Id;
                    Model.UpdateDate = DateTime.Now;
                    Model.IsChartCenter = IsChartCenter;
                    DB.AccountActivity.Add(Model);
                    DB.SaveChanges();

                    // add log 
                    Helper.AddUsedLog(CurrentUser.Id, "เพิ่มข้อมูลผังบัญชีย่อย", HttpContext, SystemName);
                }
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult FromEditChartAccount(Int64 AccountChartId, bool FormType, Int64 AccountParentId)
        {
            ViewBag.FormType = FormType;
            ViewBag.AccountName = DB.AccountActivity.Where(w => w.AccountChartId == AccountParentId).Select(s => s.AccountName).FirstOrDefault();

            var Get = DB.AccountActivity.Where(w => w.AccountChartId == AccountChartId).FirstOrDefault();

            return PartialView("FromEditChartAccount", Get);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FromEditChartAccount(AccountActivity Model, bool FormType)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var Get = DB.AccountActivity.Where(w => w.AccountChartId == Model.AccountChartId).FirstOrDefault();
                if (FormType == true)
                {
                    Get.AccountName = Model.AccountName;
                    Get.AccountType = Model.AccountType;
                    Get.UpdateBy = CurrentUser.Id;
                    Get.UpdateDate = DateTime.Now;
                    DB.AccountActivity.Update(Get);
                    DB.SaveChanges();

                    // add log 
                    Helper.AddUsedLog(CurrentUser.Id, "แก้ไขข้อมูลผังบัญชี", HttpContext, SystemName);
                }
                else
                {
                    Get.AccountName = Model.AccountName;
                    Get.AccountType = Model.AccountType;
                    Get.UpdateBy = CurrentUser.Id;
                    Get.UpdateDate = DateTime.Now;
                    DB.AccountActivity.Update(Get);
                    DB.SaveChanges();

                    // add log 
                    Helper.AddUsedLog(CurrentUser.Id, "แก้ไขข้อมูลผังบัญชีย่อย", HttpContext, SystemName);
                }
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteChartAccount(Int64 AccountChartId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var Get = DB.AccountActivity.Where(w => w.AccountChartId == AccountChartId).FirstOrDefault();
                if (DB.AccountActivity.Where(w => w.AccountParentId == AccountChartId).Count() > 0)
                {
                    return Json(new { valid = false, message = "ไม่สามารถลบรายการนี้ได้ กรุณาตรวจสอบ" });
                }

                DB.AccountActivity.Remove(Get);
                DB.SaveChanges();


                // add log
                Helper.AddUsedLog(CurrentUser.Id, "ลบข้อมูลผังบัญชี", HttpContext, SystemName);
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        #endregion

        #region helper function 

        [HttpGet]
        public IActionResult GetBookBankByOrgId(int OrgId)
        {
            List<SelectListItem> BookBanks = new List<SelectListItem>();
            var GetBookBanks = DB.AccountBookBank.Where(w => w.OrgId == OrgId && w.IsBrance == false).ToList();
            if (GetBookBanks != null)
            {
                foreach (var Get in GetBookBanks)
                {
                    BookBanks.Add(
                        new SelectListItem()
                        {
                            Text = Get.BookBankName,
                            Value = Get.ProjectBbId.ToString()
                        });
                }
            }

            return Json(BookBanks);
        }

        [HttpGet]
        public IActionResult GetBookBankByVillageId(int VillageId)
        {
            var GetVillage = DB.Village.Where(w => w.VillageId == VillageId).FirstOrDefault();
            List<SelectListItem> BookBanks = new List<SelectListItem>();
            if (GetVillage != null)
            {
                var GetBookBanks = DB.AccountBookBank.Where(w => w.UpdateBy == GetVillage.UserId).ToList();
                foreach (var Get in GetBookBanks)
                {
                    BookBanks.Add(
                        new SelectListItem()
                        {
                            Text = Get.BookBankName,
                            Value = Get.ProjectBbId.ToString()
                        });
                }
            }

            return Json(BookBanks);
        }

        public string SpaceString(int Count)
        {
            var JsonValues = new List<object>();
            string Spacing = "\xA0";
            for (int i = 1; i < Count; i++)
            {
                JsonValues.Add(Spacing);
            }

            return string.Join("", JsonValues.ToArray());
        }

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
            int Year = Convert.ToInt32(StringDate.Split("/")[2]) - 543;
            int Month = Convert.ToInt32(StringDate.Split("/")[1]);
            int Day = Convert.ToInt32(StringDate.Split("/")[0]);
            return new DateTime(Year, Month, Day);
        }
        public List<SelectListItem> SelectMyProject(Int64 VillageId)
        {
            List<SelectListItem> SelectMyProject = new List<SelectListItem>();
            foreach (var item in DB.ProjectBudget.Where(w => w.VillageId == VillageId && w.StatusId == 17))
            {
                SelectMyProject.Add(
                   new SelectListItem()
                   {
                       Text = item.ProjectName,
                       Value = item.ProjectId.ToString()
                   });
            }

            return SelectMyProject;
        }

        public List<SelectListItem> SelectMyActivity(Int64 ProjectId, Int64 VillageId)
        {
            List<SelectListItem> SelectMyActivity = new List<SelectListItem>();

            /*
             *  1 รอตรวจสอบ
             *  2 แก้ไข
             *  3 รออนุมัติ
             *  4 ไม่อนุมัติ
             *  5 อนุมัติ
             */

            foreach (var item in DB.ProjectActivity.Where(w => w.VillageId == VillageId && w.ProjectId == ProjectId && w.StatusId == 5))
            {
                SelectMyActivity.Add(
                   new SelectListItem()
                   {
                       Text = item.ActivityDetail,
                       Value = item.ProjectActivityId.ToString()
                   });
            }

            return SelectMyActivity;
        }

        public IActionResult GetAmountFromAccountBudget(int AccBudgetId)
        {
            var GetAcctAmountString = DB.AccountBudget.Where(w => w.AccountBudgetd == AccBudgetId).FirstOrDefault();
            string Amount = "";
            if (GetAcctAmountString != null)
            {
                return Json(new { Amount = GetAcctAmountString.SubAmount.ToString("N") });
            }

            return Json(new { Amount });
        }

        public IActionResult GetAmountAcctBudget(int AccBudgetId)
        {

            var GetAcctAmountString = DB.AccountBudget.Where(w => w.AccountBudgetd == AccBudgetId).FirstOrDefault();
            string Amount = "";
            if (GetAcctAmountString != null)
            {
                return Json(new { Amount = GetAcctAmountString.SubAmount == 0 ? GetAcctAmountString.Amount.ToString("0") : GetAcctAmountString.SubAmount.ToString("0") });
            }

            return Json(new { Amount });

        }

        public string GetSubstringByString(string a, string b, string c)
        {
            return c.Substring((c.IndexOf(a) + a.Length), (c.IndexOf(b) - c.IndexOf(a) - a.Length));
        }

        [HttpGet]
        public IActionResult GetVillageAccBudget(int AccBudgetId)
        {

            var Models = new List<SelectListItem>();

            foreach (var item in DB.ProjectBudget.Where(w => w.StatusId == 17 && w.AccountBudgetd == AccBudgetId))
            {
                if (DB.Village.Where(w => w.VillageId == item.VillageId).Count() > 0)
                {
                    Models.Add(
                       new SelectListItem()
                       {
                           Text = DB.Village.Where(w => w.VillageId == item.VillageId).Select(s => s.VillageName).FirstOrDefault(),
                           Value = item.VillageId.ToString()
                       });
                }
            }

            return Json(Models.Select(s => new { s.Value, s.Text }).Distinct());
        }

        #endregion

        #region report

        //รายงานที่แกะจากระบบเก่า http://110.164.199.58/Account/Login?ReturnUrl=%2f

        [HttpGet]
        public async Task<IActionResult> ReportIndex()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            var Models = new List<LedgerReportViewModel>();
            var AccountActivity = DB.AccountActivity.Select(s => s.AccountType).FirstOrDefault();

            var ProjectBudget = DB.ProjectBudget.ToList();
            foreach (var GetsBudget in ProjectBudget)
            {
                var Gets = DB.TransactionAccountActivity.Where(w => w.ProjectId == GetsBudget.ProjectId).ToList();
                foreach (var Get in Gets)
                {

                    var Model = new LedgerReportViewModel();
                    Model.Amount = Get.Amount;
                    Model.TransactionType = AccountActivity;
                    Model.Receiver = Get.Receiver;
                    Model.ReceiverDate = Helper.getDateThai(Get.ReceiverDate);
                    Models.Add(Model);
                }
            }

            return View("ReportIndex", Models);
        }

        [HttpGet]
        public IActionResult ReportActTransactionProvinceIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            ViewBag.ProvinceId = new SelectList(DB.SystemProvince.OrderBy(o => o.ProvinceId).ToList(), "ProvinceId", "ProvinceName");

            ViewBag.BankCode = new SelectList(DB.AccountBankMaster.OrderBy(o => o.BankId).ToList(), "BankCode", "BankName");

            return View("ReportActTransactionProvinceIndex");
        }
        [HttpGet]
        public IActionResult GetActTransactionProvince(int BudgetYear, int ProvinceId, string BankCodes)
        {
            //master
            var GetAccountBankMaster = DB.AccountBankMaster.ToList();
            var GetSystemProvince = DB.SystemProvince.ToList();
            var GetVillage = DB.Village.Where(w => w.ProvinceId == ProvinceId).ToList();


            var Models = new List<ReportActTransactionProvince>();

            //Get By BankCode
            if (BankCodes != null)
            {
                string[] BankCode = BankCodes.Split(',');
                for (var i = 0; i < BankCode.Length; i++)
                {

                    var GetBookBank = DB.AccountBookBank.Where(w => w.BankCode == Convert.ToInt32(BankCode[i])).ToList();
                    foreach (var item in GetBookBank)
                    {

                        var GetTransacionAccountBudget = DB.TransacionAccountBudget.Where(w => w.SenderBookBankId == item.ProjectBbId && w.TransactionType == false && w.TransactionYear == BudgetYear && (GetVillage.Select(s => s.VillageId).ToArray().Contains(w.ReceiverOrgId) || GetVillage.Select(s => s.OrgId).ToArray().Contains(w.ReceiverOrgId))).FirstOrDefault();

                        if (GetTransacionAccountBudget != null)
                        {

                            var Model = new ReportActTransactionProvince();

                            Model.BookbankName = GetAccountBankMaster.Where(w => w.BankCode == Convert.ToInt32(BankCode[i])).Select(s => s.BankName).FirstOrDefault();
                            Model.VillageNumber = DB.TransacionAccountBudget.Where(w => w.SenderBookBankId == item.ProjectBbId && GetVillage.Select(s => s.VillageId).ToArray().Contains(w.ReceiverOrgId) && w.TransactionType == false).Count();
                            Model.Amount = DB.TransacionAccountBudget.Where(w => w.SenderBookBankId == item.ProjectBbId && GetVillage.Select(s => s.VillageId).ToArray().Contains(w.ReceiverOrgId) && w.TransactionType == false).Sum(s => s.Amount);
                            Models.Add(Model);

                        }

                    }

                }
            }

            ViewBag.ProvinceName = GetSystemProvince.Where(w => w.ProvinceId == ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();

            return PartialView("GetActTransactionProvince", Models);
        }

        [HttpGet]
        public IActionResult ReportApproveActTransactionProvinceIndex()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //Org Id
            var OrgId = DB.SystemOrgStructures.ToList();
            OrgId.Add(new SystemOrgStructures
            {
                OrgId = 0,
                OrgName = "กรุณาเลือกสาขา"
            });
            ViewBag.OrgIds = new SelectList(OrgId.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            //Province
            var ProvinceId = DB.SystemProvince.ToList();
            ProvinceId.Add(new SystemProvince
            {
                ProvinceId = 0,
                ProvinceName = "กรุณาเลือกจังหวัด"
            });
            ViewBag.ProvinceIds = new SelectList(ProvinceId.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");


            return View("ReportApproveActTransactionProvinceIndex");
        }
        public IActionResult GetReportApproveActTransactionProvince(int BudgetYear, int OrgId, int ProvinceId)
        {

            //master
            var GetSystemProvince = DB.SystemProvince.ToList();
            var GetVillage = DB.Village.ToList();
            var GetSystemOrgStructures = DB.SystemOrgStructures.ToList();

            var Models = new List<ReportApproveActTransactionProvince>();

            //Get All
            var GetProjectBudget = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.StatusId == 5).ToList();
            var GetTransacionAccountBudget = DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear).ToList();

            foreach (var item in GetSystemProvince.Where(w => GetVillage.Select(s => s.ProvinceId).Distinct().ToArray().Contains(w.ProvinceId)))
            {
                var Model = new ReportApproveActTransactionProvince();
                Model.ProvinceName = GetSystemProvince.Where(w => w.ProvinceId == item.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
                Model.ProvinceId = item.ProvinceId;
                Model.VillageNumberAll = 0;
                Model.OrgId = 0;

                //Approve
                Model.VillageNumber = 0;
                Model.ProjectNumber = 0;
                Model.AmountProject = 0;

                //Act Transaction
                Model._VillageNumber = 0;
                Model._ProjectNumber = 0;
                Model._AmountProject = 0;

                foreach (var _item in GetVillage.Where(w => w.ProvinceId == item.ProvinceId))
                {
                    Model.OrgId = _item.OrgId;
                    Model.VillageNumberAll += 1;

                    //Approve
                    Model.VillageNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId).GroupBy(g => g.VillageId).Count();
                    Model.ProjectNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId).Count();
                    Model.AmountProject += GetProjectBudget.Where(w => w.VillageId == _item.VillageId).Sum(s => s.Amount);

                    //Act Transaction
                    Model._VillageNumber += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).GroupBy(g => g.ReceiverOrgId).Count();
                    Model._ProjectNumber += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).Count();
                    Model._AmountProject += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).Sum(s => s.Amount);
                }

                Models.Add(Model);
            }

            return PartialView("GetReportApproveActTransactionProvince", Models);
        }

        [HttpGet]
        public IActionResult ReportApproveAndPendingActTransactionProvince()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //Org Id
            var OrgId = DB.SystemOrgStructures.ToList();
            OrgId.Add(new SystemOrgStructures
            {
                OrgId = 0,
                OrgName = "กรุณาเลือกสาขา"
            });
            ViewBag.OrgIds = new SelectList(OrgId.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            //Province
            var ProvinceId = DB.SystemProvince.ToList();
            ProvinceId.Add(new SystemProvince
            {
                ProvinceId = 0,
                ProvinceName = "กรุณาเลือกจังหวัด"
            });
            ViewBag.ProvinceIds = new SelectList(ProvinceId.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");

            return View("ReportApproveAndPendingActTransactionProvince");
        }
        [HttpGet]
        public IActionResult GetReportApproveAndPendingActTransactionProvince(int BudgetYear, int OrgId, int ProvinceId)
        {

            //master
            var GetSystemProvince = DB.SystemProvince.ToList();
            var GetVillage = DB.Village.ToList();
            var GetSystemOrgStructures = DB.SystemOrgStructures.ToList();
            var GetSystemDistrict = DB.SystemDistrict.ToList();

            var Models = new List<ReportApproveAndPendingActTransactionProvince>();

            //Get All
            var GetProjectBudget = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear).ToList();
            var GetTransacionAccountBudget = DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear).ToList();

            foreach (var item in GetSystemProvince.Where(w => GetVillage.Select(s => s.ProvinceId).Distinct().ToArray().Contains(w.ProvinceId)))
            {
                var Model = new ReportApproveAndPendingActTransactionProvince();
                Model.ProvinceName = GetSystemProvince.Where(w => w.ProvinceId == item.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
                Model.ProvinceId = item.ProvinceId;
                Model.VillageNumberAll = 0;
                Model.OrgId = 0;

                //Pending
                Model.PendingVillageNumber = 0;
                Model.PendingProjectNumber = 0;
                Model.PendingAmountProject = 0;

                //Approve
                Model.VillageNumber = 0;
                Model.ProjectNumber = 0;
                Model.AmountProject = 0;

                //Act Transaction
                Model._VillageNumber = 0;
                Model._ProjectNumber = 0;
                Model._AmountProject = 0;

                foreach (var _item in GetVillage.Where(w => w.ProvinceId == item.ProvinceId))
                {
                    Model.OrgId = _item.OrgId;
                    Model.VillageNumberAll += 1;

                    //Pending
                    Model.PendingVillageNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 1).GroupBy(g => g.VillageId).Count();
                    Model.PendingProjectNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 1).Count();
                    Model.PendingAmountProject += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 1).Sum(s => s.Amount);

                    //Approve
                    Model.VillageNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 5).GroupBy(g => g.VillageId).Count();
                    Model.ProjectNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 5).Count();
                    Model.AmountProject += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 5).Sum(s => s.Amount);

                    //Act Transaction
                    Model._VillageNumber += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).GroupBy(g => g.ReceiverOrgId).Count();
                    Model._ProjectNumber += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).Count();
                    Model._AmountProject += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).Sum(s => s.Amount);
                }

                Models.Add(Model);

                //Distric Level 
                foreach (var itemlv2 in GetSystemDistrict.Where(w => GetVillage.Select(s => s.DistrictId).Distinct().ToArray().Contains(w.AmphurId)))
                {
                    var ModelLv2 = new ReportApproveAndPendingActTransactionProvince();
                    ModelLv2.DistrictName = GetSystemDistrict.Where(w => w.AmphurId == itemlv2.ProvinceId).Select(s => s.AmphurName).FirstOrDefault();
                    ModelLv2.ProvinceId = item.ProvinceId;
                    ModelLv2.VillageNumberAll = 0;
                    ModelLv2.OrgId = 0;

                    //Pending
                    ModelLv2.PendingVillageNumber = 0;
                    ModelLv2.PendingProjectNumber = 0;
                    ModelLv2.PendingAmountProject = 0;

                    //Approve
                    ModelLv2.VillageNumber = 0;
                    ModelLv2.ProjectNumber = 0;
                    ModelLv2.AmountProject = 0;

                    //Act Transaction
                    ModelLv2._VillageNumber = 0;
                    ModelLv2._ProjectNumber = 0;
                    ModelLv2._AmountProject = 0;

                    foreach (var _itemlv2 in GetVillage.Where(w => w.DistrictId == itemlv2.AmphurId))
                    {
                        ModelLv2.OrgId = _itemlv2.OrgId;
                        ModelLv2.VillageNumberAll += 1;

                        //Pending
                        ModelLv2.PendingVillageNumber += GetProjectBudget.Where(w => w.VillageId == _itemlv2.VillageId && w.StatusId == 1).GroupBy(g => g.VillageId).Count();
                        ModelLv2.PendingProjectNumber += GetProjectBudget.Where(w => w.VillageId == _itemlv2.VillageId && w.StatusId == 1).Count();
                        ModelLv2.PendingAmountProject += GetProjectBudget.Where(w => w.VillageId == _itemlv2.VillageId && w.StatusId == 1).Sum(s => s.Amount);

                        //Approve
                        ModelLv2.VillageNumber += GetProjectBudget.Where(w => w.VillageId == _itemlv2.VillageId && w.StatusId == 5).GroupBy(g => g.VillageId).Count();
                        ModelLv2.ProjectNumber += GetProjectBudget.Where(w => w.VillageId == _itemlv2.VillageId && w.StatusId == 5).Count();
                        ModelLv2.AmountProject += GetProjectBudget.Where(w => w.VillageId == _itemlv2.VillageId && w.StatusId == 5).Sum(s => s.Amount);

                        //Act Transaction
                        ModelLv2._VillageNumber += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _itemlv2.VillageId).GroupBy(g => g.ReceiverOrgId).Count();
                        ModelLv2._ProjectNumber += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _itemlv2.VillageId).Count();
                        ModelLv2._AmountProject += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _itemlv2.VillageId).Sum(s => s.Amount);
                    }

                    Models.Add(ModelLv2);
                }

            }

            return PartialView("GetReportApproveAndPendingActTransactionProvince", Models);
        }

        [HttpGet]
        public IActionResult ReportEntireProjectIndex()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //Org Id
            var OrgId = DB.SystemOrgStructures.ToList();
            OrgId.Add(new SystemOrgStructures
            {
                OrgId = 0,
                OrgName = "กรุณาเลือกสาขา"
            });
            ViewBag.OrgIds = new SelectList(OrgId.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            //Province
            var ProvinceId = DB.SystemProvince.ToList();
            ProvinceId.Add(new SystemProvince
            {
                ProvinceId = 0,
                ProvinceName = "กรุณาเลือกจังหวัด"
            });
            ViewBag.ProvinceIds = new SelectList(ProvinceId.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");

            return View("ReportEntireProjectIndex");
        }
        [HttpGet]
        public IActionResult GetReportEntireProject(int BudgetYear, int OrgId, int ProvinceId, int StatusId)
        {

            //master

            ViewBag.Village = DB.Village.ToList();
            ViewBag.Province = DB.SystemProvince.ToList();
            ViewBag.District = DB.SystemDistrict.ToList();
            ViewBag.SubDistrict = DB.SystemSubDistrict.ToList();
            ViewBag.Helper = Helper;
            ViewBag.AccountBudget = DB.AccountBudget.ToList();

            //Get All
            var GetProjectBudget = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear).ToList();

            return PartialView("GetReportEntireProject", GetProjectBudget);
        }

        [HttpGet]
        public IActionResult ReportProjectTypeIndex()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            var OrgIds = DB.SystemOrgStructures.ToList();
            OrgIds.Add(new SystemOrgStructures
            {
                OrgId = 0,
                OrgName = "กรุณาเลือกสาขา"
            });
            ViewBag.OrgIds = new SelectList(OrgIds.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            var ProvinceIds = DB.SystemProvince.ToList();
            ProvinceIds.Add(new SystemProvince
            {
                ProvinceId = 0,
                ProvinceName = "กรุณาเลือกจังหวัด"
            });
            ViewBag.ProvinceIds = new SelectList(ProvinceIds.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");

            return View("ReportProjectTypeIndex");
        }
        [HttpGet]
        public IActionResult GetReportProjectType(int BudgetYear, int OrgId, int ProvinceId)
        {
            var GetVillageIds = DB.Village.Where(w => (ProvinceId == 0 ? DB.Village.Select(s => s.ProvinceId).ToArray() : new int[] { ProvinceId }).Contains(w.ProvinceId) && (OrgId == 0 ? DB.Village.Select(s => s.OrgId).ToArray() : new int[] { OrgId }).Contains(w.OrgId)).Select(s => s.VillageId).ToList();
            var GetProjectBudget = DB.ProjectBudget.ToList();
            var GetAccountBudget = DB.AccountBudget.ToList();
            var GetTransactionAccountActivity = DB.TransactionAccountActivity.ToList();

            decimal TotalAmount = 0;

            var Models = new List<ReportProjectType>();

            foreach (var item in GetAccountBudget.Where(w => w.ParentId == 0))
            {
                var Model = new ReportProjectType();
                Model.ProjectTypeName = item.AccName;
                Model.ProjectNumber = 0;
                Model.Amount = 0;

                foreach (var itemlv2 in GetProjectBudget.Where(w => GetAccountBudget.Where(w => w.ParentId == item.AccountBudgetd).Select(s => s.AccountBudgetd).ToArray().Contains(w.AccountBudgetd) && GetVillageIds.ToArray().Contains(w.VillageId)).ToList())
                {
                    Model.ProjectNumber += 1;
                    Model.Amount += itemlv2.Amount;
                    TotalAmount += itemlv2.Amount;
                }

                Models.Add(Model);

            }

            ViewBag.TotalAmount = TotalAmount;

            return PartialView("GetReportProjectType", Models);
        }

        [HttpGet]
        public IActionResult ReportActByTransaction()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            var OrgIds = DB.SystemOrgStructures.ToList();
            OrgIds.Add(new SystemOrgStructures
            {
                OrgId = 0,
                OrgName = "กรุณาเลือกสาขา"
            });
            ViewBag.OrgIds = new SelectList(OrgIds.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            var ProvinceIds = DB.SystemProvince.ToList();
            ProvinceIds.Add(new SystemProvince
            {
                ProvinceId = 0,
                ProvinceName = "กรุณาเลือกจังหวัด"
            });
            ViewBag.ProvinceIds = new SelectList(ProvinceIds.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");

            return View("ReportActByTransaction");
        }
        [HttpGet]
        public IActionResult GetReportActByTransaction(int BudgetYear, int OrgId, int ProvinceId)
        {

            //master
            ViewBag.Village = DB.Village.ToList();
            ViewBag.Helper = Helper;
            ViewBag.AccountBudget = DB.AccountBudget.ToList();
            ViewBag.OrgName = DB.SystemOrgStructures.ToList();
            ViewBag.BookBank = DB.AccountBookBank.ToList();
            ViewBag.BankMaster = DB.AccountBankMaster.ToList();

            //filter
            var GetVillageIds = DB.Village.Where(w => (ProvinceId == 0 ? DB.Village.Select(s => s.ProvinceId).ToArray() : new int[] { ProvinceId }).Contains(w.ProvinceId)).Select(s => s.VillageId).ToArray();
            var GetOrgIds = DB.Village.Where(w => (OrgId == 0 ? DB.Village.Select(s => s.OrgId).ToArray() : new int[] { OrgId }).Contains(w.OrgId)).Select(s => s.OrgId).ToArray();

            //Data
            var GetTransacionAccountBudget = DB.TransacionAccountBudget.Where(w => w.TransactionType == false && w.TransactionYear == BudgetYear && (GetOrgIds.Contains(w.ReceiverOrgId) || GetVillageIds.Contains(w.ReceiverOrgId))).ToList();

            return PartialView("GetReportActByTransaction", GetTransacionAccountBudget);
        }

        [HttpGet]
        public IActionResult ReportTransferSummaryByVillage()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            var OrgIds = DB.SystemOrgStructures.ToList();
            OrgIds.Add(new SystemOrgStructures
            {
                OrgId = 0,
                OrgName = "กรุณาเลือกสาขา"
            });
            ViewBag.OrgIds = new SelectList(OrgIds.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            var ProvinceIds = DB.SystemProvince.ToList();
            ProvinceIds.Add(new SystemProvince
            {
                ProvinceId = 0,
                ProvinceName = "กรุณาเลือกจังหวัด"
            });
            ViewBag.ProvinceIds = new SelectList(ProvinceIds.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");

            return View("ReportTransferSummaryByVillage");
        }
        [HttpGet]
        public IActionResult GetReportTransferSummaryByVillage(int BudgetYear, int OrgId, int ProvinceId)
        {

            //master
            ViewBag.Village = DB.Village.ToList();
            ViewBag.Helper = Helper;
            ViewBag.AccountBudget = DB.AccountBudget.ToList();
            ViewBag.OrgName = DB.SystemOrgStructures.ToList();
            ViewBag.BookBank = DB.AccountBookBank.ToList();
            ViewBag.BankMaster = DB.AccountBankMaster.ToList();


            //filter
            var GetVillageIds = DB.Village.Where(w => (ProvinceId == 0 ? DB.Village.Select(s => s.ProvinceId).ToArray() : new int[] { ProvinceId }).Contains(w.ProvinceId)).Select(s => s.VillageId).ToArray();
            var GetOrgIds = DB.Village.Where(w => (OrgId == 0 ? DB.Village.Select(s => s.OrgId).ToArray() : new int[] { OrgId }).Contains(w.OrgId)).Select(s => s.OrgId).ToArray();

            //Data
            var GetTransacionAccountBudget = DB.TransacionAccountBudget.Where(w => w.TransactionType == false && w.TransactionYear == BudgetYear && (GetOrgIds.Contains(w.ReceiverOrgId) || GetVillageIds.Contains(w.ReceiverOrgId))).ToList();

            return PartialView("GetReportTransferSummaryByVillage", GetTransacionAccountBudget);
        }

        [HttpGet]
        public IActionResult ReportRequestSummaryByProvince()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //Org Id
            var OrgId = DB.SystemOrgStructures.ToList();
            OrgId.Add(new SystemOrgStructures
            {
                OrgId = 0,
                OrgName = "กรุณาเลือกสาขา"
            });
            ViewBag.OrgIds = new SelectList(OrgId.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            //Province
            var ProvinceId = DB.SystemProvince.ToList();
            ProvinceId.Add(new SystemProvince
            {
                ProvinceId = 0,
                ProvinceName = "กรุณาเลือกจังหวัด"
            });
            ViewBag.ProvinceIds = new SelectList(ProvinceId.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");

            return View("ReportRequestSummaryByProvince");
        }
        [HttpGet]
        public IActionResult GetReportRequestSummaryByProvince(int BudgetYear, int OrgId, int ProvinceId)
        {

            //master
            var GetSystemProvince = DB.SystemProvince.ToList();
            var GetVillage = DB.Village.ToList();
            var GetSystemOrgStructures = DB.SystemOrgStructures.ToList();
            var GetSystemDistrict = DB.SystemDistrict.ToList();

            var Models = new List<ReportRequestSummaryByProvince>();

            //Get All
            var GetProjectBudget = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear).ToList();
            var GetTransacionAccountBudget = DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear).ToList();

            foreach (var item in GetSystemProvince.Where(w => GetVillage.Select(s => s.ProvinceId).Distinct().ToArray().Contains(w.ProvinceId)))
            {
                var Model = new ReportRequestSummaryByProvince();
                Model.ProvinceName = GetSystemProvince.Where(w => w.ProvinceId == item.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();
                Model.ProvinceId = item.ProvinceId;
                Model.VillageNumberAll = 0;
                Model.OrgId = 0;

                //Pending
                Model.PendingVillageNumber = 0;
                Model.PendingProjectNumber = 0;
                Model.PendingAmountProject = 0;

                //Draft
                Model.DraftProjectNumber = 0;

                //Approve
                Model.VillageNumber = 0;
                Model.ProjectNumber = 0;
                Model.AmountProject = 0;

                //Act Transaction
                Model._VillageNumber = 0;
                Model._ProjectNumber = 0;
                Model._AmountProject = 0;

                foreach (var _item in GetVillage.Where(w => w.ProvinceId == item.ProvinceId))
                {
                    Model.OrgId = _item.OrgId;
                    Model.VillageNumberAll += 1;

                    //Pending
                    Model.PendingVillageNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 1).GroupBy(g => g.VillageId).Count();
                    Model.PendingProjectNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 1).Count();
                    Model.PendingAmountProject += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 1).Sum(s => s.Amount);

                    //Draft
                    Model.DraftProjectNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 0).Count();

                    //Approve
                    Model.VillageNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 5).GroupBy(g => g.VillageId).Count();
                    Model.ProjectNumber += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 5).Count();
                    Model.AmountProject += GetProjectBudget.Where(w => w.VillageId == _item.VillageId && w.StatusId == 5).Sum(s => s.Amount);

                    //Act Transaction
                    Model._VillageNumber += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).GroupBy(g => g.ReceiverOrgId).Count();
                    Model._ProjectNumber += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).Count();
                    Model._AmountProject += GetTransacionAccountBudget.Where(w => w.ReceiverOrgId == _item.VillageId).Sum(s => s.Amount);
                }

                Models.Add(Model);

            }

            return PartialView("GetReportRequestSummaryByProvince", Models);
        }

        [HttpGet]
        public IActionResult ReportSummarizeProjectIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //Org Id
            var OrgId = DB.SystemOrgStructures.ToList();
            OrgId.Add(new SystemOrgStructures
            {
                OrgId = 0,
                OrgName = "กรุณาเลือกสาขา"
            });
            ViewBag.OrgIds = new SelectList(OrgId.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            //Province
            var ProvinceId = DB.SystemProvince.ToList();
            ProvinceId.Add(new SystemProvince
            {
                ProvinceId = 0,
                ProvinceName = "กรุณาเลือกจังหวัด"
            });
            ViewBag.ProvinceIds = new SelectList(ProvinceId.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");

            return View("ReportSummarizeProjectIndex");
        }
        [HttpGet]
        public IActionResult GetReportSummarizeProject(int BudgetYear, int OrgId, int ProvinceId)
        {

            //master

            ViewBag.Village = DB.Village.ToList();
            ViewBag.Province = DB.SystemProvince.ToList();
            ViewBag.District = DB.SystemDistrict.ToList();
            ViewBag.SubDistrict = DB.SystemSubDistrict.ToList();
            ViewBag.Helper = Helper;
            ViewBag.AccountBudget = DB.AccountBudget.ToList();
            ViewBag.ProjectBudget = DB.ProjectBudget.ToList();

            var Models = new List<ProjectBudget>();

            //Get All
            foreach (var item in DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear).ToList().GroupBy(g => g.VillageId))
            {
                var Model = new ProjectBudget();

                Model = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == item.Select(s => s.VillageId).FirstOrDefault()).FirstOrDefault();

                Models.Add(Model);
            }

            var GetProjectBudget = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear).ToList();

            return PartialView("GetReportSummarizeProject", Models);
        }



        //รายงานที่ Design ใหม่

        [HttpGet]
        public IActionResult ReportParentAccountBudgetIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            return View("ReportParentAccountBudgetIndex");
        }
        [HttpGet]
        public IActionResult GetReportParentAccountBudget(int BudgetYear)
        {
            //master
            ViewBag.Helper = Helper;

            var GetAccountBudget = DB.AccountBudget.Where(w => w.BudgetYear == BudgetYear && w.ParentId == 0).ToList();

            return PartialView("GetReportParentAccountBudget", GetAccountBudget);
        }

        [HttpGet]
        public IActionResult ReportChildAccountBudgetIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            return View("ReportChildAccountBudgetIndex");
        }
        [HttpGet]
        public IActionResult GetReportChildAccountBudget(int BudgetYear)
        {
            //master
            ViewBag.Helper = Helper;

            var GetAccountBudget = DB.AccountBudget.Where(w => w.BudgetYear == BudgetYear && w.ParentId != 0).ToList();

            return PartialView("GetReportChildAccountBudget", GetAccountBudget);
        }

        [HttpGet]
        public IActionResult ReportChildAccountBudgetByMonthIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            return View("ReportChildAccountBudgetByMonthIndex");
        }
        [HttpGet]
        public IActionResult GetReportChildAccountBudgetByMonth(int BudgetYear, int Month)
        {
            //master
            var Models = new List<ReportChildAccountBudgetByMonth>();
            var GetAccountBudget = DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear && w.UpdateDate.Month == Month).ToList();

            foreach (var item in GetAccountBudget)
            {
                var Model = new ReportChildAccountBudgetByMonth();
                Model.Title = item.Title;
                Model.AccName = DB.AccountBudget.Where(w => w.AccountBudgetd == item.AccountBudgetd).Select(s => s.AccName).FirstOrDefault();
                Model.OrgName = DB.Village.Where(w => w.VillageId == item.ReceiverOrgId).Count() > 0 ? DB.Village.Where(w => w.VillageId == item.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault() : DB.SystemOrgStructures.Where(w => w.OrgId == item.ReceiverOrgId).Select(s => s.OrgName).FirstOrDefault();
                Model.Amount = item.Amount.ToString("N");
                Model.SenderDate = Helper.getDateThai(item.SenderDate);
                Models.Add(Model);
            }


            return PartialView("GetReportChildAccountBudgetByMonth", Models);
        }

        public IActionResult ReportTransAccountBudgetByVillageIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.Village = new SelectList(DB.Village.ToList(), "VillageId", "VillageName");

            return View("ReportTransAccountBudgetByVillageIndex");
        }
        [HttpGet]
        public IActionResult GetReportTransAccountBudgetByVillage(int BudgetYear, int ReceiverOrgId)
        {
            //master
            var Models = new List<ReportTransAccountBudgetByVillageIndex>();
            var GetTransacionAccountBudget = DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear && w.ReceiverOrgId == ReceiverOrgId).ToList();

            foreach (var item in GetTransacionAccountBudget)
            {
                var Model = new ReportTransAccountBudgetByVillageIndex();
                Model.Title = item.Title;
                Model.AccName = DB.AccountBudget.Where(w => w.AccountBudgetd == item.AccountBudgetd).Select(s => s.AccName).FirstOrDefault();
                Model.VillageName = DB.Village.Where(w => w.VillageId == item.ReceiverOrgId).Count() > 0 ? DB.Village.Where(w => w.VillageId == item.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault() : DB.SystemOrgStructures.Where(w => w.OrgId == item.ReceiverOrgId).Select(s => s.OrgName).FirstOrDefault();
                Model.Amount = item.Amount.ToString("N");
                Model.SenderDate = Helper.getDateThai(item.SenderDate);
                Models.Add(Model);
            }

            return PartialView("GetReportTransAccountBudgetByVillage", Models);
        }

        [HttpGet]
        public IActionResult ReportTransAccountBudgetByOrgIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.Org = new SelectList(DB.SystemOrgStructures.ToList(), "OrgId", "OrgName");

            return View("ReportTransAccountBudgetByOrgIndex");
        }
        [HttpGet]
        public IActionResult GetReportTransAccountBudgetByOrg(int BudgetYear, int ReceiverOrgId)
        {
            //master
            var Models = new List<ReportTransAccountBudgetByOrgIndex>();
            var GetTransacionAccountBudget = DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear && w.ReceiverOrgId == ReceiverOrgId).ToList();

            foreach (var item in GetTransacionAccountBudget)
            {
                var Model = new ReportTransAccountBudgetByOrgIndex();
                Model.Title = item.Title;
                Model.AccName = DB.AccountBudget.Where(w => w.AccountBudgetd == item.AccountBudgetd).Select(s => s.AccName).FirstOrDefault();
                Model.OrgName = DB.Village.Where(w => w.VillageId == item.ReceiverOrgId).Count() > 0 ? DB.Village.Where(w => w.VillageId == item.ReceiverOrgId).Select(s => s.VillageName).FirstOrDefault() : DB.SystemOrgStructures.Where(w => w.OrgId == item.ReceiverOrgId).Select(s => s.OrgName).FirstOrDefault();
                Model.Amount = item.Amount.ToString("N");
                Model.SenderDate = Helper.getDateThai(item.SenderDate);
                Models.Add(Model);
            }

            return PartialView("GetReportTransAccountBudgetByOrg", Models);
        }

        [HttpGet]
        public IActionResult ReportTransAccountBudgetActivityByVillageIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.VillageId = new SelectList(DB.Village.ToList(), "VillageId", "VillageName");

            return View("ReportTransAccountBudgetActivityByVillageIndex");
        }
        [HttpGet]
        public IActionResult GetReportTransAccountBudgetActivityByVillage(int BudgetYear, int VillageId)
        {
            //master
            var Models = new List<RootReportTransAccountBudgetActivityByVillageIndex>();
            var GetTransacionAccountBudget = DB.TransactionAccountActivity.Where(w => w.TransactionYear == BudgetYear && w.VillageId == VillageId).ToList();
            string[] StatusName = { "", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ" };

            foreach (var item in GetTransacionAccountBudget)
            {
                var Model = new RootReportTransAccountBudgetActivityByVillageIndex();
                Model.ProjectName = DB.ProjectBudget.Where(w => w.AccountBudgetd == item.ProjectId).Select(s => s.ProjectName).FirstOrDefault();
                Model.ActivityCount = DB.ProjectActivity.Where(w => w.ProjectId == DB.ProjectBudget.Where(w => w.AccountBudgetd == item.ProjectId).Select(s => s.ProjectId).FirstOrDefault()).Count();
                Model.StatusName = StatusName[item.TransactionType];
                Model.Amount = item.Amount.ToString("N");
                Model.VillageName = DB.Village.Where(w => w.VillageId == item.VillageId).Select(s => s.VillageName).FirstOrDefault();
                Models.Add(Model);
            }

            return PartialView("GetReportTransAccountBudgetActivityByVillage", Models);
        }

        [HttpGet]
        public IActionResult ReportTransAccountBudgetActivityByVillageAndMonthIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.VillageId = new SelectList(DB.Village.ToList(), "VillageId", "VillageName");

            return View("ReportTransAccountBudgetActivityByVillageAndMonthIndex");
        }
        [HttpGet]
        public IActionResult GetReportTransAccountBudgetActivityByVillageAndMonth(int BudgetYear, int VillageId, int Month)
        {
            //master
            var Models = new List<RootReportTransAccountBudgetActivityByVillageIndex>();
            var GetTransacionAccountBudget = DB.TransactionAccountActivity.Where(w => w.TransactionYear == BudgetYear && w.VillageId == VillageId && w.ReceiverDate.Month == Month).ToList();
            string[] StatusName = { "", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ" };

            foreach (var item in GetTransacionAccountBudget)
            {
                var Model = new RootReportTransAccountBudgetActivityByVillageIndex();
                Model.ProjectName = DB.ProjectBudget.Where(w => w.AccountBudgetd == item.ProjectId).Select(s => s.ProjectName).FirstOrDefault();
                Model.ActivityCount = DB.ProjectActivity.Where(w => w.ProjectId == DB.ProjectBudget.Where(w => w.AccountBudgetd == item.ProjectId).Select(s => s.ProjectId).FirstOrDefault()).Count();
                Model.StatusName = StatusName[item.TransactionType];
                Model.Amount = item.Amount.ToString("N");
                Model.VillageName = DB.Village.Where(w => w.VillageId == item.VillageId).Select(s => s.VillageName).FirstOrDefault();
                Models.Add(Model);
            }

            return PartialView("GetReportTransAccountBudgetActivityByVillageAndMonth", Models);
        }

        [HttpGet]
        public IActionResult ReportTransBudgetByTypeAndProject()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.Project = new SelectList(DB.ProjectBudget.ToList(), "ProjectId", "ProjectName");

            return View("ReportTransBudgetByTypeAndProject");
        }
        [HttpGet]
        public IActionResult GetReportTransBudgetByTypeAndProject(int BudgetYear, int ProjectId, int TransType)
        {

            //master
            string[] TransactionType = { "เงินสด", "โอนเข้าบัญชี", "เช็ค" };
            var Models = new List<ReportTransBudgetByTypeAndProject>();
            var GetTransacionAccountBudget = DB.TransactionAccountActivity.Where(w => w.TransactionYear == BudgetYear && w.ProjectId == ProjectId && w.TransactionType == TransType).ToList();

            foreach (var item in GetTransacionAccountBudget)
            {
                var Model = new ReportTransBudgetByTypeAndProject();
                Model.Project = DB.ProjectBudget.Where(w => w.ProjectId == item.ProjectId).Select(s => s.ProjectName).FirstOrDefault();
                Model.Activities = DB.ProjectActivity.Where(w => w.ProjectActivityId == item.ActivityId).Select(s => s.ActivityDetail).FirstOrDefault();
                Model.Amount = item.Amount.ToString("N");
                Model.Transfer = DB.Village.Where(w => w.VillageId == item.VillageId).Select(s => s.VillageName).FirstOrDefault();
                Model.Type = TransactionType[item.TransactionType];
                Model.PaymentDate = Helper.getDateThai(item.ReceiverDate);

                Models.Add(Model);
            }


            return PartialView("GetReportTransBudgetByTypeAndProject", Models);
        }

        public IActionResult ReportTransBudgetActivityByChard()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.Project = new SelectList(DB.ProjectBudget.ToList(), "ProjectId", "ProjectName");
            ViewBag.AccChard = new SelectList(DB.AccountActivity.ToList(), "AccountChartId", "AccountName");

            return View("ReportTransBudgetActivityByChard");
        }
        [HttpGet]
        public IActionResult GetReportTransBudgetActivityByChard(int BudgetYear, int ProjectId, int AccChard)
        {

            //master
            var Models = new List<ReportTransBudgetActivityByChard>();
            var GetTransacionAccountBudget = DB.TransactionAccountActivity.Where(w => w.TransactionYear == BudgetYear && w.ProjectId == ProjectId && w.AccChartId == AccChard).ToList();
            string[] TransType = { "เงินสด", "โอนเข้าบัญชี", "เช็ค" };

            foreach (var item in GetTransacionAccountBudget)
            {
                var Model = new ReportTransBudgetActivityByChard();
                Model.Project = DB.ProjectBudget.Where(w => w.ProjectId == item.ProjectId).Select(s => s.ProjectName).FirstOrDefault();
                Model.Activities = DB.ProjectActivity.Where(w => w.ProjectActivityId == item.ActivityId).Select(s => s.ActivityDetail).FirstOrDefault();
                Model.Amount = item.Amount.ToString("N");
                Model.Transferee = DB.Village.Where(w => w.VillageId == item.VillageId).Select(s => s.VillageName).FirstOrDefault();
                Model.Type = TransType[item.TransactionType];
                Model.PaymentDate = Helper.getDateThai(item.ReceiverDate);
                Model.Categories = DB.AccountActivity.Where(w => w.AccountChartId == item.AccChartId).Select(s => s.AccountName).FirstOrDefault();

                Models.Add(Model);
            }

            return PartialView("GetReportTransBudgetActivityByChard", Models);
        }

        public IActionResult ReportTransBudgetActivityByChardAndType()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.Project = new SelectList(DB.ProjectBudget.ToList(), "ProjectId", "ProjectName");

            return View("ReportTransBudgetActivityByChardAndType");
        }
        public IActionResult GetReportTransBudgetActivityByChardAndType(int BudgetYear, int ProjectId, int TransType)
        {

            //master
            var Models = new List<ReportTransBudgetActivityByChardAndType>();
            var GetTransacionAccountBudget = DB.TransactionAccountActivity.Where(w => w.TransactionYear == BudgetYear && w.ProjectId == ProjectId && w.TransactionType == TransType).ToList();
            string[] TransTypes = { "เงินสด", "โอนเข้าบัญชี", "เช็ค" };

            foreach (var item in GetTransacionAccountBudget)
            {
                var Model = new ReportTransBudgetActivityByChardAndType();
                Model.Project = DB.ProjectBudget.Where(w => w.ProjectId == item.ProjectId).Select(s => s.ProjectName).FirstOrDefault();
                Model.Activities = DB.ProjectActivity.Where(w => w.ProjectActivityId == item.ActivityId).Select(s => s.ActivityDetail).FirstOrDefault();
                Model.AmountBaht = item.Amount.ToString("N");
                Model.Transferee = DB.Village.Where(w => w.VillageId == item.VillageId).Select(s => s.VillageName).FirstOrDefault();
                Model.Type = TransTypes[item.TransactionType];
                Model.PaymentDate = Helper.getDateThai(item.ReceiverDate);
                Model.Categories = DB.AccountActivity.Where(w => w.AccountChartId == item.AccChartId).Select(s => s.AccountName).FirstOrDefault();

                Models.Add(Model);
            }

            return PartialView("GetReportTransBudgetActivityByChardAndType", Models);
        }

        #endregion

    }
}
