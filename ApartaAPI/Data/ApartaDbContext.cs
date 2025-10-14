using System;
using System.Collections.Generic;
using ApartaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartaAPI.Data;

public partial class ApartaDbContext : DbContext
{
    public ApartaDbContext()
    {
    }

    public ApartaDbContext(DbContextOptions<ApartaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Apartment> Apartments { get; set; }

    public virtual DbSet<ApartmentMember> ApartmentMembers { get; set; }

    public virtual DbSet<Asset> Assets { get; set; }

    public virtual DbSet<Building> Buildings { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<FeePeriod> FeePeriods { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Meter> Meters { get; set; }

    public virtual DbSet<MeterReading> MeterReadings { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PermissionGroup> PermissionGroups { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Propose> Proposes { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceBooking> ServiceBookings { get; set; }

    public virtual DbSet<Models.Task> Tasks { get; set; }

    public virtual DbSet<TaskAssignment> TaskAssignments { get; set; }

    public virtual DbSet<UnitPrice> UnitPrices { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Utility> Utilities { get; set; }

    public virtual DbSet<UtilityBooking> UtilityBookings { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VisitLog> VisitLogs { get; set; }

    public virtual DbSet<Visitor> Visitors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Apartment>(entity =>
        {
            entity.HasKey(e => e.ApartmentId).HasName("PK__APARTMEN__DC51C2EC99CDFC6F");

            entity.ToTable("APARTMENT");

            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.Area).HasColumnName("area");
            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasColumnName("building_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Building).WithMany(p => p.Apartments)
                .HasForeignKey(d => d.BuildingId)
                .HasConstraintName("FK_Apartment_Building");
        });

        modelBuilder.Entity<ApartmentMember>(entity =>
        {
            entity.HasKey(e => e.ApartmentMemberId).HasName("PK__APARTMEN__07BC3F23639E4B63");

            entity.ToTable("APARTMENT_MEMBER");

            entity.Property(e => e.ApartmentMemberId)
                .HasMaxLength(50)
                .HasColumnName("apartment_member_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.FaceImageUrl).HasColumnName("face_image_url");
            entity.Property(e => e.FamilyRole)
                .HasMaxLength(50)
                .HasColumnName("family_role");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.IdNumber)
                .HasMaxLength(50)
                .HasColumnName("id_number");
            entity.Property(e => e.Info)
                .HasMaxLength(255)
                .HasColumnName("info");
            entity.Property(e => e.IsOwned).HasColumnName("is_owned");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Nationality)
                .HasMaxLength(50)
                .HasColumnName("nationality");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.ApartmentMembers)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_ApartmentMember_Apartment");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.AssetId).HasName("PK__ASSET__D28B561D803BFA3D");

            entity.ToTable("ASSET");

            entity.Property(e => e.AssetId)
                .HasMaxLength(50)
                .HasColumnName("asset_id");
            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasColumnName("building_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Info)
                .HasColumnType("text")
                .HasColumnName("info");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Building).WithMany(p => p.Assets)
                .HasForeignKey(d => d.BuildingId)
                .HasConstraintName("FK_Asset_Building");
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.BuildingId).HasName("PK__BUILDING__9C9FBF7FD667F7E6");

            entity.ToTable("BUILDING");

            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasColumnName("building_id");
            entity.Property(e => e.BuildingCode)
                .HasMaxLength(50)
                .HasColumnName("building_code");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.NumApartments).HasColumnName("num_apartments");
            entity.Property(e => e.NumResidents).HasColumnName("num_residents");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(50)
                .HasColumnName("project_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Project).WithMany(p => p.Buildings)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Building_Project");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContactId).HasName("PK__CONTRACT__024E7A864269BB85");

            entity.ToTable("CONTRACT");

            entity.Property(e => e.ContactId)
                .HasMaxLength(50)
                .HasColumnName("contact_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_Contract_Apartment");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId).HasName("PK__EXPENSE__404B6A6B004972DD");

            entity.ToTable("EXPENSE");

            entity.Property(e => e.ExpenseId)
                .HasMaxLength(50)
                .HasColumnName("expense_id");
            entity.Property(e => e.ActualPaymentDate).HasColumnName("actual_payment_date");
            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasColumnName("building_id");
            entity.Property(e => e.CreateDate).HasColumnName("create_date");
            entity.Property(e => e.ExpenseDescription)
                .HasMaxLength(255)
                .HasColumnName("expense_description");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.TypeExpense)
                .HasMaxLength(50)
                .HasColumnName("type_expense");

            entity.HasOne(d => d.Building).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.BuildingId)
                .HasConstraintName("FK_Expense_Building");
        });

        modelBuilder.Entity<FeePeriod>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FEE_PERI__3213E83FF41E609A");

            entity.ToTable("FEE_PERIOD");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Items)
                .HasColumnType("text")
                .HasColumnName("items");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.FeePeriods)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_FeePeriod_Apartment");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__LOG__9E2397E001B268A0");

            entity.ToTable("LOG");

            entity.Property(e => e.LogId)
                .HasMaxLength(50)
                .HasColumnName("log_id");
            entity.Property(e => e.Action)
                .HasMaxLength(255)
                .HasColumnName("action");
            entity.Property(e => e.Details)
                .HasColumnType("text")
                .HasColumnName("details");
            entity.Property(e => e.Timestamp)
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
            entity.Property(e => e.UserAccountId)
                .HasMaxLength(50)
                .HasColumnName("user_account_id");

            entity.HasOne(d => d.UserAccount).WithMany(p => p.Logs)
                .HasForeignKey(d => d.UserAccountId)
                .HasConstraintName("FK_Log_User");
        });

        modelBuilder.Entity<Meter>(entity =>
        {
            entity.HasKey(e => e.MeterId).HasName("PK__METER__6647C3150B565480");

            entity.ToTable("METER");

            entity.Property(e => e.MeterId)
                .HasMaxLength(50)
                .HasColumnName("meter_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<MeterReading>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__METER_RE__3213E83FC3492F33");

            entity.ToTable("METER_READING");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentReading).HasColumnName("currentReading");
            entity.Property(e => e.MeterId)
                .HasMaxLength(50)
                .HasColumnName("meter_id");
            entity.Property(e => e.PreviousReading).HasColumnName("previousReading");
            entity.Property(e => e.ReadingDate).HasColumnName("reading_date");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.MeterReadings)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_MeterReading_Apartment");

            entity.HasOne(d => d.Meter).WithMany(p => p.MeterReadings)
                .HasForeignKey(d => d.MeterId)
                .HasConstraintName("FK_MeterReading_Meter");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.NewsId).HasName("PK__NEWS__4C27CCD8EFA9F3F9");

            entity.ToTable("NEWS");

            entity.Property(e => e.NewsId)
                .HasMaxLength(50)
                .HasColumnName("news_id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ManagementStaffId)
                .HasMaxLength(50)
                .HasColumnName("management_staff_id");
            entity.Property(e => e.PublishedDate)
                .HasColumnType("datetime")
                .HasColumnName("published_date");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ManagementStaff).WithMany(p => p.News)
                .HasForeignKey(d => d.ManagementStaffId)
                .HasConstraintName("FK_News_ManagementStaff");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__ORDER__46596229EF4948A2");

            entity.ToTable("ORDER");

            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .HasColumnName("order_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Discount).HasColumnName("discount");
            entity.Property(e => e.ExpiredAt)
                .HasColumnType("datetime")
                .HasColumnName("expired_at");
            entity.Property(e => e.NumMonths).HasColumnName("num_months");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(50)
                .HasColumnName("order_code");
            entity.Property(e => e.PaymentInfo)
                .HasColumnType("text")
                .HasColumnName("payment_info");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(50)
                .HasColumnName("project_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Tax).HasColumnName("tax");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");

            entity.HasOne(d => d.Project).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Order_Project");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__PAYMENT__ED1FC9EAC32FC847");

            entity.ToTable("PAYMENT");

            entity.Property(e => e.PaymentId)
                .HasMaxLength(50)
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId)
                .HasMaxLength(50)
                .HasColumnName("order_id");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.ReceiptId)
                .HasMaxLength(50)
                .HasColumnName("receipt_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Payment_Order");

            entity.HasOne(d => d.Receipt).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ReceiptId)
                .HasConstraintName("FK_Payment_Receipt");
        });

        modelBuilder.Entity<PermissionGroup>(entity =>
        {
            entity.HasKey(e => e.PermissionGroupId).HasName("PK__PERMISSI__EE3284C6A4580D8D");

            entity.ToTable("PERMISSION_GROUP");

            entity.Property(e => e.PermissionGroupId)
                .HasMaxLength(50)
                .HasColumnName("permission_group_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Permissions)
                .HasColumnType("text")
                .HasColumnName("permissions");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__PROJECT__BC799E1FFB04D601");

            entity.ToTable("PROJECT");

            entity.Property(e => e.ProjectId)
                .HasMaxLength(50)
                .HasColumnName("project_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.NumApartments).HasColumnName("num_apartments");
            entity.Property(e => e.NumBuildings).HasColumnName("num_buildings");
            entity.Property(e => e.ProjectCode)
                .HasMaxLength(50)
                .HasColumnName("project_code");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Propose>(entity =>
        {
            entity.HasKey(e => e.ProposeId).HasName("PK__PROPOSE__D223B0C94D45F31E");

            entity.ToTable("PROPOSE");

            entity.Property(e => e.ProposeId)
                .HasMaxLength(50)
                .HasColumnName("propose_id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ResidentId)
                .HasMaxLength(50)
                .HasColumnName("resident_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Resident).WithMany(p => p.Proposes)
                .HasForeignKey(d => d.ResidentId)
                .HasConstraintName("FK_Propose_User");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__RECEIPT__91F52C1F72C3ACFE");

            entity.ToTable("RECEIPT");

            entity.Property(e => e.ReceiptId)
                .HasMaxLength(50)
                .HasColumnName("receipt_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Discount).HasColumnName("discount");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Tax).HasColumnName("tax");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_Receipt_Apartment");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__ROLE__760965CC0B207B02");

            entity.ToTable("ROLE");

            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(255)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__SERVICE__3E0DB8AFACA94A05");

            entity.ToTable("SERVICE");

            entity.Property(e => e.ServiceId)
                .HasMaxLength(50)
                .HasColumnName("service_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ServiceBooking>(entity =>
        {
            entity.HasKey(e => e.ServiceBookingId).HasName("PK__SERVICE___E1542436A6E214D0");

            entity.ToTable("SERVICE_BOOKING");

            entity.Property(e => e.ServiceBookingId)
                .HasMaxLength(50)
                .HasColumnName("service_booking_id");
            entity.Property(e => e.BookingDate).HasColumnName("booking_date");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PaymentAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("payment_amount");
            entity.Property(e => e.ResidentId)
                .HasMaxLength(50)
                .HasColumnName("resident_id");
            entity.Property(e => e.ServiceId)
                .HasMaxLength(50)
                .HasColumnName("service_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Resident).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.ResidentId)
                .HasConstraintName("FK_ServiceBooking_User");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_ServiceBooking_Service");
        });

        modelBuilder.Entity<Models.Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__TASK__0492148D3B35836C");

            entity.ToTable("TASK");

            entity.Property(e => e.TaskId)
                .HasMaxLength(50)
                .HasColumnName("task_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.ServiceBookingId)
                .HasMaxLength(50)
                .HasColumnName("service_booking_id");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Task_User");

            entity.HasOne(d => d.ServiceBooking).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ServiceBookingId)
                .HasConstraintName("FK_Task_ServiceBooking");
        });

        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            entity.HasKey(e => e.TaskAssignmentId).HasName("PK__TASK_ASS__D3B560371758DDF4");

            entity.ToTable("TASK_ASSIGNMENT");

            entity.Property(e => e.TaskAssignmentId)
                .HasMaxLength(50)
                .HasColumnName("task_assignment_id");
            entity.Property(e => e.AssignedDate)
                .HasColumnType("datetime")
                .HasColumnName("assigned_date");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ManagementStaffId)
                .HasMaxLength(50)
                .HasColumnName("management_staff_id");
            entity.Property(e => e.ServiceStaffId)
                .HasMaxLength(50)
                .HasColumnName("service_staff_id");
            entity.Property(e => e.TaskId)
                .HasMaxLength(50)
                .HasColumnName("task_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ManagementStaff).WithMany(p => p.TaskAssignmentManagementStaffs)
                .HasForeignKey(d => d.ManagementStaffId)
                .HasConstraintName("FK_TaskAssignment_ManagementStaff");

            entity.HasOne(d => d.ServiceStaff).WithMany(p => p.TaskAssignmentServiceStaffs)
                .HasForeignKey(d => d.ServiceStaffId)
                .HasConstraintName("FK_TaskAssignment_ServiceStaff");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskAssignments)
                .HasForeignKey(d => d.TaskId)
                .HasConstraintName("FK_TaskAssignment_Task");
        });

        modelBuilder.Entity<UnitPrice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UNIT_PRI__3213E83FC055C513");

            entity.ToTable("UNIT_PRICE");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("id");
            entity.Property(e => e.CalculationMethod)
                .HasMaxLength(50)
                .HasColumnName("calculation_method");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FeePeriodId)
                .HasMaxLength(50)
                .HasColumnName("fee_period_id");
            entity.Property(e => e.FeeType)
                .HasMaxLength(50)
                .HasColumnName("fee_type");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.FeePeriod).WithMany(p => p.UnitPrices)
                .HasForeignKey(d => d.FeePeriodId)
                .HasConstraintName("FK_UnitPrice_FeePeriod");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__USER__B9BE370F7C807B2B");

            entity.ToTable("USER");

            entity.HasIndex(e => e.StaffCode, "UQ__USER__097F3286581A2118").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__USER__AB6E61648F3DD5A9").IsUnique();

            entity.HasIndex(e => e.Phone, "UQ__USER__B43B145F5C559557").IsUnique();

            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .HasColumnName("user_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted");
            entity.Property(e => e.LastLoginAt)
                .HasColumnType("datetime")
                .HasColumnName("last_login_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PermissionId)
                .HasMaxLength(50)
                .HasColumnName("permission_id");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .HasColumnName("role_id");
            entity.Property(e => e.StaffCode)
                .HasMaxLength(50)
                .HasColumnName("staff_code");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Users)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_User_Apartment");

            entity.HasOne(d => d.Permission).WithMany(p => p.Users)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("FK_User_PermissionGroup");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        modelBuilder.Entity<Utility>(entity =>
        {
            entity.HasKey(e => e.UtilityId).HasName("PK__UTILITY__3F785C7052742E07");

            entity.ToTable("UTILITY");

            entity.Property(e => e.UtilityId)
                .HasMaxLength(50)
                .HasColumnName("utility_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UtilityBooking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UTILITY___3213E83F8974191B");

            entity.ToTable("UTILITY_BOOKING");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("id");
            entity.Property(e => e.BookingDate).HasColumnName("booking_date");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ResidentId)
                .HasMaxLength(50)
                .HasColumnName("resident_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UtilityId)
                .HasMaxLength(50)
                .HasColumnName("utility_id");

            entity.HasOne(d => d.Resident).WithMany(p => p.UtilityBookings)
                .HasForeignKey(d => d.ResidentId)
                .HasConstraintName("FK_UtilityBooking_User");

            entity.HasOne(d => d.Utility).WithMany(p => p.UtilityBookings)
                .HasForeignKey(d => d.UtilityId)
                .HasConstraintName("FK_UtilityBooking_Utility");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__VEHICLE__F2947BC1F773EBA1");

            entity.ToTable("VEHICLE");

            entity.Property(e => e.VehicleId)
                .HasMaxLength(50)
                .HasColumnName("vehicle_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Info)
                .HasColumnType("text")
                .HasColumnName("info");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehivleNumber)
                .HasMaxLength(50)
                .HasColumnName("vehivle_number");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_Vehicle_Apartment");
        });

        modelBuilder.Entity<VisitLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VISIT_LO__3213E83F13B2D045");

            entity.ToTable("VISIT_LOG");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .HasColumnName("id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CheckinTime)
                .HasColumnType("datetime")
                .HasColumnName("checkin_time");
            entity.Property(e => e.CheckoutTime)
                .HasColumnType("datetime")
                .HasColumnName("checkout_time");
            entity.Property(e => e.Purpose)
                .HasMaxLength(255)
                .HasColumnName("purpose");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.VisitorId)
                .HasMaxLength(50)
                .HasColumnName("visitor_id");

            entity.HasOne(d => d.Apartment).WithMany(p => p.VisitLogs)
                .HasForeignKey(d => d.ApartmentId)
                .HasConstraintName("FK_VisitLog_User");

            entity.HasOne(d => d.Visitor).WithMany(p => p.VisitLogs)
                .HasForeignKey(d => d.VisitorId)
                .HasConstraintName("FK_VisitLog_Visitor");
        });

        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(e => e.VisitorId).HasName("PK__VISITOR__87ED1B516CD3C345");

            entity.ToTable("VISITOR");

            entity.Property(e => e.VisitorId)
                .HasMaxLength(50)
                .HasColumnName("visitor_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IdNumber)
                .HasMaxLength(50)
                .HasColumnName("id_number");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
