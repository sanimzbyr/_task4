
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public class BulkActionRequest
{
    public string? actionType { get; set; }
    public List<int>? ids { get; set; }
}

[CheckUserStatus]
public class UserManagementController : Controller
{
    private readonly AppDbContext _db;

    public UserManagementController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(string filter = "")
    {
        var users = _db.Users
            .Where(u => u.Status != UserStatus.Deleted)
            .OrderByDescending(u => u.LastLoginTime)
            .ToList();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var q = filter.ToLower();
            users = users.Where(u => (u.Name ?? "").ToLower().Contains(q) || (u.Email ?? "").ToLower().Contains(q)).ToList();
        }

        return View(users);
    }

    // AJAX bulk endpoint
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction([FromBody] BulkActionRequest req)
    {
        if (req == null || req.ids == null || req.ids.Count == 0)
            return Json(new { success = false, message = "No items selected." });

        var users = _db.Users.Where(u => req.ids.Contains(u.Id)).ToList();
        if (users.Count == 0) return Json(new { success = false, message = "No users found." });

        switch (req.actionType)
        {
            case "Block":
                foreach (var u in users) u.Status = UserStatus.Blocked;
                break;
            case "Unblock":
                foreach (var u in users) u.Status = UserStatus.Active;
                break;
            case "Delete":
                foreach (var u in users) u.Status = UserStatus.Deleted;
                break;
            default:
                return Json(new { success = false, message = "Unknown action." });
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true, message = $"{req.actionType} completed." });
    }

    // Keep legacy actions (optional; used by non-AJAX forms)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockUsers(List<int> userIds)
    {
        var users = _db.Users.Where(u => userIds.Contains(u.Id)).ToList();
        foreach (var user in users) user.Status = UserStatus.Blocked;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Users blocked successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnblockUsers(List<int> userIds)
    {
        var users = _db.Users.Where(u => userIds.Contains(u.Id)).ToList();
        foreach (var user in users) user.Status = UserStatus.Active;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Users unblocked successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUsers(List<int> userIds)
    {
        var users = _db.Users.Where(u => userIds.Contains(u.Id)).ToList();
        foreach (var user in users) user.Status = UserStatus.Deleted;
        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = "Users deleted successfully.";
        return RedirectToAction("Index");
    }
}
