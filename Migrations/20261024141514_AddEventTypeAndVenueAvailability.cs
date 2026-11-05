using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEaseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTypeAndVenueAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "dbo_Venues",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "EventTypeId",
                table: "dbo_Events",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "dbo_Events",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BookingCode",
                table: "dbo_Bookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "dbo_EventTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_EventTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_Events_EventTypeId",
                table: "dbo_Events",
                column: "EventTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_Events_dbo_EventTypes_EventTypeId",
                table: "dbo_Events",
                column: "EventTypeId",
                principalTable: "dbo_EventTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dbo_Events_dbo_EventTypes_EventTypeId",
                table: "dbo_Events");

            migrationBuilder.DropTable(
                name: "dbo_EventTypes");

            migrationBuilder.DropIndex(
                name: "IX_dbo_Events_EventTypeId",
                table: "dbo_Events");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "dbo_Venues");

            migrationBuilder.DropColumn(
                name: "EventTypeId",
                table: "dbo_Events");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "dbo_Events");

            migrationBuilder.AlterColumn<string>(
                name: "BookingCode",
                table: "dbo_Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
