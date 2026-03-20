using Bogus;
using CanalDenuncias.Domain.Entities;

namespace CanalDenuncias.Tests.Domain.Fixtures;

[CollectionDefinition(nameof(UsuarioFixture))]
public class UsuarioTestFixtureCollection : ICollectionFixture<UsuarioFixture>
{
}

public class UsuarioFixture
{
    public Faker _faker { get; } = new("pt_BR");

    public const string CpfValido = "52998224725";

    public Usuario CreateValidUsuario(
        string? nome = null,
        string? telefone = null,
        string? email = null,
        string? cpf = null)
    {
        return new Usuario(
            nome: nome ?? _faker.Name.FullName(),
            telefone: telefone ?? _faker.Random.ReplaceNumbers("###########"), // 11 dígitos
            email: email ?? _faker.Internet.Email(),
            cPF: cpf ?? CpfValido
        );
    }

    public string CreateStringComTamanho(int length, char c = 'a') => new(c, length);
}