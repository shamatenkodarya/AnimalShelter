using DAL.Entities;

namespace DAL.Repositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser> GetUserWithRolesAsync(string userId);
        Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(string role);
        Task UpdateUserAsync(ApplicationUser user);
    }
}