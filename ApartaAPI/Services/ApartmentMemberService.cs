using AutoMapper;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using ApartaAPI.DTOs.ApartmentMembers;

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

        public async Task<IEnumerable<ApartmentMemberDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ApartmentMemberDto>>(entities);
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