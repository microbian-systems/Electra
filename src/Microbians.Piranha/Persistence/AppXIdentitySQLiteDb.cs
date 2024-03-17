namespace Microbians.Piranha;

public class AppXIdentitySQLiteDb : Db<AppXIdentitySQLiteDb>
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="options">Configuration options</param>
    public AppXIdentitySQLiteDb(DbContextOptions<AppXIdentitySQLiteDb> options) : base(options) { }
}