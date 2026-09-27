using BLL.DTO;
using BLL.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;    
using System.Security.Claims;      
using System;      

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(IUserService userService, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Users()
    {
        var users = await _userService.GetAllUsersAsync();
        return View(users ?? new List<UserDTO>());
    }

    public async Task<IActionResult> ManageRoles(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest("Пользователь с ID не указан.");

        var user = await _userService.GetUserWithRolesAsync(userId);
        if (user == null) return NotFound($"Пользователь с ID '{userId}' не найден.");

        var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        ViewBag.AllRoles = allRoles ?? new List<string>();      

        return View(user);      
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRoles(string userId, List<string> roles)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest("Пользователь с ID не указан.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound($"Пользователь с ID '{userId}' не найден.");

        var currentRoles = await _userService.GetUserRolesAsync(userId);

        var rolesToRemove = currentRoles.Except(roles ?? new List<string>()).ToList();
        var rolesToAdd = (roles ?? new List<string>()).Except(currentRoles).ToList();

        try
        {
            if (rolesToRemove.Any())
            {
                bool isLastAdmin = await IsLastAdminAsync(user, rolesToRemove);
                if (isLastAdmin)
                {
                    TempData["ErrorMessage"] = "Нельзя удалить роль 'Admin' у единственного администратора.";
                    return RedirectToAction(nameof(ManageRoles), new { userId = userId });
                }

                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Ошибка при удалении ролей: " + string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(ManageRoles), new { userId = userId });
                }
            }

            if (rolesToAdd.Any())
            {
                foreach (var roleToAdd in rolesToAdd)
                {
                    if (!await _roleManager.RoleExistsAsync(roleToAdd))
                    {
                        TempData["ErrorMessage"] = $"Роль '{roleToAdd}' не существует.";
                        return RedirectToAction(nameof(ManageRoles), new { userId = userId });
                    }
                }
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Ошибка при добавлении ролей: " + string.Join(", ", addResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(ManageRoles), new { userId = userId });
                }
            }

            TempData["SuccessMessage"] = $"Роли пользователя '{user.Email}' успешно обновлены.";
            return RedirectToAction(nameof(Users));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Ошибка при обновлении ролей: {ex.Message}";
            return RedirectToAction(nameof(ManageRoles), new { userId = userId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest("User ID не указан.");

        var user = await _userService.GetUserWithRolesAsync(userId);
        if (user == null) return NotFound($"Пользователь с ID '{userId}' не найден.");

        return View(user);    
    }

    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return BadRequest("Пользователь с ID не указан.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["WarningMessage"] = "Пользователь уже удален.";
            return RedirectToAction(nameof(Users));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] = "Вы не можете удалить свою собственную учетную запись.";
            return RedirectToAction(nameof(Users));
        }

        bool isLastAdmin = await IsLastAdminAsync(user, new List<string> { "Admin" });        
        if (isLastAdmin)
        {
            TempData["ErrorMessage"] = "Нельзя удалить единственного администратора.";
            return RedirectToAction(nameof(Users));
        }


        try
        {
            await _userService.DeleteUserAsync(userId);    
            TempData["SuccessMessage"] = $"Пользователь '{user.Email}' успешно удален.";
            return RedirectToAction(nameof(Users));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Ошибка при удалении пользователя: {ex.Message}";
            return RedirectToAction(nameof(Users));
        }
    }

    private async Task<bool> IsLastAdminAsync(ApplicationUser user, IEnumerable<string> rolesToCheck)
    {
        if (rolesToCheck.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1 && admins.Any(a => a.Id == user.Id))
                {
                    return true;     
                }
            }
        }
        return false;            
    }
}