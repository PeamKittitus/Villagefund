using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DZ_VILLAGEFUND_WEB.Data;
using DZ_VILLAGEFUND_WEB.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DZ_VILLAGEFUND_WEB.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LogoutModel> _logger;
        private ApplicationDbContext DB;
        public DZ_VILLAGEFUND_WEB.Helpers.Utility Helper;
        private readonly IConfiguration _configuration;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger, ApplicationDbContext db,UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            DB = db;
            _configuration = configuration;
            Helper = new DZ_VILLAGEFUND_WEB.Helpers.Utility(DB, _configuration);
        }

        public async Task<IActionResult> OnGet(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            // log
            var CurrentUser = await _userManager.FindByIdAsync(_userManager.GetUserId(User));
            Helper.AddUsedLog(CurrentUser.Id, "ออกจากระบบ", HttpContext, "ระบบ Authentication");

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                return RedirectToPage();
            }
        }
    }
}
