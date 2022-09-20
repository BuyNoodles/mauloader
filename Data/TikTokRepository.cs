using Mauloader_bot.Entities;
using System.Text.Json;

namespace Mauloader_bot.Data
{
    public class TikTokRepository
    {
        private static readonly string _apiUrl = "https://api.tikmate.app/api/lookup";
        private static readonly string _downloadUrl = "https://tikmate.app/download";
        private static readonly string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36";

        public static async Task<TikTok> FetchVideo(string url)
        {
            var parameters = new Dictionary<string, string> { { "url", url } };
            var encodedContent = new FormUrlEncodedContent(parameters);

            HttpClient req = new HttpClient();

            var msg = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            msg.Headers.Add("User-Agent", _userAgent);
            msg.Content = encodedContent;

            var response = await req.SendAsync(msg);
            string content = await response.Content.ReadAsStringAsync();

            TikTok? tiktok = JsonSerializer.Deserialize<TikTok>(content);

            tiktok.DownloadLink = $"{_downloadUrl}/{tiktok.Token}/{tiktok.Id}.mp4";

            return tiktok;
        }

        public static async Task<bool> DownloadVideo(string url, string fileName)
        {
            string file = $"Files/{fileName}.mp4";

            if (File.Exists(file)) return false;

            HttpClient req = new HttpClient();
            var msg = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            msg.Headers.Add("User-Agent", _userAgent);

            using (var stream = await req.GetStreamAsync(url))
            {
                using (var fs = new FileStream(file, FileMode.CreateNew))
                {
                    await stream.CopyToAsync(fs);
                }
            }

            return true;
        }
    }
}
