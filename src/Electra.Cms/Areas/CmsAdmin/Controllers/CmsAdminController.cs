using Electra.Cms.Areas.CmsAdmin.Models;
using Electra.Cms.Blocks;
using Electra.Cms.Models;
using Electra.Models.Entities;
using Electra.Core.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Electra.Cms.Areas.CmsAdmin.Controllers
{
    [Area("CmsAdmin")]
    [Route("cms/admin")]
    [Authorize(Roles = CmsRoles.Admin)]
    public class CmsAdminController : Controller
    {
        private readonly IAsyncDocumentSession _session;
        private readonly IBlockRegistry _blockRegistry;
        private readonly UserManager<ElectraUser> _userManager;
        private readonly SignInManager<ElectraUser> _signInManager;
        private readonly RoleManager<ElectraRole> _roleManager;

        public CmsAdminController(
            IAsyncDocumentSession session, 
            IBlockRegistry blockRegistry,
            UserManager<ElectraUser> userManager,
            SignInManager<ElectraUser> signInManager,
            RoleManager<ElectraRole> roleManager)
        {
            _session = session;
            _blockRegistry = blockRegistry;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [AllowAnonymous]
        [Route("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "User account locked out.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [Route("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index), "Home", new { area = "" });
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Index()
        {
            var sites = await _session.Query<SiteDocument>().ToListAsync();
            return View(sites);
        }

        // --- Pages Management ---

        [Route("site/{siteId}/pages")]
        public async Task<IActionResult> Pages(string siteId)
        {
            var site = await _session.LoadAsync<SiteDocument>(siteId);
            if (site == null) return NotFound();

            var pages = await _session.Query<PageDocument>()
                .Where(x => x.SiteId == siteId)
                .ToListAsync();

            return View(new PageListViewModel { Site = site, Pages = pages });
        }

        [Route("site/{siteId}/page/add")]
        public async Task<IActionResult> AddPage(string siteId)
        {
            var site = await _session.LoadAsync<SiteDocument>(siteId);
            if (site == null) return NotFound();

            var page = new PageDocument
            {
                Id = "pages/", // RavenDB will complete the ID
                SiteId = siteId,
                Template = "default",
                PublishedState = PagePublishedState.Draft,
                Metadata = new PageMetadata(),
                Blocks = new List<BlockDocument>()
            };

            return View("Edit", new PageEditViewModel
            {
                Page = page,
                Site = site,
                AvailableBlocks = _blockRegistry.GetAllBlocks()
            });
        }

        [Route("page/{pageId}/edit")]
        public async Task<IActionResult> Edit(string pageId)
        {
            var page = await _session.LoadAsync<PageDocument>(pageId);
            if (page == null) return NotFound();

            var site = await _session.LoadAsync<SiteDocument>(page.SiteId);
            
            return View(new PageEditViewModel 
            { 
                Page = page, 
                Site = site,
                AvailableBlocks = _blockRegistry.GetAllBlocks()
            });
        }

        [HttpPost]
        [Route("page/{pageId}/save")]
        public async Task<IActionResult> Save(string pageId, [FromForm] PageDocument model, [FromForm] string blocksRaw)
        {
            PageDocument page;
            List<BlockDocument> blocks = new List<BlockDocument>();
            
            if (!string.IsNullOrEmpty(blocksRaw))
            {
                try {
                    blocks = System.Text.Json.JsonSerializer.Deserialize<List<BlockDocument>>(blocksRaw) ?? new List<BlockDocument>();
                } catch {
                    ModelState.AddModelError("blocksRaw", "Invalid JSON format for blocks.");
                }
            }

            if (string.IsNullOrEmpty(pageId) || pageId == "new")
            {
                page = model;
                page.Blocks = blocks;
                page.LastModifiedUtc = System.DateTime.UtcNow;
                await _session.StoreAsync(page);
            }
            else
            {
                page = await _session.LoadAsync<PageDocument>(pageId);
                if (page == null) return NotFound();

                page.Metadata.Title = model.Metadata.Title;
                page.Metadata.SeoDescription = model.Metadata.SeoDescription;
                page.Slug = model.Slug;
                page.FullUrl = model.FullUrl;
                page.Blocks = blocks;
                page.PublishedState = model.PublishedState;
                page.Template = model.Template;
                page.Version++;
                page.LastModifiedUtc = System.DateTime.UtcNow;
            }

            await _session.SaveChangesAsync();

            return RedirectToAction("Pages", new { siteId = page.SiteId });
        }

        [HttpPost]
        [Route("page/{pageId}/delete")]
        public async Task<IActionResult> DeletePage(string pageId)
        {
            var page = await _session.LoadAsync<PageDocument>(pageId);
            if (page == null) return NotFound();
            
            string siteId = page.SiteId;
            _session.Delete(page);
            await _session.SaveChangesAsync();

            return RedirectToAction("Pages", new { siteId });
        }

        [Route("page/{pageId}/preview")]
        public async Task<IActionResult> Preview(string pageId)
        {
            var page = await _session.LoadAsync<PageDocument>(pageId);
            if (page == null) return NotFound();

            var site = await _session.LoadAsync<SiteDocument>(page.SiteId);
            
            return View(new PageEditViewModel { Page = page, Site = site });
        }

        // --- User Management ---

        [Route("users")]
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            return View(new UserListViewModel { Users = users });
        }

        [Route("user/{userId}/edit")]
        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.Select(r => r.Name).Where(n => n != null).Select(n => n!).ToList();

            return View(new UserEditViewModel
            {
                User = user,
                UserRoles = userRoles,
                AllRoles = allRoles
            });
        }

        [HttpPost]
        [Route("user/{userId}/save")]
        public async Task<IActionResult> SaveUser(string userId, [FromForm] ElectraUser model, [FromForm] string[] selectedRoles)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email; // Using email as username
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                var allRoles = _roleManager.Roles.Select(r => r.Name).Where(n => n != null).Select(n => n!).ToList();
                return View("EditUser", new UserEditViewModel { User = user, UserRoles = selectedRoles ?? new string[0], AllRoles = allRoles });
            }

            // Update Roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (selectedRoles != null)
            {
                await _userManager.AddToRolesAsync(user, selectedRoles);
            }

            return RedirectToAction("Users");
        }

        [HttpPost]
        [Route("user/{userId}/delete")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return RedirectToAction("Users");
        }
    }
}
