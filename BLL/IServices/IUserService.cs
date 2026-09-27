using BLL.DTO;
using DAL.Entities;

namespace BLL.IServices
{
    public interface IUserService
    {
        Task<UserDTO> GetUserWithRolesAsync(string userId);
        Task AssignRoleAsync(string userId, string role);
        Task<IEnumerable<UserDTO>> GetAllUsersAsync();
        Task RemoveRoleAsync(string userId, string role);
        Task DeleteUserAsync(string userId);
        Task<IList<string>> GetUserRolesAsync(string userId);
    }
}