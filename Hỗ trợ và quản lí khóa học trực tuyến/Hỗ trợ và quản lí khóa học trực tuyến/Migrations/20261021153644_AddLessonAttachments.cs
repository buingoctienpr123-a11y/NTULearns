using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonAttachments",
                columns: table => new
                {
                    LessonAttachmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LessonId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonAttachments", x => x.LessonAttachmentId);
                    table.ForeignKey(
                        name: "FK_LessonAttachments_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "LessonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonAttachments_LessonId",
                table: "LessonAttachments",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonAttachments_StoredFileName",
                table: "LessonAttachments",
                column: "StoredFileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonAttachments");
        }
    }
}
