using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DZ_VILLAGEFUND_WEB.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmAccount : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LogoutModel> _logger;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        public ConfirmAccount(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger, ApplicationDbContext db, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
            [RegularExpression("^[0-9]*$", ErrorMessage = "กรุณากรอกเฉพาะตัวเลข")]
            public string OTP { get; set; }
            public string UserId { get; set; }
        }

        public IActionResult OnGetAsync(string Id = null, string OTP = null)
        {
            ViewData["SuccessMessage"] = "";
            ViewData["OTP"] = OTP;
            ViewData["UserId"] = Id;
            return Page();

        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // get user
            var FileUser = await DB.Users.FindAsync(Input.UserId);
            if (ModelState.IsValid)
            {
                if (FileUser != null)
                {
                    if (DateTime.Now > FileUser.OTPExpiryDate)
                    {
                        ViewData["SuccessMessage"] = "unsuccess";
                        ViewData["Message"] = "ลิงก์ของท่านหมดอายุ";
                        ViewData["UserId"] = FileUser.Id;
                        return Page();
                    }

                    if (FileUser.OTP != Input.OTP)
                    {
                        ViewData["SuccessMessage"] = "unsuccess";
                        ViewData["Message"] = "รหัส OTP ของท่านไม่ถูกต้อง";
                        ViewData["UserId"] = FileUser.Id;
                        return Page();
                    }

                    FileUser.Status = true;
                    FileUser.OTP = null;
                    FileUser.OTPExpiryDate = DateTime.Now.AddDays(-10);
                    DB.Users.Update(FileUser);
                    await DB.SaveChangesAsync();
                }
            }
            else
            {
                ViewData["SuccessMessage"] = "unsuccess";
                ViewData["Message"] = "รหัส OTP ของท่านไม่ถูกต้อง";
                ViewData["UserId"] = FileUser.Id;
                return Page();
            }

            ViewData["SuccessMessage"] = "success";

            return Page();
        }
    }
}
