using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.DateCollection;
using DZ_VILLAGEFUND_WEB.ViewModels.EAccount;
using DZ_VILLAGEFUND_WEB.ViewModels.EProjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.Messaging;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using LicenseContext = OfficeOpenXml.LicenseContext;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Engineering;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    [Authorize]
    public class ElectronicProjectsController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IHostingEnvironment _environment;
        private readonly IConfiguration _configuration;
        private string SystemName = "e-Project";
        public ElectronicProjectsController(
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

        #region project

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            string UserRole = Helper.GetRoleUser(CurrentUser.Id);
            if (UserRole == "HeadQuarterAdmin" || UserRole == "officer" || UserRole == "administrator" || UserRole == "BranchAdmin" || UserRole == "ProvinceAdmin")
            {
                return RedirectToAction("Approve");
            }

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects(int BudgetYear)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var GetStatus = DB.SystemTransactionType.Where(w => w.SystemGroup == 1).ToList();
            /*
             *  1 รอตรวจสอบ
             *  2 แก้ไข
             *  3 รออนุมัติ
             *  4 ไม่อนุมัติ
             *  5 อนุมัติ
             */
            string[] StatusName = { "ร่าง", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ", "ผ่านเรื่อง" };

            var Models = new List<ProjectListViewModel>();
            var Gets = await DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == CurrentUser.VillageId).ToListAsync();
            foreach (var Get in Gets)
            {
                decimal Total = DB.ProjectActivity.Where(w => w.ProjectId == Get.ProjectId).Count();
                decimal ActiveStatus = DB.ProjectActivity.Where(w => w.ProjectId == Get.ProjectId && w.StatusId == 5).Count();

                var Model = new ProjectListViewModel();
                Model.Amount = Get.Amount;
                Model.Progress = (Total == 0 ? 0 : (ActiveStatus / Total) * 100);
                Model.ProjectId = Get.ProjectId;
                Model.ProjectName = Get.ProjectName;
                Model.ProjectTime = Helper.getDateThai(Get.StartProjectDate) + " ถึง " + Helper.getDateThai(Get.EndProjectDate);
                Model.StatusId = Get.StatusId;
                Model.StatusName = GetStatus.Where(w => w.TransactionTypeId == Get.StatusId).Select(s => s.TransactionTypeNameTH).FirstOrDefault();
                Model.ProjectCode = Get.ProjectCode;
                Model.ApproveBy = DB.Users.Where(w => w.Id == Get.ApproveBy).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Model.ApproveDate = Helper.getDateThai(Get.ApproveDate);
                Models.Add(Model);
            }

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return PartialView("GetProjects", Models);
        }

        [HttpGet]
        public IActionResult CreateProject(Int64 ProjectId)
        {
            var GetProject = DB.ProjectBudget.Where(w => w.ProjectId == ProjectId).FirstOrDefault();

            //Select Project From Account Budget
            var GetAccountBudget = DB.AccountBudget.Where(w => w.ParentId != 0 && w.IsActive == true && w.BudgetYear == Helper.CurrentBudgetYear() && w.OpenDate <= DateTime.Now && w.CloseDate >= DateTime.Now);
            ViewBag.AccountBudget = new SelectList(GetAccountBudget, "AccountBudgetd", "AccName");
            ViewBag.Qualification = GetAccountBudget.ToDictionary(x => x.AccountBudgetd, x => x.Qualification);
            ViewBag.SubAmount = GetAccountBudget.ToDictionary(x => x.AccountBudgetd, x => x.SubAmount.ToString("N"));


            //Check ProjectId
            ViewBag.PjId = ProjectId;



            return View("CreateProject", GetProject);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(ProjectBudget Model, IFormFile[] FileUpload, DateCollection Date)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                if (Model.ProjectId != 0)
                {
                    var GetProjectBudget = DB.ProjectBudget.Where(w => w.ProjectId == Model.ProjectId).FirstOrDefault();
                    if (Model.AccountBudgetd != GetProjectBudget.AccountBudgetd)
                    {
                        if (DB.ProjectBudget.Where(w => w.VillageId == CurrentUser.VillageId && w.AccountBudgetd == Model.AccountBudgetd).Count() > 0)
                        {
                            return Json(new { valid = false, message = "ท่านเคยยื่นโครงการนี้แล้ว" });
                        }
                    }

                    DateTime _StartDate = new DateTime(Date.StartYear, Date.StartMonth, Date.StartDay);
                    DateTime _EndDate = new DateTime(Date.EndYear, Date.EndMonth, Date.EndDay);
                    if (_StartDate == _EndDate || _EndDate < _StartDate)
                    {
                        return Json(new { valid = false, message = "วันที่เริ่มต้นโครงการไม่ถูกต้อง" });
                    }

                    var GetAcctBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefault();
                    var GetProject = await DB.ProjectBudget.Where(w => w.ProjectId == Model.ProjectId).FirstOrDefaultAsync();
                    GetProject.ProjectName = GetAcctBudget.AccName;
                    GetProject.Amount = GetAcctBudget.SubAmount;
                    GetProject.AccountBudgetd = GetAcctBudget.AccountBudgetd;
                    GetProject.StartProjectDate = _StartDate;
                    GetProject.EndProjectDate = _EndDate;
                    GetProject.UpdateBy = CurrentUser.Id;
                    GetProject.UpdateDate = DateTime.Now;
                    GetProject.StatusId = 12;
                    DB.ProjectBudget.Update(GetProject);
                    await DB.SaveChangesAsync();

                    // update file 
                    if (FileUpload.Length > 0)
                    {
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                        foreach (var File in FileUpload)
                        {
                            if (File.ContentType != "application/x-msdownload" && Path.GetExtension(File.FileName) != ".msi" && Path.GetExtension(File.FileName) != ".bat")
                            {
                                string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                                string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                                using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                                {
                                    await File.CopyToAsync(fileStream);
                                }

                                // insert file data
                                var FileProject = new ProjectBudgetDocument();
                                FileProject.ProjectId = GetProject.ProjectId;
                                FileProject.TransactionYear = Helper.CurrentBudgetYear();
                                FileProject.FileName = File.FileName;
                                FileProject.GencodeFileName = UniqueFileName;
                                FileProject.FileType = 0;
                                FileProject.UpdateBy = CurrentUser.Id;
                                FileProject.UpdateDate = DateTime.Now;
                                DB.ProjectBudgetDocument.Add(FileProject);
                            }
                            else
                            {
                                return Json(new { valid = false, message = " อัปโหลดไฟล์ document, sheet, zip หรือ multimedia เท่านั้น" });
                            }
                        }

                        await DB.SaveChangesAsync();
                    }
                }
                else
                {

                    if (DB.ProjectBudget.Where(w => w.VillageId == CurrentUser.VillageId && w.AccountBudgetd == Model.AccountBudgetd).Count() > 0)
                    {
                        return Json(new { valid = false, message = "ท่านเคยยื่นโครงการนี้แล้ว" });
                    }

                    DateTime _StartDate = new DateTime(Date.StartYear, Date.StartMonth, Date.StartDay);
                    DateTime _EndDate = new DateTime(Date.EndYear, Date.EndMonth, Date.EndDay);
                    if (_StartDate == _EndDate || _EndDate < _StartDate)
                    {
                        return Json(new { valid = false, message = "วันที่เริ่มต้นโครงการไม่ถูกต้อง" });
                    }

                    // project budget
                    var GetAcctBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == Model.AccountBudgetd).FirstOrDefault();

                    Model.ProjectName = GetAcctBudget.AccName;
                    Model.Amount = GetAcctBudget.SubAmount;
                    Model.AccountBudgetd = GetAcctBudget.AccountBudgetd;
                    Model.VillageId = CurrentUser.VillageId;
                    Model.ProjectCode = GetAcctBudget.AccCode;
                    Model.TransactionYear = Helper.CurrentBudgetYear();
                    Model.Period = 0;
                    Model.StatusId = 12;
                    Model.SignProjectDate = DateTime.Now;
                    Model.UpdateBy = CurrentUser.Id;
                    Model.UpdateDate = DateTime.Now;
                    Model.ApproveBy = null;
                    Model.ApproveDate = DateTime.Now;
                    Model.OrgId = CurrentUser.OrgId;
                    Model.StartProjectDate = _StartDate;
                    Model.EndProjectDate = _EndDate;
                    DB.ProjectBudget.Add(Model);
                    await DB.SaveChangesAsync();

                    // update file 
                    if (FileUpload.Length > 0)
                    {
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                        foreach (var File in FileUpload)
                        {
                            if (File.ContentType != "application/x-msdownload" && Path.GetExtension(File.FileName) != ".msi" && Path.GetExtension(File.FileName) != ".bat")
                            {
                                string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                                string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                                using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                                {
                                    await File.CopyToAsync(fileStream);
                                }

                                // insert file data
                                var FileProject = new ProjectBudgetDocument();
                                FileProject.ProjectId = Model.ProjectId;
                                FileProject.TransactionYear = Helper.CurrentBudgetYear();
                                FileProject.FileName = File.FileName;
                                FileProject.GencodeFileName = UniqueFileName;
                                FileProject.FileType = 0;
                                FileProject.UpdateBy = CurrentUser.Id;
                                FileProject.UpdateDate = DateTime.Now;
                                DB.ProjectBudgetDocument.Add(FileProject);
                            }
                            else
                            {
                                return Json(new { valid = false, message = " อัปโหลดไฟล์ document, sheet, zip หรือ multimedia เท่านั้น" });
                            }
                        }

                        await DB.SaveChangesAsync();
                    }
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ", id = Model.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteProject(Int64 ProjectId)
        {
            try
            {
                var Get = await DB.ProjectBudget.Where(w => w.ProjectId == ProjectId).FirstOrDefaultAsync();
                if (Get.StatusId != 12)
                {
                    return Json(new { valid = false, message = "ไม่สามรถลบข้อมูลได้กรุณาตรวจสอบ" });
                }
                DB.ProjectBudget.Remove(Get);
                DB.SaveChanges();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult GetPeriodDetail(Int64 AccBudgetId)
        {
            var GetPeriods = DB.ProjectPeriod.Where(w => w.AccBudgetId == AccBudgetId).ToList();

            return Json(new { GetPeriods });
        }

        [HttpGet]
        public IActionResult GetPeriodFiles(Int64 AccBudgetId)
        {

            var Gets = new List<ProjectBudgetDocument>();

            var ProjectFiles = DB.ProjectBudgetDocument.Where(w => w.ProjectId == AccBudgetId).ToList();

            foreach (var item in ProjectFiles)
            {
                if (Helper.GetRoleUser(item.UpdateBy) == "HeadQuarterAdmin")
                {
                    Gets.Add(item);
                }
            }

            return Json(new { Gets });
        }

        [HttpGet]
        public IActionResult GetAccStart(Int64 AccBudgetId)
        {
            var GetAcctDate = DB.AccountBudget.Where(w => w.AccountBudgetd == AccBudgetId).FirstOrDefault();

            if (GetAcctDate != null)
            {
                return Json(new { GetAcctDate.AccStart.Day, GetAcctDate.AccStart.Month, GetAcctDate.AccStart.Year });
            }

            return Json(new { DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year });
        }

        [HttpGet]
        public IActionResult GetAccEndDate(Int64 AccBudgetId)
        {
            var GetAcctDate = DB.AccountBudget.Where(w => w.AccountBudgetd == AccBudgetId).FirstOrDefault();

            if (GetAcctDate != null)
            {
                return Json(new { GetAcctDate.AccEndDate.Day, GetAcctDate.AccEndDate.Month, GetAcctDate.AccEndDate.Year });
            }

            return Json(new { DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year });
        }

        [HttpGet]
        public IActionResult GetQualificationContent(Int64 AccBudgetId)
        {
            var GetAcctQualificationContent = DB.AccountBudget.Where(w => w.AccountBudgetd == AccBudgetId).FirstOrDefault();

            if (GetAcctQualificationContent != null)
            {
                return base.Content(GetAcctQualificationContent.Qualification);
            }

            return base.Content("");
        }

        [HttpGet]
        public IActionResult GetAmountString(Int64 AccBudgetId)
        {
            var GetAcctAmountString = DB.AccountBudget.Where(w => w.AccountBudgetd == AccBudgetId).FirstOrDefault();
            string Amount = "";
            if (GetAcctAmountString != null)
            {
                return Json(new { Amount = GetAcctAmountString.SubAmount.ToString("N") });
            }

            return Json(new { Amount });
        }

        // period 

        [HttpGet]
        public IActionResult CreatePeriod(Int64 ProjectId)
        {
            ViewBag.ProjectId = ProjectId;
            return View("CreatePeriod");
        }

        [HttpGet]
        public async Task<IActionResult> GetPeriods(Int64 ProjectId)
        {
            var Models = new List<PeriodViewModel>();
            var Gets = await DB.ProjectActivity.Where(w => w.ProjectId == ProjectId).ToListAsync();
            foreach (var Get in Gets)
            {
                var Model = new PeriodViewModel();
                Model.ProjectPeriodDetail = Get.ActivityDetail;
                Model.ProjectPeriodId = Get.ProjectActivityId;
                Model.StartToEnd = Helper.getDateThai(Get.StartActivityDate) + " ถึง " + Helper.getDateThai(Get.EndActivityDate);
                Model.Status = Get.StatusId;
                Model.Period = Get.Period;
                Models.Add(Model);
            }


            return PartialView("GetPeriods", Models);
        }

        [HttpGet]
        public IActionResult FormAddPeriod(Int64 ProjectId)
        {

            ViewBag.ProjectId = ProjectId;

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

            return PartialView("FormAddPeriod");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddPeriod(ProjectActivity Model, DateCollection Date)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                //if (DB.ProjectActivity.Where(w => w.ProjectId == Model.ProjectId && w.Period == Model.Period).Count() > 0)
                //{
                //    return Json(new { valid = false, message = "กรุณาตรวจสอบ งวดงาน" });
                //}

                DateTime _StartProjectPeriodDate = new DateTime(Date.StartYear, Date.StartMonth, Date.StartDay);
                DateTime _EndProjectPeriodDate = new DateTime(Date.EndYear, Date.EndMonth, Date.EndDay);
                if (_EndProjectPeriodDate < _StartProjectPeriodDate)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบวันที่" });
                }

                var GetProJect = DB.ProjectBudget.Where(w => w.ProjectId == Model.ProjectId).FirstOrDefault();
                var GetAccountBudget = DB.ProjectBudget.Where(w => w.AccountBudgetd == GetProJect.AccountBudgetd && w.StartProjectDate <= _StartProjectPeriodDate && w.EndProjectDate >= _EndProjectPeriodDate).ToList();
                var GetPjActivity = DB.ProjectActivity.Where(w => w.ProjectId == Model.ProjectId).ToList();
                if (GetAccountBudget.Count() == 0)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบวันที่ของกิจกรรม" });
                }

                //Check งบกิจกรรม ไม่ให้เกินงบโครงการ
                var TotalBudgetActivity = GetPjActivity.Sum(s => s.ActivityBudget) + GetAccountBudget.Sum(s => s.Amount);
                if (Model.ActivityBudget > TotalBudgetActivity)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบงบประมาณกิจกรรม" });
                }

                Model.VillageId = CurrentUser.VillageId;
                Model.TransactionYear = Helper.CurrentBudgetYear();
                Model.StatusId = 0;
                Model.UpdateBy = CurrentUser.Id;
                Model.UpdateDate = DateTime.Now;
                Model.ApproveDate = DateTime.Now;
                Model.StartActivityDate = _StartProjectPeriodDate;
                Model.EndActivityDate = _EndProjectPeriodDate;
                Model.Period = GetPjActivity.Count() + 1;
                DB.ProjectActivity.Add(Model);
                await DB.SaveChangesAsync();

            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeletePeriod(Int64 ProjectPeriodId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // get data
                var Get = await DB.ProjectActivity.Where(w => w.ProjectActivityId == ProjectPeriodId).FirstOrDefaultAsync();
                DB.ProjectActivity.Remove(Get);
                await DB.SaveChangesAsync();

                //// delete file
                //var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                //var GetFiles = await DB.ProjectBudgetDocument.Where(w => w.ProjectPeriodId == ProjectPeriodId).ToListAsync();
                //if (GetFiles.Count() > 0)
                //{
                //    foreach (var GetFile in GetFiles)
                //    {
                //        string Oldfilepath = Path.Combine(Uploads, GetFile.GencodeFileName);
                //        if (System.IO.File.Exists(Oldfilepath))
                //        {
                //            System.IO.File.Delete(Oldfilepath);
                //        }

                //        DB.ProjectBudgetDocument.Remove(GetFile);
                //        await DB.SaveChangesAsync();

                //    }
                //}
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> ViewPeiodDocuments(Int64 ProjectPeriodId)
        {
            var GetFile = await DB.ProjectBudgetDocument.Where(w => w.ProjectPeriodId == ProjectPeriodId).ToListAsync();
            return PartialView("ViewPeiodDocuments", GetFile);
        }

        [HttpGet]
        public async Task<IActionResult> PeriodDetail(Int64 ProjectPeriodId, int Record, Int64 ProjectId)
        {
            var Model = new PeriodViewModelDetail();
            var Get = DB.ProjectActivity.Where(w => w.ProjectActivityId == ProjectPeriodId).FirstOrDefault();
            Model.PeriodName = Get.ActivityDetail;
            Model.StartDate = Helper.getDateThai(Get.StartActivityDate);
            Model.EndDate = Helper.getDateThai(Get.EndActivityDate);
            Model.PeriodId = ProjectPeriodId;
            Model.Comment = Get.ActivityComment;
            Model.Status = Get.StatusId;
            Model.ActivityBudget = Get.ActivityBudget.ToString("N");
            ViewBag.Record = Record;
            ViewBag.ProjectPeriodId = ProjectPeriodId;
            ViewBag.ProjectId = ProjectId;

            //File
            List<PeriodFileList> Periods = new List<PeriodFileList>();
            var GetPeriodFile = DB.TransactionFilePeriod.Where(w => w.ProjectPeriodId == ProjectPeriodId).ToList();
            foreach (var item in GetPeriodFile)
            {
                var Period = new PeriodFileList();
                Period.FilePeriodId = item.FilePeriodId;
                Period.ProjectPeriodId = item.ProjectPeriodId;
                Period.TransactionYear = item.TransactionYear;
                Period.FileName = item.FileName;
                Period.GencodeFileName = item.GencodeFileName;
                Period.UpdateDate = Helper.getDateThai(item.UpdateDate);
                Period.ApproverMark = item.Approvemark;
                Periods.Add(Period);
            }
            ViewBag.PeriodFile = Periods.ToList();

            //User
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.UserRole = Helper.GetUserRoleName(CurrentUser.Id);

            ViewBag.StatusProject = DB.ProjectBudget.Where(w => w.ProjectId == ProjectId).Select(s => s.StatusId).FirstOrDefault();

            return View(Model);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateActivityFileStatus(Int64 FilePeriodId)
        {
            try
            {
                //User
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                var Get = DB.TransactionFilePeriod.Where(w => w.FilePeriodId == FilePeriodId).FirstOrDefault();

                Get.Approvemark = true;

                DB.TransactionFilePeriod.Update(Get);
                DB.SaveChanges();

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "อนุมัติเอกสารกิจกรรมโครงการ", HttpContext, SystemName);
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, Message = Error.Message });
            }
            return Json(new { valid = true, Message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProjectFileStatus(Int64 TransactionDocId)
        {
            try
            {
                //User
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                var Get = DB.ProjectBudgetDocument.Where(w => w.TransactionDocId == TransactionDocId).FirstOrDefault();

                Get.Approvemark = true;

                DB.ProjectBudgetDocument.Update(Get);
                DB.SaveChanges();

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "อนุมัติเอกสารโครงการ", HttpContext, SystemName);
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, Message = Error.Message });
            }
            return Json(new { valid = true, Message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpPost]
        public async Task<IActionResult> PostFilePeriod(Int64 ProjectPeriodId, IFormFile[] FileUpload)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                //Update Status
                //var GetPeriod = DB.ProjectActivity.Where(w => w.ProjectActivityId == ProjectPeriodId).FirstOrDefault();
                //GetPeriod.StatusId = 1;
                //DB.ProjectActivity.Update(GetPeriod);
                //DB.SaveChanges();

                // Upload file 

                if (FileUpload == null)
                {
                    throw new Exception(" อัปโหลดไฟล์ Document, Sheet, Image, Multimedia, Zip หรือ Rar และมีขนาด 20 mb เท่านั้น");
                }

                if (FileUpload.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                    foreach (var File in FileUpload)
                    {
                        if (File.ContentType != "application/x-msdownload" && Path.GetExtension(File.FileName) != ".msi" && Path.GetExtension(File.FileName) != ".bat")
                        {
                            string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await File.CopyToAsync(fileStream);
                            }

                            // insert file data
                            var FilePeriod = new TransactionFilePeriod();
                            FilePeriod.TransactionYear = Helper.CurrentBudgetYear();
                            FilePeriod.FileName = File.FileName;
                            FilePeriod.GencodeFileName = UniqueFileName;
                            FilePeriod.ProjectPeriodId = ProjectPeriodId;
                            FilePeriod.UpdateBy = CurrentUser.Id;
                            FilePeriod.UpdateDate = DateTime.Now;
                            DB.TransactionFilePeriod.Add(FilePeriod);
                        }
                        else
                        {
                            throw new Exception(" อัปโหลดไฟล์ Document, Sheet, Image, Multimedia, Zip หรือ Rar เท่านั้น");
                        }
                    }

                    await DB.SaveChangesAsync();
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteFilePeriod(Int64 FilePeriodId)
        {
            try
            {

                // delete file
                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                var GetFiles = await DB.TransactionFilePeriod.Where(w => w.FilePeriodId == FilePeriodId).ToListAsync();
                if (GetFiles.Count() > 0)
                {
                    foreach (var GetFile in GetFiles)
                    {
                        string Oldfilepath = Path.Combine(Uploads, GetFile.GencodeFileName);
                        if (System.IO.File.Exists(Oldfilepath))
                        {
                            System.IO.File.Delete(Oldfilepath);
                        }

                        DB.TransactionFilePeriod.Remove(GetFile);
                        await DB.SaveChangesAsync();

                    }
                }
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        //Period For Admin 

        [HttpGet]
        public IActionResult PeriodListIndex()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PeriodList(int BudgetYear)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string RoleName = Helper.GetUserRoleName(CurrentUser.Id);

            /*
            *  1 รอตรวจสอบ
            *  2 แก้ไข
            *  3 รออนุมัติ
            *  4 ไม่อนุมัติ
            *  5 อนุมัติ
            */



            string[] StatusName = { "ร่าง", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ" };
            var Models = new List<PeriodViewModel>();
            var GetUnderOrgs = DB.SystemOrgStructures.Where(w => w.ParentId == CurrentUser.OrgId).ToList();
            if (RoleName == "HeadQuarterAdmin" || RoleName == "administrator")
            {
                GetUnderOrgs = DB.SystemOrgStructures.ToList();
            }
            if (RoleName == "SuperUser")
            {
                GetUnderOrgs = DB.SystemOrgStructures.Where(w => w.OrgId == CurrentUser.OrgId).ToList();
            }


            //Activity Table
            foreach (var GetUnderOrg in GetUnderOrgs)
            {
                var GetVillages = DB.Village.Where(w => w.OrgId == GetUnderOrg.OrgId).ToList();
                foreach (var GetVillage in GetVillages)
                {
                    var GetPeriod = DB.ProjectActivity.Where(w => w.StatusId != 5 && w.TransactionYear == BudgetYear && w.VillageId == GetVillage.VillageId);
                    foreach (var item in GetPeriod)
                    {
                        var Model = new PeriodViewModel();

                        //Check Aprrove Project
                        if (DB.ProjectBudget.Where(w => w.ProjectId == item.ProjectId).FirstOrDefault() != null && DB.ProjectBudget.Where(w => w.ProjectId == item.ProjectId).FirstOrDefault().StatusId == 17)
                        {
                            Model.ProjectId = item.ProjectId;
                            Model.ProjectCode = DB.ProjectBudget.Where(w => w.ProjectId == item.ProjectId).Select(s => s.ProjectCode).FirstOrDefault();
                            Model.ProjectPeriodId = item.ProjectActivityId;
                            Model.ProjectName = DB.ProjectBudget.Where(w => w.ProjectId == item.ProjectId).Select(s => s.ProjectName).FirstOrDefault();
                            Model.VillageName = DB.Village.Where(w => w.VillageId == item.VillageId).Select(s => s.VillageName).FirstOrDefault();
                            Model.Period = item.Period;
                            Model.Status = item.StatusId;
                            Model.StatusName = StatusName[item.StatusId];
                            if (Model.ProjectCode != null)
                            {
                                Models.Add(Model);
                            }
                        }
                    }
                }
            }

            return PartialView(Models);
        }

        [HttpPost]
        public async Task<IActionResult> PriodApproved(Int64 ProjectPeriodId)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
                /*
                *  1 รอตรวจสอบ
                *  2 แก้ไข
                *  3 รออนุมัติ
                *  4 ไม่อนุมัติ
                *  5 อนุมัติ
                */
                var Period = DB.ProjectActivity.Where(w => w.ProjectActivityId == ProjectPeriodId).FirstOrDefault();
                Period.StatusId = 5;
                Period.ApproveBy = CurrentUser.Id;
                DB.Update(Period);
                DB.SaveChanges();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpPost]
        public async Task<IActionResult> PriodReply(Int64 ProjectPeriodId, string Comment)
        {
            try
            {
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
                /*
                *  1 รอตรวจสอบ
                *  2 แก้ไข
                *  3 รออนุมัติ
                *  4 ไม่อนุมัติ
                *  5 อนุมัติ
                */
                var Period = DB.ProjectActivity.Where(w => w.ProjectActivityId == ProjectPeriodId).FirstOrDefault();
                if (Comment == "" || Comment == null)
                {
                    throw new Exception("กรุณาระบุหมายเหตุ");
                }
                Period.StatusId = 2;
                Period.ActivityComment = Comment;
                Period.ApproveBy = CurrentUser.Id;
                DB.Update(Period);
                DB.SaveChanges();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }
        // assets 

        [HttpGet]
        public IActionResult CreateAsset(Int64 ProjectId)
        {
            ViewBag.ProjectId = ProjectId;
            return View("CreateAsset");
        }

        [HttpGet]
        public async Task<IActionResult> GetAssets(Int64 ProjectId)
        {
            var Gets = await DB.ProjectAsset.Where(w => w.ProjectId == ProjectId).ToListAsync();
            return PartialView("GetAssets", Gets);
        }

        [HttpGet]
        public IActionResult FormAddAsset(Int64 ProjectId, Int64 ProjectAssetId)
        {
            ViewBag.ProjectId = ProjectId;
            //For Edit Asset
            ViewBag.ProjectAssetId = ProjectAssetId;
            var GetProjectAsset = DB.ProjectAsset.Where(w => w.ProjectAssetId == ProjectAssetId).FirstOrDefault();
            return PartialView("FormAddAsset", GetProjectAsset);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddAsset(ProjectAsset Model, Int64 ProjectAssetId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var GetProjectAsset = DB.ProjectAsset.Where(w => w.ProjectAssetId == ProjectAssetId).FirstOrDefault();
                if (GetProjectAsset != null)
                {
                    GetProjectAsset.AssetCode = Model.AssetCode;
                    GetProjectAsset.AssetName = Model.AssetName;
                    GetProjectAsset.AssetAge = Model.AssetAge;
                    GetProjectAsset.Amount = Model.Amount;
                    GetProjectAsset.UpdateDate = DateTime.Now;
                    GetProjectAsset.UpdateBy = CurrentUser.Id;
                    DB.ProjectAsset.Update(GetProjectAsset);
                    await DB.SaveChangesAsync();
                }
                else
                {
                    Model.VillageId = CurrentUser.VillageId;
                    Model.StatusId = 1;
                    Model.UpdateBy = CurrentUser.Id;
                    Model.UpdateDate = DateTime.Now;
                    DB.ProjectAsset.Add(Model);
                    await DB.SaveChangesAsync();
                }
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteAsset(Int64 ProjectAssetId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var Get = await DB.ProjectAsset.Where(w => w.ProjectAssetId == ProjectAssetId).FirstOrDefaultAsync();
                DB.ProjectAsset.Remove(Get);
                await DB.SaveChangesAsync();
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        // documents
        [HttpGet]
        public IActionResult CreateDocument(Int64 ProjectId)
        {
            ViewBag.ProjectId = ProjectId;
            ViewBag.StatusProject = DB.ProjectBudget.Where(w => w.ProjectId == ProjectId).Select(s => s.StatusId).FirstOrDefault();

            return View("CreateDocument");
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments(Int64 ProjectId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            var Gets = await DB.ProjectBudgetDocument.Where(w => w.ProjectId == ProjectId && w.UpdateBy == CurrentUser.Id).ToListAsync();

            return PartialView("GetDocuments", Gets);
        }

        [HttpGet]
        public IActionResult FormAddProjectFile(Int64 ProjectId)
        {
            ViewBag.ProjectId = ProjectId;
            return PartialView("FormAddProjectFile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddProjectFile(Int64 ProjectId, IFormFile[] Document)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // update file 
                if (Document.Length > 0)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                    foreach (var File in Document)
                    {

                        if (File.ContentType != "application/x-msdownload" && Path.GetExtension(File.FileName) != ".msi" && Path.GetExtension(File.FileName) != ".bat")
                        {
                            string file = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await File.CopyToAsync(fileStream);
                            }

                            // add file
                            var NewFile = new ProjectBudgetDocument();
                            NewFile.FileName = File.FileName;
                            NewFile.FileType = 1;
                            NewFile.GencodeFileName = UniqueFileName;
                            NewFile.ProjectId = ProjectId;
                            NewFile.ProjectPeriodId = 0;
                            NewFile.TransactionYear = (DateTime.Now.Year + 543);
                            NewFile.UpdateBy = CurrentUser.Id;
                            NewFile.UpdateDate = DateTime.Now;
                            DB.ProjectBudgetDocument.Add(NewFile);
                            DB.SaveChanges();

                        }
                        else
                        {
                            throw new Exception(" อัปโหลดไฟล์ Document, Sheet, Image, Multimedia, Zip หรือ Rar เท่านั้น");
                        }

                    }

                    await DB.SaveChangesAsync();
                }
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteProjectFile(Int64 TransactionDocId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var Get = DB.ProjectBudgetDocument.Where(w => w.TransactionDocId == TransactionDocId).FirstOrDefault();
                if (Get != null)
                {
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                    string Oldfilepath = Path.Combine(Uploads, Get.GencodeFileName);
                    if (System.IO.File.Exists(Oldfilepath))
                    {
                        System.IO.File.Delete(Oldfilepath);
                    }

                    // delete record
                    DB.ProjectBudgetDocument.Remove(Get);
                    DB.SaveChanges();
                }
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        // approve
        [HttpGet]
        //[Authorize(Roles = "administrator, BranchAdmin, HeadQuarterAdmin, officer")]
        public IActionResult Approve()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            return View("Approve");
        }

        [HttpGet]
        // [Authorize(Roles = "administrator, BranchAdmin, HeadQuarterAdmin, officer")]
        public async Task<IActionResult> GetApproves(int BudgetYear, int Month)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string RoleName = Helper.GetUserRoleName(CurrentUser.Id);
            var GetStatus = DB.SystemTransactionType.Where(w => w.SystemGroup == 1).ToList();

            /*
             *  1 รอตรวจสอบ
             *  2 แก้ไข
             *  3 รออนุมัติ
             *  4 ไม่อนุมัติ
             *  5 อนุมัติ
             */

            var Gets = new List<ProjectBudget>();
            var Models = new List<ProjectListViewModel>();
            if (RoleName == "ProvinceAdmin")
            {
                Gets = await DB.ProjectBudget.Where(w => (w.StatusId == 13 || w.StatusId == 14) && w.OrgId == CurrentUser.OrgId && w.TransactionYear == BudgetYear).ToListAsync();
            }
            else if (RoleName == "BranchAdmin")
            {
                Gets = await DB.ProjectBudget.Where(w => (w.StatusId == 13) && w.OrgId == CurrentUser.OrgId && w.TransactionYear == BudgetYear).ToListAsync();
            }
            else
            {
                Gets = await DB.ProjectBudget.Where(w => (w.StatusId == 15 || w.StatusId == 16 || w.StatusId == 17) && w.TransactionYear == BudgetYear).ToListAsync();
            }

            foreach (var Get in Gets.Where(w => w.UpdateDate.Month == Month))
            {
                decimal Total = DB.ProjectActivity.Where(w => w.ProjectId == Get.ProjectId).Count();
                decimal ActiveStatus = DB.ProjectActivity.Where(w => w.ProjectId == Get.ProjectId && w.StatusId == 5).Count();
                var GetAccBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == Get.AccountBudgetd).FirstOrDefault();
                int CountAllActivity = DB.ProjectActivity.Where(w => w.ProjectId == Get.ProjectId).Count();

                DateTime StartDate = Get.StartProjectDate;
                DateTime EndDate = Get.EndProjectDate;
                TimeSpan DiffDate = (EndDate - StartDate);

                var Model = new ProjectListViewModel();
                Model.Amount = Get.Amount;
                Model.Progress = (Total == 0 ? 0 : (ActiveStatus / Total) * 100);
                Model.ProjectId = Get.ProjectId;
                Model.ProjectName = Get.ProjectName;
                Model.ProjectTime = Helper.getDateThai(Get.StartProjectDate) + " ถึง " + Helper.getDateThai(Get.EndProjectDate);
                Model.StatusId = Get.StatusId;
                Model.StatusName = GetStatus.Where(w => w.TransactionTypeId == Get.StatusId).Select(s => s.TransactionTypeNameTH).FirstOrDefault();
                Model.ProjectCode = Get.ProjectCode;
                Model.RiskActiivity = Helper.CheckActivityRish(Get.AccountBudgetd, CountAllActivity);
                Model.RiskTime = Helper.CheckTimeRish(Get.AccountBudgetd, Convert.ToInt32(DiffDate.TotalDays));
                Model.VillageName = DB.Village.Where(w => w.VillageId == Get.VillageId).Select(s => s.VillageName).FirstOrDefault();
                Model.FullName = DB.Users.Where(w => w.VillageId == Get.VillageId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Model.VillageCodeText = DB.Village.Where(w => w.VillageId == Get.VillageId).Select(s => s.VillageCodeText).FirstOrDefault();
                Models.Add(Model);
            }

            ViewBag.RoleName = RoleName;

            return PartialView("GetApproves", Models);
        }

        [HttpGet]
        public async Task<IActionResult> ViewDetails(Int64 ProjectId)
        {
            // current user, get user role
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.UserRole = Helper.GetUserRoleName(CurrentUser.Id);

            #region get data project detail

            var Get = await DB.ProjectBudget.Where(w => w.ProjectId == ProjectId).FirstOrDefaultAsync();
            var Village = await DB.Village.Where(w => w.VillageId == Get.VillageId).FirstOrDefaultAsync();
            var ProjectDetail = new ProjectDetail();
            ProjectDetail.Address = (Village == null ? "-" : Village.VillageName + " " + Village.VillageMoo
                                   + " ตำบล/แขวง" + DB.SystemSubDistrict.Where(w => w.SubdistrictId == Village.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault()
                                   + " อำเภอ/เขต" + DB.SystemDistrict.Where(w => w.AmphurId == Village.DistrictId).Select(s => s.AmphurName).FirstOrDefault()
                                   + " จังหวัด" + DB.SystemProvince.Where(w => w.ProvinceId == Village.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault()
                                   + " " + Village.PostCode);
            ProjectDetail.Amount = Get.Amount;
            ProjectDetail.BudgetYear = Get.TransactionYear;
            ProjectDetail.ProjecId = Get.ProjectId;
            ProjectDetail.ProJectCode = Get.ProjectCode;
            ProjectDetail.ProjectName = Get.ProjectName;
            ProjectDetail.VillageName = (Village == null ? "-" : Village.VillageName);
            ProjectDetail.Phone = (Village == null ? "-" : Village.Phone);
            ProjectDetail.Email = (Village == null ? "-" : Village.Email);
            ProjectDetail.Comment = Get.ProjectComment;
            ProjectDetail.StatusId = Get.StatusId;

            #endregion

            #region priod

            /*
             *  1 รอตรวจสอบ
             *  2 แก้ไข
             *  3 รออนุมัติ
             *  4 ไม่อนุมัติ
             *  5 อนุมัติ
             */

            string[] StatusName = { "", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ" };

            var PeriodModels = new List<PeriodViewModelDetail>();
            var GetPeriods = await DB.ProjectActivity.Where(w => w.ProjectId == ProjectId).ToListAsync();
            foreach (var GetPeriod in GetPeriods)
            {
                var PeriodModel = new PeriodViewModelDetail();
                PeriodModel.EndDate = Helper.getDateThai(GetPeriod.EndActivityDate);
                PeriodModel.Period = GetPeriod.Period;
                PeriodModel.PeriodId = GetPeriod.ProjectActivityId;
                PeriodModel.PeriodName = GetPeriod.ActivityDetail;
                PeriodModel.StartDate = Helper.getDateThai(GetPeriod.StartActivityDate);
                PeriodModel.StatusName = StatusName[GetPeriod.StatusId];
                PeriodModel.Status = GetPeriod.StatusId;
                PeriodModel.ProjectActivityId = GetPeriod.ProjectActivityId;
                PeriodModels.Add(PeriodModel);
            }

            ViewBag.Periods = PeriodModels;

            #endregion

            #region asset

            ViewBag.Assets = await DB.ProjectAsset.Where(w => w.ProjectId == ProjectId).ToListAsync();

            #endregion

            #region document

            ViewBag.Documents = await DB.ProjectBudgetDocument.Where(w => w.ProjectId == ProjectId).ToListAsync();

            #endregion

            ViewBag.Helper = Helper;

            return View("ViewDetails", ProjectDetail);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateActivityStatus(Int64 ProjectActivityId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // update status
                var Get = DB.ProjectActivity.Where(w => w.ProjectActivityId == ProjectActivityId).FirstOrDefault();
                Get.StatusId = 1;
                DB.ProjectActivity.Update(Get);
                DB.SaveChanges();

                // add log
                Helper.AddUsedLog(CurrentUser.Id, "เปลี่ยนสถานะกิจกรรมโครงการ", HttpContext, SystemName);
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        //[Authorize(Roles = "administrator,BranchAdmin, HeadQuarterAdmin, officer")]
        public async Task<IActionResult> UpdateProjectStatus(int StatusId, Int64 ProjectId, string Comment, bool IsSendMail)
        {
            // current user, get user role
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string RoleUser = Helper.GetUserRoleName(CurrentUser.Id);
            var GetStatus = DB.SystemTransactionType.Where(w => w.SystemGroup == 1).ToList();
            try
            {
                var GetProject = await DB.ProjectBudget.Where(w => w.ProjectId == ProjectId).FirstOrDefaultAsync();
                GetProject.StatusId = StatusId;
                GetProject.ProjectComment = Comment;
                GetProject.UpdateBy = CurrentUser.Id;
                GetProject.UpdateDate = DateTime.Now;
                GetProject.OrgId = (RoleUser == "BranchAdmin" ? DB.Users.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.OrgId).FirstOrDefault() : GetProject.OrgId);
                DB.ProjectBudget.Update(GetProject);
                await DB.SaveChangesAsync();

                var GetUserDetail = DB.Users.Where(w => w.Id == GetProject.UpdateBy).FirstOrDefault();

                // send e-mail
                string Message = "<div style='margin-bottom:10px'><h1><b>สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ</b><sup style='font-size:10px'>สทบ.</sup></h1></div>"
                      + "\n <br/> " + "<div style='background-color:#f26822 ;height:8px; margin-bottom:20px'>&nbsp;</div>"
                      + "\n <br/> " + " โครงการ: " + GetProject.ProjectName + "<br/>"
                      + "\n <br/> " + " สถานะ: " + GetStatus.Where(w => w.TransactionTypeId == GetProject.StatusId).Select(s => s.TransactionTypeNameTH).FirstOrDefault() + "<br/>"
                      + "\n <br/> " + " หมายเหตุ: " + GetProject.ProjectComment + "<br/>"
                      + "\n <br/> " + " *** กรุณาตรวจสอบข้อมูล"
                      + "\n <br/> "
                      + "\n <br/> - ที่ตั้งสำนักงานส่วนกลาง สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.)"
                      + "\n <br/> - เบอร์โทรศัพท์: 02-100-4209"
                      + "\n <br/> - เบอร์แฟกซ์: 02-100-4203";

                if (IsSendMail == true)
                {
                    await Helper.SendMail(DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.Email).FirstOrDefault(), Message);
                }
            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProjectStatusRangeApproves([FromBody] Int64[] ProjectIds)
        {
            // current user, get user role
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string RoleUser = Helper.GetUserRoleName(CurrentUser.Id);
            var GetStatus = DB.SystemTransactionType.Where(w => w.SystemGroup == 1).ToList();
            try
            {
                //Check Ids
                if (ProjectIds.Count() == 0)
                {
                    return Json(new { valid = false, warning = true, message = "กรุณาเลือกโครงการ" });
                }

                var GetProjects = DB.ProjectBudget.Where(w => ProjectIds.Contains(w.ProjectId)).ToList();

                //Assign StatusId 13 รอตรวจสอบ 15 รออนุมัติ 17 อนุมัติ
                int StatusId = 13;
                if (RoleUser == "HeadQuarterAdmin")
                {
                    StatusId = 17;
                }
                else if (RoleUser == "BranchAdmin")
                {
                    StatusId = 15;
                }
                else
                {
                    StatusId = 13;
                }

                GetProjects.ForEach(i =>
                {
                    i.StatusId = StatusId;
                    i.ProjectComment = "";
                    i.UpdateBy = CurrentUser.Id;
                    i.UpdateDate = DateTime.Now;
                    i.ApproveBy = (RoleUser == "HeadQuarterAdmin" ? CurrentUser.Id : "");
                    i.OrgId = Helper.GetParentOrgByOrgId(CurrentUser.OrgId);
                });
                DB.ProjectBudget.UpdateRange(GetProjects);
                DB.SaveChanges();

            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        //[Authorize(Roles = "administrator,BranchAdmin, HeadQuarterAdmin, officer")]
        public async Task<IActionResult> UpdateForwordProjectStatus(int StatusId, Int64 ProjectId, string EventName, string Comment, bool IsSendMail)
        {
            // current user, get user role
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetRoleName = Helper.GetUserRoleName(CurrentUser.Id);
            try
            {
                var GetProject = await DB.ProjectBudget.Where(w => w.ProjectId == ProjectId).FirstOrDefaultAsync();
                if (EventName == "approve")
                {
                    GetProject.StatusId = StatusId;
                    GetProject.UpdateBy = CurrentUser.Id;
                    GetProject.UpdateDate = DateTime.Now;
                    GetProject.ApproveDate = DateTime.Now;
                    GetProject.ApproveBy = CurrentUser.Id;
                    GetProject.ProjectComment = (Comment == null ? GetProject.ProjectComment : Comment);

                    //Send Email to Village
                    if (GetRoleName != "SuperUser")
                    {
                        if (IsSendMail == true)
                        {
                            SendMail(DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.Email).FirstOrDefault(), DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.VillageName).FirstOrDefault(), GetProject.ProjectName, "ได้รับการอนุมัติแล้ว");
                        }
                    }

                }
                if (EventName == "notapprove")
                {
                    GetProject.StatusId = StatusId;
                    GetProject.UpdateBy = CurrentUser.Id;
                    GetProject.UpdateDate = DateTime.Now;
                    GetProject.ProjectComment = (Comment == null ? GetProject.ProjectComment : Comment);
                    GetProject.OrgId = (GetRoleName == "SuperUser" ? CurrentUser.OrgId : Helper.GetParentOrgByOrgId(CurrentUser.OrgId));

                    //Send Email to Village
                    if (GetRoleName != "SuperUser")
                    {
                        if (IsSendMail == true)
                        {
                            SendMail(DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.Email).FirstOrDefault(), DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.VillageName).FirstOrDefault(), GetProject.ProjectName, "ไม่ได้รับการอนุมัติกรุณาตรวจสอบข้อมูล");
                        }
                    }

                }
                if (EventName == "pass")
                {
                    GetProject.StatusId = StatusId;
                    GetProject.UpdateBy = CurrentUser.Id;
                    GetProject.UpdateDate = DateTime.Now;
                    GetProject.ProjectComment = (Comment == null ? GetProject.ProjectComment : Comment);
                    GetProject.OrgId = (GetRoleName == "SuperUser" ? CurrentUser.OrgId : Helper.GetParentOrgByOrgId(CurrentUser.OrgId));

                    //Send Email to Village
                    if (GetRoleName != "SuperUser")
                    {
                        if (IsSendMail == true)
                        {
                            SendMail(DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.Email).FirstOrDefault(), DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.VillageName).FirstOrDefault(), GetProject.ProjectName, "ได้ส่งโครงการให้ทาง สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.) พิจารณาอนุมัติแล้ว");
                        }
                    }

                }
                if (EventName == "reply")
                {
                    GetProject.StatusId = StatusId;
                    GetProject.UpdateBy = CurrentUser.Id;
                    GetProject.UpdateDate = DateTime.Now;
                    GetProject.ProjectComment = (Comment == null ? GetProject.ProjectComment : Comment);
                    GetProject.OrgId = (GetRoleName == "SuperUser" ? CurrentUser.OrgId : Helper.GetParentOrgByOrgId(CurrentUser.OrgId));

                    //Send Email to Village
                    if (GetRoleName != "SuperUser")
                    {
                        if (IsSendMail == true)
                        {
                            SendMail(DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.Email).FirstOrDefault(), DB.Village.Where(w => w.VillageId == GetProject.VillageId).Select(s => s.VillageName).FirstOrDefault(), GetProject.ProjectName, "ทางเจ้าหน้าที่ได้ส่งกลับแก้ไขโครงการ กรุณาตรวจสอบข้อมูล");
                        }
                    }

                }
                if (EventName == "" || EventName == null)
                {
                    GetProject.StatusId = StatusId;
                    GetProject.UpdateBy = CurrentUser.Id;
                    GetProject.UpdateDate = DateTime.Now;
                    GetProject.ProjectComment = (Comment == null ? GetProject.ProjectComment : Comment);
                    GetProject.OrgId = (GetRoleName == "SuperUser" ? CurrentUser.OrgId : Helper.GetParentOrgByOrgId(CurrentUser.OrgId));

                    //Send Email to Province Admin
                    if (GetRoleName == "SuperUser")
                    {
                        SendManyMail(DB.SystemOrgStructures.Where(w => w.OrgId == CurrentUser.OrgId).Select(s => s.OrgName).FirstOrDefault(), GetProject.ProjectName, "ได้ทำการยื่นโครงการ กรุณาตรวจสอบข้อมูล");
                    }

                }

                DB.ProjectBudget.Update(GetProject);

                await DB.SaveChangesAsync();



            }
            catch (Exception Err)
            {
                return Json(new { valid = false, message = Err.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        #endregion

        #region setting 

        [HttpGet]
        public IActionResult StructureIndex()
        {
            return View("StructureIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetStructures()
        {
            // current user, get user role
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetModels = await DB.SystemOrgStructures.OrderBy(o => o.OrgId).ToListAsync();

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return PartialView("GetStructures", GetModels);
        }

        [HttpGet]
        public IActionResult FormAddStructure()
        {
            return PartialView("FormAddStructure");
        }
        [HttpGet]
        public IActionResult FormEditStructure(int Id)
        {

            var GetSysOrg = DB.SystemOrgStructures.Where(w => w.OrgId == Id).FirstOrDefault();

            return PartialView("FormEditStructure", GetSysOrg);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FormAddOrgStructure(string OrgName)
        {
            try
            {
                var Model = new SystemOrgStructures();
                Model.ParentId = 1;
                Model.OrgName = OrgName;
                DB.SystemOrgStructures.Add(Model);
                DB.SaveChanges();

                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FormEditOrgStructure(int OrgId, string OrgName)
        {
            try
            {
                var GetSysOrg = DB.SystemOrgStructures.Where(w => w.OrgId == OrgId).FirstOrDefault();
                GetSysOrg.OrgName = OrgName;
                DB.SystemOrgStructures.Update(GetSysOrg);
                DB.SaveChanges();

                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }
        [HttpGet]
        public IActionResult DeleteOrg(int Id)
        {
            try
            {
                var GetSysOrg = DB.SystemOrgStructures.Where(w => w.OrgId == Id).FirstOrDefault();
                DB.SystemOrgStructures.Remove(GetSysOrg);
                DB.SaveChanges();

                return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public IActionResult ProjectTrackingIndex()
        {

            var CurrentBudgetYear = Helper.CurrentBudgetYear();

            ViewBag.CurrentBudgetYear = CurrentBudgetYear;

            var Village = DB.Village.ToList();
            var _Village = new List<SelectListItem>();
            foreach (var item in Village)
            {

                _Village.Add(new SelectListItem
                {
                    Text = item.VillageCodeText + "-" + item.VillageName,
                    Value = item.VillageId.ToString()
                });

            }
            ViewBag.Villages = new SelectList(_Village, "Value", "Text");

            ViewBag.CurrentProjects = new SelectList(DB.ProjectBudget.Where(w => w.StatusId == 17 && w.TransactionYear == CurrentBudgetYear).ToList(), "ProjectId", "ProjectName");

            return View("ProjectTrackingIndex");
        }

        [HttpGet]
        public IActionResult GetProjectTracking(int VillageId, int CurrentBudgetYear)
        {

            var Project = new SelectList(DB.ProjectBudget.Where(w => w.StatusId == 17 && w.VillageId == VillageId && w.TransactionYear == CurrentBudgetYear).ToList(), "ProjectId", "ProjectName");

            return Json(new { Project });
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectDetail(int BudgetYear, Int64 VillageId, Int64 CurrentProjectId)
        {

            // current user, get user role
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.UserRole = Helper.GetUserRoleName(CurrentUser.Id);

            #region get data project detail

            var Get = await DB.ProjectBudget.Where(w => w.ProjectId == CurrentProjectId && w.TransactionYear == BudgetYear).FirstOrDefaultAsync();
            var ProjectDetail = new ProjectDetail();
            if (Get != null)
            {
                var Village = await DB.Village.Where(w => w.VillageId == VillageId).FirstOrDefaultAsync();
                ProjectDetail.Address = (Village == null ? "-" : Village.VillageName + " " + Village.VillageMoo
                       + " ตำบล/แขวง" + DB.SystemSubDistrict.Where(w => w.SubdistrictId == Village.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault()
                       + " อำเภอ/เขต" + DB.SystemDistrict.Where(w => w.AmphurId == Village.DistrictId).Select(s => s.AmphurName).FirstOrDefault()
                       + " จังหวัด" + DB.SystemProvince.Where(w => w.ProvinceId == Village.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault()
                       + " " + Village.PostCode);
                ProjectDetail.Amount = Get.Amount;
                ProjectDetail.BudgetYear = Get.TransactionYear;
                ProjectDetail.ProjecId = Get.ProjectId;
                ProjectDetail.ProJectCode = Get.ProjectCode;
                ProjectDetail.ProjectName = Get.ProjectName + " (" + Get.ProjectCode + ")";
                ProjectDetail.VillageName = (Village == null ? "-" : Village.VillageName);
                ProjectDetail.Phone = (Village == null ? "-" : Village.Phone);
                ProjectDetail.Email = (Village == null ? "-" : Village.Email);
                ProjectDetail.Comment = Get.ProjectComment;
                ProjectDetail.StatusId = Get.StatusId;
            }

            #endregion

            #region priod

            /*
             *  1 รอตรวจสอบ
             *  2 แก้ไข
             *  3 รออนุมัติ
             *  4 ไม่อนุมัติ
             *  5 อนุมัติ
             */

            string[] StatusName = { "", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ" };

            var PeriodModels = new List<PeriodViewModelDetail>();
            var GetPeriods = await DB.ProjectActivity.Where(w => w.ProjectId == CurrentProjectId && w.TransactionYear == BudgetYear).ToListAsync();
            foreach (var GetPeriod in GetPeriods)
            {
                var PeriodModel = new PeriodViewModelDetail();
                PeriodModel.EndDate = Helper.getDateThai(GetPeriod.EndActivityDate);
                PeriodModel.Period = GetPeriod.Period;
                PeriodModel.PeriodId = GetPeriod.ProjectActivityId;
                PeriodModel.PeriodName = GetPeriod.ActivityDetail;
                PeriodModel.StartDate = Helper.getDateThai(GetPeriod.StartActivityDate);
                PeriodModel.StatusName = StatusName[GetPeriod.StatusId];
                PeriodModel.Status = GetPeriod.StatusId;
                PeriodModel.ProjectActivityId = GetPeriod.ProjectActivityId;
                PeriodModels.Add(PeriodModel);
            }

            ViewBag.Periods = PeriodModels;

            #endregion

            #region asset

            ViewBag.Assets = await DB.ProjectAsset.Where(w => w.ProjectId == CurrentProjectId).ToListAsync();

            #endregion

            return PartialView("GetProjectDetail", ProjectDetail);
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

        #endregion

        #region mile stone

        [HttpGet]
        public async Task<IActionResult> MileStoneIndex()
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            //ViewBag.Village = new SelectList(DB.Village.ToList(), "VillageId", "VillageName");

            var Village = DB.Village.ToList();
            var _Village = new List<SelectListItem>();
            foreach (var item in Village)
            {

                _Village.Add(new SelectListItem
                {
                    Text = item.VillageCodeText + "-" + item.VillageName,
                    Value = item.VillageId.ToString()
                });

            }
            ViewBag.Village = new SelectList(_Village, "Value", "Text");

            return View("MileStoneIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetMileStoneIndex(Int64 ProjectId, int BudgetYear, Int64 VillageId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            var Models = new List<MileStoneViewModel>();
            var GetProjects = DB.ProjectBudget.Where(w => w.AccountBudgetd == ProjectId && w.TransactionYear == BudgetYear && w.VillageId == VillageId).ToList();
            foreach (var GetProject in GetProjects)
            {
                var GetActivities = DB.ProjectActivity.Where(w => w.ProjectId == GetProject.ProjectId).ToList();
                foreach (var Get in GetActivities)
                {
                    var Model = new MileStoneViewModel();
                    Model.ActivityName = Get.ActivityDetail;
                    Model.Id = Get.ProjectActivityId;
                    Model.ProjectName = GetProject.ProjectName;
                    Model.AccCode = DB.AccountBudget.Where(w => w.AccountBudgetd == GetProject.AccountBudgetd).Select(s => s.AccCode).FirstOrDefault() as string;
                    Models.Add(Model);
                }
            }

            return PartialView("GetMileStoneIndex", Models);
        }

        [HttpGet]
        public IActionResult GetProjectByBudget(int BudgetYear, Int64 VillageId)
        {
            List<SelectListItem> Project = new List<SelectListItem>();
            var GetProjects = DB.AccountBudget.Where(w => w.BudgetYear == BudgetYear && w.ParentId != 0 && DB.ProjectBudget.Where(w => w.VillageId == VillageId).Any(s => s.AccountBudgetd == w.AccountBudgetd)).ToList();
            foreach (var GetProject in GetProjects)
            {
                Project.Add(new SelectListItem() { Text = GetProject.AccName, Value = GetProject.AccountBudgetd.ToString() });
            }

            return Json(Project);
        }

        [HttpGet]
        public IActionResult ViewMileStone(string ActivityId)
        {
            var Models = new List<ProjectActivity>();
            string[] _ActivityId = ActivityId.Split(",");
            foreach (var Id in _ActivityId)
            {
                var Gets = DB.ProjectActivity.Where(w => w.ProjectActivityId == Convert.ToInt64(Id)).ToList();
                foreach (var Get in Gets)
                {
                    var Model = new ProjectActivity();
                    Model.ActivityDetail = Get.ActivityDetail;
                    Model.ProjectActivityId = Get.ProjectActivityId;
                    Models.Add(Model);
                }
            }

            return View("ViewMileStone", Models);
        }

        [HttpGet]
        public IActionResult GetActivityDetail(Int64 ProjectActivityId)
        {
            string[] StatusName = { "", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ" };
            var GetActivity = DB.ProjectActivity.Where(w => w.ProjectActivityId == ProjectActivityId).FirstOrDefault();
            var GetVillage = DB.Village.Where(w => w.VillageId == GetActivity.VillageId).FirstOrDefault();
            var Model = new MileStoneDetail();
            Model.AcName = GetActivity.ActivityDetail;
            Model.EndDate = Helper.getDateThai(GetActivity.EndActivityDate);
            Model.StartDate = Helper.getDateThai(GetActivity.StartActivityDate);
            Model.StatusName = StatusName[GetActivity.StatusId];
            Model.VillageName = GetVillage.VillageName;
            Model.Address = GetVillage.VillageAddress + " "
                            + GetVillage.VillageMoo + "  ต." + DB.SystemSubDistrict.Where(w => w.SubdistrictId == GetVillage.SubDistrictId).Select(s => s.SubdistrictName).FirstOrDefault() +
                            " อ." + DB.SystemDistrict.Where(w => w.AmphurId == GetVillage.DistrictId).Select(s => s.AmphurName).FirstOrDefault() +
                            " จ." + DB.SystemProvince.Where(w => w.ProvinceId == GetVillage.ProvinceId).Select(s => s.ProvinceName).FirstOrDefault() + " "
                            + GetVillage.PostCode;
            Model.Phone = (GetVillage.Phone != "" ? GetVillage.Phone : "-");
            Model.Comment = GetActivity.ActivityComment;


            // get file
            ViewBag.GetFiles = DB.TransactionFilePeriod.Where(w => w.ProjectPeriodId == ProjectActivityId).ToList();

            return PartialView("GetActivityDetail", Model);
        }

        #endregion

        #region Report

        [HttpGet]
        public async Task<IActionResult> ReportPeriodDetail(Int64 ProjectPeriodId, int Record, int ProjectId)
        {

            var Model = new PeriodViewModelDetail();
            var Get = DB.ProjectActivity.Where(w => w.ProjectActivityId == ProjectPeriodId).FirstOrDefault();
            Model.PeriodName = Get.ActivityDetail;
            Model.StartDate = Helper.getDateThai(Get.StartActivityDate);
            Model.EndDate = Helper.getDateThai(Get.EndActivityDate);
            Model.PeriodId = ProjectPeriodId;
            Model.Comment = Get.ActivityComment;
            ViewBag.Record = Record;
            ViewBag.ProjectPeriodId = ProjectPeriodId;
            ViewBag.ProjectId = ProjectId;

            //File
            List<PeriodFileList> Periods = new List<PeriodFileList>();
            var GetPeriodFile = DB.TransactionFilePeriod.Where(w => w.ProjectPeriodId == ProjectPeriodId).ToList();
            foreach (var item in GetPeriodFile)
            {
                var Period = new PeriodFileList();
                Period.FilePeriodId = item.FilePeriodId;
                Period.ProjectPeriodId = item.ProjectPeriodId;
                Period.TransactionYear = item.TransactionYear;
                Period.FileName = item.FileName;
                Period.GencodeFileName = item.GencodeFileName;
                Period.UpdateDate = Helper.getDateThai(item.UpdateDate);
                Period.ApproverMark = item.Approvemark;
                Periods.Add(Period);
            }
            ViewBag.PeriodFile = Periods.ToList();

            //User
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.UserRole = Helper.GetUserRoleName(CurrentUser.Id);

            return View("ReportPeriodDetail", Model);
        }

        [HttpGet]
        public IActionResult ReportApproveAndPendingActTransactionProvince()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            ViewBag.ProvinceIds = new SelectList(DB.SystemProvince.ToList(), "ProvinceId", "ProvinceName");

            return View("ReportApproveAndPendingActTransactionProvince");
        }
        [HttpGet]
        public IActionResult GetReportApproveAndPendingActTransactionProvince(int BudgetYear, int ProvinceId)
        {


            var Models = new List<ReportApproveAndPendingActTransactionProvince>();

            //Get Project
            var GetProjectBudget = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear).ToList().GroupBy(g => g.VillageId);

            foreach (var item in GetProjectBudget)
            {
                var Model = new ReportApproveAndPendingActTransactionProvince();
                var DistrictId = DB.Village.Where(w => w.VillageId == Int64.Parse(item.Key.ToString()) && w.ProvinceId == ProvinceId && w.IsActive == true).Select(s => s.DistrictId).FirstOrDefault();
                Model.DistrictName = DB.SystemDistrict.Where(w => w.AmphurId == int.Parse(DistrictId.ToString())).Select(s => s.AmphurName).FirstOrDefault();
                Model.VillageNumberAll = DB.ProjectBudget.Where(w => w.VillageId == Int64.Parse(item.Key.ToString()) && w.StatusId >= 12 && w.TransactionYear == BudgetYear).Count();
                Model.VillageNumber = DB.ProjectBudget.Where(w => w.VillageId == Int64.Parse(item.Key.ToString()) && w.StatusId == 17 && w.TransactionYear == BudgetYear).Count();
                Model.AmountProject = DB.ProjectBudget.Where(w => w.VillageId == Int64.Parse(item.Key.ToString()) && w.StatusId == 17 && w.TransactionYear == BudgetYear).Select(s => s.Amount).Sum();

                if (!string.IsNullOrEmpty(Model.DistrictName))

                    Models.Add(Model);

            }

            return PartialView("GetReportApproveAndPendingActTransactionProvince", Models);
        }

        [HttpGet]
        public IActionResult ReportApproveAndPendingActTransactionOrgId()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            ViewBag.OrgId = new SelectList(DB.SystemOrgStructures.Where(w => w.OrgId < 32 && w.ParentId != 0).ToList().OrderBy(o => o.OrgId), "OrgId", "OrgName");

            return View("ReportApproveAndPendingActTransactionOrgId");
        }
        [HttpGet]
        public IActionResult GetReportApproveAndPendingActTransactionOrgId(int BudgetYear, int OrgId)
        {


            var Models = new List<ReportApproveAndPendingActTransactionProvince>();

            //Get Project
            var GetProjectBudget = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && DB.Village.Where(w => DB.SystemOrgStructures.Where(w => w.ParentId == OrgId).Select(s => s.OrgId).Contains(w.OrgId)).Select(s => s.VillageId).Contains(w.VillageId)).ToList().GroupBy(g => g.VillageId);

            foreach (var item in GetProjectBudget)
            {
                var Model = new ReportApproveAndPendingActTransactionProvince();
                Model.VillageName = DB.Village.Where(w => w.VillageId == Int64.Parse(item.Key.ToString())).Select(s => s.VillageName).FirstOrDefault();
                Model.VillageNumberAll = DB.ProjectBudget.Where(w => w.VillageId == Int64.Parse(item.Key.ToString()) && w.StatusId > 12 && w.TransactionYear == BudgetYear).Count();
                Model.VillageNumber = DB.ProjectBudget.Where(w => w.VillageId == Int64.Parse(item.Key.ToString()) && w.StatusId == 17 && w.TransactionYear == BudgetYear && w.OrgId == OrgId).Count();
                Model.AmountProject = DB.ProjectBudget.Where(w => w.VillageId == Int64.Parse(item.Key.ToString()) && w.StatusId == 17 && w.TransactionYear == BudgetYear && w.OrgId == OrgId).Select(s => s.Amount).Sum();
                Model.VillageCode = DB.Village.Where(w => w.VillageId == Int64.Parse(item.Key.ToString())).Select(s => s.VillageCodeText).FirstOrDefault();
                //if (!string.IsNullOrEmpty(Model.ProvinceName))

                Models.Add(Model);

            }

            return PartialView("GetReportApproveAndPendingActTransactionOrgId", Models);
        }

        [HttpGet]
        public IActionResult ReportRequestSummaryByProvince()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //Province
            var ProvinceId = DB.SystemProvince.ToList();
            ViewBag.ProvinceIds = new SelectList(ProvinceId.OrderBy(o => o.ProvinceId), "ProvinceId", "ProvinceName");

            return View("ReportRequestSummaryByProvince");
        }
        [HttpGet]
        public IActionResult GetReportRequestSummaryByProvince(int BudgetYear, int ProvinceId)
        {

            //master
            var Model = new ReportRequestSummaryByProvince();
            Model.ProvinceName = DB.SystemProvince.Where(w => w.ProvinceId == ProvinceId).Select(s => s.ProvinceName).FirstOrDefault();

            var Get = DB.Village.Where(w => w.ProvinceId == ProvinceId).ToList();

            foreach (var item in Get)
            {

                //Pending
                Model.PendingVillageNumber += DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == item.VillageId && w.StatusId == 13).GroupBy(g => g.VillageId).Count();
                Model.PendingProjectNumber += DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == item.VillageId && w.StatusId == 13).GroupBy(g => g.VillageId).Count();
                Model.PendingAmountProject += DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == item.VillageId && w.StatusId == 13).Sum(s => s.Amount);

                //Draft
                Model.DraftProjectNumber += DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == item.VillageId && w.StatusId == 12).Count();

                //Approve
                Model.VillageNumber += DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == item.VillageId && w.StatusId == 17).GroupBy(g => g.VillageId).Count();
                Model.ProjectNumber += DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == item.VillageId && w.StatusId == 17).GroupBy(g => g.VillageId).Count();
                Model.AmountProject += DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.VillageId == item.VillageId && w.StatusId == 17).Sum(s => s.Amount);

                //Act Transaction
                Model._VillageNumber += DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear && w.ReceiverOrgId == item.VillageId).GroupBy(g => g.ReceiverOrgId).Count();
                Model._ProjectNumber += DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear && w.ReceiverOrgId == item.VillageId).GroupBy(g => g.ReceiverOrgId).Count();
                Model._AmountProject += DB.TransacionAccountBudget.Where(w => w.TransactionYear == BudgetYear && w.ReceiverOrgId == item.VillageId).Sum(s => s.Amount);

                Model.VillageNumberAll += 1;

            }

            return PartialView("GetReportRequestSummaryByProvince", Model);
        }

        [HttpGet]
        public IActionResult ReportProjectFromAcctBudgetByMonthIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            return View("ReportProjectFromAcctBudgetByMonthIndex");
        }
        [HttpGet]
        public async Task<IActionResult> GetReportProjectFromAcctBudgetByMonth(int BudgetYear, int Month)
        {

            /* get data account budget */
            var Models = new List<EprojectStructures>();
            var GetDatas = await DB.AccountBudget.Where(w => w.BudgetYear == BudgetYear && w.UpdateDate.Month == Month).ToListAsync();
            foreach (var Get in GetDatas)
            {
                var Model = new EprojectStructures();
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

            return PartialView("GetReportProjectFromAcctBudgetByMonth", Models);
        }

        [HttpGet]
        public IActionResult ReportProjectTypeIndex()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            var OrgIds = DB.SystemOrgStructures.ToList();

            ViewBag.OrgIds = new SelectList(OrgIds.OrderBy(o => o.OrgId), "OrgId", "OrgName");

            return View("ReportProjectTypeIndex");
        }
        [HttpGet]
        public IActionResult GetReportProjectType(int BudgetYear, int OrgId)
        {

            decimal TotalAmount = 0;

            var Models = new List<ReportProjectType>();

            var GetProjectBudget = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.OrgId == OrgId).ToList().Select(s => new { s.AccountBudgetd, s.ProjectId }).Distinct();

            foreach (var item in GetProjectBudget)
            {
                var Model = new ReportProjectType();
                Model.ProjectTypeName = DB.ProjectBudget.Where(w => w.AccountBudgetd == item.ProjectId).Select(s => s.ProjectName).FirstOrDefault();
                Model.ProjectNumber = DB.ProjectBudget.Where(w => w.AccountBudgetd == item.ProjectId).Count();
                Model.Amount = DB.ProjectBudget.Where(w => w.AccountBudgetd == item.ProjectId).Sum(s => s.Amount);
                Model.AccCode = DB.AccountBudget.Where(w => w.AccountBudgetd == item.AccountBudgetd).Select(s => s.AccCode).FirstOrDefault();
                TotalAmount += Model.Amount;

                Models.Add(Model);

            }

            ViewBag.TotalAmount = TotalAmount;

            return PartialView("GetReportProjectType", Models);
        }

        [HttpGet]
        public IActionResult ReportProjectBudgetRisk()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.OrgId = new SelectList(DB.SystemOrgStructures.Where(w => w.OrgId < 32 && w.ParentId != 0).ToList().OrderBy(o => o.OrgId), "OrgId", "OrgName");

            return View("ReportProjectBudgetRisk");
        }
        [HttpGet]
        public async Task<IActionResult> GetProjectBudgetRisk(int BudgetYear, int OrgId)
        {


            var Models = new List<ProjectListViewModel>();
            var Gets = await DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && DB.Village.Where(w => DB.SystemOrgStructures.Where(w => w.ParentId == OrgId).Select(s => s.OrgId).Contains(w.OrgId)).Select(s => s.VillageId).Contains(w.VillageId)).ToListAsync();

            foreach (var Get in Gets)
            {
                decimal Total = DB.ProjectActivity.Where(w => w.ProjectId == Get.ProjectId).Count();
                decimal ActiveStatus = DB.ProjectActivity.Where(w => w.ProjectId == Get.ProjectId && w.StatusId == 5).Count();
                var GetAccBudget = DB.AccountBudget.Where(w => w.AccountBudgetd == Get.AccountBudgetd).FirstOrDefault();
                int CountAllActivity = DB.ProjectActivity.Where(w => w.ProjectId == Get.ProjectId).Count();

                DateTime StartDate = Get.StartProjectDate;
                DateTime EndDate = Get.EndProjectDate;
                TimeSpan DiffDate = (EndDate - StartDate);

                var Model = new ProjectListViewModel();
                Model.Amount = Get.Amount;
                Model.Progress = (Total == 0 ? 0 : (ActiveStatus / Total) * 100);
                Model.ProjectId = Get.ProjectId;
                Model.ProjectName = Get.ProjectName;
                Model.RiskActiivity = Helper.CheckActivityRish(Get.AccountBudgetd, CountAllActivity);
                Model.RiskTime = Helper.CheckTimeRish(Get.AccountBudgetd, Convert.ToInt32(DiffDate.TotalDays));
                Model.VillageName = DB.Village.Where(w => w.VillageId == Get.VillageId).Select(s => s.VillageName).FirstOrDefault();
                Model.VillageCodeText = DB.Village.Where(w => w.VillageId == Get.VillageId).Select(s => s.VillageCodeText).FirstOrDefault();
                Model.AccCode = DB.AccountBudget.Where(w => w.AccountBudgetd == Get.AccountBudgetd).Select(s => s.AccCode).FirstOrDefault();
                Models.Add(Model);

            }

            return PartialView("GetProjectBudgetRisk", Models);
        }

        [HttpGet]
        public IActionResult ReportProjectApprove()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.OrgId = new SelectList(DB.SystemOrgStructures.Where(w => w.OrgId < 32 && w.ParentId != 0).ToList().OrderBy(o => o.OrgId), "OrgId", "OrgName");

            return View("ReportProjectApprove");
        }
        [HttpGet]
        public IActionResult GetReportProjectApprove(int BudgetYear, int OrgId)
        {


            var Models = new List<ProjectListViewModel>();
            var Gets = DB.ProjectActivity.Where(w => w.TransactionYear == BudgetYear && DB.Village.Where(w => DB.SystemOrgStructures.Where(w => w.ParentId == OrgId).Select(s => s.OrgId).Contains(w.OrgId)).Select(s => s.VillageId).Contains(w.VillageId)).ToList().GroupBy(g => g.VillageId);

            //StatusName = { "", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ" };

            foreach (var Get in Gets)
            {

                var Model = new ProjectListViewModel();
                Model.VillageName = DB.Village.Where(w => w.VillageId == Int64.Parse(Get.Key.ToString())).Select(s => s.VillageName).FirstOrDefault();
                Model.StatusWait = DB.ProjectActivity.Where(w => w.VillageId == Int64.Parse(Get.Key.ToString()) && w.StatusId == 3).Count();
                Model.StatusApprove = DB.ProjectActivity.Where(w => w.VillageId == Int64.Parse(Get.Key.ToString()) && w.StatusId == 5).Count();
                Model.VillageCodeText = DB.Village.Where(w => w.VillageId == Int64.Parse(Get.Key.ToString())).Select(s => s.VillageCodeText).FirstOrDefault();

                Models.Add(Model);

            }

            return PartialView("GetReportProjectApprove", Models);
        }

        [HttpGet]
        public IActionResult ReportVillageApprove()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.OrgId = new SelectList(DB.SystemOrgStructures.Where(w => w.OrgId < 32 && w.ParentId != 0).ToList().OrderBy(o => o.OrgId), "OrgId", "OrgName");

            return View("ReportVillageApprove");
        }
        public IActionResult GetReportVillageApprove(int BudgetYear, int OrgId)
        {


            var Models = new List<ProjectListViewModel>();
            var Gets = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.StatusId == 17 && DB.Village.Where(w => DB.SystemOrgStructures.Where(w => w.ParentId == OrgId).Select(s => s.OrgId).Contains(w.OrgId)).Select(s => s.VillageId).Contains(w.VillageId)).ToList();

            //StatusName = { "", "รอตรวจสอบ", "ส่งกลับแก้ไข", "รออนุมัติ", "ไม่อนุมัติ", "อนุมัติ" };

            foreach (var Get in Gets)
            {

                var Model = new ProjectListViewModel();

                Model.Amount = DB.ProjectBudget.Where(w => w.VillageId == Get.VillageId && w.ProjectId == Get.ProjectId).Select(s => s.Amount).FirstOrDefault();
                Model.ProjectName = DB.ProjectBudget.Where(w => w.VillageId == Get.VillageId && w.ProjectId == Get.ProjectId).Select(s => s.ProjectName).FirstOrDefault();
                Model.VillageName = DB.Village.Where(w => w.VillageId == Get.VillageId).Select(s => s.VillageName).FirstOrDefault();
                Model.VillageCodeText = DB.Village.Where(w => w.VillageId == Get.VillageId).Select(s => s.VillageCodeText).FirstOrDefault();
                Model.AccCode = DB.AccountBudget.Where(w => w.AccountBudgetd == Get.AccountBudgetd).Select(s => s.AccCode).FirstOrDefault();



                Models.Add(Model);

            }

            return PartialView("GetReportVillageApprove", Models);
        }

        [HttpGet]
        public IActionResult ReportSummarizeProjectIndex()
        {

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //Org Id
            var OrgId = DB.SystemOrgStructures.ToList();
            ViewBag.OrgIds = new SelectList(OrgId.OrderBy(o => o.OrgId), "OrgId", "OrgName");


            return View("ReportSummarizeProjectIndex");
        }
        [HttpGet]
        public IActionResult GetReportSummarizeProject(int BudgetYear, int OrgId)
        {

            //master
            var Models = new List<ReportSummarizeProject>();

            var Gets = DB.ProjectBudget.Where(w => w.TransactionYear == BudgetYear && w.OrgId == OrgId).ToList().GroupBy(g => g.VillageId);
            //Get All
            foreach (var item in Gets)
            {
                var Model = new ReportSummarizeProject();
                Model.FundCode = DB.Village.Where(w => w.VillageId == item.Key).Select(s => s.VillageCodeText).FirstOrDefault();
                Model.Village = DB.Village.Where(w => w.VillageId == item.Key).Select(s => s.VillageName).FirstOrDefault();
                Model.Moo = DB.Village.Where(w => w.VillageId == item.Key).Select(s => s.VillageMoo).FirstOrDefault().ToString();
                Model.SubDistrict = DB.SystemSubDistrict.Where(w => w.SubdistrictId == DB.Village.Where(w => w.VillageId == item.Key).Select(s => s.SubDistrictId).FirstOrDefault()).Select(s => s.SubdistrictName).FirstOrDefault();
                Model.District = DB.SystemDistrict.Where(w => w.AmphurId == DB.Village.Where(w => w.VillageId == item.Key).Select(s => s.DistrictId).FirstOrDefault()).Select(s => s.AmphurName).FirstOrDefault();
                Model.Province = DB.SystemProvince.Where(w => w.ProvinceId == DB.Village.Where(w => w.VillageId == item.Key).Select(s => s.ProvinceId).FirstOrDefault()).Select(s => s.ProvinceName).FirstOrDefault();
                Model.ProjectCount = DB.ProjectBudget.Where(w => w.VillageId == item.Key).Count();
                Model.Budget = DB.ProjectBudget.Where(w => w.VillageId == item.Key).Sum(s => s.Amount).ToString("N");
                Model.Draft = DB.ProjectBudget.Where(w => w.VillageId == item.Key && w.StatusId == 12).Count().ToString();
                Model.WaitingForCheck = DB.ProjectBudget.Where(w => w.VillageId == item.Key && w.StatusId == 13).Count().ToString();
                Model.SendBackEdit = DB.ProjectBudget.Where(w => w.VillageId == item.Key && w.StatusId == 14).Count().ToString();
                Model.PendingApproval = DB.ProjectBudget.Where(w => w.VillageId == item.Key && w.StatusId == 15).Count().ToString();
                Model.Disapproved = DB.ProjectBudget.Where(w => w.VillageId == item.Key && w.StatusId == 16).Count().ToString();
                Model.Approve = DB.ProjectBudget.Where(w => w.VillageId == item.Key && w.StatusId == 17).Count().ToString();

                Models.Add(Model);
            }

            return PartialView("GetReportSummarizeProject", Models);
        }

        [HttpGet]
        public IActionResult ReportAll()
        {
            return View("ReportAll");
        }
        [HttpGet]
        public async Task<IActionResult> GetReportAll()
        {

            var Get = await DB.ProjectBudget.ToListAsync();

            ViewBag.Helper = Helper;
            ViewBag.Acctbudget = DB.AccountBudget.ToList();

            return PartialView("GetReportAll", Get);
        }

        #region WithHodingTax

        public IActionResult ReportWithHodingTax()
        {
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            var Village = DB.Village.ToList();
            var _Village = new List<SelectListItem>();
            foreach (var item in Village)
            {

                _Village.Add(new SelectListItem
                {
                    Text = item.VillageCodeText + "-" + item.VillageName,
                    Value = item.VillageId.ToString()
                });

            }

            ViewBag.Villages = new SelectList(_Village, "Value", "Text");

            return View("ReportWithHodingTax");
        }

        [HttpGet]
        public async Task<IActionResult> GetReportWithHodingTax()
        {

            var Get = await DB.ProjectBudget.ToListAsync();

            ViewBag.Helper = Helper;
            ViewBag.Acctbudget = DB.AccountBudget.ToList();

            return PartialView("GetReportWithHodingTax", Get);
        }

        #endregion


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


            //StringDate Parameter Can Use Only dd/mm/yyyy Date Formats

            return new DateTime(Convert.ToInt32(StringDate.Split("/")[2]), Convert.ToInt32(StringDate.Split("/")[1]), Convert.ToInt32(StringDate.Split("/")[0]));
        }

        public async void SendMail(string Email, string Village, string Project, string Message)
        {

            string Subject = "ระบบยื่นขอโครงการ : สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.)";
            string Body = "<div style='margin-bottom:10px'><h1><b>สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ</b><sup style='font-size:10px'>สทบ.</sup></h1></div>"
                   + "\n <br/><div style='background-color:#f26822 ;height:8px; margin-bottom:20px'>&nbsp;</div>"
                   + "\n <br/> เรียนกองทุนหมู่บ้าน " + Village + " "
                   + "\n <br/> เนื่องจากโครงการ '" + Project + "' " + Message + ""
                   + "\n <br/> "
                   + "\n <br/> - ที่ตั้งสำนักงานส่วนกลาง สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.)"
                   + "\n <br/> - เบอร์โทรศัพท์: 02-100-4209"
                   + "\n <br/> - เบอร์แฟกซ์: 02-100-4203";

            await Helper.SendMailWithSubject(Email, Subject, Body);
        }

        public async void SendManyMail(string Branch, string Project, string Message)
        {

            // current user, get user role
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            //Send Email to Province Admin
            var GetAdmin = DB.UserRoles.Where(w => w.RoleId == DB.Roles.Where(w => w.Name == "ProvinceAdmin").Select(s => s.Id).FirstOrDefault()).Select(s => s.UserId).ToArray();
            var GetEmails = DB.Users.Where(w => GetAdmin.Contains(w.Id) && w.OrgId == CurrentUser.OrgId).Select(s => s.Email).ToArray();

            string Subject = "ระบบยื่นขอโครงการ : สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.)";
            string Body = "<div style='margin-bottom:10px'><h1><b>สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ</b><sup style='font-size:10px'>สทบ.</sup></h1></div>"
                   + "\n <br/><div style='background-color:#f26822 ;height:8px; margin-bottom:20px'>&nbsp;</div>"
                   + "\n <br/> เรียนเจ้าหน้าสาขา " + Branch + " "
                   + "\n <br/> เนื่องจากโครงการ '" + Project + "' " + Message + ""
                   + "\n <br/> "
                   + "\n <br/> - ที่ตั้งสำนักงานส่วนกลาง สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.)"
                   + "\n <br/> - เบอร์โทรศัพท์: 02-100-4209"
                   + "\n <br/> - เบอร์แฟกซ์: 02-100-4203";

            await Helper.SendMailMultipleRecipients(GetEmails, Subject, Body);
        }

        #endregion

        #region StructureCenter

        [HttpGet]
        public async Task<IActionResult> StructureCenterIndex()
        {

            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return View("StructureCenterIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetStructuresCenter(int BudgetYear)
        {

            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            //Master
            var GetSysLookup = DB.SystemLookupMaster.ToList();

            /* get data account budget */
            var Models = new List<ViewModels.EAccount.ViewStructures>();
            var GetDatas = await DB.AccountBudgetCenter.Where(w => w.BudgetYear == BudgetYear).OrderBy(o => o.AccountBudgetCenterId).ToListAsync();
            foreach (var Get in GetDatas)
            {

                Models.Add(new ViewModels.EAccount.ViewStructures()
                {
                    Amount = Get.Amount,
                    ProjectCode = Get.AccCode,
                    ProjectName = Get.AccName,
                    StartEndDate = Helper.getDateThai(Get.AccStart) + " ถึง " + Helper.getDateThai(Get.AccEndDate),
                    Status = Get.IsActive,
                    SubAmount = Get.SubAmount,
                    AccountBudgetd = Get.AccountBudgetCenterId,
                    ParentId = Get.ParentId,
                    LookupValueDivision = GetSysLookup.Where(w => w.LookupGroupId == 1 && w.LookupValue == Get.LookupValueDivision).Select(s => s.LookupText).FirstOrDefault(),
                    LookupValueDepartment = GetSysLookup.Where(w => w.LookupGroupId == 2 && w.LookupValue == Get.LookupValueDepartment).Select(s => s.LookupText).FirstOrDefault()
                });
            }

            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            return PartialView("GetStructuresCenter", Models);
        }

        [HttpGet]
        public IActionResult FormAddStructureCenter()
        {
            /* get master data */
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();

            //กลุ่ม
            ViewBag.UserLookupValueDivision = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 1).ToList(), "LookupValue", "LookupText");
            //ฝ่าย
            ViewBag.UserLookupValueDepartment = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 2).ToList(), "LookupValue", "LookupText");

            return View("FormAddStructureCenter");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddStructureCenter(AccountBudgetCenter Model)
        {
            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // set data
                Model.UpdateBy = CurrentUser.Id;
                Model.UpdateDate = DateTime.Now;
                Model.ParentId = 0;
                Model.AccCode = Model.BudgetYear + "-" + (DB.AccountBudgetCenter.Where(w => w.ParentId == 0).Count() + 1).ToString().PadLeft(3, '0');
                Model.AccStart = DateTime.Now;
                Model.AccEndDate = DateTime.Now;
                Model.OpenDate = DateTime.Now;
                Model.CloseDate = DateTime.Now;
                Model.IsActive = true;
                DB.AccountBudgetCenter.Add(Model);
                await DB.SaveChangesAsync();
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult FormAddSubStructureCenter(Int64 AccountBudgetd)
        {
            ViewBag.ParentId = AccountBudgetd;

            return View("FormAddSubStructureCenter");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddSubStructureCenter(AccountBudgetViewModel Model, int StartDay, int StartMonth, int StartYear,
            int EndDay, int EndMonth, int EndYear,
            int OpenDay, int OpenMonth, int OpenYear,
            int CloseDay, int CloseMonth, int CloseYear,
            IFormFile[] DocumentFile)
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
                var GetData = await DB.AccountBudgetCenter.Where(w => w.AccountBudgetCenterId == Model.ParentId).FirstOrDefaultAsync();

                decimal AllAmount = DB.AccountBudgetCenter.Where(w => w.ParentId == Model.ParentId).Sum(s => s.SubAmount) + Model.SubAmount;

                decimal TotalSub = GetData.Amount - AllAmount;
                if (TotalSub < 0)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบงบประมาณ" });
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

                int CountParent = DB.AccountBudgetCenter.Where(w => w.ParentId == Model.ParentId).Count();

                // set data
                var ThisAccountBudget = new AccountBudgetCenter();
                ThisAccountBudget.AccName = Model.AccName;
                ThisAccountBudget.Amount = GetData.Amount;
                ThisAccountBudget.ParentId = Model.ParentId;
                ThisAccountBudget.UpdateBy = CurrentUser.Id;
                ThisAccountBudget.UpdateDate = DateTime.Now;
                ThisAccountBudget.BudgetYear = GetData.BudgetYear;
                ThisAccountBudget.AccCode = GetData.AccCode + "-" + (CountParent == 0 ? 1 : CountParent + 1).ToString().PadLeft(3, '0');
                ThisAccountBudget.IsActive = true;//Defalt Open Project
                ThisAccountBudget.AccStart = _StartDate;
                ThisAccountBudget.AccEndDate = _EndDate;
                ThisAccountBudget.OpenDate = __OpenDate;
                ThisAccountBudget.CloseDate = _CloseDate;
                ThisAccountBudget.SubAmount = Model.SubAmount;
                ThisAccountBudget.Qualification = Model.Qualification;
                ThisAccountBudget.IsApproveProvince = Model.IsApproveProvince;
                ThisAccountBudget.IsApproveBranch = Model.IsApproveBranch;
                ThisAccountBudget.IsApproveCenter = Model.IsApproveCenter;
                ThisAccountBudget.LookupValueDivision = GetData.LookupValueDivision;
                ThisAccountBudget.LookupValueDepartment = GetData.LookupValueDepartment;
                DB.AccountBudgetCenter.Add(ThisAccountBudget);
                await DB.SaveChangesAsync();

                // upload file
                if (DocumentFile.Count() > 0)
                {
                    foreach (var _File in DocumentFile)
                    {
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                        string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(_File.ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                        using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                        {
                            await _File.CopyToAsync(fileStream);
                        }

                        var NewFile = new ProjectBudgetDocumentCenter();
                        NewFile.ProjectId = ThisAccountBudget.AccountBudgetCenterId;
                        NewFile.ProjectPeriodId = 0;
                        NewFile.TransactionYear = ThisAccountBudget.BudgetYear;
                        NewFile.FileName = _File.FileName;
                        NewFile.GencodeFileName = UniqueFileName;
                        NewFile.FileType = 1;
                        NewFile.UpdateDate = DateTime.Now;
                        NewFile.UpdateBy = CurrentUser.Id;
                        DB.ProjectBudgetDocumentCenter.Add(NewFile);
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
        public async Task<IActionResult> FormEditStructureCenter(Int64 AccountBudgetd, string IsParent)
        {
            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

            // get data
            var GetData = await DB.AccountBudgetCenter.Where(w => w.AccountBudgetCenterId == AccountBudgetd).FirstOrDefaultAsync();
            ViewBag.CurrentBudgetYear = Helper.CurrentBudgetYear();
            ViewBag.IsParent = IsParent;

            //กลุ่ม
            ViewBag.UserLookupValueDivision = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 1).ToList(), "LookupValue", "LookupText");
            //ฝ่าย
            ViewBag.UserLookupValueDepartment = new SelectList(DB.SystemLookupMaster.Where(w => w.LookupGroupId == 2).ToList(), "LookupValue", "LookupText");

            ViewBag.Helper = Helper;

            return View("FormEditStructureCenter", GetData);
        }

        [HttpGet]
        public async Task<IActionResult> GetFileEditStructureCenter(int AccountBudgetd)
        {
            //get File
            //Issue 156
            var File = new List<ProjectBudgetDocumentCenter>();
            var HeadQuarterFile = await DB.ProjectBudgetDocumentCenter.Where(w => w.ProjectId == AccountBudgetd).ToListAsync();
            foreach (var item in HeadQuarterFile)
            {
                if (Helper.GetRoleUser(item.UpdateBy) == "HeadQuarterAdmin")
                {
                    File.Add(item);
                }
            }

            ViewBag.Helper = Helper;

            return PartialView("GetFileEditStructureCenter", File);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditStructureCenter(AccountBudgetCenter Model, string PARENT, int StartDay, int StartMonth, int StartYear, int EndDay, int EndMonth, int EndYear,
            int OpenDay, int OpenMonth, int OpenYear,
            int CloseDay, int CloseMonth, int CloseYear,
            IFormFile[] DocumentFile)
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
                var GetData = await DB.AccountBudgetCenter.Where(w => w.AccountBudgetCenterId == Model.AccountBudgetCenterId).FirstOrDefaultAsync();
                if (PARENT == "MAIN")
                {

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
                    GetData.LookupValueDivision = Model.LookupValueDivision;
                    GetData.LookupValueDepartment = Model.LookupValueDepartment;
                    DB.AccountBudgetCenter.Update(GetData);
                    await DB.SaveChangesAsync();

                    //Update Child
                    var GetChildData = DB.AccountBudgetCenter.Where(w => w.ParentId == Model.AccountBudgetCenterId).ToList();
                    if (GetChildData.Count() > 0)
                    {
                        foreach (var item in GetChildData)
                        {
                            item.Amount = GetData.Amount;
                            item.LookupValueDivision = GetData.LookupValueDivision;
                            item.LookupValueDepartment = GetData.LookupValueDepartment;
                        }

                        DB.UpdateRange(GetChildData);
                        DB.SaveChanges();

                    }

                    // update parent budget year
                    var SubAccounts = await DB.AccountBudgetCenter.Where(w => w.ParentId == Model.AccountBudgetCenterId).ToListAsync();
                    if (SubAccounts.Count() > 0)
                    {
                        foreach (var SubAccount in SubAccounts)
                        {
                            var UpdateSubAccount = await DB.AccountBudgetCenter.Where(w => w.AccountBudgetCenterId == SubAccount.AccountBudgetCenterId).FirstOrDefaultAsync();
                            UpdateSubAccount.BudgetYear = Model.BudgetYear;
                            UpdateSubAccount.IsActive = Model.IsActive;
                            UpdateSubAccount.UpdateBy = CurrentUser.Id;
                            UpdateSubAccount.UpdateDate = DateTime.Now;
                            DB.AccountBudgetCenter.Update(UpdateSubAccount);
                        }

                        await DB.SaveChangesAsync();
                    }
                }
                else
                {

                    decimal AllAmount = DB.AccountBudgetCenter.Where(w => w.ParentId == Model.ParentId && w.AccountBudgetCenterId != Model.AccountBudgetCenterId).Sum(s => s.SubAmount) + Model.SubAmount;

                    decimal TotalSub = GetData.Amount - AllAmount;
                    if (TotalSub < 0)
                    {
                        return Json(new { valid = false, message = "กรุณาตรวจสอบงบประมาณ" });
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

                    // update data
                    GetData.AccName = Model.AccName;
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
                    DB.AccountBudgetCenter.Update(GetData);
                    await DB.SaveChangesAsync();

                    // upload file
                    if (DocumentFile.Count() > 0)
                    {
                        foreach (var _File in DocumentFile)
                        {
                            var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                            string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(_File.ContentDisposition).FileName.Trim('"');
                            string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                            using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                            {
                                await _File.CopyToAsync(fileStream);
                            }

                            var NewFile = new ProjectBudgetDocumentCenter();
                            NewFile.ProjectId = GetData.AccountBudgetCenterId;
                            NewFile.ProjectPeriodId = 0;
                            NewFile.TransactionYear = GetData.BudgetYear;
                            NewFile.FileName = _File.FileName;
                            NewFile.GencodeFileName = UniqueFileName;
                            NewFile.FileType = 1;
                            NewFile.UpdateDate = DateTime.Now;
                            NewFile.UpdateBy = CurrentUser.Id;
                            DB.ProjectBudgetDocumentCenter.Add(NewFile);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditStructureCenterFileUpload(AccountBudgetCenter Model, IFormFile[] DocumentFile)
        {
            try
            {

                /* get  current user */
                var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));

                var GetData = await DB.AccountBudgetCenter.Where(w => w.AccountBudgetCenterId == Model.AccountBudgetCenterId).FirstOrDefaultAsync();

                // upload file
                if (DocumentFile.Count() > 0)
                {
                    foreach (var _File in DocumentFile)
                    {
                        var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                        string file = System.Net.Http.Headers.ContentDispositionHeaderValue.Parse(_File.ContentDisposition).FileName.Trim('"');
                        string UniqueFileName = string.Format(@"{0}", Guid.NewGuid()) + file.ToString();
                        using (var fileStream = new FileStream(Path.Combine(Uploads, UniqueFileName), FileMode.Create))
                        {
                            await _File.CopyToAsync(fileStream);
                        }

                        var NewFile = new ProjectBudgetDocumentCenter();
                        NewFile.ProjectId = GetData.AccountBudgetCenterId;
                        NewFile.ProjectPeriodId = 0;
                        NewFile.TransactionYear = GetData.BudgetYear;
                        NewFile.FileName = _File.FileName;
                        NewFile.GencodeFileName = UniqueFileName;
                        NewFile.FileType = 1;
                        NewFile.UpdateDate = DateTime.Now;
                        NewFile.UpdateBy = CurrentUser.Id;
                        DB.ProjectBudgetDocumentCenter.Add(NewFile);
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
        public async Task<IActionResult> DeleteAcctCenterFile(Int64 AccountBudgetd)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var Get = DB.ProjectBudgetDocumentCenter.Where(w => w.TransactionDocId == AccountBudgetd).FirstOrDefault();
                DB.ProjectBudgetDocumentCenter.Remove(Get);
                DB.SaveChanges();

                // delete file
                var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
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

        [HttpGet]
        public async Task<IActionResult> DeleteStructureCenter(Int64 AccountBudgetd)
        {
            /* get  current user */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                // check data 

                if (DB.AccountBudgetCenter.Where(w => w.ParentId == AccountBudgetd).Count() > 0)
                {
                    return Json(new { valid = false, message = "ไม่สามารถลบข้อมูลได้กรุณาตรวจสอบ" });
                }

                // delete data
                var GetData = await DB.AccountBudgetCenter.Where(w => w.AccountBudgetCenterId == AccountBudgetd).FirstOrDefaultAsync();
                if (GetData.DocumentFile != null)
                {
                    // delete old file
                    var Uploads = Path.Combine(_environment.ContentRootPath, "wwwroot/uploads/e-project/");
                    string oldfilepath = Path.Combine(Uploads, GetData.DocumentFile);
                    if (System.IO.File.Exists(oldfilepath))
                    {
                        System.IO.File.Delete(oldfilepath);
                    }
                }


                DB.AccountBudgetCenter.Remove(GetData);
                await DB.SaveChangesAsync();

            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public IActionResult UpdateStatusProjectCenter(Int64 AccountBudgetd)
        {
            try
            {
                var GetProject = DB.AccountBudgetCenter.Where(w => w.AccountBudgetCenterId == AccountBudgetd).FirstOrDefault();
                if (GetProject != null)
                {

                    //เช็คโครงการย่อยไม่ให้เปิดการใช้งานถ้าโครงการใหญ่ปิดอยู่
                    var GetParent = DB.AccountBudgetCenter.Where(w => w.AccountBudgetCenterId == GetProject.ParentId).FirstOrDefault();
                    if (GetParent != null && GetParent.IsActive == false && GetProject.IsActive == false)
                    {
                        return Json(new { valid = false, message = "สถานะโครงการใหญ่ปิดการใช้งานอยู่" });
                    }

                    // update main status
                    GetProject.IsActive = (GetProject.IsActive == false ? true : false);
                    DB.AccountBudgetCenter.Update(GetProject);
                    DB.SaveChanges();


                    // check parent
                    var GetProjectPrents = DB.AccountBudgetCenter.Where(w => w.ParentId == AccountBudgetd).ToList();
                    if (GetProjectPrents.Count > 0)
                    {
                        foreach (var GetProjectPrent in GetProjectPrents)
                        {
                            GetProjectPrent.IsActive = GetProject.IsActive;
                            DB.AccountBudgetCenter.UpdateRange(GetProjectPrents);
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

        //Import 
        [HttpGet]
        public async Task<IActionResult> ExportExcelStructureProject(int BudgetYear)
        {
            try
            {
                //Database
                var GetDatas = await DB.AccountBudgetCenter.Where(w => w.BudgetYear == BudgetYear).ToListAsync();

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
                                var ParentProject = new AccountBudgetCenter();

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

                                DB.AccountBudgetCenter.Add(ParentProject);
                                DB.SaveChanges();

                            }
                            else
                            {
                                var ChildProject = new AccountBudgetCenter();
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

                                DB.AccountBudgetCenter.Add(ChildProject);
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

        #endregion
    }
}
