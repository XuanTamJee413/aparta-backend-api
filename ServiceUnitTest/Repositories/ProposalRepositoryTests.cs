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
        public async Task GetResidentProposalsAsync_WithValidResident_ReturnsProposals()
        {
            // Arrange
            var residentId = "res1";
            var otherId = "res2";
            var proposals = new List<Proposal>
            {
                new Proposal { ProposalId = "p1", ResidentId = residentId, CreatedAt = DateTime.Now, Content = "Content 1", Status = "Pending" },
                new Proposal { ProposalId = "p2", ResidentId = residentId, CreatedAt = DateTime.Now.AddMinutes(-1), Content = "Content 2", Status = "Completed" },
                new Proposal { ProposalId = "p3", ResidentId = otherId, Content = "Content 3", Status = "Pending" }
            };
            await _context.Proposals.AddRangeAsync(proposals);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetResidentProposalsAsync(residentId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.ProposalId == "p1");
            result.Should().Contain(p => p.ProposalId == "p2");
            result.First().ProposalId.Should().Be("p1"); // Ordered by CreatedAt Desc
        }

        [Fact]
        public async Task GetResidentProposalsAsync_WithNoProposals_ReturnsEmptyList()
        {
            // Act
            var result = await _repository.GetResidentProposalsAsync("res_no_proposals");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetStaffAssignedProposalsAsync_WithAssignedAndPendingInScope_ReturnsCorrectProposals()
        {
            // Arrange
            var staffId = "staff1";
            var buildingId = "b1";
            var apartmentId = "apt1";

            var building = new Building { BuildingId = buildingId, BuildingCode = "B1" };
            var apartment = new Apartment { ApartmentId = apartmentId, BuildingId = buildingId };
            var resident = new User { UserId = "res1", ApartmentId = apartmentId, Apartment = apartment };
            
            var assignment = new StaffBuildingAssignment 
            { 
                AssignmentId = "assign1", UserId = staffId, BuildingId = buildingId, IsActive = true 
            };
            
            // p1: Assigned directly to staff
            var p1 = new Proposal { ProposalId = "p1", OperationStaffId = staffId, ResidentId = "res1", Resident = resident, Status = "Processing" };
            // p2: Pending in building assigned to staff
            var p2 = new Proposal { ProposalId = "p2", OperationStaffId = null, ResidentId = "res1", Resident = resident, Status = "Pending" };
            // p3: Assigned to other staff
            var p3 = new Proposal { ProposalId = "p3", OperationStaffId = "other", ResidentId = "res1", Resident = resident, Status = "Processing" };
            // p4: Pending but in other building (not assigned)
            var p4 = new Proposal { ProposalId = "p4", OperationStaffId = null, ResidentId = "res2", Resident = new User { Apartment = new Apartment { BuildingId = "b2" } }, Status = "Pending" };

            await _context.Buildings.AddAsync(building);
            await _context.StaffBuildingAssignments.AddAsync(assignment);
            await _context.Proposals.AddRangeAsync(p1, p2, p3, p4);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetStaffAssignedProposalsAsync(staffId);
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.ProposalId == "p1");
            result.Should().Contain(p => p.ProposalId == "p2");
        }

        [Fact]
        public async Task GetStaffAssignedProposalsAsync_WithNoAssignments_ReturnsOnlyDirectlyAssigned()
        {
            // Arrange
            var staffId = "staff1";
            var p1 = new Proposal { ProposalId = "p1", OperationStaffId = staffId, Status = "Processing" };
            var p2 = new Proposal { ProposalId = "p2", OperationStaffId = null, Status = "Pending" }; // Should not be returned as no building assignment

            await _context.Proposals.AddRangeAsync(p1, p2);
            await _context.SaveChangesAsync();

            // Act
            var query = _repository.GetStaffAssignedProposalsAsync(staffId);
            var result = await query.ToListAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().ProposalId.Should().Be("p1");
        }

        [Fact]
        public async Task GetProposalDetailsAsync_WithValidId_ReturnsProposalWithIncludes()
        {
            // Arrange
            var proposalId = "p1";
            var resident = new User { UserId = "res1", Name = "Resident" };
            var staff = new User { UserId = "staff1", Name = "Staff" };
            var proposal = new Proposal { ProposalId = proposalId, ResidentId = "res1", OperationStaffId = "staff1", Resident = resident, OperationStaff = staff };

            await _context.Users.AddRangeAsync(resident, staff);
            await _context.Proposals.AddAsync(proposal);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetProposalDetailsAsync(proposalId);

            // Assert
            result.Should().NotBeNull();
            result!.Resident.Should().NotBeNull();
            result.OperationStaff.Should().NotBeNull();
        }

        [Fact]
        public async Task GetProposalDetailsAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetProposalDetailsAsync("invalid_id");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetStaffProposalsQuery_WithValidStaff_ReturnsDirectlyAssignedProposals()
        {
            // Arrange
            var staffId = "staff1";
            var p1 = new Proposal { ProposalId = "p1", OperationStaffId = staffId };
            var p2 = new Proposal { ProposalId = "p2", OperationStaffId = "other" };

            await _context.Proposals.AddRangeAsync(p1, p2);
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
