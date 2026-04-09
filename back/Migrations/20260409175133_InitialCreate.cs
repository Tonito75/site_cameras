using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortalCameras.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DB already exists — created via EnsureCreated()
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
