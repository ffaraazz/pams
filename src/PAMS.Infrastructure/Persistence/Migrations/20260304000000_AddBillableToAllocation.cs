using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillableToAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "billable",
                schema: "pams",
                table: "allocations",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billable",
                schema: "pams",
                table: "allocations");
        }
    }
}
