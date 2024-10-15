using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
namespace App
{
    public static class Youtube
    {
        public static async Task<Song> SearchURL(string url)
        {
            var youtube = new YoutubeClient();
            var data = await youtube.Videos.GetAsync(url);
            if (data == null) return null;

            string title = data.Title;
            string author = data.Author.ChannelTitle;
            TimeSpan duration = data.Duration ?? TimeSpan.Zero;

            return new Song(title, url, author, duration);
        }
        public static async Task<Song> SearchTitle(string query)
        {
            var youtube = new YoutubeClient();

            var videos = await youtube.Search.GetVideosAsync(query);
            Console.WriteLine("find count : "+videos.Count());
            foreach (var data in videos)
            {
                string title = data.Title;
                string url = data.Url;
                string author = data.Author.ChannelTitle;
                TimeSpan duration = data.Duration ?? TimeSpan.Zero;
                return new Song(title, url, author, duration);
            }
            return null;
        }
        public static async Task DownloadMp3(string url)
        {
            try
            {
                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(url);
                
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (audioStreamInfo != null)
                {
                    var stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo);
                    string path = video.Title;
                    path = path.Replace('/', '-');
                    var outputFilePath = $"Audio/{path}.mp3";
                    using (var httpClient = new HttpClient())
                    using (var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                    Console.WriteLine($"Audio saved to {outputFilePath}");
                }
                else
                {
                    Console.WriteLine("No audio stream found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading MP3: {ex.Message}");
            }
        }


    }

}