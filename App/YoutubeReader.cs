using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;


namespace App
{

    public static class Youtube
    {
        public static string apiKey = string.Empty; // 발급받은 API 키를 여기에 입력

        public static async Task<string> GetFirstVideoUrlAsync(string query)
        {
            // YouTube 서비스 초기화
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "YouTubeSearchApp"
            });

            // 검색 요청 생성
            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.Q = query; // 검색어 설정
            searchRequest.MaxResults = 1; // 첫 번째 결과만 가져옴
            searchRequest.Type = "video"; // 비디오만 검색

            // 검색 요청 실행
            var searchResponse = await searchRequest.ExecuteAsync();

            // 첫 번째 영상의 URL 가져오기
            if (searchResponse.Items.Count > 0)
            {
                var videoId = searchResponse.Items[0].Id.VideoId;
                var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
                return videoUrl;
            }
            else
            {
                return null;
            }
        }

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
            string url = await GetFirstVideoUrlAsync(query);

            if (url != null)
            {
                return await SearchURL(url);
            }
            else return null;
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