using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TestApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialClinics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clinics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Specialty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClinicId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Clinics",
                columns: new[] { "Id", "Address", "City", "Name" },
                values: new object[,]
                {
                    { 1, "Str. Aviatorilor 21", "Bucharest", "Aurora Medical Center" },
                    { 2, "Str. Observatorului 8", "Cluj-Napoca", "Someș Health Clinic" },
                    { 3, "Bd. Carol I 40", "Iași", "Copou Family Clinic" },
                    { 4, "Str. Gheorghe Lazăr 15", "Timișoara", "Bega Medical" }
                });

            migrationBuilder.InsertData(
                table: "Doctors",
                columns: new[] { "Id", "ClinicId", "Name", "Specialty" },
                values: new object[,]
                {
                    { 1, 1, "Dr. Ana Ionescu", "Cardiology" },
                    { 2, 1, "Dr. Mihai Stan", "Dermatology" },
                    { 3, 1, "Dr. Elena Dobre", "Family medicine" },
                    { 4, 2, "Dr. Andrei Pop", "Pediatrics" },
                    { 5, 2, "Dr. Ioana Mureșan", "Ophthalmology" },
                    { 6, 3, "Dr. Radu Ciobanu", "Family medicine" },
                    { 7, 3, "Dr. Cristina Lungu", "Dermatology" },
                    { 8, 4, "Dr. Vlad Petrescu", "Orthopedics" },
                    { 9, 4, "Dr. Simona Rusu", "Cardiology" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_ClinicId",
                table: "Doctors",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Specialty",
                table: "Doctors",
                column: "Specialty");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Clinics");
        }
    }
}
