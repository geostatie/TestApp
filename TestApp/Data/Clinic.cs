namespace TestApp.Data;

public sealed class Clinic
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string City { get; set; }
    public required string Address { get; set; }

    public List<Doctor> Doctors { get; set; } = [];
}
