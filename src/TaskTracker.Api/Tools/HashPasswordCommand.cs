using System.Text;
using Microsoft.AspNetCore.Identity;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Api.Tools;

/// <summary>
/// Developer helper: <c>dotnet run --project src/TaskTracker.Api -- --hash-password</c>
/// prompts for a password (input hidden) and prints the PBKDF2 hash to store in User Secrets
/// as DemoUser:PasswordHash. Runs before the web host is built, so no other configuration is needed.
/// Must be run in an interactive terminal (not the Visual Studio Package Manager Console).
/// </summary>
public static class HashPasswordCommand
{
    public const string Argument = "--hash-password";

    public static void Run()
    {
        var password = ReadHidden("Password: ");
        var confirm = ReadHidden("Confirm password: ");

        if (password.Length < 8)
        {
            Console.Error.WriteLine("Password must be at least 8 characters.");
            Environment.ExitCode = 1;
            return;
        }

        if (password != confirm)
        {
            Console.Error.WriteLine("Passwords do not match.");
            Environment.ExitCode = 1;
            return;
        }

        var hash = new PasswordHasher<DemoUserOptions>().HashPassword(new DemoUserOptions(), password);

        Console.WriteLine();
        Console.WriteLine("Store this hash with:");
        Console.WriteLine($"dotnet user-secrets set \"DemoUser:PasswordHash\" \"{hash}\" --project src/TaskTracker.Api");
    }

    private static string ReadHidden(string prompt)
    {
        Console.Write(prompt);
        var builder = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return builder.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (builder.Length > 0) builder.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                builder.Append(key.KeyChar);
            }
        }
    }
}
