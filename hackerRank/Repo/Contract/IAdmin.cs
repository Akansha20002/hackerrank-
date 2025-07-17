using HackerRank.Models;

namespace HackerRank.Repo.Contract
{
    public interface IAdmin
    {
        void AddAdmin(Admin admin);
        void DeleteAdminById(int adminId);
        

    }
}
