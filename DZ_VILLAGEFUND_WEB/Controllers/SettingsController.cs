using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using DZ_VILLAGEFUND_WEB.ViewModels.Setting;
using DZ_VILLAGEFUND_WEB.ViewModels.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Controllers
{
    [Authorize(Roles = "administrator")]
    public class SettingsController : Controller
    {
        private readonly ILogger<SettingsController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        public SettingsController(
             ILogger<SettingsController> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
             IConfiguration configuration,
            ApplicationDbContext db)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
        }

        [HttpGet]
        public async Task<IActionResult> Overview()
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            return View("Overview");
        }

        #region menu management

        [HttpGet]
        public IActionResult MenusIndex()
        {
            return View("MenusIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetMenus()
        {
            /*
             * get current user
             */

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var GetMenus = await DB.SystemMenus.ToListAsync();

            return PartialView("GetMenus", GetMenus);
        }

        [HttpGet]
        public IActionResult FormAddMenu()
        {
            return View("FormAddMenu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddMenu(SystemMenus Model)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                Model.ParentId = 0;
                Model.Position = 0;

                DB.SystemMenus.Add(Model);
                await DB.SaveChangesAsync();

                // log
                Helper.AddUsedLog(CurrentUser.Id, "สร้างเมนูใหม่", HttpContext, "ระบบจัดการเมนู");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePosition(string Position)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string msg = "";
            try
            {
                var ListMenus = await DB.SystemMenus.ToListAsync();

                List<ListMenusViewModel> UpdateParentId = JsonConvert.DeserializeObject<List<ListMenusViewModel>>(Position);
                // Lv1
                int NewPosition = 1;
                foreach (var NewParent in UpdateParentId)
                {
                    var MainOrg = DB.SystemMenus.Where(a => a.Id == NewParent.id).FirstOrDefault();
                    MainOrg.ParentId = 0;
                    MainOrg.Position = NewPosition;
                    DB.SystemMenus.Update(MainOrg);
                    DB.SaveChanges();

                    if (NewParent.children != null)
                    {
                        //Lv2
                        int SubPosition = 1;
                        foreach (var List in NewParent.children)
                        {
                            var SubOrg = ListMenus.Where(a => a.Id == List.id).FirstOrDefault();
                            SubOrg.ParentId = NewParent.id;
                            SubOrg.Position = SubPosition;
                            DB.SystemMenus.Update(SubOrg);
                            DB.SaveChanges();

                            if (List.children != null)
                            {
                                //Lv3
                                int SubPosition2 = 1;
                                foreach (var List2 in List.children)
                                {
                                    var SubOrg2 = ListMenus.Where(a => a.Id == List2.id).FirstOrDefault();
                                    SubOrg2.ParentId = List.id;
                                    SubOrg2.Position = SubPosition2;
                                    DB.SystemMenus.Update(SubOrg2);
                                    DB.SaveChanges();

                                    SubPosition2++;
                                }
                                //End Lv3
                            }
                            SubPosition++;
                        }
                        //End Lv2
                    }
                    NewPosition++;
                }
                msg = "บันทึกข้อมูลสำเร็จ";


                // log
                Helper.AddUsedLog(CurrentUser.Id, "แก้ไขตำแหน่งเมนูใหม่", HttpContext, "ระบบจัดการเมนู");
            }
            catch (Exception e)
            {
                msg = "Error is : " + e.InnerException;
                return Json(new { valid = false, message = msg });
            }

            return Json(new { valid = true, message = msg });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteMenu(int Id)
        {
            try
            {
                if (DB.SystemMenus.Where(w => w.ParentId == Id).Count() > 0)
                {
                    return Json(new { valid = false, message = "ไม่สามารถลบข้อมูลได้ กรุณาตรวจสอบ" });
                }

                var GetMenu = await DB.SystemMenus.Where(w => w.Id == Id).FirstOrDefaultAsync();
                DB.SystemMenus.Remove(GetMenu);
                await DB.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                return Json(new { valid = false, message = ex.Message });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });

        }

        [HttpGet]
        public async Task<IActionResult> FormEditMenu(int Id)
        {
            var GetMenu = await DB.SystemMenus.Where(w => w.Id == Id).FirstOrDefaultAsync();
            return View("FormEditMenu", GetMenu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditMenu(SystemMenus Model)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            try
            {
                var GetMenu = await DB.SystemMenus.Where(w => w.Id == Model.Id).FirstOrDefaultAsync();
                GetMenu.MenuName = Model.MenuName;
                GetMenu.ControllerName = Model.ControllerName;
                GetMenu.ActionName = Model.ActionName;
                GetMenu.Icon = Model.Icon;
                DB.SystemMenus.Update(GetMenu);
                await DB.SaveChangesAsync();

                // log
                Helper.AddUsedLog(CurrentUser.Id, "สร้างเมนูใหม่", HttpContext, "ระบบจัดการเมนู");
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SystemMenus()
        {
            /*
             * get current user
             * list menu
             */

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var UserRole = await DB.UserRoles.Where(a => a.UserId == CurrentUser.Id).FirstOrDefaultAsync();

            var Models = new List<SystemMenus>();
            var Gets = await DB.SystemRolemenus.Where(w => w.RoleId == UserRole.RoleId).ToListAsync();
            foreach (var Get in Gets)
            {
                var GetMenus = await DB.SystemMenus.Where(w => w.Id == Get.MenuId).ToListAsync();
                foreach (var GetMenu in GetMenus)
                {
                    var Model = new SystemMenus();
                    Model.Id = GetMenu.Id;
                    Model.MenuName = GetMenu.MenuName;
                    Model.ParentId = GetMenu.ParentId;
                    Model.Position = GetMenu.Position;
                    Model.ControllerName = GetMenu.ControllerName;
                    Model.ActionName = GetMenu.ActionName;
                    Model.Icon = GetMenu.Icon;
                    Model.IsLink = GetMenu.IsLink;
                    Model.PathLink = GetMenu.PathLink;
                    Models.Add(Model);
                }
            }

            return PartialView(Models);
        }

        #endregion

        #region role management

        [HttpGet]
        public IActionResult RoleIndex()
        {
            return View("RoleIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            /*
             * get current user
             * get role
             */

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            var Models = new List<ListRoleViewModel>();
            var GetRoles = await DB.Roles.ToListAsync();
            foreach (var GetRole in GetRoles)
            {
                var Model = new ListRoleViewModel();
                Model.Id = GetRole.Id;
                Model.Name = GetRole.Name;
                Model.NormalizedName = GetRole.NormalizedName;
                Model.Permisstion = DB.SystemPermission.Where(w => w.RoleId == GetRole.Id).ToList();
                Models.Add(Model);
            }

            return PartialView("GetRoles", Models);
        }

        [HttpGet]
        public IActionResult FormAddRole()
        {
            return View("FormAddRole");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddRole(ListRoleViewModel Model)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string Message = "";
            try
            {
                var Role = new ApplicationRole
                {
                    Name = Model.Name
                };

                var Result = await _roleManager.CreateAsync(Role);
                if (Result.Succeeded)
                {
                    // add new permission
                    var GetRole = await _roleManager.FindByNameAsync(Model.Name);
                    var SetPermission = new SystemPermission();
                    SetPermission.RoleId = GetRole.Id;
                    DB.SystemPermission.Add(SetPermission);
                    await DB.SaveChangesAsync();

                    // log
                    Helper.AddUsedLog(CurrentUser.Id, "สร้างสิทธิการเข้าถึง", HttpContext, "ระบบจัดสิทธิ์");

                    return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
                }
                else
                {
                    foreach (var Error in Result.Errors)
                    {
                        Message = Error.Description + "<br/>";
                    }
                    return Json(new { valid = false, message = Message });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> FormEditRole(string Id)
        {
            /*
             * get role 
             * model
             */

            var Model = new ListRoleViewModel();
            var GetRole = await DB.Roles.Where(w => w.Id == Id).FirstOrDefaultAsync();
            Model.Id = GetRole.Id;
            Model.Name = GetRole.Name;

            return View("FormEditRole", Model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditRole(ListRoleViewModel Model)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string Message = "";
            try
            {
                ApplicationRole thisRole = await _roleManager.FindByIdAsync(Model.Id);
                thisRole.Name = Model.Name;
                var Result = await _roleManager.UpdateAsync(thisRole);
                if (Result.Succeeded)
                {
                    // log
                    Helper.AddUsedLog(CurrentUser.Id, "แก้ไขสิทธิการเข้าถึง", HttpContext, "ระบบจัดสิทธิ์");
                    return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
                }
                else
                {
                    foreach (var Error in Result.Errors)
                    {
                        Message = Error.Description + "<br/>";
                    }
                    return Json(new { valid = false, message = Message });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteRole(string Id)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string Message = "";
            try
            {
                if (await DB.UserRoles.Where(w => w.RoleId == Id).CountAsync() > 0)
                {
                    // log
                    Helper.AddUsedLog(CurrentUser.Id, "ลบสิทธิการเข้าถึง", HttpContext, "ระบบจัดสิทธิ์");

                    return Json(new { valid = false, message = "ไม่สามารถลบข้อมูลนี้ได้ กรุณาตรวจสอบ" });
                }

                ApplicationRole ThisRole = await _roleManager.FindByIdAsync(Id);
                var Result = await _roleManager.DeleteAsync(ThisRole);
                if (Result.Succeeded)
                {
                    return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = false, message = Message });
        }

        [HttpGet]
        public async Task<IActionResult> SetPermission(bool Active, string RoleId, string IsAction)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string msg = "";
            try
            {
                var GetPermission = DB.SystemPermission.FirstOrDefault(f => f.RoleId == RoleId);
                GetPermission.IsInsert = (IsAction == "insert" ? Active : GetPermission.IsInsert);
                GetPermission.IsUpdate = (IsAction == "update" ? Active : GetPermission.IsUpdate);
                GetPermission.IsDelete = (IsAction == "delete" ? Active : GetPermission.IsDelete);
                DB.SystemPermission.Update(GetPermission);
                await DB.SaveChangesAsync();

                // log
                Helper.AddUsedLog(CurrentUser.Id, "กำหนดสิทธิการเข้าถึง", HttpContext, "ระบบจัดสิทธิ์");
            }
            catch (Exception e)
            {
                msg = "Error is : " + e.InnerException.Message;
                return Json(new { valid = false, message = msg });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }


        #endregion

        #region role menus

        [HttpGet]
        public IActionResult RoleMenuIndex()
        {
            /*
             * create dropdown list
             */

            ViewBag.Role = new SelectList(DB.Roles.OrderBy(f => f.Name).ToList(), "Id", "Name");
            return View("RoleMenuIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetRoleMenus(string RoleId)
        {
            /*
             * get current user
             * get get menu
             */

            var Models = new List<GetRoleMenuViewModel>();
            var Gets = await DB.SystemMenus.ToListAsync();
            foreach (var Get in Gets)
            {
                var Model = new GetRoleMenuViewModel();
                Model.MenuId = Get.Id;
                Model.MenuName = Get.MenuName;
                Model.ParentId = Get.ParentId;
                Model.SystemRolemenus = DB.SystemRolemenus.Where(w => w.MenuId == Get.Id).ToList();
                Models.Add(Model);
            }

            ViewBag.RoleId = RoleId;

            return PartialView("GetRoleMenus", Models);
        }

        [HttpPost]
        public async Task<IActionResult> AddRoleMenu(int[] MenuItemId, string RoleId)
        {
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string msg = "";
            try
            {
                if (MenuItemId != null)
                {
                    if (await DB.SystemRolemenus.Where(a => a.RoleId == RoleId).CountAsync() > 0)
                    {
                        var InModel = await DB.SystemRolemenus.Where(f => f.RoleId == RoleId).ToListAsync();
                        DB.SystemRolemenus.RemoveRange(InModel);
                        await DB.SaveChangesAsync();
                    }

                    foreach (var Data in MenuItemId)
                    {
                        var modelSave = new SystemRolemenus();
                        modelSave.MenuId = Data;
                        modelSave.RoleId = RoleId;
                        DB.SystemRolemenus.Add(modelSave);
                    }

                    await DB.SaveChangesAsync();

                    // log
                    Helper.AddUsedLog(CurrentUser.Id, "กำหนดสิทธิการมองเห็นเมนู", HttpContext, "ระบบจัดสิทธิ์");
                }
            }
            catch (Exception e)
            {
                msg = "Error is : " + e.InnerException;
                return Json(new { valid = false, message = msg });
            }
            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        #endregion

        #region user management

        [HttpGet]
        public async Task<IActionResult> UserIndex()
        {
            /*
             * current user
             * get permission
             */

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.Roles = new SelectList(DB.Roles.OrderBy(f => f.Name).ToList(), "Id", "Name");

            return View("UserIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(string RoleId)
        {
            /*
             * current user
             * get permission
             * get master data
             */

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            var Roles = await DB.Roles.ToListAsync();
            var GetUsers = await DB.Users.ToListAsync();

            // get user data 
            var Models = new List<ListUsersViewModel>();
            var Gets = await DB.UserRoles.Where(w => w.RoleId == RoleId).ToListAsync();
            foreach (var Get in Gets)
            {
                var Model = new ListUsersViewModel();
                Model.Id = Get.UserId;
                Model.RoleName = Roles.Where(w => w.Id == Get.RoleId).Select(s => s.Name).FirstOrDefault();
                Model.UserName = GetUsers.Where(w => w.Id == Get.UserId).Select(s => s.UserName).FirstOrDefault();
                Model.Status = GetUsers.Where(w => w.Id == Get.UserId).Select(s => s.Status).FirstOrDefault();
                Model.FullName = GetUsers.Where(w => w.Id == Get.UserId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Models.Add(Model);
            }

            return PartialView("GetUsers", Models);
        }

        [HttpGet]
        public async Task<IActionResult> FormAddUser()
        {
            /*
            * current user, get permission
            */

            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);
            ViewBag.Roles = new SelectList(DB.Roles.OrderBy(f => f.Name).ToList(), "Id", "Name");

            var ModelOrgs = new List<SelectListItem>();
            var GetOrgsLevel1 = DB.SystemOrgStructures.Where(w => w.ParentId == 0).OrderBy(d => d.OrgId).ToList();
            foreach (var Level1 in GetOrgsLevel1)
            {
                ModelOrgs.Add(new SelectListItem
                {
                    Value = Level1.OrgId.ToString(),
                    Text = Level1.OrgName
                });

                foreach (var Level2 in DB.SystemOrgStructures.Where(w => w.ParentId == Level1.OrgId).OrderBy(d => d.OrgId).ToList())
                {
                    ModelOrgs.Add(new SelectListItem
                    {
                        Value = Level2.OrgId.ToString(),
                        Text = SpaceString(4) + "- " + Level2.OrgName
                    });

                    foreach (var Level3 in DB.SystemOrgStructures.Where(w => w.ParentId == Level2.OrgId).OrderBy(d => d.OrgId).ToList())
                    {
                        ModelOrgs.Add(new SelectListItem
                        {
                            Value = Level3.OrgId.ToString(),
                            Text = SpaceString(8) + "- " + Level3.OrgName
                        });
                    }
                }
            }

            ViewBag.OrgId = ModelOrgs;


            return View("FormAddUser");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormAddUser(ApplicationUser Model, string Password, string Roles)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string Message = string.Empty;
            try
            {
                var GetEmail = await DB.Users.Where(w => w.Email == Model.Email).CountAsync();
                if (GetEmail > 0)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจสอบอีเมล" });
                }

                var GetPhone = await DB.Users.Where(w => w.PhoneNumber == Model.PhoneNumber).CountAsync();
                if (GetPhone > 0)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจเบอร์โทร" });
                }

                var GetIdCard = await DB.Users.Where(w => w.CitizenId == Model.CitizenId).CountAsync();
                if (GetIdCard > 0)
                {
                    return Json(new { valid = false, message = "กรุณาตรวจเลขบัตรประชำตัวประชาชน" });
                }

                var user = new ApplicationUser
                {
                    FirstName = Model.FirstName,
                    LastName = Model.LastName,
                    UserName = Model.UserName,
                    Email = Model.Email,
                    EmailConfirmed = true,
                    CitizenId = Model.CitizenId,
                    PhoneNumber = Model.PhoneNumber,
                    PhoneNumberConfirmed = true,
                    OrgId = Model.OrgId,
                    OTP = null,
                    OTPExpiryDate = DateTime.Now.AddDays(-10),
                    Status = true
                };

                var result = await _userManager.CreateAsync(user, Password);
                if (result.Succeeded)
                {
                    var currentUser = await _userManager.FindByEmailAsync(user.Email);
                    var GetAllRoles = await _roleManager.Roles.Where(w => w.Id == Roles).ToListAsync();
                    foreach (var GetAllRole in GetAllRoles)
                    {
                        var roleresult = await _userManager.AddToRoleAsync(currentUser, GetAllRole.NormalizedName);
                    }

                    // add log
                    Helper.AddUsedLog(CurrentUser.Id, "เพิ่มข้อมูลผู้ใช้งาน", HttpContext, "ระบบจัดการผู้ใช้งาน");
                }
                else
                {
                    foreach (var Error in result.Errors)
                    {
                        Message = Error.Description + "<br/>";
                    }
                    return Json(new { valid = false, message = Message });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> FormEditUser(string UserId)
        {
            /* current user, get permission */
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            ViewBag.Permission = Helper.GetUserPermission(CurrentUser.Id);

            var GetUser = await _userManager.FindByIdAsync(UserId);
            var GetUserRoleId = await DB.UserRoles.Where(w => w.UserId == UserId).Select(s => s.RoleId).FirstOrDefaultAsync();
            ViewBag.Roles = new SelectList(DB.Roles.OrderBy(f => f.Name).ToList(), "Id", "Name", GetUserRoleId);

            var ModelOrgs = new List<SelectListItem>();
            var GetOrgsLevel1 = DB.SystemOrgStructures.Where(w => w.ParentId == 0).OrderBy(d => d.OrgId).ToList();
            foreach (var Level1 in GetOrgsLevel1)
            {
                ModelOrgs.Add(new SelectListItem
                {
                    Value = Level1.OrgId.ToString(),
                    Text = Level1.OrgName,
                    Selected = (GetUser.OrgId == Level1.OrgId ? true : false)
                });

                foreach (var Level2 in DB.SystemOrgStructures.Where(w => w.ParentId == Level1.OrgId).OrderBy(d => d.OrgId).ToList())
                {
                    ModelOrgs.Add(new SelectListItem
                    {
                        Value = Level2.OrgId.ToString(),
                        Text = SpaceString(4) + "- " + Level2.OrgName,
                        Selected = (GetUser.OrgId == Level2.OrgId ? true : false)
                    });

                    foreach (var Level3 in DB.SystemOrgStructures.Where(w => w.ParentId == Level2.OrgId).OrderBy(d => d.OrgId).ToList())
                    {
                        ModelOrgs.Add(new SelectListItem
                        {
                            Value = Level3.OrgId.ToString(),
                            Text = SpaceString(8) + "- " + Level3.OrgName,
                            Selected = (GetUser.OrgId == Level3.OrgId ? true : false)
                        });
                    }
                }
            }

            ViewBag.OrgId = ModelOrgs;

            return View("FormEditUser", GetUser);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormEditUser(ApplicationUser Model, string Password, string Roles)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string Message = string.Empty;
            try
            {
                // set data user
                var ThisUser = await _userManager.FindByIdAsync(Model.Id);
                ThisUser.UserName = Model.UserName;
                ThisUser.FirstName = Model.FirstName;
                ThisUser.LastName = Model.LastName;
                ThisUser.Email = Model.Email;
                ThisUser.PhoneNumber = Model.PhoneNumber;
                ThisUser.CitizenId = Model.CitizenId;
                ThisUser.OrgId = Model.OrgId;

                // reset new password
                if (Password != null)
                {
                    var Code = await _userManager.GeneratePasswordResetTokenAsync(ThisUser);
                    var ChangePassword = await _userManager.ResetPasswordAsync(ThisUser, Code, Password);
                    if (!ChangePassword.Succeeded)
                    {
                        return Json(new { valid = false, message = ChangePassword.Errors.Select(s => s.Code + " " + s.Description).FirstOrDefault() });
                    }
                }

                var result = await _userManager.UpdateAsync(ThisUser);
                if (result.Succeeded)
                {
                    if (DB.UserRoles.Where(w => w.UserId == Model.Id).Count() > 0)
                    {
                        var RoleThisUser = DB.UserRoles.Where(w => w.UserId == Model.Id).FirstOrDefault();
                        DB.UserRoles.Remove(RoleThisUser);
                        DB.SaveChanges();

                        var GetAllRoles = await _roleManager.Roles.Where(w => w.Id == Roles).ToListAsync();
                        foreach (var GetAllRole in GetAllRoles)
                        {
                            var roleresult = await _userManager.AddToRoleAsync(ThisUser, GetAllRole.NormalizedName);
                        }
                    }

                    // add log
                    Helper.AddUsedLog(CurrentUser.Id, "แก้ไขข้อมูลผู้ใช้งาน", HttpContext, "ระบบจัดการผู้ใช้งาน");

                    Message = "บักทึกข้อมูลสำเร็จ";
                }
                else
                {
                    foreach (var Error in result.Errors)
                    {
                        Message = Error.Description + "<br/>";
                    }

                    return Json(new { valid = false, message = Message });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = "บันทึกข้อมูลสำเร็จ" });
        }

        [HttpGet]
        public async Task<IActionResult> DeleteUser(string UserId)
        {
            // current user
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            string Message = "";
            try
            {
                if (CurrentUser.Id == UserId)
                {
                    return Json(new { valid = false, message = "ไม่สามารถลบข้อมูลได้ กรุณาตรวจสอบ" });
                }

                var FileUser = await _userManager.FindByIdAsync(UserId);
                var Result = await _userManager.DeleteAsync(FileUser);
                if (Result.Succeeded)
                {
                    //Remove From Transaction Village 
                    var GetTransVillage = DB.TransactionReqVillage.Where(w => w.UserId == UserId).FirstOrDefault();
                    if (GetTransVillage != null)
                    {

                        //Remove TransactionMemberVillage Member
                        var GetTransMembers = DB.TransactionMemberVillage.Where(w => w.TransactionVillageId == GetTransVillage.TransactionVillageId).ToList();
                        if (GetTransMembers.Count() > 0)
                        {
                            DB.TransactionMemberVillage.RemoveRange(GetTransMembers);
                            DB.SaveChanges();
                        }

                        //Remove Transaction Village
                        var GetTransVillages = DB.TransactionVillage.Where(w => w.TransactionVillageId == GetTransVillage.TransactionVillageId).ToList();
                        if (GetTransVillages.Count() > 0)
                        {
                            DB.TransactionVillage.RemoveRange(GetTransVillages);
                            DB.SaveChanges();
                        }

                        DB.TransactionReqVillage.Remove(GetTransVillage);
                        DB.SaveChanges();
                    }

                    //Remove From Village
                    var GetVillage = DB.Village.Where(w => w.UserId == UserId).FirstOrDefault();
                    if (GetVillage != null)
                    {

                        //Remove Member
                        var GetMembers = DB.MemberVillage.Where(w => w.VillageId == GetVillage.VillageId).ToList();
                        if (GetMembers.Count() > 0)
                        {
                            DB.MemberVillage.RemoveRange(GetMembers);
                            DB.SaveChanges();
                        }

                        DB.Village.Remove(GetVillage);
                        DB.SaveChanges();
                    }

                    //Remove From TransMember
                    var GetTransMember = DB.TransactionMemberVillage.Where(w => w.UserId == UserId).FirstOrDefault();
                    if (GetTransMember != null)
                    {
                        DB.TransactionMemberVillage.Remove(GetTransMember);
                        DB.SaveChanges();
                    }

                    //Remove From Member
                    var GetMember = DB.MemberVillage.Where(w => w.UserId == UserId).FirstOrDefault();
                    if (GetMember != null)
                    {
                        DB.MemberVillage.Remove(GetMember);
                        DB.SaveChanges();
                    }

                    // add log
                    Helper.AddUsedLog(CurrentUser.Id, "ลบข้อมูลผู้ใช้งาน", HttpContext, "ระบบจัดการผู้ใช้งาน");
                    Message = "บันทึกข้อมูลสำเร็จ";
                }
                else
                {
                    foreach (var Error in Result.Errors)
                    {
                        Message = Error.Description + "<br/>";
                    }

                    return Json(new { valid = false, message = Message });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = Message });
        }

        [HttpGet]
        public async Task<IActionResult> TranslateData(string Keyword)
        {
            return Json(new { data = await Helper.TranslateThToEn(Keyword) });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResendEmail(string UserId)
        {
            string Message = "";
            try
            {
                var GetUser = DB.Users.Where(w => w.Id == UserId).FirstOrDefault();
                if (GetUser != null)
                {
                    if (GetUser.Status == true)
                    {
                        return Json(new { valid = false, message = "ชื่อผู้ใช่นี้ยืนยันตัวตนแล้ว" });
                    }

                    Random generator = new Random();
                    String SecurityCode = generator.Next(0, 999999).ToString("D6");

                    GetUser.OTPExpiryDate = DateTime.Now.AddMinutes(15);
                    GetUser.OTP = SecurityCode;
                    DB.Users.Update(GetUser);
                    DB.SaveChanges();

                    // send mail
                    SendMail(GetUser.Email, GetUser.Id, GetUser.OTP);

                    Message = "กรุณาตรวจสอบ E-mail ของท่าน OTP จะหมด อายุ " + Helper.getDateThaiAndTime(GetUser.OTPExpiryDate);
                }
                else
                {
                    return Json(new { valid = false, message = "ไม่พบข้อมูลในระบบ" });
                }
            }
            catch (Exception Error)
            {
                return Json(new { valid = false, message = Error.Message });
            }

            return Json(new { valid = true, message = Message });
        }

        public async void SendMail(string Email, string UserId, string SecurityCode)
        {
            try
            {
                var host = HttpContext.Request.Host.ToString();
                bool IsHttps = HttpContext.Request.IsHttps;

                string message = "<div style='margin-bottom:10px'><h1><b>สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ</b><sup style='font-size:10px'>สทบ.</sup></h1></div>"
                       + "\n <br/> " + "<div style='background-color:#f26822 ;height:8px; margin-bottom:20px'>&nbsp;</div>"
                       + "\n <br/> รหัส OTP ของท่านคือ " + SecurityCode + " ยืนยันการเข้าใช้งานระบบ" + " กรุณากด link เพื่อยืนยัน <a href='" + (IsHttps == true ? "https" : "http") + "://" + host + "/Identity/Account/ConfirmAccount?Id=" + UserId + "&OTP=" + SecurityCode + "'>ยืนยัน</a><br/>"
                       + "\n <br/> "
                       + "\n <br/> - ที่ตั้งสำนักงานส่วนกลาง สำนักงานกองทุนหมู่บ้านและชุมชนเมืองแห่งชาติ (สทบ.)"
                       + "\n <br/> - เบอร์โทรศัพท์: 02-100-4209"
                       + "\n <br/> - เบอร์แฟกซ์: 02-100-4203";

                await Helper.SendMail(Email, message);

            }
            catch (Exception Error)
            {

            }

        }

        #endregion

        #region used log

        [HttpGet]
        public IActionResult UsedIndex()
        {
            return View("UsedIndex");
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(int Month, int Year)
        {
            // get master data
            var Users = await DB.Users.ToListAsync();

            var Models = new List<UsedLogsViewModel>();
            var GetLogs = await DB.SystemLogs.Where(w => w.ActionDate.Month == Month && w.ActionDate.Year == Year).OrderByDescending(r => r.ActionDate).ToListAsync();
            foreach (var Get in GetLogs)
            {
                var Model = new UsedLogsViewModel();
                Model.Action = Get.Action;
                Model.ActionDate = Helper.getShortDateThaiAndTime(Get.ActionDate);
                Model.FullName = Users.Where(w => w.Id == Get.UserId).Select(s => s.FirstName + " " + s.LastName).FirstOrDefault();
                Model.IPAddress = Get.IPAddress;
                Model.SystemName = Get.SystemName;
                Models.Add(Model);
            }

            return PartialView("GetLogs", Models);
        }

        #endregion

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
    }
}
