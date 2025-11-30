using AutoMapper;
using ApartaAPI.DTOs.ApartmentMembers;
using ApartaAPI.DTOs.Common;
using ApartaAPI.Models;
using ApartaAPI.Repositories.Interfaces;
using ApartaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ApartaAPI.Services
{
    public class ApartmentMemberService : IApartmentMemberService
    {
        private readonly IRepository<ApartmentMember> _repository;
        private readonly IRepository<Apartment> _apartmentRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;

        public ApartmentMemberService(
            IRepository<ApartmentMember> repository,
            IRepository<Apartment> apartmentRepository,
            ICloudinaryService cloudinaryService,
            IMapper mapper)
        {
            _repository = repository;
            _apartmentRepository = apartmentRepository;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<ApartmentMemberDto>>> GetAllAsync(ApartmentMemberQueryParameters query)
        {
            var rawSearch = string.IsNullOrWhiteSpace(query.SearchTerm)
                ? null
                : query.SearchTerm.Trim();

            var searchTerm = rawSearch?.ToLowerInvariant();

            List<string>? apartmentIdsByCode = null;
            if (searchTerm != null)
            {
                var matchedApartments = await _apartmentRepository.FindAsync(a =>
                    a.Code != null && a.Code.ToLower().Contains(searchTerm));

                apartmentIdsByCode = matchedApartments
                    .Where(a => a != null && a.ApartmentId != null)
                    .Select(a => a.ApartmentId)
                    .ToList();
            }

            Expression<Func<ApartmentMember, bool>> predicate = m =>
                (!query.IsOwned.HasValue || m.IsOwner == query.IsOwned.Value) &&

                (string.IsNullOrEmpty(query.ApartmentId) || m.ApartmentId == query.ApartmentId) &&

                (
                    searchTerm == null
                    ||
                    (m.Name != null && m.Name.ToLower().Contains(searchTerm))
                    ||
                    (m.PhoneNumber != null && m.PhoneNumber.ToLower().Contains(searchTerm))
                    ||
                    (m.IdNumber != null && m.IdNumber.ToLower().Contains(searchTerm))
                    ||
                    (apartmentIdsByCode != null && apartmentIdsByCode.Contains(m.ApartmentId))
                );

            var entities = await _repository.FindAsync(predicate);

            var validEntities = entities
                .Where(m => m != null)
                .ToList();

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
                return ApiResponse<IEnumerable<ApartmentMemberDto>>
                    .Fail(ApiResponse.SM01_NO_RESULTS);
            }

            return ApiResponse<IEnumerable<ApartmentMemberDto>>.Success(dtos);
        }

        public async Task<ApartmentMemberDto?> GetByIdAsync(string id)
        {
            var entity = await _repository.FirstOrDefaultAsync(m => m.ApartmentMemberId == id);
            return _mapper.Map<ApartmentMemberDto?>(entity);
        }

        public async Task<ApartmentMemberDto> CreateAsync(
            ApartmentMemberCreateDto dto,
            IFormFile? faceImageFile = null,
            CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(dto.IdNumber))
            {
                var existingMember = await _repository.FirstOrDefaultAsync(m => m.IdNumber == dto.IdNumber);
                if (existingMember != null)
                {
                    var error = ApiResponse.Fail(ApiResponse.SM16_DUPLICATE_CODE, "ID Number");
                    throw new InvalidOperationException(error.Message);
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existingPhone = await _repository.FirstOrDefaultAsync(m => m.PhoneNumber == dto.PhoneNumber);
                if (existingPhone != null)
                {
                    var error = ApiResponse.Fail(ApiResponse.SM16_DUPLICATE_CODE, "số điện thoại");
                    throw new InvalidOperationException(error.Message);
                }
            }

            var now = DateTime.UtcNow;

            var entity = _mapper.Map<ApartmentMember>(dto);

            entity.ApartmentMemberId = Guid.NewGuid().ToString("N");
            entity.CreatedAt ??= now;
            entity.UpdatedAt = now;

            if (faceImageFile != null && faceImageFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(
                    faceImageFile,
                    folder: "aparta/apartment-members",
                    cancellationToken: cancellationToken
                );

                entity.FaceImageUrl = uploadResult.SecureUrl;
            }
            else if (!string.IsNullOrWhiteSpace(dto.FaceImageUrl))
            {
                entity.FaceImageUrl = dto.FaceImageUrl;
            }

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return _mapper.Map<ApartmentMemberDto>(entity);
        }

        public async Task<bool> UpdateAsync(string id, ApartmentMemberUpdateDto dto)
        {
            var entity = await _repository.FirstOrDefaultAsync(m => m.ApartmentMemberId == id);
            if (entity == null) return false;

            if (!string.IsNullOrWhiteSpace(dto.IdNumber))
            {
                var existingMember = await _repository.FirstOrDefaultAsync(
                    m => m.IdNumber == dto.IdNumber && m.ApartmentMemberId != id
                );

                if (existingMember != null)
                {
                    var error = ApiResponse.Fail(ApiResponse.SM16_DUPLICATE_CODE, "ID Number");
                    throw new InvalidOperationException(error.Message);
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existingPhone = await _repository.FirstOrDefaultAsync(
                    m => m.PhoneNumber == dto.PhoneNumber && m.ApartmentMemberId != id
                );

                if (existingPhone != null)
                {
                    var error = ApiResponse.Fail(ApiResponse.SM16_DUPLICATE_CODE, "số điện thoại");
                    throw new InvalidOperationException(error.Message);
                }
            }

            _mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            return await _repository.SaveChangesAsync();
        }

        public async Task<ApiResponse<string>> UpdateFaceImageAsync(
            string memberId, IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return ApiResponse<string>.Fail(ApiResponse.SM25_INVALID_INPUT);
            }

            var entity = await _repository.FirstOrDefaultAsync(m => m.ApartmentMemberId == memberId);
            if (entity == null)
            {
                return ApiResponse<string>.Fail(ApiResponse.SM01_NO_RESULTS);
            }

            try
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(
                    file,
                    folder: "aparta/apartment-members",
                    cancellationToken: cancellationToken
                );

                if (string.IsNullOrWhiteSpace(uploadResult.SecureUrl))
                {
                    return ApiResponse<string>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
                }

                entity.FaceImageUrl = uploadResult.SecureUrl;
                entity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(entity);
                await _repository.SaveChangesAsync();

                return ApiResponse<string>.Success(
                    uploadResult.SecureUrl,
                    ApiResponse.SM03_UPDATE_SUCCESS
                );
            }
            catch (Exception)
            {
                return ApiResponse<string>.Fail(ApiResponse.SM40_SYSTEM_ERROR);
            }
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
