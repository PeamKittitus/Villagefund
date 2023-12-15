using DZ_VILLAGEFUND_WEB.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DZ_VILLAGEFUND_WEB.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        /*
         * system setting
         */

        public DbSet<SystemLookupMaster> SystemLookupMaster { get; set; }
        public DbSet<SystemMenus> SystemMenus { get; set; }
        public DbSet<SystemOrgStructures> SystemOrgStructures { get; set; }
        public DbSet<SystemRolemenus> SystemRolemenus { get; set; }
        public DbSet<SystemPermission> SystemPermission { get; set; }
        public DbSet<SystemLogs> SystemLogs { get; set; }
        public DbSet<Village> Village { get; set; }
        public DbSet<MemberVillage> MemberVillage { get; set; }
        public DbSet<TransactionNews> TransactionNews { get; set; }
        public DbSet<TransactionFileNews> TransactionFileNews { get; set; }
        public DbSet<TransactionReader> TransactionReader { get; set; }
        public DbSet<SystemDistrict> SystemDistrict { get; set; }
        public DbSet<SystemProvince> SystemProvince { get; set; }
        public DbSet<SystemSubDistrict> SystemSubDistrict { get; set; }
        public DbSet<SystemMemberStatus> SystemMemberStatus { get; set; }
        public DbSet<SystemMemberPosition> SystemMemberPosition { get; set; }
        public DbSet<SystemTransactionType> SystemTransactionType { get; set; }
        public DbSet<TransactionMemberVillage> TransactionMemberVillage { get; set; }
        public DbSet<TransactionReqVillage> TransactionReqVillage { get; set; }
        public DbSet<TransactionVillage> TransactionVillage { get; set; }
        public DbSet<TransactionFileVillage> TransactionFileVillage { get; set; }
        public DbSet<ProjectActivity> ProjectActivity { get; set; }
        public DbSet<SystemUserToken> SystemUserToken { get; set; }
        public DbSet<SystemOccupationMaster> SystemOccupationMaster { get; set; }

        /* accounting */
        public DbSet<AccountBankMaster> AccountBankMaster { get; set; }
        public DbSet<TransactionFileBudget> TransactionFileBudget { get; set; }
        public DbSet<TransacionAccountBudget> TransacionAccountBudget { get; set; }
        public DbSet<TransactionAccountActivity> TransactionAccountActivity { get; set; }
        public DbSet<TransactionAccountActivityCenter> TransactionAccountActivityCenter { get; set; }
        public DbSet<TransactionFileAccActivity> TransactionFileAccActivity { get; set; }
        public DbSet<TransactionFileAccActivityCenter> TransactionFileAccActivityCenter { get; set; }
        public DbSet<AccountBudget> AccountBudget { get; set; }
        public DbSet<AccountBookBank> AccountBookBank { get; set; }
        public DbSet<AccountActivity> AccountActivity { get; set; }
        public DbSet<TransactionFileBookbank> TransactionFileBookbank { get; set; }


        /* e project */
        public DbSet<ProjectAsset> ProjectAsset { get; set; }
        public DbSet<ProjectBudget> ProjectBudget { get; set; }
        public DbSet<ProjectPeriod> ProjectPeriod { get; set; }
        public DbSet<TransactionFileProject> TransactionFileProject { get; set; }
        public DbSet<ProjectBudgetDocument> ProjectBudgetDocument { get; set; }
        public DbSet<TransactionFilePeriod> TransactionFilePeriod { get; set; }
        public DbSet<ProjectRisk> ProjectRisk { get; set; }
        public DbSet<AccountBudgetCenter> AccountBudgetCenter { get; set; }
        public DbSet<ProjectBudgetDocumentCenter> ProjectBudgetDocumentCenter { get; set; }

        /* e office */
        public DbSet<EOfficeArchives> EOfficeArchives { get; set; }
        public DbSet<EOfficeArchivesAccesslevel> EOfficeArchivesAccesslevel { get; set; }
        public DbSet<EOfficeArchivesCommand> EOfficeArchivesCommand { get; set; }
        public DbSet<EOfficeArchivesExpedition> EOfficeArchivesExpedition { get; set; }
        public DbSet<EOfficeArchivesFile> EOfficeArchivesFile { get; set; }
        public DbSet<EOfficeArchivesRoutingTransaction> EOfficeArchivesRoutingTransaction { get; set; }
        public DbSet<EOfficeArchivesStatus> EOfficeArchivesStatus { get; set; }
        public DbSet<EOfficeArchivesType> EOfficeArchivesType { get; set; }
        public DbSet<EOfficeOrganizations> EOfficeOrganizations { get; set; }
        public DbSet<EOfficeOrgUsers> EOfficeOrgUsers { get; set; }

    }
}
