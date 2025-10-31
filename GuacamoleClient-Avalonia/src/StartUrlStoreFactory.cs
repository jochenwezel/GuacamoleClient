namespace GuacClient;
public static class StartUrlStoreFactory
{
    public static IStartUrlStore Create()
        => System.OperatingSystem.IsWindows()
           ? new WindowsRegistryStartUrlStore()
           : new JsonFileStartUrlStore();
}
