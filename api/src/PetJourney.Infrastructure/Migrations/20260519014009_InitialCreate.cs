using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetJourneyTutorApi.Migrations
{
    /// <inheritdoc />
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(PetJourneyTutorApi.Data.AppDbContext))]
    [Migration("20260519014009_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TBCLINICA",
                columns: table => new
                {
                    IDCLINICA = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NMCLINICA = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    DSENDERECO = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    NRTELEFONE = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    DSEMAIL = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: true),
                    DSSTATUS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBCLINICA", x => x.IDCLINICA);
                });

            migrationBuilder.CreateTable(
                name: "TBLEMBRETE",
                columns: table => new
                {
                    IDLEMBRETE = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    IDPET = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DSTIPO = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    DSDESCRICAO = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: false),
                    DTLEMBRETE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DTNOTIFICADO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    DSSTATUS = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false, defaultValue: "PENDENTE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBLEMBRETE", x => x.IDLEMBRETE);
                });

            migrationBuilder.CreateTable(
                name: "TBPET",
                columns: table => new
                {
                    IDPET = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NMPET = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    DTNASCIMENTO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    DSSEXO = table.Column<string>(type: "NVARCHAR2(1)", maxLength: 1, nullable: false),
                    IDTUTOR = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    IDESPECIE = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    IDRACA = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    IDCLINICA = table.Column<int>(type: "NUMBER(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBPET", x => x.IDPET);
                });

            migrationBuilder.CreateTable(
                name: "TBTUTOR",
                columns: table => new
                {
                    IDTUTOR = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NMTUTOR = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    DSEMAIL = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    NRTELEFONE = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    DSPLANO = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true, defaultValue: "FREE"),
                    DTCADASTRO = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBTUTOR", x => x.IDTUTOR);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TBTUTOR_DSEMAIL",
                table: "TBTUTOR",
                column: "DSEMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBCLINICA");

            migrationBuilder.DropTable(
                name: "TBLEMBRETE");

            migrationBuilder.DropTable(
                name: "TBPET");

            migrationBuilder.DropTable(
                name: "TBTUTOR");
        }
    }
}
