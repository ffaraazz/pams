using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectRoleToAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "project_role",
                schema: "pams",
                table: "allocations",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "project_role",
                schema: "pams",
                table: "allocations");
        }
    }
}
