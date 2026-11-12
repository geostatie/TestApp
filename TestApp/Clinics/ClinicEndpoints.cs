using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TestApp.Data;

namespace TestApp.Clinics;

internal static class ClinicEndpoints
{
    public static void MapClinicEndpoints(this IEndpointRouteBuilder app)
    {
        var clinics = app.MapGroup("/clinics").WithTags("Clinics");

        // Both queries project straight into response records. EF Core then selects
        // only those columns and never tracks the rows, so there is no AsNoTracking
        // to remember and no entity can leak into a response by accident.
        clinics.MapGet("/", async (ClinicDb db, CancellationToken ct) =>
                await db.Clinics
                    .OrderBy(c => c.City).ThenBy(c => c.Name)
                    .Select(c => new ClinicSummary(c.Id, c.Name, c.City, c.Address, c.Doctors.Count))
                    .ToListAsync(ct))
            .WithName("ListClinics");

        clinics.MapGet("/{id:int}/doctors", async Task<Results<Ok<List<DoctorSummary>>, NotFound>> (
                int id, ClinicDb db, CancellationToken ct) =>
            {
                // An unknown clinic is a 404, not an empty list: "this clinic has no
                // doctors" and "there is no such clinic" are different answers.
                if (!await db.Clinics.AnyAsync(c => c.Id == id, ct))
                {
                    return TypedResults.NotFound();
                }

                var doctors = await db.Doctors
                    .Where(d => d.ClinicId == id)
                    .OrderBy(d => d.Name)
                    .Select(d => new DoctorSummary(d.Id, d.Name, d.Specialty))
                    .ToListAsync(ct);

                return TypedResults.Ok(doctors);
            })
            .WithName("ListClinicDoctors");
    }
}

internal record ClinicSummary(int Id, string Name, string City, string Address, int DoctorCount);

internal record DoctorSummary(int Id, string Name, string Specialty);
