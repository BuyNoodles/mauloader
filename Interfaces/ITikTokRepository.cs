using mauloader_bot.Entities;

namespace mauloader_bot.Interfaces
{
    public interface ITikTokRepository
    {
        Task<TikTok> FetchVideo(string url);
        Task<bool> DownloadVideo(string url, string fileName);
    }
}
