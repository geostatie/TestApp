using Microsoft.EntityFrameworkCore;

namespace TestApp.Data;

/// <summary>
/// Example clinics and doctors, all fictional. They are part of the model, so a
/// migration inserts them and every environment starts with the same rows.
/// </summary>
/// <remarks>
/// HasData needs fixed ids, and changing a row here produces a migration that
/// updates it. That suits reference data; real user data never goes here.
/// </remarks>
internal static class SeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Clinic>().HasData(
            new Clinic { Id = 1, Name = "Aurora Medical Center", City = "Bucharest", Address = "Str. Aviatorilor 21" },
            new Clinic { Id = 2, Name = "Someș Health Clinic", City = "Cluj-Napoca", Address = "Str. Observatorului 8" },
            new Clinic { Id = 3, Name = "Copou Family Clinic", City = "Iași", Address = "Bd. Carol I 40" },
            new Clinic { Id = 4, Name = "Bega Medical", City = "Timișoara", Address = "Str. Gheorghe Lazăr 15" });

        modelBuilder.Entity<Doctor>().HasData(
            new Doctor { Id = 1, ClinicId = 1, Name = "Dr. Ana Ionescu", Specialty = "Cardiology" },
            new Doctor { Id = 2, ClinicId = 1, Name = "Dr. Mihai Stan", Specialty = "Dermatology" },
            new Doctor { Id = 3, ClinicId = 1, Name = "Dr. Elena Dobre", Specialty = "Family medicine" },
            new Doctor { Id = 4, ClinicId = 2, Name = "Dr. Andrei Pop", Specialty = "Pediatrics" },
            new Doctor { Id = 5, ClinicId = 2, Name = "Dr. Ioana Mureșan", Specialty = "Ophthalmology" },
            new Doctor { Id = 6, ClinicId = 3, Name = "Dr. Radu Ciobanu", Specialty = "Family medicine" },
            new Doctor { Id = 7, ClinicId = 3, Name = "Dr. Cristina Lungu", Specialty = "Dermatology" },
            new Doctor { Id = 8, ClinicId = 4, Name = "Dr. Vlad Petrescu", Specialty = "Orthopedics" },
            new Doctor { Id = 9, ClinicId = 4, Name = "Dr. Simona Rusu", Specialty = "Cardiology" });
    }
}
