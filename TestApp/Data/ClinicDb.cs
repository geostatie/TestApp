using Microsoft.EntityFrameworkCore;

namespace TestApp.Data;

public sealed class ClinicDb(DbContextOptions<ClinicDb> options) : DbContext(options)
{
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Doctor> Doctors => Set<Doctor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Clinic>(clinic =>
        {
            clinic.Property(c => c.Name).HasMaxLength(200);
            clinic.Property(c => c.City).HasMaxLength(100);
            clinic.Property(c => c.Address).HasMaxLength(300);

            // Doctors belong to exactly one clinic. Restrict, not cascade: deleting
            // a clinic that still has doctors is a mistake to surface, not to hide.
            clinic.HasMany(c => c.Doctors)
                .WithOne(d => d.Clinic)
                .HasForeignKey(d => d.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Doctor>(doctor =>
        {
            doctor.Property(d => d.Name).HasMaxLength(200);
            doctor.Property(d => d.Specialty).HasMaxLength(100);

            // Patients will look doctors up by specialty first.
            doctor.HasIndex(d => d.Specialty);
        });

        SeedData.Apply(modelBuilder);
    }
}
