namespace DAB.Data.Interfaces;

public interface IAnnouncementSink
{
    public Task SaveAsync(ulong userId, Stream data);

    public Task<Stream> LoadAsync(ulong userId);

    public Task<bool> UserHasDataAsync(ulong userId);
}
