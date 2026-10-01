using AcademicSurveySystem.Application.Identity.UserPasswordReset;

namespace AcademicSurveySystem.Api.Maintenance;

public sealed class UserPasswordResetCommand
{
    private readonly IUserPasswordResetService _passwordResetService;

    public UserPasswordResetCommand(IUserPasswordResetService passwordResetService)
    {
        _passwordResetService = passwordResetService;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        Console.Write("Email: ");
        var email = Console.ReadLine()?.Trim();

        Console.Write("New password: ");
        var newPassword = ReadHiddenConsoleInput();

        Console.Write("Confirm password: ");
        var confirmedPassword = ReadHiddenConsoleInput();

        if (!PasswordsMatch(newPassword, confirmedPassword))
        {
            Console.WriteLine("Password reset failed. Passwords do not match.");
            return 1;
        }

        var result = await _passwordResetService.ResetAsync(
            email,
            newPassword,
            cancellationToken);

        Console.WriteLine(result.Message);

        return result.Succeeded ? 0 : 1;
    }

    public static bool PasswordsMatch(string? newPassword, string? confirmedPassword)
    {
        return string.Equals(newPassword, confirmedPassword, StringComparison.Ordinal);
    }

    public static string ReadHiddenConsoleInput()
    {
        var input = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(input.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Count > 0)
                {
                    input.RemoveAt(input.Count - 1);
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                input.Add(key.KeyChar);
            }
        }
    }
}
