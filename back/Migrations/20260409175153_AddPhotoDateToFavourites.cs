using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCameras.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoDateToFavourites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add column as nullable first for backfill
            migrationBuilder.AddColumn<DateTime>(
                name: "PhotoDate",
                table: "Favourites",
                type: "datetime2",
                nullable: true);

            // Backfill: parse date from filename (format: _yyyyMMddHHmmss.ext)
            // Falls back to AddedAt if pattern not found
            migrationBuilder.Sql(@"
                UPDATE f
                SET f.PhotoDate = COALESCE(
                    (SELECT TRY_CONVERT(datetime2,
                        SUBSTRING(f2.RelativePath, p + 1, 4) + '-' +
                        SUBSTRING(f2.RelativePath, p + 5, 2) + '-' +
                        SUBSTRING(f2.RelativePath, p + 7, 2) + ' ' +
                        SUBSTRING(f2.RelativePath, p + 9, 2) + ':' +
                        SUBSTRING(f2.RelativePath, p + 11, 2) + ':' +
                        SUBSTRING(f2.RelativePath, p + 13, 2))
                     FROM (VALUES(PATINDEX('%_[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9].%', f2.RelativePath))) AS v(p)
                     WHERE p > 0),
                    f.AddedAt
                )
                FROM Favourites f
                JOIN Favourites f2 ON f.Id = f2.Id
            ");

            // Make column NOT NULL
            migrationBuilder.AlterColumn<DateTime>(
                name: "PhotoDate",
                table: "Favourites",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoDate",
                table: "Favourites");
        }
    }
}
