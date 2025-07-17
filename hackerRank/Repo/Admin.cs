using HackerRank.Repo.Contract;
using HackerRank.Models;    
namespace HackerRank.Repo
{
    public class Admin : IAdmin
    {
        private readonly AppDbContext _context;
        public Admin(AppDbContext context)
        {
            _context = context;
        }

        public void AddAdmin(Admin admin)
        {
            if (admin == null)
                throw new ArgumentNullException(nameof(admin));

            _context.Add(admin);
        }

        public void DeleteAdminById(int adminId)
        {
            
            //var ad = _context.AdminModel.
            //var admin = _context.AdminModel.FirstOrDefault(a => a.Id == adminId);
            //if (admin != null)
            //{
            //    _context.Remove(admin);
            //}
            //else
            //{
            //    throw new KeyNotFoundException($"Admin with Id {adminId} not found.");
            //}
        }
    }
}
