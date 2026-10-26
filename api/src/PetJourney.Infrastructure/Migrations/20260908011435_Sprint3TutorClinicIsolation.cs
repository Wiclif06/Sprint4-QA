using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetJourneyTutorApi.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3TutorClinicIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PJTUTORCLINICA",
                columns: table => new
                {
                    IDTUTOR = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IDCLINICA = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PJTUTORCLINICA", x => new { x.IDTUTOR, x.IDCLINICA });
                    table.ForeignKey(
                        name: "FK_PJTUTORCLINICA_TBCLINICA_IDCLINICA",
                        column: x => x.IDCLINICA,
                        principalTable: "TBCLINICA",
                        principalColumn: "IDCLINICA",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PJTUTORCLINICA_TBTUTOR_IDTUTOR",
                        column: x => x.IDTUTOR,
                        principalTable: "TBTUTOR",
                        principalColumn: "IDTUTOR",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("INSERT INTO PJTUTORCLINICA (IDTUTOR, IDCLINICA) SELECT DISTINCT IDTUTOR, IDCLINICA FROM TBPET WHERE IDCLINICA IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TBRACA_IDESPECIE",
                table: "TBRACA",
                column: "IDESPECIE");

            migrationBuilder.CreateIndex(
                name: "IX_TBPET_IDCLINICA",
                table: "TBPET",
                column: "IDCLINICA");

            migrationBuilder.CreateIndex(
                name: "IX_TBPET_IDESPECIE",
                table: "TBPET",
                column: "IDESPECIE");

            migrationBuilder.CreateIndex(
                name: "IX_TBPET_IDRACA",
                table: "TBPET",
                column: "IDRACA");

            migrationBuilder.CreateIndex(
                name: "IX_TBPET_IDTUTOR",
                table: "TBPET",
                column: "IDTUTOR");

            migrationBuilder.CreateIndex(
                name: "IX_TBLEMBRETE_IDPET",
                table: "TBLEMBRETE",
                column: "IDPET");

            migrationBuilder.CreateIndex(
                name: "IX_PJTUTORCLINICA_IDCLINICA",
                table: "PJTUTORCLINICA",
                column: "IDCLINICA");

            migrationBuilder.AddForeignKey(
                name: "FK_TBLEMBRETE_TBPET_IDPET",
                table: "TBLEMBRETE",
                column: "IDPET",
                principalTable: "TBPET",
                principalColumn: "IDPET",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBPET_TBCLINICA_IDCLINICA",
                table: "TBPET",
                column: "IDCLINICA",
                principalTable: "TBCLINICA",
                principalColumn: "IDCLINICA",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBPET_TBESPECIE_IDESPECIE",
                table: "TBPET",
                column: "IDESPECIE",
                principalTable: "TBESPECIE",
                principalColumn: "IDESPECIE",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBPET_TBRACA_IDRACA",
                table: "TBPET",
                column: "IDRACA",
                principalTable: "TBRACA",
                principalColumn: "IDRACA",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBPET_TBTUTOR_IDTUTOR",
                table: "TBPET",
                column: "IDTUTOR",
                principalTable: "TBTUTOR",
                principalColumn: "IDTUTOR",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TBRACA_TBESPECIE_IDESPECIE",
                table: "TBRACA",
                column: "IDESPECIE",
                principalTable: "TBESPECIE",
                principalColumn: "IDESPECIE",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TBLEMBRETE_TBPET_IDPET",
                table: "TBLEMBRETE");

            migrationBuilder.DropForeignKey(
                name: "FK_TBPET_TBCLINICA_IDCLINICA",
                table: "TBPET");

            migrationBuilder.DropForeignKey(
                name: "FK_TBPET_TBESPECIE_IDESPECIE",
                table: "TBPET");

            migrationBuilder.DropForeignKey(
                name: "FK_TBPET_TBRACA_IDRACA",
                table: "TBPET");

            migrationBuilder.DropForeignKey(
                name: "FK_TBPET_TBTUTOR_IDTUTOR",
                table: "TBPET");

            migrationBuilder.DropForeignKey(
                name: "FK_TBRACA_TBESPECIE_IDESPECIE",
                table: "TBRACA");

            migrationBuilder.DropTable(
                name: "PJTUTORCLINICA");

            migrationBuilder.DropIndex(
                name: "IX_TBRACA_IDESPECIE",
                table: "TBRACA");

            migrationBuilder.DropIndex(
                name: "IX_TBPET_IDCLINICA",
                table: "TBPET");

            migrationBuilder.DropIndex(
                name: "IX_TBPET_IDESPECIE",
                table: "TBPET");

            migrationBuilder.DropIndex(
                name: "IX_TBPET_IDRACA",
                table: "TBPET");

            migrationBuilder.DropIndex(
                name: "IX_TBPET_IDTUTOR",
                table: "TBPET");

            migrationBuilder.DropIndex(
                name: "IX_TBLEMBRETE_IDPET",
                table: "TBLEMBRETE");
        }
    }
}
