namespace mauloader_bot.Interfaces
{
    public interface IRedditRepository
    {
        bool FetchVideo(string url);
        bool CompressVideo();
    }
}
