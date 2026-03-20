using System.Net.Mail;

namespace CanalDenuncias.Domain.Utils;

public static class DomainValidator
{
    public static bool CPFValidator(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        // Remove caracteres não numéricos
        cpf = new string(cpf.Where(char.IsDigit).ToArray());

        // O CPF deve ter 11 dígitos e não pode ser uma sequência repetida (ex: 111.111.111-11)
        if (cpf.Length != 11 || InvalidSequence(cpf)) return false;

        // Cálculo do primeiro dígito
        int soma = 0;
        int[] peso1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        for (int i = 0; i < 9; i++)
            soma += int.Parse(cpf[i].ToString()) * peso1[i];

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;

        if (digito1 != int.Parse(cpf[9].ToString())) return false;

        // Cálculo do segundo dígito
        soma = 0;
        int[] peso2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        for (int i = 0; i < 10; i++)
            soma += int.Parse(cpf[i].ToString()) * peso2[i];

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;

        return digito2 == int.Parse(cpf[10].ToString());
    }

    private static bool InvalidSequence(string cpf)
    {
        // CPFs com todos os números iguais são inválidos, apesar de passarem no cálculo matemático
        string[] invalidos = {
            "00000000000", "11111111111", "22222222222", "33333333333", "44444444444",
            "55555555555", "66666666666", "77777777777", "88888888888", "99999999999"
        };
        return invalidos.Contains(cpf);
    }

    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);

            // MailAddress aceita emails sem domínio (ex: "user@localhost"),
            // então verificamos se há um ponto no email
            return addr.Address == email && email.Contains('.');
        }
        catch
        {
            return false;
        }
    }
}