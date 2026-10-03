using Npgsql;

namespace AgentFlow.Api.Data;

public class Db
{
    private readonly string _cs;

    public Db(IConfiguration cfg)
    {
        _cs = cfg.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException(
                  "Missing ConnectionStrings:DefaultConnection. " +
                  "Add it to appsettings.Development.json.");
    }

    public NpgsqlConnection Open()
    {
        var c = new NpgsqlConnection(_cs);
        c.Open();
        return c;
    }
}