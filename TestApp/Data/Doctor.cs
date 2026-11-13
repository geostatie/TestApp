namespace TestApp.Data;

public sealed class Doctor
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // Free text for now. It becomes its own table once patients search and
    // filter by specialty, which is when the list of values needs to be fixed.
    public required string Specialty { get; set; }

    public int ClinicId { get; set; }
    public Clinic? Clinic { get; set; }
}
