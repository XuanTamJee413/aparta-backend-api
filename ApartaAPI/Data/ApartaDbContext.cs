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

    public virtual DbSet<Interaction> Interactions { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Meter> Meters { get; set; }

    public virtual DbSet<MeterReading> MeterReadings { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PermissionGroup> PermissionGroups { get; set; }

    public virtual DbSet<PriceQuotation> PriceQuotations { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Proposal> Proposals { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceBooking> ServiceBookings { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<Models.Task> Tasks { get; set; }

    public virtual DbSet<TaskAssignment> TaskAssignments { get; set; }

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
            entity.HasKey(e => e.ApartmentId).HasName("PK__APARTMEN__DC51C2EC7DE49FAE");

            entity.ToTable("APARTMENT");

            entity.HasIndex(e => new { e.BuildingId, e.Code }, "UQ_Apartment_Code_Building").IsUnique();

            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("apartment_id");
            entity.Property(e => e.Area).HasColumnName("area");
            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasColumnName("building_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
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

            entity.ToTable(tb => tb.UseSqlOutputClause(false));

            entity.HasOne(d => d.Building).WithMany(p => p.Apartments)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Apartment_Building");
        });

        modelBuilder.Entity<ApartmentMember>(entity =>
        {
            entity.HasKey(e => e.ApartmentMemberId).HasName("PK__APARTMEN__07BC3F23062F4794");

            entity.ToTable("APARTMENT_MEMBER");

            entity.HasIndex(e => e.IdNumber, "UQ__APARTMEN__D58CDE11342FD0E9").IsUnique();

            entity.Property(e => e.ApartmentMemberId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("apartment_member_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
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
            entity.Property(e => e.IsOwner).HasColumnName("is_owner");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Nationality)
                .HasMaxLength(50)
                .HasColumnName("nationality");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.ToTable(tb => tb.UseSqlOutputClause(false));

            entity.HasOne(d => d.Apartment).WithMany(p => p.ApartmentMembers)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ApartmentMember_Apartment");
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.HasKey(e => e.AssetId).HasName("PK__ASSET__D28B561D6F30662D");

            entity.ToTable("ASSET");

            entity.Property(e => e.AssetId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("asset_id");
            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasColumnName("building_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Info).HasColumnName("info");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Building).WithMany(p => p.Assets)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Asset_Building");
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.BuildingId).HasName("PK__BUILDING__9C9FBF7FBCE5497E");

            entity.ToTable("BUILDING");

            entity.HasIndex(e => e.BuildingCode, "UQ__BUILDING__B04D26DBFD232E91").IsUnique();

            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("building_id");
            entity.Property(e => e.BuildingCode)
                .HasMaxLength(50)
                .HasColumnName("building_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.NumApartments)
                .HasDefaultValue(0)
                .HasColumnName("num_apartments");
            entity.Property(e => e.NumResidents)
                .HasDefaultValue(0)
                .HasColumnName("num_residents");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(50)
                .HasColumnName("project_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.ToTable(tb => tb.UseSqlOutputClause(false));

            entity.HasOne(d => d.Project).WithMany(p => p.Buildings)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Building_Project");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__CONTRACT__F8D6642382F87C2E");

            entity.ToTable("CONTRACT");

            entity.Property(e => e.ContractId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("contract_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Contract_Apartment");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId).HasName("PK__EXPENSE__404B6A6B3DECB470");

            entity.ToTable("EXPENSE");

            entity.Property(e => e.ExpenseId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("expense_id");
            entity.Property(e => e.ActualPaymentDate).HasColumnName("actual_payment_date");
            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasColumnName("building_id");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("create_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpenseDescription)
                .HasMaxLength(255)
                .HasColumnName("expense_description");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.TypeExpense)
                .HasMaxLength(50)
                .HasColumnName("type_expense");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Building).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Expense_Building");
        });

        modelBuilder.Entity<FeePeriod>(entity =>
        {
            entity.HasKey(e => e.FeePeriodId).HasName("PK__FEE_PERI__41E3C089D4B052CE");

            entity.ToTable("FEE_PERIOD");

            entity.Property(e => e.FeePeriodId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("fee_period_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Items).HasColumnName("items");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(50)
                .HasColumnName("project_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Project).WithMany(p => p.FeePeriods)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FeePeriod_Project");
        });

        modelBuilder.Entity<Interaction>(entity =>
        {
            entity.HasKey(e => e.InteractionId).HasName("PK__INTERACT__605F8FE6D7B409C1");

            entity.ToTable("INTERACTION");

            entity.Property(e => e.InteractionId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("interaction_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ResidentId)
                .HasMaxLength(50)
                .HasColumnName("resident_id");
            entity.Property(e => e.StaffId)
                .HasMaxLength(50)
                .HasColumnName("staff_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Resident).WithMany(p => p.InteractionResidents)
                .HasForeignKey(d => d.ResidentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Interaction_Resident");

            entity.HasOne(d => d.Staff).WithMany(p => p.InteractionStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Interaction_Staff");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK__INVOICE__F58DFD499D6EA752");

            entity.ToTable("INVOICE");

            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.FeeType)
                .HasMaxLength(50)
                .HasColumnName("fee_type");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.StaffId)
                .HasMaxLength(50)
                .HasColumnName("staff_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invoice_Apartment");

            entity.HasOne(d => d.Staff).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_Invoice_Staff");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__LOG__9E2397E044C7D5AE");

            entity.ToTable("LOG");

            entity.Property(e => e.LogId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("log_id");
            entity.Property(e => e.Action)
                .HasMaxLength(255)
                .HasColumnName("action");
            entity.Property(e => e.Details).HasColumnName("details");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Logs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Log_User");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK__MESSAGE__0BBF6EE602F93070");

            entity.ToTable("MESSAGE");

            entity.Property(e => e.MessageId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("message_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.InteractionId)
                .HasMaxLength(50)
                .HasColumnName("interaction_id");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.SenderId)
                .HasMaxLength(50)
                .HasColumnName("sender_id");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("sent_at");

            entity.HasOne(d => d.Interaction).WithMany(p => p.Messages)
                .HasForeignKey(d => d.InteractionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_Interaction");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Message_Sender");
        });

        modelBuilder.Entity<Meter>(entity =>
        {
            entity.HasKey(e => e.MeterId).HasName("PK__METER__6647C3157098003C");

            entity.ToTable("METER");

            entity.Property(e => e.MeterId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("meter_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
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
            entity.HasKey(e => e.MeterReadingId).HasName("PK__METER_RE__BDCAA50ED79AB34D");

            entity.ToTable("METER_READING");

            entity.Property(e => e.MeterReadingId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("meter_reading_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentReading).HasColumnName("current_reading");
            entity.Property(e => e.MeterId)
                .HasMaxLength(50)
                .HasColumnName("meter_id");
            entity.Property(e => e.PreviousReading).HasColumnName("previous_reading");
            entity.Property(e => e.ReadingDate).HasColumnName("reading_date");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.MeterReadings)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MeterReading_Apartment");

            entity.HasOne(d => d.Meter).WithMany(p => p.MeterReadings)
                .HasForeignKey(d => d.MeterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MeterReading_Meter");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.NewsId).HasName("PK__NEWS__4C27CCD8CB045A65");

            entity.ToTable("NEWS");

            entity.Property(e => e.NewsId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("news_id");
            entity.Property(e => e.AuthorUserId)
                .HasMaxLength(50)
                .HasColumnName("author_user_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PublishedDate)
                .HasColumnType("datetime")
                .HasColumnName("published_date");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Status)
                .HasMaxLength(255)
                .HasColumnName("status");
            entity.HasOne(d => d.AuthorUser).WithMany(p => p.News)
                .HasForeignKey(d => d.AuthorUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_News_Author");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__PAYMENT__ED1FC9EAF671965C");

            entity.ToTable("PAYMENT");

            entity.Property(e => e.PaymentId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasColumnName("invoice_id");
            entity.Property(e => e.PaymentCode)
                .HasMaxLength(100)
                .HasColumnName("payment_code");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Invoice");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PK__PERMISSI__E5331AFAF4AB49CD");

            entity.ToTable("PERMISSION");

            entity.HasIndex(e => e.Name, "UQ__PERMISSI__72E12F1B3DD54233").IsUnique();

            entity.Property(e => e.PermissionId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("permission_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PermissionGroupId)
                .HasMaxLength(50)
                .HasColumnName("permission_group_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.PermissionGroup).WithMany(p => p.Permissions)
                .HasForeignKey(d => d.PermissionGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Permission_Group");
        });

        modelBuilder.Entity<PermissionGroup>(entity =>
        {
            entity.HasKey(e => e.PermissionGroupId).HasName("PK__PERMISSI__EE3284C68DB7AB33");

            entity.ToTable("PERMISSION_GROUP");

            entity.HasIndex(e => e.Name, "UQ__PERMISSI__72E12F1B32874269").IsUnique();

            entity.Property(e => e.PermissionGroupId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("permission_group_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<PriceQuotation>(entity =>
        {
            entity.HasKey(e => e.PriceQuotationId).HasName("PK__PRICE_QU__D90AA4C3E1DA5484");

            entity.ToTable("PRICE_QUOTATION");

            entity.Property(e => e.PriceQuotationId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("price_quotation_id");
            entity.Property(e => e.BuildingId)
                .HasMaxLength(50)
                .HasColumnName("building_id");
            entity.Property(e => e.CalculationMethod)
                .HasMaxLength(50)
                .HasColumnName("calculation_method");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FeeType)
                .HasMaxLength(50)
                .HasColumnName("fee_type");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Building).WithMany(p => p.PriceQuotations)
                .HasForeignKey(d => d.BuildingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PriceQuotation_Building");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__PROJECT__BC799E1F1E63F8E3");

            entity.ToTable("PROJECT");

            entity.HasIndex(e => e.ProjectCode, "UQ__PROJECT__891B3A6F0B3C4833").IsUnique();

            entity.Property(e => e.ProjectId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("project_id");
            entity.Property(e => e.AdminId)
                .HasMaxLength(50)
                .HasColumnName("admin_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.NumApartments)
                .HasDefaultValue(0)
                .HasColumnName("num_apartments");
            entity.Property(e => e.NumBuildings)
                .HasDefaultValue(0)
                .HasColumnName("num_buildings");
            entity.Property(e => e.ProjectCode)
                .HasMaxLength(50)
                .HasColumnName("project_code");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.HasOne(d => d.Admin).WithMany(p => p.Projects)
                .HasForeignKey(d => d.AdminId)
                .HasConstraintName("FK_Project_AdminUser");
        });

        modelBuilder.Entity<Proposal>(entity =>
        {
            entity.HasKey(e => e.ProposalId).HasName("PK__PROPOSAL__A7BC641C8C1E6FC0");

            entity.ToTable("PROPOSAL");

            entity.Property(e => e.ProposalId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("proposal_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.OperationStaffId)
                .HasMaxLength(50)
                .HasColumnName("operation_staff_id");
            entity.Property(e => e.Reply).HasColumnName("reply");
            entity.Property(e => e.ResidentId)
                .HasMaxLength(50)
                .HasColumnName("resident_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.OperationStaff).WithMany(p => p.ProposalOperationStaffs)
                .HasForeignKey(d => d.OperationStaffId)
                .HasConstraintName("FK_Proposal_Staff");

            entity.HasOne(d => d.Resident).WithMany(p => p.ProposalResidents)
                .HasForeignKey(d => d.ResidentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proposal_Resident");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__RECEIPT__91F52C1F45568F9C");

            entity.ToTable("RECEIPT");

            entity.Property(e => e.ReceiptId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("receipt_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Discount)
                .HasDefaultValue(0.0)
                .HasColumnName("discount");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Tax)
                .HasDefaultValue(0.0)
                .HasColumnName("tax");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Receipt_Apartment");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__ROLE__760965CC5394B2D8");

            entity.ToTable("ROLE");

            entity.HasIndex(e => e.RoleName, "UQ__ROLE__783254B11417ED30").IsUnique();

            entity.Property(e => e.RoleId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.RoleName)
                .HasMaxLength(255)
                .HasColumnName("role_name");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermission_Permission"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermission_Role"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId").HasName("PK__ROLE_PER__C85A5463D7BB52E9");
                        j.ToTable("ROLE_PERMISSION");
                        j.IndexerProperty<string>("RoleId")
                            .HasMaxLength(50)
                            .HasColumnName("role_id");
                        j.IndexerProperty<string>("PermissionId")
                            .HasMaxLength(50)
                            .HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__SERVICE__3E0DB8AF045053BA");

            entity.ToTable("SERVICE");

            entity.Property(e => e.ServiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("service_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<ServiceBooking>(entity =>
        {
            entity.HasKey(e => e.ServiceBookingId).HasName("PK__SERVICE___E1542436E9AB571A");

            entity.ToTable("SERVICE_BOOKING");

            entity.Property(e => e.ServiceBookingId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("service_booking_id");
            entity.Property(e => e.BookingDate)
                .HasColumnType("datetime")
                .HasColumnName("booking_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PaymentAmount)
                .HasColumnType("decimal(18, 2)")
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
			entity.Property(e => e.ResidentNote)
				.HasMaxLength(255)
				.HasColumnName("resident_note");
			entity.Property(e => e.StaffNote)
				.HasMaxLength(255)
				.HasColumnName("staff_note");

			entity.HasOne(d => d.Resident).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.ResidentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceBooking_Resident");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceBookings)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceBooking_Service");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__SUBSCRIP__863A7EC1775F8154");

            entity.ToTable("SUBSCRIPTION");

            entity.HasIndex(e => e.SubscriptionCode, "UQ__SUBSCRIP__5D7197A285C0C66A").IsUnique();

            entity.Property(e => e.SubscriptionId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("subscription_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.AmountPaid)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount_paid");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Discount)
                .HasDefaultValue(0.0)
                .HasColumnName("discount");
            entity.Property(e => e.ExpiredAt)
                .HasColumnType("datetime")
                .HasColumnName("expired_at");
            entity.Property(e => e.NumMonths).HasColumnName("num_months");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentNote).HasColumnName("payment_note");
            entity.Property(e => e.ProjectId)
                .HasMaxLength(50)
                .HasColumnName("project_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.SubscriptionCode)
                .HasMaxLength(50)
                .HasColumnName("subscription_code");
            entity.Property(e => e.Tax)
                .HasDefaultValue(0.0)
                .HasColumnName("tax");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Project).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscription_Project");
        });

        modelBuilder.Entity<Models.Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__TASK__0492148D45762CC0");

            entity.ToTable("TASK");

            entity.Property(e => e.TaskId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("task_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.OperationStaffId)
                .HasMaxLength(50)
                .HasColumnName("operation_staff_id");
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

            entity.HasOne(d => d.OperationStaff).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.OperationStaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Task_Assigner");

            entity.HasOne(d => d.ServiceBooking).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ServiceBookingId)
                .HasConstraintName("FK_Task_ServiceBooking");
        });

        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            entity.HasKey(e => e.TaskAssignmentId).HasName("PK__TASK_ASS__D3B56037EB3681D0");

            entity.ToTable("TASK_ASSIGNMENT");

            entity.Property(e => e.TaskAssignmentId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("task_assignment_id");
            entity.Property(e => e.AssignedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("assigned_date");
            entity.Property(e => e.AssigneeUserId)
                .HasMaxLength(50)
                .HasColumnName("assignee_user_id");
            entity.Property(e => e.AssignerUserId)
                .HasMaxLength(50)
                .HasColumnName("assigner_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.TaskId)
                .HasMaxLength(50)
                .HasColumnName("task_id");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.AssigneeUser).WithMany(p => p.TaskAssignmentAssigneeUsers)
                .HasForeignKey(d => d.AssigneeUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskAssignment_Assignee");

            entity.HasOne(d => d.AssignerUser).WithMany(p => p.TaskAssignmentAssignerUsers)
                .HasForeignKey(d => d.AssignerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskAssignment_Assigner");

            entity.HasOne(d => d.Task).WithMany(p => p.TaskAssignments)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TaskAssignment_Task");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__USER__B9BE370F117CB554");

            entity.ToTable("USER");

            entity.HasIndex(e => e.StaffCode, "UQ__USER__097F3286445150B8").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__USER__AB6E616418EE0ADD").IsUnique();

            entity.HasIndex(e => e.Phone, "UQ__USER__B43B145F399DABEE").IsUnique();

            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("user_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
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

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Role");
        });

        modelBuilder.Entity<Utility>(entity =>
        {
            entity.HasKey(e => e.UtilityId).HasName("PK__UTILITY__3F785C70A74615A4");

            entity.ToTable("UTILITY");

            entity.Property(e => e.UtilityId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("utility_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Location)
                .HasMaxLength(255)
                .HasColumnName("location");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PeriodTime).HasColumnName("period_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UtilityBooking>(entity =>
        {
            entity.HasKey(e => e.UtilityBookingId).HasName("PK__UTILITY___30D7D31170F146D4");

            entity.ToTable("UTILITY_BOOKING");

            entity.Property(e => e.UtilityBookingId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("utility_booking_id");
            entity.Property(e => e.BookedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("booked_at");
            entity.Property(e => e.BookingDate)
                .HasColumnType("datetime")
                .HasColumnName("booking_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UtilityBooking_Resident");

            entity.HasOne(d => d.Utility).WithMany(p => p.UtilityBookings)
                .HasForeignKey(d => d.UtilityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UtilityBooking_Utility");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.VehicleId).HasName("PK__VEHICLE__F2947BC1B72C5C08");

            entity.ToTable("VEHICLE");

            entity.HasIndex(e => e.VehicleNumber, "UQ_Vehicle_Number").IsUnique();

            entity.Property(e => e.VehicleId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("vehicle_id");
            entity.Property(e => e.ApartmentId)
                .HasMaxLength(50)
                .HasColumnName("apartment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Info).HasColumnName("info");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.VehicleNumber)
                .HasMaxLength(50)
                .HasColumnName("vehicle_number");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Vehicles)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Vehicle_Apartment");
        });

        modelBuilder.Entity<VisitLog>(entity =>
        {
            entity.HasKey(e => e.VisitLogId).HasName("PK__VISIT_LO__A7A3DCAC2EBBBB2A");

            entity.ToTable("VISIT_LOG");

            entity.Property(e => e.VisitLogId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("visit_log_id");
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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VisitLog_Apartment");

            entity.HasOne(d => d.Visitor).WithMany(p => p.VisitLogs)
                .HasForeignKey(d => d.VisitorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VisitLog_Visitor");
        });

        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.HasKey(e => e.VisitorId).HasName("PK__VISITOR__87ED1B5193229DCF");

            entity.ToTable("VISITOR");

            entity.HasIndex(e => e.IdNumber, "UQ__VISITOR__D58CDE1199266B68").IsUnique();

            entity.Property(e => e.VisitorId)
                .HasMaxLength(50)
                .HasDefaultValueSql("(newid())")
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
