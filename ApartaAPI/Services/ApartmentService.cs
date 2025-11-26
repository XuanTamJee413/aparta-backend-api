using ApartaAPI.DTOs.Apartments;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System.Linq.Expressions;

namespace ApartaAPI.Services
{
    public class ApartmentService : IApartmentService
    {
        private readonly IRepository<Apartment> _repository;
        private readonly IRepository<Building> _buildingRepository;
        private readonly IMapper _mapper;

        public ApartmentService(
            IRepository<Apartment> repository,
            IRepository<Building> buildingRepository,
            IMapper mapper)
        {
            _repository = repository;
            _buildingRepository = buildingRepository;
            _mapper = mapper;
        }


        private async Task<Building> GetBuildingAsync(string buildingId)
        {
            var building = await _buildingRepository.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
            if (building == null)
                throw new InvalidOperationException("Không tìm thấy tòa nhà.");

            if (building.TotalFloors <= 0)
                throw new InvalidOperationException("Số tầng của tòa nhà không hợp lệ.");

            if (string.IsNullOrWhiteSpace(building.BuildingCode))
                throw new InvalidOperationException("Tòa nhà chưa có mã.");

            return building;
        }

        private static string NormalizeBuildingCode(string buildingCode)
        {
            return buildingCode.Trim().ToUpperInvariant();
        }

        private static void EnsureFloorInRange(int floor, Building building)
        {
            if (floor < 1 || floor > building.TotalFloors)
            {
                var code = NormalizeBuildingCode(building.BuildingCode);
                throw new InvalidOperationException(
                    $"Tầng {floor} không hợp lệ. Tòa nhà {code} chỉ có từ tầng 1 đến tầng {building.TotalFloors}."
                );
            }
        }

        private static string GenerateApartmentCode(string buildingCode, int floor, int roomIndex)
        {
            if (floor <= 0) throw new ArgumentOutOfRangeException(nameof(floor));
            if (roomIndex <= 0) throw new ArgumentOutOfRangeException(nameof(roomIndex));

            var normalizedCode = NormalizeBuildingCode(buildingCode);
            return $"{normalizedCode}-{floor}{roomIndex:D2}";
        }


        public async Task<ApiResponse<IEnumerable<ApartmentDto>>> GetAllAsync(ApartmentQueryParameters query)
        {
            if (query == null)
            {
                query = new ApartmentQueryParameters(null, null, null, null, null);
            }

            var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim().ToLowerInvariant();

            var statusFilter = string.IsNullOrWhiteSpace(query.Status)
                ? null
                : query.Status.Trim().ToLowerInvariant();

            var buildingIdFilter = string.IsNullOrWhiteSpace(query.BuildingId)
                ? null
                : query.BuildingId.Trim();

            Expression<Func<Apartment, bool>> predicate = a =>
                (buildingIdFilter == null || a.BuildingId == buildingIdFilter) &&
                (statusFilter == null || (a.Status != null && a.Status.ToLower() == statusFilter)) &&
                (searchTerm == null ||
                    (a.Code != null && a.Code.ToLower().Contains(searchTerm)) ||
                    (a.Type != null && a.Type.ToLower().Contains(searchTerm))
                );

            var entities = await _repository.FindAsync(predicate);

            if (entities == null)
            {
                entities = new List<Apartment>();
            }

            var validEntities = entities.Where(e => e != null).ToList();

            IOrderedEnumerable<Apartment> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "code":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(a => a.Code)
                        : validEntities.OrderBy(a => a.Code);
                    break;

                case "area":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(a => a.Area)
                        : validEntities.OrderBy(a => a.Area);
                    break;

                case "floor":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(a => a.Floor)
                        : validEntities.OrderBy(a => a.Floor);
                    break;

                default:
                    sortedEntities = validEntities.OrderByDescending(a => a.CreatedAt);
                    break;
            }

            var dtos = _mapper.Map<IEnumerable<ApartmentDto>>(sortedEntities);

            if (!dtos.Any())
            {
                return ApiResponse<IEnumerable<ApartmentDto>>.Success(new List<ApartmentDto>(), "SM01");
            }

            return ApiResponse<IEnumerable<ApartmentDto>>.Success(dtos);
        }

        public async Task<ApartmentDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.ApartmentId == id);
            return _mapper.Map<ApartmentDto?>(entity);
        }

        
        public async Task<ApartmentDto> CreateAsync(ApartmentCreateDto dto)
        {
            if (dto.Floor is null || dto.Floor <= 0)
                throw new InvalidOperationException("Tầng phải lớn hơn 0.");

            if (string.IsNullOrWhiteSpace(dto.BuildingId))
                throw new InvalidOperationException("Tòa nhà không hợp lệ.");

            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new InvalidOperationException("Mã căn hộ là bắt buộc.");

            var buildingId = dto.BuildingId.Trim();
            var building = await GetBuildingAsync(buildingId);
            var buildingCode = NormalizeBuildingCode(building.BuildingCode);
            var floor = dto.Floor.Value;

            EnsureFloorInRange(floor, building);

            var rawCode = dto.Code.Trim().ToUpperInvariant();
            var prefix = $"{buildingCode}-{floor}";

            if (!rawCode.StartsWith(prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Mã căn hộ phải có dạng {buildingCode}-{floor}xx. " +
                    $"Ví dụ: {buildingCode}-{floor}01, {buildingCode}-{floor}02..."
                );
            }

            var existingApartmentCode = await _repository.FirstOrDefaultAsync(
                 a => a.Code == rawCode && a.BuildingId == buildingId
             );

            if (existingApartmentCode != null)
            {
                throw new InvalidOperationException($"Mã căn hộ '{rawCode}' đã tồn tại trong tòa nhà này.");
            }

            var now = DateTime.UtcNow;
            var entity = _mapper.Map<Apartment>(dto);

            entity.ApartmentId = Guid.NewGuid().ToString("N");
            entity.BuildingId = buildingId;
            entity.Code = rawCode;
            entity.Floor = floor;
            entity.Status = "Chưa Thuê";
            entity.CreatedAt = now;
            entity.UpdatedAt = now;

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<ApartmentDto>(entity);
        }

       
        public async Task<IEnumerable<ApartmentDto>> CreateBulkAsync(ApartmentBulkCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.BuildingId))
                throw new InvalidOperationException("Tòa nhà không hợp lệ.");

            if (dto.Rooms == null || dto.Rooms.Count == 0)
                throw new InvalidOperationException("Cần cấu hình ít nhất một phòng.");

            var buildingId = dto.BuildingId.Trim();
            var building = await GetBuildingAsync(buildingId);
            var buildingCode = NormalizeBuildingCode(building.BuildingCode);

            if (dto.StartFloor <= 0 || dto.EndFloor < dto.StartFloor)
                throw new InvalidOperationException("Khoảng tầng không hợp lệ.");

            if (dto.StartFloor < 1 || dto.EndFloor > building.TotalFloors)
            {
                throw new InvalidOperationException(
                    $"Khoảng tầng không hợp lệ. Tòa nhà {buildingCode} chỉ có từ tầng 1 đến tầng {building.TotalFloors}."
                );
            }

            var now = DateTime.UtcNow;

            var toCreate = new List<(int Floor, int RoomIndex, string Code, string? Type, double? Area)>();

            foreach (var floor in Enumerable.Range(dto.StartFloor, dto.EndFloor - dto.StartFloor + 1))
            {
                EnsureFloorInRange(floor, building);

                foreach (var room in dto.Rooms)
                {
                    if (room.RoomIndex <= 0) continue;

                    var code = GenerateApartmentCode(buildingCode, floor, room.RoomIndex);
                    toCreate.Add((floor, room.RoomIndex, code, room.Type, room.Area));
                }
            }

            var codeSet = toCreate.Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existing = await _repository.FindAsync(a =>
                a.BuildingId == buildingId && a.Code != null && codeSet.Contains(a.Code));

            if (existing.Any())
            {
                var duplicatedCodes = existing
                    .Where(e => e.Code != null)
                    .Select(e => e.Code!)
                    .Distinct();

                throw new InvalidOperationException(
                    "Các mã căn hộ sau đã tồn tại trong tòa nhà này: " +
                    string.Join(", ", duplicatedCodes)
                );
            }

            var createdEntities = new List<Apartment>();

            foreach (var item in toCreate)
            {
                var entity = new Apartment
                {
                    ApartmentId = Guid.NewGuid().ToString("N"),
                    BuildingId = buildingId,
                    Code = item.Code,
                    Type = item.Type,
                    Status = "Chưa Thuê",
                    Area = item.Area,
                    Floor = item.Floor,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _repository.AddAsync(entity);
                createdEntities.Add(entity);
            }

            await _repository.SaveChangesAsync();

            return _mapper.Map<IEnumerable<ApartmentDto>>(createdEntities);
        }

        public async Task<bool> UpdateAsync(string id, ApartmentUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.ApartmentId == id);
            if (entity == null) return false;

            if ("Đã Thuê".Equals(entity.Status, StringComparison.OrdinalIgnoreCase) ||
                "Đã Trả Phòng".Equals(entity.Status, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Không thể cập nhật căn hộ vì trạng thái hiện tại không cho phép.");
            }

            var building = await GetBuildingAsync(entity.BuildingId);
            var buildingCode = NormalizeBuildingCode(building.BuildingCode);

            if (dto.Floor.HasValue)
            {
                EnsureFloorInRange(dto.Floor.Value, building);
                entity.Floor = dto.Floor.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var newCode = dto.Code.Trim().ToUpperInvariant();
                var floorForCode = dto.Floor ?? entity.Floor;

                if (floorForCode.HasValue)
                {
                    var prefix = $"{buildingCode}-{floorForCode.Value}";
                    if (!newCode.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Mã căn hộ phải có dạng {buildingCode}-{floorForCode.Value}xx."
                        );
                    }
                }

                var existing = await _repository.FirstOrDefaultAsync(
                    a => a.Code == newCode &&
                         a.BuildingId == entity.BuildingId &&
                         a.ApartmentId != id
                );

                if (existing != null)
                {
                    throw new InvalidOperationException($"Mã căn hộ '{newCode}' đã tồn tại trong tòa nhà này.");
                }

                entity.Code = newCode;
            }

            if (!string.IsNullOrWhiteSpace(dto.Type))
            {
                entity.Type = dto.Type;
            }

            if (dto.Area.HasValue)
            {
                entity.Area = dto.Area.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                entity.Status = dto.Status;
            }

            if (dto.Floor.HasValue && dto.Floor.Value != entity.Floor)
            {
            }

            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(a => a.ApartmentId == id);
            if (entity == null) return false;

            try
            {
                await _repository.RemoveAsync(entity);
                return await _repository.SaveChangesAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
