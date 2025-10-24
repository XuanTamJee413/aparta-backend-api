using ApartaAPI.DTOs.Assets;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;

namespace ApartaAPI.Services
{
    public class AssetService : IAssetService
    {
        private readonly IRepository<Asset> _repository;
        private readonly IMapper _mapper;

        public AssetService(IRepository<Asset> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<AssetDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<AssetDto>>(entities);
        }

        public async Task<AssetDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.AssetId == id);
            return _mapper.Map<AssetDto?>(entity);
        }

        public async Task<AssetDto> CreateAsync(AssetCreateDto dto)
        {
            var now = DateTime.UtcNow;
            var entity = _mapper.Map<Asset>(dto);

            entity.AssetId = Guid.NewGuid().ToString("N");
            entity.CreatedAt = now;
            entity.UpdatedAt = now;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<AssetDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, AssetUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.AssetId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.AssetId == id);
            if (entity == null) return false;

            await _repository.RemoveAsync(entity);
            return await _repository.SaveChangesAsync();
        }
    }
}
