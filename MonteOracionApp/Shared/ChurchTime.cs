namespace MonteOracionApp.Shared;

public static class ChurchTime
{
    public static DateTime Now => DateTime.UtcNow.AddHours(-5);
    public static DateTime Today => Now.Date;
}
