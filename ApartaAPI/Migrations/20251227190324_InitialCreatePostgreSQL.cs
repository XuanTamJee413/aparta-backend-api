using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApartaAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreatePostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PERMISSION_GROUP",
                columns: table => new
                {
                    permission_group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PERMISSI__EE3284C68DB7AB33", x => x.permission_group_id);
                });

            migrationBuilder.CreateTable(
                name: "ROLE",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    role_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_system_defined = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ROLE__760965CC5394B2D8", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "SERVICE",
                columns: table => new
                {
                    service_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SERVICE__3E0DB8AF045053BA", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "VISITOR",
                columns: table => new
                {
                    visitor_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    id_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VISITOR__87ED1B5193229DCF", x => x.visitor_id);
                });

            migrationBuilder.CreateTable(
                name: "PERMISSION",
                columns: table => new
                {
                    permission_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    permission_group_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PERMISSI__E5331AFAF4AB49CD", x => x.permission_id);
                    table.ForeignKey(
                        name: "FK_Permission_Group",
                        column: x => x.permission_group_id,
                        principalTable: "PERMISSION_GROUP",
                        principalColumn: "permission_group_id");
                });

            migrationBuilder.CreateTable(
                name: "ROLE_PERMISSION",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    permission_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ROLE_PER__C85A5463D7BB52E9", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission",
                        column: x => x.permission_id,
                        principalTable: "PERMISSION",
                        principalColumn: "permission_id");
                    table.ForeignKey(
                        name: "FK_RolePermission_Role",
                        column: x => x.role_id,
                        principalTable: "ROLE",
                        principalColumn: "role_id");
                });

            migrationBuilder.CreateTable(
                name: "APARTMENT",
                columns: table => new
                {
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    building_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    area = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    floor = table.Column<int>(type: "integer", nullable: true),
                    handover_date = table.Column<DateOnly>(type: "date", nullable: true),
                    occupancy_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValue: "Vacant")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__APARTMEN__DC51C2EC7DE49FAE", x => x.apartment_id);
                });

            migrationBuilder.CreateTable(
                name: "RECEIPT",
                columns: table => new
                {
                    receipt_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax = table.Column<double>(type: "double precision", nullable: true, defaultValue: 0.0),
                    discount = table.Column<double>(type: "double precision", nullable: true, defaultValue: 0.0),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RECEIPT__91F52C1F45568F9C", x => x.receipt_id);
                    table.ForeignKey(
                        name: "FK_Receipt_Apartment",
                        column: x => x.apartment_id,
                        principalTable: "APARTMENT",
                        principalColumn: "apartment_id");
                });

            migrationBuilder.CreateTable(
                name: "USER",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    staff_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    password_reset_token = table.Column<string>(type: "text", nullable: true),
                    reset_token_expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_first_login = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__USER__B9BE370F117CB554", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_User_Apartment",
                        column: x => x.apartment_id,
                        principalTable: "APARTMENT",
                        principalColumn: "apartment_id");
                    table.ForeignKey(
                        name: "FK_User_Role",
                        column: x => x.role_id,
                        principalTable: "ROLE",
                        principalColumn: "role_id");
                });

            migrationBuilder.CreateTable(
                name: "VEHICLE",
                columns: table => new
                {
                    vehicle_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    vehicle_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    info = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VEHICLE__F2947BC1B72C5C08", x => x.vehicle_id);
                    table.ForeignKey(
                        name: "FK_Vehicle_Apartment",
                        column: x => x.apartment_id,
                        principalTable: "APARTMENT",
                        principalColumn: "apartment_id");
                });

            migrationBuilder.CreateTable(
                name: "VISIT_LOG",
                columns: table => new
                {
                    visit_log_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    visitor_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    checkin_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    checkout_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    purpose = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VISIT_LO__A7A3DCAC2EBBBB2A", x => x.visit_log_id);
                    table.ForeignKey(
                        name: "FK_VisitLog_Apartment",
                        column: x => x.apartment_id,
                        principalTable: "APARTMENT",
                        principalColumn: "apartment_id");
                    table.ForeignKey(
                        name: "FK_VisitLog_Visitor",
                        column: x => x.visitor_id,
                        principalTable: "VISITOR",
                        principalColumn: "visitor_id");
                });

            migrationBuilder.CreateTable(
                name: "APARTMENT_MEMBER",
                columns: table => new
                {
                    apartment_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    face_image_url = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    info = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    id_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    is_owner = table.Column<bool>(type: "boolean", nullable: false),
                    nationality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    family_role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    role_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    head_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_app_access = table.Column<bool>(type: "boolean", nullable: false),
                    temporary_registration_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__APARTMEN__07BC3F23062F4794", x => x.apartment_member_id);
                    table.ForeignKey(
                        name: "FK_ApartmentMember_Apartment",
                        column: x => x.apartment_id,
                        principalTable: "APARTMENT",
                        principalColumn: "apartment_id");
                    table.ForeignKey(
                        name: "FK_ApartmentMember_Head",
                        column: x => x.head_member_id,
                        principalTable: "APARTMENT_MEMBER",
                        principalColumn: "apartment_member_id");
                    table.ForeignKey(
                        name: "FK_ApartmentMember_Role",
                        column: x => x.role_id,
                        principalTable: "ROLE",
                        principalColumn: "role_id");
                    table.ForeignKey(
                        name: "FK_ApartmentMember_User",
                        column: x => x.user_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "INTERACTION",
                columns: table => new
                {
                    interaction_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resident_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__INTERACT__605F8FE6D7B409C1", x => x.interaction_id);
                    table.ForeignKey(
                        name: "FK_Interaction_Resident",
                        column: x => x.resident_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Interaction_Staff",
                        column: x => x.staff_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "INVOICE",
                columns: table => new
                {
                    invoice_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fee_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__INVOICE__F58DFD499D6EA752", x => x.invoice_id);
                    table.ForeignKey(
                        name: "FK_Invoice_Apartment",
                        column: x => x.apartment_id,
                        principalTable: "APARTMENT",
                        principalColumn: "apartment_id");
                    table.ForeignKey(
                        name: "FK_Invoice_Staff",
                        column: x => x.staff_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "NEWS",
                columns: table => new
                {
                    news_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    author_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    published_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__NEWS__4C27CCD8CB045A65", x => x.news_id);
                    table.ForeignKey(
                        name: "FK_News_Author",
                        column: x => x.author_user_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "PROJECT",
                columns: table => new
                {
                    project_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    admin_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    project_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_account_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PayOSClientId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PayOSApiKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PayOSChecksumKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PROJECT__BC799E1F1E63F8E3", x => x.project_id);
                    table.ForeignKey(
                        name: "FK_Project_AdminUser",
                        column: x => x.admin_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "PROPOSAL",
                columns: table => new
                {
                    proposal_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    resident_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    operation_staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    reply = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PROPOSAL__A7BC641C8C1E6FC0", x => x.proposal_id);
                    table.ForeignKey(
                        name: "FK_Proposal_Resident",
                        column: x => x.resident_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Proposal_Staff",
                        column: x => x.operation_staff_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "SERVICE_BOOKING",
                columns: table => new
                {
                    service_booking_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    service_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resident_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    booking_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payment_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    resident_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    staff_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SERVICE___E1542436E9AB571A", x => x.service_booking_id);
                    table.ForeignKey(
                        name: "FK_ServiceBooking_Resident",
                        column: x => x.resident_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_ServiceBooking_Service",
                        column: x => x.service_id,
                        principalTable: "SERVICE",
                        principalColumn: "service_id");
                });

            migrationBuilder.CreateTable(
                name: "CONTRACT",
                columns: table => new
                {
                    contract_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    image = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    contract_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    contract_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Lease"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                    deposit_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true, defaultValue: 0m),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", nullable: true, defaultValue: 0m),
                    representative_member_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CONTRACT__F8D6642382F87C2E", x => x.contract_id);
                    table.ForeignKey(
                        name: "FK_Contract_Apartment",
                        column: x => x.apartment_id,
                        principalTable: "APARTMENT",
                        principalColumn: "apartment_id");
                    table.ForeignKey(
                        name: "FK_Contract_Representative",
                        column: x => x.representative_member_id,
                        principalTable: "APARTMENT_MEMBER",
                        principalColumn: "apartment_member_id");
                });

            migrationBuilder.CreateTable(
                name: "MESSAGE",
                columns: table => new
                {
                    message_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    interaction_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sender_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    is_read = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MESSAGE__0BBF6EE602F93070", x => x.message_id);
                    table.ForeignKey(
                        name: "FK_Message_Interaction",
                        column: x => x.interaction_id,
                        principalTable: "INTERACTION",
                        principalColumn: "interaction_id");
                    table.ForeignKey(
                        name: "FK_Message_Sender",
                        column: x => x.sender_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "INVOICE_ITEM",
                columns: table => new
                {
                    invoice_item_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    invoice_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fee_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__INVOICE___84ECDEE99590E6F5", x => x.invoice_item_id);
                    table.ForeignKey(
                        name: "FK_InvoiceItem_Invoice",
                        column: x => x.invoice_id,
                        principalTable: "INVOICE",
                        principalColumn: "invoice_id");
                });

            migrationBuilder.CreateTable(
                name: "PAYMENT",
                columns: table => new
                {
                    payment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    invoice_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payment_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PAYMENT__ED1FC9EAF671965C", x => x.payment_id);
                    table.ForeignKey(
                        name: "FK_Payment_Invoice",
                        column: x => x.invoice_id,
                        principalTable: "INVOICE",
                        principalColumn: "invoice_id");
                });

            migrationBuilder.CreateTable(
                name: "BUILDING",
                columns: table => new
                {
                    building_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    building_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    reading_window_start = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    reading_window_end = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    total_floors = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    total_basements = table.Column<int>(type: "integer", nullable: false),
                    total_area = table.Column<double>(type: "double precision", nullable: true),
                    handover_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reception_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BUILDING__9C9FBF7FBCE5497E", x => x.building_id);
                    table.ForeignKey(
                        name: "FK_Building_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECT",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "FEE_PERIOD",
                columns: table => new
                {
                    fee_period_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    items = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__FEE_PERI__41E3C089D4B052CE", x => x.fee_period_id);
                    table.ForeignKey(
                        name: "FK_FeePeriod_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECT",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "SUBSCRIPTION",
                columns: table => new
                {
                    subscription_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    subscription_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    payment_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    payment_note = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    num_months = table.Column<int>(type: "integer", nullable: false),
                    tax = table.Column<double>(type: "double precision", nullable: true, defaultValue: 0.0),
                    discount = table.Column<double>(type: "double precision", nullable: true, defaultValue: 0.0),
                    expired_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SUBSCRIP__863A7EC1775F8154", x => x.subscription_id);
                    table.ForeignKey(
                        name: "FK_Subscription_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECT",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "TASK",
                columns: table => new
                {
                    task_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    service_booking_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    operation_staff_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    assignee_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    verify_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TASK__0492148D45762CC0", x => x.task_id);
                    table.ForeignKey(
                        name: "FK_Task_Assigner",
                        column: x => x.operation_staff_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Task_ServiceBooking",
                        column: x => x.service_booking_id,
                        principalTable: "SERVICE_BOOKING",
                        principalColumn: "service_booking_id");
                });

            migrationBuilder.CreateTable(
                name: "METER_READING",
                columns: table => new
                {
                    meter_reading_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    apartment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fee_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reading_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reading_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    recorded_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    billing_period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    invoice_item_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__METER_RE__BDCAA50ED79AB34D", x => x.meter_reading_id);
                    table.ForeignKey(
                        name: "FK_MeterReading_Apartment",
                        column: x => x.apartment_id,
                        principalTable: "APARTMENT",
                        principalColumn: "apartment_id");
                    table.ForeignKey(
                        name: "FK_MeterReading_InvoiceItem",
                        column: x => x.invoice_item_id,
                        principalTable: "INVOICE_ITEM",
                        principalColumn: "invoice_item_id");
                    table.ForeignKey(
                        name: "FK_MeterReading_User",
                        column: x => x.recorded_by,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "ASSET",
                columns: table => new
                {
                    asset_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    building_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    info = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ASSET__D28B561D6F30662D", x => x.asset_id);
                    table.ForeignKey(
                        name: "FK_Asset_Building",
                        column: x => x.building_id,
                        principalTable: "BUILDING",
                        principalColumn: "building_id");
                });

            migrationBuilder.CreateTable(
                name: "EXPENSE",
                columns: table => new
                {
                    expense_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    building_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type_expense = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expense_description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    create_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "now()"),
                    actual_payment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EXPENSE__404B6A6B3DECB470", x => x.expense_id);
                    table.ForeignKey(
                        name: "FK_Expense_Building",
                        column: x => x.building_id,
                        principalTable: "BUILDING",
                        principalColumn: "building_id");
                });

            migrationBuilder.CreateTable(
                name: "PRICE_QUOTATION",
                columns: table => new
                {
                    price_quotation_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    building_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    fee_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PRICE_QU__D90AA4C3E1DA5484", x => x.price_quotation_id);
                    table.ForeignKey(
                        name: "FK_PriceQuotation_Building",
                        column: x => x.building_id,
                        principalTable: "BUILDING",
                        principalColumn: "building_id");
                });

            migrationBuilder.CreateTable(
                name: "STAFF_BUILDING_ASSIGNMENT",
                columns: table => new
                {
                    assignment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    building_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assignment_start_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "now()"),
                    assignment_end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    scope_of_work = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    assigned_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__STAFF_BU__DA8918146AE379A2", x => x.assignment_id);
                    table.ForeignKey(
                        name: "FK_StaffAssign_Admin",
                        column: x => x.assigned_by,
                        principalTable: "USER",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_StaffAssign_Building",
                        column: x => x.building_id,
                        principalTable: "BUILDING",
                        principalColumn: "building_id");
                    table.ForeignKey(
                        name: "FK_StaffAssign_User",
                        column: x => x.user_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "UTILITY",
                columns: table => new
                {
                    utility_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    period_time = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    open_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    close_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    building_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UTILITY__3F785C70A74615A4", x => x.utility_id);
                    table.ForeignKey(
                        name: "FK_UTILITY_BUILDING",
                        column: x => x.building_id,
                        principalTable: "BUILDING",
                        principalColumn: "building_id");
                });

            migrationBuilder.CreateTable(
                name: "TASK_ASSIGNMENT",
                columns: table => new
                {
                    task_assignment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    task_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigner_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assignee_user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TASK_ASS__D3B56037EB3681D0", x => x.task_assignment_id);
                    table.ForeignKey(
                        name: "FK_TaskAssignment_Assignee",
                        column: x => x.assignee_user_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_TaskAssignment_Assigner",
                        column: x => x.assigner_user_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_TaskAssignment_Task",
                        column: x => x.task_id,
                        principalTable: "TASK",
                        principalColumn: "task_id");
                });

            migrationBuilder.CreateTable(
                name: "UTILITY_BOOKING",
                columns: table => new
                {
                    utility_booking_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "gen_random_uuid()"),
                    utility_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resident_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    booking_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    booked_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    resident_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    staff_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UTILITY___30D7D31170F146D4", x => x.utility_booking_id);
                    table.ForeignKey(
                        name: "FK_UtilityBooking_Resident",
                        column: x => x.resident_id,
                        principalTable: "USER",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_UtilityBooking_Utility",
                        column: x => x.utility_id,
                        principalTable: "UTILITY",
                        principalColumn: "utility_id");
                });

            migrationBuilder.CreateIndex(
                name: "UQ_Apartment_Code_Building",
                table: "APARTMENT",
                columns: new[] { "building_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_APARTMENT_MEMBER_apartment_id",
                table: "APARTMENT_MEMBER",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_APARTMENT_MEMBER_head_member_id",
                table: "APARTMENT_MEMBER",
                column: "head_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_APARTMENT_MEMBER_role_id",
                table: "APARTMENT_MEMBER",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_APARTMENT_MEMBER_user_id",
                table: "APARTMENT_MEMBER",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ__APARTMEN__D58CDE11342FD0E9",
                table: "APARTMENT_MEMBER",
                column: "id_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ASSET_building_id",
                table: "ASSET",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_BUILDING_project_id",
                table: "BUILDING",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_CONTRACT_apartment_id",
                table: "CONTRACT",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_CONTRACT_representative_member_id",
                table: "CONTRACT",
                column: "representative_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_EXPENSE_building_id",
                table: "EXPENSE",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_PERIOD_project_id",
                table: "FEE_PERIOD",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_INTERACTION_resident_id",
                table: "INTERACTION",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "IX_INTERACTION_staff_id",
                table: "INTERACTION",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_apartment_id",
                table: "INVOICE",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_staff_id",
                table: "INVOICE",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_INVOICE_ITEM_invoice_id",
                table: "INVOICE_ITEM",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_MESSAGE_interaction_id",
                table: "MESSAGE",
                column: "interaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_MESSAGE_sender_id",
                table: "MESSAGE",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_METER_READING_apartment_id",
                table: "METER_READING",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_METER_READING_invoice_item_id",
                table: "METER_READING",
                column: "invoice_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_METER_READING_recorded_by",
                table: "METER_READING",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "IX_NEWS_author_user_id",
                table: "NEWS",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_invoice_id",
                table: "PAYMENT",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_PERMISSION_permission_group_id",
                table: "PERMISSION",
                column: "permission_group_id");

            migrationBuilder.CreateIndex(
                name: "UQ__PERMISSI__72E12F1B3DD54233",
                table: "PERMISSION",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__PERMISSI__72E12F1B32874269",
                table: "PERMISSION_GROUP",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PRICE_QUOTATION_building_id",
                table: "PRICE_QUOTATION",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_admin_id",
                table: "PROJECT",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "UQ__PROJECT__891B3A6F0B3C4833",
                table: "PROJECT",
                column: "project_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROPOSAL_operation_staff_id",
                table: "PROPOSAL",
                column: "operation_staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_PROPOSAL_resident_id",
                table: "PROPOSAL",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "IX_RECEIPT_apartment_id",
                table: "RECEIPT",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "UQ__ROLE__783254B11417ED30",
                table: "ROLE",
                column: "role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROLE_PERMISSION_permission_id",
                table: "ROLE_PERMISSION",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICE_BOOKING_resident_id",
                table: "SERVICE_BOOKING",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICE_BOOKING_service_id",
                table: "SERVICE_BOOKING",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_BUILDING_ASSIGNMENT_assigned_by",
                table: "STAFF_BUILDING_ASSIGNMENT",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_BUILDING_ASSIGNMENT_building_id",
                table: "STAFF_BUILDING_ASSIGNMENT",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_BUILDING_ASSIGNMENT_user_id",
                table: "STAFF_BUILDING_ASSIGNMENT",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTION_project_id",
                table: "SUBSCRIPTION",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "UQ__SUBSCRIP__5D7197A285C0C66A",
                table: "SUBSCRIPTION",
                column: "subscription_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TASK_operation_staff_id",
                table: "TASK",
                column: "operation_staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_service_booking_id",
                table: "TASK",
                column: "service_booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_ASSIGNMENT_assignee_user_id",
                table: "TASK_ASSIGNMENT",
                column: "assignee_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_ASSIGNMENT_assigner_user_id",
                table: "TASK_ASSIGNMENT",
                column: "assigner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_ASSIGNMENT_task_id",
                table: "TASK_ASSIGNMENT",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_USER_apartment_id",
                table: "USER",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_USER_role_id",
                table: "USER",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "UQ__USER__097F3286445150B8",
                table: "USER",
                column: "staff_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__USER__AB6E616418EE0ADD",
                table: "USER",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__USER__B43B145F399DABEE",
                table: "USER",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UTILITY_building_id",
                table: "UTILITY",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_UTILITY_BOOKING_resident_id",
                table: "UTILITY_BOOKING",
                column: "resident_id");

            migrationBuilder.CreateIndex(
                name: "IX_UTILITY_BOOKING_utility_id",
                table: "UTILITY_BOOKING",
                column: "utility_id");

            migrationBuilder.CreateIndex(
                name: "IX_VEHICLE_apartment_id",
                table: "VEHICLE",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Vehicle_Number",
                table: "VEHICLE",
                column: "vehicle_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VISIT_LOG_apartment_id",
                table: "VISIT_LOG",
                column: "apartment_id");

            migrationBuilder.CreateIndex(
                name: "IX_VISIT_LOG_visitor_id",
                table: "VISIT_LOG",
                column: "visitor_id");

            migrationBuilder.CreateIndex(
                name: "UQ__VISITOR__D58CDE1199266B68",
                table: "VISITOR",
                column: "id_number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Apartment_Building",
                table: "APARTMENT",
                column: "building_id",
                principalTable: "BUILDING",
                principalColumn: "building_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Apartment_Building",
                table: "APARTMENT");

            migrationBuilder.DropTable(
                name: "ASSET");

            migrationBuilder.DropTable(
                name: "CONTRACT");

            migrationBuilder.DropTable(
                name: "EXPENSE");

            migrationBuilder.DropTable(
                name: "FEE_PERIOD");

            migrationBuilder.DropTable(
                name: "MESSAGE");

            migrationBuilder.DropTable(
                name: "METER_READING");

            migrationBuilder.DropTable(
                name: "NEWS");

            migrationBuilder.DropTable(
                name: "PAYMENT");

            migrationBuilder.DropTable(
                name: "PRICE_QUOTATION");

            migrationBuilder.DropTable(
                name: "PROPOSAL");

            migrationBuilder.DropTable(
                name: "RECEIPT");

            migrationBuilder.DropTable(
                name: "ROLE_PERMISSION");

            migrationBuilder.DropTable(
                name: "STAFF_BUILDING_ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "SUBSCRIPTION");

            migrationBuilder.DropTable(
                name: "TASK_ASSIGNMENT");

            migrationBuilder.DropTable(
                name: "UTILITY_BOOKING");

            migrationBuilder.DropTable(
                name: "VEHICLE");

            migrationBuilder.DropTable(
                name: "VISIT_LOG");

            migrationBuilder.DropTable(
                name: "APARTMENT_MEMBER");

            migrationBuilder.DropTable(
                name: "INTERACTION");

            migrationBuilder.DropTable(
                name: "INVOICE_ITEM");

            migrationBuilder.DropTable(
                name: "PERMISSION");

            migrationBuilder.DropTable(
                name: "TASK");

            migrationBuilder.DropTable(
                name: "UTILITY");

            migrationBuilder.DropTable(
                name: "VISITOR");

            migrationBuilder.DropTable(
                name: "INVOICE");

            migrationBuilder.DropTable(
                name: "PERMISSION_GROUP");

            migrationBuilder.DropTable(
                name: "SERVICE_BOOKING");

            migrationBuilder.DropTable(
                name: "SERVICE");

            migrationBuilder.DropTable(
                name: "BUILDING");

            migrationBuilder.DropTable(
                name: "PROJECT");

            migrationBuilder.DropTable(
                name: "USER");

            migrationBuilder.DropTable(
                name: "APARTMENT");

            migrationBuilder.DropTable(
                name: "ROLE");
        }
    }
}
