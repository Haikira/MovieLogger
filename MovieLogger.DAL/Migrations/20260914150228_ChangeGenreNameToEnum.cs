using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieLogger.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ChangeGenreNameToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Name",
                table: "Genres",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Genres",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }
    }
}
