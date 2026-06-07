using System.Reflection;
using DbUp;

return RunMigrations(args);

static int RunMigrations(string[] args)
{
    var connectionString = GetConnectionString(args);
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.Error.WriteLine("Usage: jnpf-migrate --connection \"<connection-string>\"");
        return -1;
    }

    EnsureDatabase.For.SqlDatabase(connectionString);

    var upgrader = DeployChanges.To
        .SqlDatabase(connectionString)
        .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
        .WithTransactionPerScript()
        .LogToConsole()
        .Build();

    if (upgrader.IsUpgradeRequired())
    {
        Console.WriteLine("Migrations required. Executing...");
    }
    else
    {
        Console.WriteLine("No new scripts to execute.");
        return 0;
    }

    var result = upgrader.PerformUpgrade();

    if (!result.Successful)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Migration failed: {result.Error}");
        Console.ResetColor();
        return -1;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Migration completed successfully.");
    Console.ResetColor();
    return 0;
}

static string? GetConnectionString(string[] args)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--connection" or "-c")
            return args[i + 1];
    }

    return Environment.GetEnvironmentVariable("JNPF_CONNECTION_STRING");
}
