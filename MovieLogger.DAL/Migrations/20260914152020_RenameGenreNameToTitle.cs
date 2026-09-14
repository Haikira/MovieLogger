using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieLogger.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RenameGenreNameToTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Genres",
                newName: "Title");

            migrationBuilder.RenameIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                newName: "IX_Genres_Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Genres",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_Genres_Title",
                table: "Genres",
                newName: "IX_Genres_Name");
        }
    }
}
