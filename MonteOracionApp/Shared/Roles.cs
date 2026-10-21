namespace MonteOracionApp.Shared;
public static class Roles
{
    public const string Administrator = "Administrador";
    public const string Treasurer = "Tesorero";
    public const string Warehouse = "Almacenero";
    public const string Staff = Administrator + "," + Treasurer + "," + Warehouse;
    public const string Finance = Administrator + "," + Treasurer;
    public const string Stock = Administrator + "," + Warehouse;
}
