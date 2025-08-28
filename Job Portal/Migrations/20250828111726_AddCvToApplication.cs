using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Job_Portal.Migrations
{
    public partial class AddCvToApplication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CvFilePath",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvFilePath",
                table: "Applications");
        }
    }
}
