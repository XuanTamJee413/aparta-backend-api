﻿using ApartaAPI.Models;

namespace ApartaAPI.Repositories.Interfaces
{
    public interface IVisitLogRepository : IRepository<VisitLog>
    {
        IQueryable<VisitLog> GetStaffViewLogsQuery();
    }
}
