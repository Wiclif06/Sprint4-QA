using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetJourneyTutorApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeciesAndBreed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "IDESPECIE",
                table: "TBPET",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TBESPECIE",
                columns: table => new
                {
                    IDESPECIE = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NMESPECIE = table.Column<string>(type: "NVARCHAR2(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBESPECIE", x => x.IDESPECIE);
                });

            migrationBuilder.CreateTable(
                name: "TBRACA",
                columns: table => new
                {
                    IDRACA = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NMRACA = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    IDESPECIE = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TBRACA", x => x.IDRACA);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TBESPECIE");

            migrationBuilder.DropTable(
                name: "TBRACA");

            migrationBuilder.AlterColumn<int>(
                name: "IDESPECIE",
                table: "TBPET",
                type: "NUMBER(10)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");
        }
    }
}
