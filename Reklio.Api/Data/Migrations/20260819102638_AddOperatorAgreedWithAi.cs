using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reklio.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorAgreedWithAi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OperatorAgreedWithAi",
                table: "Claims",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OperatorAgreedWithAi",
                table: "Claims");
        }
    }
}
