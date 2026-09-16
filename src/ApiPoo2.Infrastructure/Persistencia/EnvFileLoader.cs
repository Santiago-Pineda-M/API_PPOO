namespace ApiPoo2.Infrastructure.Persistencia;

public static class EnvFileLoader
{
    public const string EnvFileName = ".env";

    public static void LoadFromRepositoryRoot()
    {
        var root = FindRepositoryRoot(AppContext.BaseDirectory);

        if (root is not null)
        {
            DotNetEnv.Env.Load(Path.Combine(root, EnvFileName));
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