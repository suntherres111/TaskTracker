using System.Text;
using Microsoft.AspNetCore.Identity;
using TaskTracker.Infrastructure.Auth;

namespace TaskTracker.Api.Tools;

/// <summary>
/// Developer helper: <c>dotnet run --project src/TaskTracker.Api -- --hash-password Admin</c>
/// (or <c>User</c>) prompts for a password (input hidden) and prints the command that stores its
/// PBKDF2 hash in User Secrets as DemoUsers:{Role}:PasswordHash. Runs before the web host is built,
/// so no other configuration is needed.
/// Must be run in an interactive terminal (not the Visual Studio Package Manager Console).
/// </summary>
public static class HashPasswordCommand
{
    public const string Argument = "--hash-password";

    public static void Run(string[] args)
    {
        var index = Array.IndexOf(args, Argument);
        var role = AppRoles.Normalize(index >= 0 && index + 1 < args.Length ? args[index + 1] : null);
        if (role is null)
        {
            Console.Error.WriteLine($"Usage: dotnet run --project src/TaskTracker.Api --no-launch-profile -- {Argument} <{string.Join("|", AppRoles.All)}>");
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine($"Setting the password for the {role} demo account.");
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
        Console.WriteLine($"dotnet user-secrets set \"{DemoUserOptions.SectionName}:{role}:PasswordHash\" \"{hash}\" --project src/TaskTracker.Api");
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
