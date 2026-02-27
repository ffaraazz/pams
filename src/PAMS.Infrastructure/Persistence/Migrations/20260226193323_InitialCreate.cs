using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pams");

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "pams",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    account_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    account_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "pams",
                columns: table => new
                {
                    audit_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.audit_id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "pams",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    emp_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    designation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reports_to = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.employee_id);
                    table.ForeignKey(
                        name: "FK_employees_employees_reports_to",
                        column: x => x.reports_to,
                        principalSchema: "pams",
                        principalTable: "employees",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "pams",
                columns: table => new
                {
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    skill_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.skill_id);
                });

            migrationBuilder.CreateTable(
                name: "system_configs",
                schema: "pams",
                columns: table => new
                {
                    config_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    min_allocation_pct = table.Column<int>(type: "integer", nullable: false, defaultValue: 25),
                    allocation_increment = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configs", x => x.config_id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "pams",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    project_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Upcoming"),
                    billable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.project_id);
                    table.CheckConstraint("chk_project_dates", "end_date IS NULL OR end_date >= start_date");
                    table.ForeignKey(
                        name: "FK_projects_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "pams",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_projects_employees_project_manager_id",
                        column: x => x.project_manager_id,
                        principalSchema: "pams",
                        principalTable: "employees",
                        principalColumn: "employee_id");
                });

            migrationBuilder.CreateTable(
                name: "employee_skills",
                schema: "pams",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_skills", x => new { x.employee_id, x.skill_id });
                    table.ForeignKey(
                        name: "FK_employee_skills_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "pams",
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalSchema: "pams",
                        principalTable: "skills",
                        principalColumn: "skill_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "allocations",
                schema: "pams",
                columns: table => new
                {
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
                    percentage = table.Column<short>(type: "smallint", nullable: false),
                    allocated_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_allocations", x => x.allocation_id);
                    table.CheckConstraint("chk_allocation_dates", "to_date IS NULL OR to_date >= from_date");
                    table.CheckConstraint("chk_allocation_percentage", "percentage BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_allocations_employees_allocated_by_id",
                        column: x => x.allocated_by_id,
                        principalSchema: "pams",
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_allocations_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "pams",
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_allocations_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "pams",
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_team_members",
                schema: "pams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_lead_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reportee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_team_members", x => x.id);
                    table.CheckConstraint("chk_ptm_lead_not_reportee", "team_lead_id != reportee_id");
                    table.ForeignKey(
                        name: "FK_project_team_members_employees_reportee_id",
                        column: x => x.reportee_id,
                        principalSchema: "pams",
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_team_members_employees_team_lead_id",
                        column: x => x.team_lead_id,
                        principalSchema: "pams",
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_team_members_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "pams",
                        principalTable: "projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_account_code",
                schema: "pams",
                table: "accounts",
                column: "account_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_alloc_employee_active",
                schema: "pams",
                table: "allocations",
                column: "employee_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_alloc_employee_dates",
                schema: "pams",
                table: "allocations",
                columns: new[] { "employee_id", "from_date", "to_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_alloc_project",
                schema: "pams",
                table: "allocations",
                column: "project_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_allocations_allocated_by_id",
                schema: "pams",
                table: "allocations",
                column: "allocated_by_id");

            migrationBuilder.CreateIndex(
                name: "idx_auditlog_actor",
                schema: "pams",
                table: "audit_logs",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "idx_auditlog_created",
                schema: "pams",
                table: "audit_logs",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_auditlog_entity",
                schema: "pams",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_skills_skill_id",
                schema: "pams",
                table: "employee_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_email",
                schema: "pams",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_emp_code",
                schema: "pams",
                table: "employees",
                column: "emp_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_reports_to",
                schema: "pams",
                table: "employees",
                column: "reports_to",
                filter: "reports_to IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_ptm_project",
                schema: "pams",
                table: "project_team_members",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "idx_ptm_reportee",
                schema: "pams",
                table: "project_team_members",
                column: "reportee_id");

            migrationBuilder.CreateIndex(
                name: "idx_ptm_team_lead",
                schema: "pams",
                table: "project_team_members",
                column: "team_lead_id");

            migrationBuilder.CreateIndex(
                name: "uq_ptm_project_lead_reportee",
                schema: "pams",
                table: "project_team_members",
                columns: new[] { "project_id", "team_lead_id", "reportee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_account_id",
                schema: "pams",
                table: "projects",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_project_code",
                schema: "pams",
                table: "projects",
                column: "project_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_project_manager_id",
                schema: "pams",
                table: "projects",
                column: "project_manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_skills_skill_name",
                schema: "pams",
                table: "skills",
                column: "skill_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allocations",
                schema: "pams");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "pams");

            migrationBuilder.DropTable(
                name: "employee_skills",
                schema: "pams");

            migrationBuilder.DropTable(
                name: "project_team_members",
                schema: "pams");

            migrationBuilder.DropTable(
                name: "system_configs",
                schema: "pams");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "pams");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "pams");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "pams");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "pams");
        }
    }
}
