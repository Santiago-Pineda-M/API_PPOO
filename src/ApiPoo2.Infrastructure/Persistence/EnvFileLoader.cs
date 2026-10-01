namespace ApiPoo2.Infrastructure.Persistence;

public static class EnvFileLoader
{
    public const string EnvFileName = ".env";
    public const string TestEnvFileName = ".env.test";

    public static void LoadFromRepositoryRoot()
    {
        Load(EnvFileName);
    }

    public static void LoadTestEnvironment()
    {
        Load(TestEnvFileName);
        OverwriteProcessEnvironment(TestEnvFileName);
    }

    private static void OverwriteProcessEnvironment(string fileName)
    {
        // DotNetEnv no sobrescribe variables ya existentes. Para garantizar que las pruebas
        // usen Docker y nunca Neon, las claves del archivo de pruebas se aplican por fuerza.
        var root = FindRepositoryRoot(AppContext.BaseDirectory);
        if (root is null)
        {
            return;
        }

        var path = Path.Combine(root, fileName);
        if (!File.Exists(path))
        {
            return;
        }

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = trimmed[..separator].Trim();
            var value = trimmed[(separator + 1)..].Trim().Trim('"', '\'');
            if (key.Length > 0)
            {
                Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
            }
        }
    }

    private static void Load(string fileName)
    {
        var root = FindRepositoryRoot(AppContext.BaseDirectory);

        if (root is not null)
        {
            var path = Path.Combine(root, fileName);
            if (File.Exists(path))
            {
                DotNetEnv.Env.Load(path);
            }
        }
    }

    private static string? FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, EnvFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
