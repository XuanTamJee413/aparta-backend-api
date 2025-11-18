using ApartaAPI.DTOs.Common;
using ApartaAPI.DTOs.Contracts;
using ApartaAPI.Models; 
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using AutoMapper;
using System.Linq.Expressions;

namespace ApartaAPI.Services
{
    public class ContractService : IContractService
    {
        private readonly IRepository<Contract> _contractRepository;
        private readonly IRepository<Apartment> _apartmentRepository;
        private readonly IRepository<ApartmentMember> _apartmentMemberRepository; 
        private readonly IRepository<User> _userRepository;
        private readonly IMapper _mapper;

        public ContractService(
            IRepository<Contract> contractRepository,
            IRepository<Apartment> apartmentRepository,
            IRepository<ApartmentMember> apartmentMemberRepository, 
            IRepository<User> userRepository,
            IMapper mapper)
        {
            _contractRepository = contractRepository;
            _apartmentRepository = apartmentRepository;
            _apartmentMemberRepository = apartmentMemberRepository; 
            _userRepository = userRepository;
            _mapper = mapper;
        }


        public async Task<ApiResponse<IEnumerable<ContractDto>>> GetAllAsync(ContractQueryParameters query)
        {
            if (query == null)
            {
                query = new ContractQueryParameters(null, null, null);
            }

            var apartmentIdFilter = string.IsNullOrWhiteSpace(query.ApartmentId)
                ? null
                : query.ApartmentId.Trim();

            Expression<Func<Contract, bool>> predicate = c =>
                (apartmentIdFilter == null || c.ApartmentId == apartmentIdFilter);

            var entities = await _contractRepository.FindAsync(predicate);

            if (entities == null)
            {
                entities = new List<Contract>();
            }

            var validEntities = entities.Where(e => e != null).ToList();

            if (!validEntities.Any())
            {
                return ApiResponse<IEnumerable<ContractDto>>.Success(
                    new List<ContractDto>(),
                    "SM01"
                );
            }

            IOrderedEnumerable<Contract> sortedEntities;
            bool isDescending = query.SortOrder?.ToLowerInvariant() == "desc";

            switch (query.SortBy?.ToLowerInvariant())
            {
                case "startdate":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(c => c.StartDate)
                        : validEntities.OrderBy(c => c.StartDate);
                    break;
                case "enddate":
                    sortedEntities = isDescending
                        ? validEntities.OrderByDescending(c => c.EndDate)
                        : validEntities.OrderBy(c => c.EndDate);
                    break;
                default:
                    sortedEntities = validEntities.OrderByDescending(c => c.CreatedAt);
                    break;
            }

            var sortedList = sortedEntities.ToList();

            var apartmentIds = sortedList
                .Select(c => c.ApartmentId)
                .Distinct()
                .ToList();

            var apartments = await _apartmentRepository.FindAsync(a => apartmentIds.Contains(a.ApartmentId));
            var apartmentDict = (apartments ?? Enumerable.Empty<Apartment>())
                .GroupBy(a => a.ApartmentId)
                .ToDictionary(g => g.Key, g => g.First());

            var ownerMembers = await _apartmentMemberRepository.FindAsync(
                m => apartmentIds.Contains(m.ApartmentId)
                     && m.IsOwner == true
                     && m.Status == "Đã Thuê"
            );
            var ownerDict = (ownerMembers ?? Enumerable.Empty<ApartmentMember>())
                .GroupBy(m => m.ApartmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault()
                );

            var users = await _userRepository.FindAsync(
                u => apartmentIds.Contains(u.ApartmentId) && !u.IsDeleted
            );
            var userDict = (users ?? Enumerable.Empty<User>())
                .GroupBy(u => u.ApartmentId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CreatedAt).FirstOrDefault()
                );

            var dtos = sortedList.Select(c =>
            {
                apartmentDict.TryGetValue(c.ApartmentId, out var apt);
                ownerDict.TryGetValue(c.ApartmentId, out var owner);
                userDict.TryGetValue(c.ApartmentId, out var user);

                var displayName = owner?.Name ?? user?.Name;
                var displayPhone = owner?.PhoneNumber ?? user?.Phone;
                var displayEmail = user?.Email;

                return new ContractDto
                {
                    ContractId = c.ContractId,
                    ApartmentId = c.ApartmentId,
                    ApartmentCode = apt?.Code,
                    OwnerName = displayName,
                    OwnerPhoneNumber = displayPhone,
                    OwnerEmail = displayEmail,
                    Image = c.Image,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    CreatedAt = c.CreatedAt
                };
            }).ToList();
            return ApiResponse<IEnumerable<ContractDto>>.Success(dtos);
        }

        public async Task<ContractDto?> GetByIdAsync(string id)
        {
            var contract = await _contractRepository.FirstOrDefaultAsync(c => c.ContractId == id);
            if (contract == null)
                return null;

            var apartment = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == contract.ApartmentId);

            var ownerMembers = await _apartmentMemberRepository.FindAsync(
                m => m.ApartmentId == contract.ApartmentId
                     && m.IsOwner == true
                     && m.Status == "Đang Thuê"
            );
            var owner = (ownerMembers ?? Enumerable.Empty<ApartmentMember>())
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();

            var users = await _userRepository.FindAsync(
                u => u.ApartmentId == contract.ApartmentId && !u.IsDeleted
            );
            var user = (users ?? Enumerable.Empty<User>())
                .OrderByDescending(u => u.CreatedAt)
                .FirstOrDefault();

            var displayName = owner?.Name ?? user?.Name;
            var displayPhone = owner?.PhoneNumber ?? user?.Phone;
            var displayEmail = user?.Email;

            var dto = new ContractDto
            {
                ContractId = contract.ContractId,
                ApartmentId = contract.ApartmentId,
                ApartmentCode = apartment?.Code,
                OwnerName = displayName,
                OwnerPhoneNumber = displayPhone,
                OwnerEmail = displayEmail,
                Image = contract.Image,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                CreatedAt = contract.CreatedAt
            };

            return dto;
        }


        public async Task<ContractDto> CreateAsync(ContractCreateDto dto)
        {
            var now = DateTime.UtcNow;

            var apartment = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == dto.ApartmentId);
            if (apartment == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy căn hộ với ID: {dto.ApartmentId}");
            }
            if (apartment.Status?.ToLowerInvariant() != "chưa thuê")
            {
                throw new InvalidOperationException("Căn hộ này không có sẵn để cho thuê.");
            }

            apartment.Status = "Đã Thuê";
            apartment.UpdatedAt = now;

            var contract = _mapper.Map<Contract>(dto); 
            contract.ContractId = Guid.NewGuid().ToString("N");
            contract.CreatedAt = now;
            contract.UpdatedAt = now;
            await _contractRepository.AddAsync(contract);

            var existingUser = await _userRepository.FirstOrDefaultAsync(u => u.ApartmentId == dto.ApartmentId);

            if (existingUser != null)
            {
                existingUser.Name = dto.OwnerName;
                existingUser.Phone = dto.OwnerPhoneNumber;
                existingUser.Email = dto.OwnerEmail;
                existingUser.UpdatedAt = now;
            }
            else
            {
                var newUser = new User
                {
                    UserId = Guid.NewGuid().ToString("N"),
                    RoleId = "EC13BABB-416F-42EB-BFD4-0725493A63D0", 
                    ApartmentId = dto.ApartmentId,
                    Email = dto.OwnerEmail, 
                    Phone = dto.OwnerPhoneNumber,
                    Name = dto.OwnerName,
                   
                    PasswordHash = "$2a$12$s7OmJwjZnyB8qCrL9KifvORA461N/6WgzDfvAyRUMhWVVkHuPecZ.", 
                    Status = "Active", 
                    IsDeleted = false,

                    AvatarUrl = null,
                    StaffCode = Guid.NewGuid().ToString("N"),
                    LastLoginAt = null,

                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _userRepository.AddAsync(newUser);
            }

            var apartmentMember = new ApartmentMember
            {
                ApartmentMemberId = Guid.NewGuid().ToString("N"),
                ApartmentId = dto.ApartmentId,
                Name = dto.OwnerName,
                PhoneNumber = dto.OwnerPhoneNumber,
                IdNumber = dto.OwnerIdNumber,
                Gender = dto.OwnerGender,
                DateOfBirth = dto.OwnerDateOfBirth,
                Nationality = dto.OwnerNationality,

                IsOwner = true,
                FamilyRole = "Chủ Hộ",
                Status = "Đang Thuê",
                CreatedAt = now,
                UpdatedAt = now
            };
            await _apartmentMemberRepository.AddAsync(apartmentMember);

            await _contractRepository.SaveChangesAsync(); 

            return _mapper.Map<ContractDto>(contract);
        }


        public async Task<bool> UpdateAsync(string id, ContractUpdateDto dto)
        {
            var entity = await _contractRepository.FirstOrDefaultAsync(c => c.ContractId == id);
            if (entity == null) return false;

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(entity);
            return await _contractRepository.SaveChangesAsync();
        }


        
        public async Task<bool> DeleteAsync(string id)
        {
            var contract = await _contractRepository.FirstOrDefaultAsync(c => c.ContractId == id);
            if (contract == null) return false;

            var apartment = await _apartmentRepository.FirstOrDefaultAsync(a => a.ApartmentId == contract.ApartmentId);
            var ownerMembers = await _apartmentMemberRepository.FindAsync(
                m => m.ApartmentId == contract.ApartmentId && m.IsOwner == true && m.Status == "Đang Thuê"
            );

            try
            {
                await _contractRepository.RemoveAsync(contract);

                if (apartment != null)
                {
                    apartment.Status = "Chưa Thuê";
                    apartment.UpdatedAt = DateTime.UtcNow;
                }

                if (ownerMembers != null)
                {
                    foreach (var member in ownerMembers)
                    {
                        member.Status = "Đã rời đi"; 
                        member.IsOwner = false; 
                        member.UpdatedAt = DateTime.UtcNow;
                    }
                }

                return await _contractRepository.SaveChangesAsync();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}