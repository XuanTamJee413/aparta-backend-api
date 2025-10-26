using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;

namespace ApartaAPI.Repositories
{
    public class VisitorRepository : Repository<Visitor>, IVisitorRepository
    {
        public VisitorRepository(ApartaDbContext context) : base(context)
        {
        }
    }
}
