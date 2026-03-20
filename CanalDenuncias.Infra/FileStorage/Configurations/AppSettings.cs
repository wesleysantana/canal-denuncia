namespace CanalDenuncias.Infra.Data.Configurations;

public sealed record AppSettings
{
    public string PathFileStorage { get; set; } = default!;
}