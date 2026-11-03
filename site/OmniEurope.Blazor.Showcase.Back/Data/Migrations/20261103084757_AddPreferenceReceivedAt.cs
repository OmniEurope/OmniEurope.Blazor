using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniEurope.Blazor.Showcase.Back.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferenceReceivedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added nullable, backfilled with the date each row already had, then made required: no column default
            // is left behind, the server assigns every later value itself.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceivedAt",
                table: "VisitorPreferences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"VisitorPreferences\" SET \"ReceivedAt\" = \"UpdatedAt\";");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ReceivedAt",
                table: "VisitorPreferences",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorPreferences_VisitorId_ReceivedAt",
                table: "VisitorPreferences",
                columns: new[] { "VisitorId", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitorPreferences_VisitorId_ReceivedAt",
                table: "VisitorPreferences");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "VisitorPreferences");
        }
    }
}
