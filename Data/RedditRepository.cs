using mauloader_bot.Interfaces;
using System.Diagnostics;

namespace mauloader_bot.Data
{
    public class RedditRepository : IRedditRepository
    {
        private readonly ILogger<RedditRepository> _logger;

        public RedditRepository(ILogger<RedditRepository> logger)
        {
            _logger = logger;
        }

        public bool FetchVideo(string url)
        {
            string executable = "youtube-dl";
            string filePath = @"Files\Reddit\reddit.mp4";

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            string s = RunProcess(executable, $" {url} -o {filePath}");

            _logger.LogInformation(s);

            return true;
        }

        public bool CompressVideo()
        {
            string executable = "ffmpeg";
            string filePath = @"Files\Reddit\reddit.mp4";
            string outputPath = @"Files\Reddit\reddit_compressed.mp4";

            string s = RunProcess(executable, $" -i {filePath} -vcodec libx265 -crf 28 -tune fastdecode -preset ultrafast -threads:v 1 -y {outputPath}");

            File.Delete(filePath);
            File.Move(outputPath, filePath);

            _logger.LogInformation(s);

            return true;
        }

        private static string RunProcess(string executable, string arguments)
        {
            Process process = new Process();

            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();

            string output = "";

            while (!process.HasExited)
            {
                output += process.StandardOutput.ReadToEnd();
            }

            return output;

        }
    }
}
