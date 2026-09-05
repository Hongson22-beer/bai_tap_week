using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ThueXe.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialRentalDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HangXe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenHang = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuocGia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MoTa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangXe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoaiXes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaLoai = table.Column<string>(type: "text", nullable: false),
                    TenLoai = table.Column<string>(type: "text", nullable: false),
                    SoCho = table.Column<int>(type: "integer", nullable: false),
                    MoTa = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiXes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "xe",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_hang_xe = table.Column<int>(type: "integer", nullable: false),
                    id_loai_xe = table.Column<int>(type: "integer", nullable: false),
                    bien_so_xe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mau_xe = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    nam_san_xuat = table.Column<int>(type: "integer", nullable: true),
                    don_gia_ngay = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    trang_thai = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    HangXeId = table.Column<int>(type: "integer", nullable: true),
                    LoaiXeId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_xe", x => x.id);
                    table.ForeignKey(
                        name: "FK_xe_HangXe_HangXeId",
                        column: x => x.HangXeId,
                        principalTable: "HangXe",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_xe_HangXe_id_hang_xe",
                        column: x => x.id_hang_xe,
                        principalTable: "HangXe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_xe_LoaiXes_LoaiXeId",
                        column: x => x.LoaiXeId,
                        principalTable: "LoaiXes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_xe_LoaiXes_id_loai_xe",
                        column: x => x.id_loai_xe,
                        principalTable: "LoaiXes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_xe_bien_so_xe",
                table: "xe",
                column: "bien_so_xe",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_xe_HangXeId",
                table: "xe",
                column: "HangXeId");

            migrationBuilder.CreateIndex(
                name: "IX_xe_id_hang_xe",
                table: "xe",
                column: "id_hang_xe");

            migrationBuilder.CreateIndex(
                name: "IX_xe_id_loai_xe",
                table: "xe",
                column: "id_loai_xe");

            migrationBuilder.CreateIndex(
                name: "IX_xe_LoaiXeId",
                table: "xe",
                column: "LoaiXeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "xe");

            migrationBuilder.DropTable(
                name: "HangXe");

            migrationBuilder.DropTable(
                name: "LoaiXes");
        }
    }
}
