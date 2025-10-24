﻿using AutoMapper;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;
using System.Linq.Expressions;

namespace ApartaAPI.Services
{
    public class ApartmentMemberService : IApartmentMemberService
    {
        private readonly IRepository<ApartmentMember> _repository;
        private readonly IMapper _mapper;

        public ApartmentMemberService(IRepository<ApartmentMember> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<ApartmentMemberDto>>> GetAllAsync(ApartmentMemberQueryParameters query)
        {
            

            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();


            Expression<Func<ApartmentMember, bool>> predicate = m =>
                (!query.IsOwned.HasValue || m.IsOwner == query.IsOwned.Value) &&

                (searchTerm == null ||
                    (m.Name != null && m.Name.ToLower().Contains(searchTerm)) ||
                    (m.PhoneNumber != null && m.PhoneNumber.ToLower().Contains(searchTerm)) ||
                    (m.IdNumber != null && m.IdNumber.ToLower().Contains(searchTerm)));
                   




            var entities = await _repository.FindAsync(predicate);

            

            var validEntities = entities.Where(m => m != null).ToList();

            IOrderedEnumerable<ApartmentMember> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "name":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(m => m.Name ?? string.Empty)
                        : validEntities.OrderBy(m => m.Name ?? string.Empty);
                    break;

                default:
                    sortedEntities = validEntities.OrderBy(m => m.Name ?? string.Empty);
                    break;
            }

            
            var dtos = _mapper.Map<IEnumerable<ApartmentMemberDto>>(sortedEntities);

            if (!dtos.Any())
            {
                return ApiResponse<IEnumerable<ApartmentMemberDto>>.Success(new List<ApartmentMemberDto>(), "SM01");
            }

            return ApiResponse<IEnumerable<ApartmentMemberDto>>.Success(dtos);
        }

        public async Task<ApartmentMemberDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(m => m.ApartmentMemberId == id);
            return _mapper.Map<ApartmentMemberDto?>(entity);
        }

        public async Task<ApartmentMemberDto> CreateAsync(ApartmentMemberCreateDto dto)
        {
            var now = DateTime.UtcNow;
            var entity = _mapper.Map<ApartmentMember>(dto);

            entity.ApartmentMemberId = Guid.NewGuid().ToString("N");

            entity.CreatedAt ??= now;
            entity.UpdatedAt = now;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<ApartmentMemberDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, ApartmentMemberUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(m => m.ApartmentMemberId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(m => m.ApartmentMemberId == id);
            if (entity == null) return false;

            await _repository.RemoveAsync(entity);
            return await _repository.SaveChangesAsync();
        }
    }
}