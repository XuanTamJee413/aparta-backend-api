using ApartaAPI.Data;
using ApartaAPI.Models;
using ApartaAPI.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace ServiceUnitTest.Repositories
{
    public class ProposalRepositoryTests
    {
        private readonly ApartaDbContext _context;
        private readonly ProposalRepository _repository;

        public ProposalRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApartaDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApartaDbContext(options);
            _repository = new ProposalRepository(_context);
        }

        [Fact]
        public async Task GetResidentProposalsAsync_KhiResidentIdTonTai_NenTraVeDanhSachProposal()
        {
            // Arrange
            var residentId = "res1";
            var otherId = "res2";
            var proposals = new List<Proposal>
            {
                new Proposal { ProposalId = "p1", ResidentId = residentId, CreatedAt = DateTime.Now, Content = "Test Content", Status = "Pending" },
                new Proposal { ProposalId = "p2", ResidentId = residentId, CreatedAt = DateTime.Now.AddMinutes(-1), Content = "Test Content", Status = "Pending" },
                new Proposal { ProposalId = "p3", ResidentId = otherId, Content = "Test Content", Status = "Pending" }
            };
            await _context.Proposals.AddRangeAsync(proposals);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetResidentProposalsAsync(residentId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.ProposalId == "p1");
            result.Should().Contain(p => p.ProposalId == "p2");
            result.Should().NotContain(p => p.ProposalId == "p3");
        }

        [Fact]
        public async Task GetStaffAssignedProposalsAsync_KhiStaffIdTonTai_NenTraVeProposalDuocGanVaPending()
        {
            // Arrange
            var staffId = "staff1";
            var buildingId = "b1";
            var apartmentId = "apt1";

            var building = new Building { BuildingId = buildingId, BuildingCode = "B1", Name = "Building 1", ProjectId = "proj1" };
            var apartment = new Apartment { ApartmentId = apartmentId, BuildingId = buildingId, Code = "A101", Status = "Active" };
            var resident = new User { UserId = "res1", ApartmentId = apartmentId, Name = "Res 1", Role = new Role { RoleId = "r1", RoleName = "Resident" }, PasswordHash = "hash", Status = "Active" };
            
            var assignment = new StaffBuildingAssignment 
            { 
                AssignmentId = "assign1", 
                UserId = staffId, 
                BuildingId = buildingId, 
                IsActive = true,
                AssignmentStartDate = DateOnly.FromDateTime(DateTime.Now)
            };
            
            var assignedProposal = new Proposal { ProposalId = "p1", OperationStaffId = staffId, ResidentId = "res1", Resident = resident, Content = "Test Content", Status = "Pending" };
            var pendingProposal = new Proposal { ProposalId = "p2", Status = "Pending", ResidentId = "res1", Resident = resident, Content = "Test Content" };
            var otherProposal = new Proposal { ProposalId = "p3", OperationStaffId = "otherStaff", ResidentId = "res1", Resident = resident, Content = "Test Content", Status = "Processing" };

            await _context.Buildings.AddAsync(building);
            await _context.Apartments.AddAsync(apartment);
            await _context.Users.AddAsync(resident);
            await _context.StaffBuildingAssignments.AddAsync(assignment);
            await _context.Proposals.AddRangeAsync(assignedProposal, pendingProposal, otherProposal);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetStaffAssignedProposalsAsync(staffId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.ProposalId == "p1");
            result.Should().Contain(p => p.ProposalId == "p2");
        }

        [Fact]
        public async Task GetProposalDetailsAsync_KhiIdTonTai_NenTraVeChiTietVoiIncludes()
        {
            // Arrange
            var proposalId = "p1";
            var resident = new User { UserId = "res1", Name = "Res 1", Role = new Role { RoleId = "r1", RoleName = "Resident" }, PasswordHash = "hash", Status = "Active" };
            var staff = new User { UserId = "staff1", Name = "Staff 1", Role = new Role { RoleId = "r2", RoleName = "Staff" }, PasswordHash = "hash", Status = "Active" };
            var proposal = new Proposal { ProposalId = proposalId, ResidentId = "res1", OperationStaffId = "staff1", Resident = resident, OperationStaff = staff, Content = "Test Content", Status = "Pending" };

            await _context.Users.AddRangeAsync(resident, staff);
            await _context.Proposals.AddAsync(proposal);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetProposalDetailsAsync(proposalId);

            // Assert
            result.Should().NotBeNull();
            result!.ProposalId.Should().Be(proposalId);
            result.Resident.Should().NotBeNull();
            result.OperationStaff.Should().NotBeNull();
        }

        [Fact]
        public async Task GetStaffProposalsQuery_KhiGoi_NenTraVeQuery()
        {
            // Arrange
            var staffId = "staff1";
            var roleResident = new Role { RoleId = "r1", RoleName = "Resident" };
            var roleStaff = new Role { RoleId = "r2", RoleName = "Staff" };

            var res1 = new User { UserId = "res1", Name = "Res 1", Role = roleResident, PasswordHash = "hash", Status = "Active" };
            var res2 = new User { UserId = "res2", Name = "Res 2", Role = roleResident, PasswordHash = "hash", Status = "Active" };
            var staff = new User { UserId = staffId, Name = "Staff 1", Role = roleStaff, PasswordHash = "hash", Status = "Active" };

            var proposals = new List<Proposal>
            {
                new Proposal { ProposalId = "p1", OperationStaffId = staffId, Content = "Test Content", Status = "Pending", ResidentId = "res1", Resident = res1, OperationStaff = staff },
                new Proposal { ProposalId = "p2", OperationStaffId = "otherStaff", Content = "Test Content", Status = "Pending", ResidentId = "res2", Resident = res2 }
            };
            
            await _context.Users.AddRangeAsync(res1, res2, staff);
            await _context.Proposals.AddRangeAsync(proposals);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetStaffProposalsQuery(staffId);
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().ProposalId.Should().Be("p1");
        }
    }
}
