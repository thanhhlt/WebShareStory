using DotNetEnv;

public class GetConnectionStringService
{
    public GetConnectionStringService()
    {
        Env.Load();
    }
    public string getConnectionString()
    {
        return Env.GetString("CONNECTIONSTRINGS");
    }
}