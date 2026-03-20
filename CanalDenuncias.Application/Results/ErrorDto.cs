namespace CanalDenuncias.Application.Results;

public sealed record ErrorDto(string Code, string Message, string? Target = null);