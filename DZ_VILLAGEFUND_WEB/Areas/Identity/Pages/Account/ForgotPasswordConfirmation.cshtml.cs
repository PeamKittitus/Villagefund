using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DZ_VILLAGEFUND_WEB.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordConfirmation : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ForgotPasswordConfirmation> _logger;
        private ApplicationDbContext DB;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        public ForgotPasswordConfirmation(SignInManager<ApplicationUser> signInManager,
            ILogger<ForgotPasswordConfirmation> logger,
            UserManager<ApplicationUser> userManager,
             RoleManager<ApplicationRole> roleManager,
             IConfiguration configuration,
             ApplicationDbContext db)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "ข้อมูลจำเป็น")]
            public string OTP { get; set; }

            [Required(ErrorMessage = "ข้อมูลจำเป็น")]
            [StringLength(30, MinimumLength = 6, ErrorMessage = "รหัสผ่านควรมีความยาวอย่างน้อย 6 ตัวอักษร")]
            [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@@#$%^&*-]).{6,}$", ErrorMessage = "รหัสผ่านควรมีความยาวอย่างน้อย 6 ตัวอักษร โดยมีตัวอักษรทั้งพิมพ์ใหญ่ พิมพ์เล็ก ตัวเลข และอักขระพิเศษผสมกัน")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Required(ErrorMessage = "ข้อมูลจำเป็น")]
            [StringLength(30, MinimumLength = 6, ErrorMessage = "รหัสผ่านควรมีความยาวอย่างน้อย 6 ตัวอักษร")]
            [RegularExpression("^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@@#$%^&*-]).{6,}$", ErrorMessage = "รหัสผ่านควรมีความยาวอย่างน้อย 6 ตัวอักษร โดยมีตัวอักษรทั้งพิมพ์ใหญ่ พิมพ์เล็ก ตัวเลข และอักขระพิเศษผสมกัน")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
            public string ConfirmPassword { get; set; }
            public string UserId { get; set; }
        }

        [TempData]
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync(string returnUrl = null, string Id = null)
        {
            var GetUser = await _userManager.FindByIdAsync(Id);
            if (GetUser != null)
            {
                ViewData["ExpiryDate"] = Helper.getDateThaiAndTime(GetUser.OTPExpiryDate);
            }

            ReturnUrl = returnUrl;
            ViewData["UserId"] = Id;

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;


        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            var FileUser = await _userManager.FindByIdAsync(Input.UserId);
            if (ModelState.IsValid)
            {
                if (FileUser != null)
                {
                    if (DateTime.Now > FileUser.OTPExpiryDate)
                    {
                        ModelState.AddModelError(string.Empty, "รหัส OTP หมดอายุ");
                        ViewData["ExpiryDate"] = Helper.getDateThaiAndTime(FileUser.OTPExpiryDate);
                        ViewData["UserId"] = FileUser.Id;
                        return Page();
                    }

                    if (FileUser.OTP.Trim() != Input.OTP.Trim())
                    {
                        ModelState.AddModelError(string.Empty, "รหัส OTP ไม่ถูกต้อง");
                        ViewData["ExpiryDate"] = Helper.getDateThaiAndTime(FileUser.OTPExpiryDate);
                        ViewData["UserId"] = FileUser.Id;
                        return Page();
                    }

                    // change password
                    var Code = await _userManager.GeneratePasswordResetTokenAsync(FileUser);
                    var ChangePassword = await _userManager.ResetPasswordAsync(FileUser, Code, Input.Password);
                    if (!ChangePassword.Succeeded)
                    {
                        ViewData["UserId"] = FileUser.Id;
                        ViewData["ExpiryDate"] = Helper.getDateThaiAndTime(FileUser.OTPExpiryDate);
                        ModelState.AddModelError(string.Empty, ChangePassword.Errors.Select(s => s.Code + " " + s.Description).FirstOrDefault());
                        return Page();
                    }

                    // update user
                    FileUser.OTP = null;
                    FileUser.OTPExpiryDate = DateTime.Now.AddDays(-2);
                    var IsUpdate = await _userManager.UpdateAsync(FileUser);
                    if (IsUpdate.Succeeded)
                    {
                        ViewData["Success"] = "เปลี่ยนรหัสผ่านสำเร็จ";
                    }
                }
            }

            ViewData["UserId"] = Input.UserId;
            ViewData["ExpiryDate"] = Helper.getDateThaiAndTime(FileUser.OTPExpiryDate);

            return Page();
        }
    }
}
