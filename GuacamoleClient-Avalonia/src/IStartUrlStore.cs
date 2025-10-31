namespace GuacClient;
public interface IStartUrlStore
{
    string? Load();
    void Save(string url);
    void Delete();
}
