using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Repositories
{
    public class InteractionRepository : Repository<Interaction>, IInteractionRepository
    {
        // Gọi base constructor để khởi tạo _context và _dbSet
        public InteractionRepository(ApartaDbContext context) : base(context)
        {
        }

        // --- Các phương thức chuyên biệt được giữ lại và triển khai ---

        public async Task<Interaction?> GetInteractionByParticipantsAsync(string residentId, string staffId)
        {
            return await _dbSet // Sử dụng _dbSet kế thừa từ Repository<T>
                .FirstOrDefaultAsync(i =>
                    (i.ResidentId == residentId && i.StaffId == staffId) ||
                    (i.ResidentId == staffId && i.StaffId == residentId));
        }

        public async Task<IEnumerable<Interaction>> GetUserInteractionsWithMessagesAsync(string userId)
        {
            return await _dbSet
                .Where(i => i.ResidentId == userId || i.StaffId == userId)
                // Phải sử dụng _context.Interactions để thực hiện Include (Load Navigation Properties)
                // hoặc sử dụng .Include(..) trên _dbSet nếu đã cấu hình đúng
                .Include(i => i.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Include(i => i.Resident)
                .Include(i => i.Staff)
                .OrderByDescending(i => i.UpdatedAt)
                .ToListAsync();
        }

        // Phương thức cơ bản: AddAsync, GetByIdAsync(string), UpdateAsync, v.v. đã được kế thừa!
    }
}
