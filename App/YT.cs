using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Google.Apis.Services;
using System.Net;
using Google.Apis.YouTube.v3;
using System.Text.RegularExpressions;

namespace App
{
    using Google.Apis.YouTube.v3.Data;
    public class YT
    {
        private static YouTubeService youtubeService;
        public static void SetApi(string key)
        {
            youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = key,
                ApplicationName = "DisBo"
            });
        }
        public static async Task<Video> GetVideoAsync(string str)
        {
            if (str.Contains("http://") || str.Contains("https://"))
            {
                return await SearchUrl(str);
            }
            else
            {
                return await SearchStr(str);
            }
        }
        public static async Task<List<Video>> GetListVideoAsync(string url)
        {
            string nextPageToken = null;
            List<Video> videos = new List<Video>();
            string listId = Url2ListId(url);

            do
            {
                var playlistRequest = youtubeService.PlaylistItems.List("snippet");
                playlistRequest.PlaylistId = listId;
                playlistRequest.PageToken = nextPageToken;

                var playlistResponse = playlistRequest.Execute();

                foreach (var playlistItem in playlistResponse.Items)
                {
                    string videoId = playlistItem.Snippet.ResourceId.VideoId;
                    string videoUrl = $"https://www.youtube.com/watch?v={videoId}";

                    videos.Add(await SearchUrl(videoUrl));
                }

                // Get the next page token if available
                nextPageToken = playlistResponse.NextPageToken;

            } while (nextPageToken != null); // Continue if there's another page of videos

            return videos;
        }
        private static async Task<Video> SearchUrl(string url)
        {
            var videoId = Url2VideoId(url);

            var searchRequest = youtubeService.Videos.List("snippet");
            searchRequest.MaxResults = 1;
            searchRequest.Id = videoId;

            var searchResponse = await searchRequest.ExecuteAsync();

            if (searchResponse.Items.Count > 0)
            {
                var videos = searchResponse.Items.OfType<Video>().ToList();
                videos[0].Snippet.Title = ReplaceInvalidFileNameChars(videos[0].Snippet.Title);
                return videos[0];
            }
            else
            {
                return null;
            }
        }
        private static async Task<Video> SearchStr(string str)
        {
            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.Q = str;
            searchRequest.MaxResults = 1;
            searchRequest.Type = "video";
            //searchRequest.Order = SearchResource.ListRequest.OrderEnum.ViewCount;

            var searchResponse = await searchRequest.ExecuteAsync();

            if (searchResponse.Items.Count > 0)
            {
                var videoId = searchResponse.Items[0].Id.VideoId;

                var videoRequest = youtubeService.Videos.List("snippet,statistics");
                videoRequest.Id = videoId;

                var videoResponse = await videoRequest.ExecuteAsync();

                if (videoResponse.Items.Count > 0)
                {
                    videoResponse.Items[0].Snippet.Title = ReplaceInvalidFileNameChars(videoResponse.Items[0].Snippet.Title);
                    return videoResponse.Items[0];
                }
            }

            return null;  // 비디오가 없으면 null 반환
        }
        public static string Url2VideoId(string url)
        {
            string pattern = @"(?:https?://(?:www\.)?youtube\.com/watch\?v=|https?://(?:www\.)?youtu\.be/)([^&?/]+)";
            Match match = Regex.Match(url, pattern);

            return match.Success ? match.Groups[1].Value : null;
        }
        public static string Url2ListId(string url)
        {
            Regex regex = new Regex(@"(?:list=)([a-zA-Z0-9_-]+)");
            Match match = regex.Match(url);

            return match.Success ? match.Groups[1].Value : null;  // playlistId가 없으면 null 반환
        }

        public static async Task<bool> DownloadMp3(Video video)
        {
            int count = 10;
            while (count-- > 0)
            {
                try
                {
                    var handler = new HttpClientHandler
                    {
                        CookieContainer = new CookieContainer()
                    };

                    var httpClient = new HttpClient(handler);
                    var response = await httpClient.GetAsync("https://www.youtube.com");
                    var cookies = handler.CookieContainer.GetCookies(new Uri("https://www.youtube.com"));
                    var youtube = new YoutubeClient(httpClient);

                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                    var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                    if (audioStreamInfo != null)
                    {
                        var stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo);
                        string path = video.Snippet.Title;
                        Console.WriteLine($"bitrate : {audioStreamInfo.Bitrate}");
                        var opusFilePath = $"Audio/{path}.opus"; // Opus 파일로 바로 저장

                        // Opus로 다운로드
                        using (var fileStream = new FileStream(opusFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await stream.CopyToAsync(fileStream);
                        }

                        Console.WriteLine($"Audio saved as Opus to {opusFilePath}");
                    }
                    else
                    {
                        Console.WriteLine("No audio stream found.");
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading or converting audio: {ex.Message}");
                    Task.Delay(5000);
                }
            }

            return false;
        }

        public static string ReplaceInvalidFileNameChars(string path)
        {
            // 파일 경로에서 사용할 수 없는 문자들
            string invalidChars = new string(System.IO.Path.GetInvalidFileNameChars());

            // 유효하지 않은 문자를 _로 대체
            foreach (char c in invalidChars)
            {
                path = path.Replace(c, ' ');
            }

            return path;
        }

        // static void ConvertMp3ToOpus(string mp3FilePath, string opusFilePath, int targetSampleRate)
        // {
        //     try
        //     {
        //         // FFmpeg을 호출하여 샘플 레이트 변환 및 Opus로 변환
        //         string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe"; // FFmpeg 경로 설정
        //         string arguments = $"-i \"{mp3FilePath}\" -c:a libopus -ar {targetSampleRate} -b:a 128k -ac 2 \"{opusFilePath}\"";
        //         // -c:a libopus : Opus 코덱 사용, -ar {targetSampleRate} : 샘플 레이트 설정, -b:a 128k : 비트레이트 설정, -ac 2 : 스테레오

        //         var processStartInfo = new ProcessStartInfo
        //         {
        //             FileName = ffmpegPath,
        //             Arguments = arguments,
        //             RedirectStandardOutput = true,
        //             RedirectStandardError = true,
        //             UseShellExecute = false,
        //             CreateNoWindow = true
        //         };

        //         using (var process = Process.Start(processStartInfo))
        //         {
        //             string output = process.StandardOutput.ReadToEnd();
        //             string error = process.StandardError.ReadToEnd();
        //             process.WaitForExit();

        //             if (process.ExitCode != 0)
        //             {
        //                 Console.WriteLine($"Error converting to Opus: {error}");
        //             }
        //             else
        //             {
        //                 Console.WriteLine($"Converted to Opus: {opusFilePath}");
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Error converting audio: {ex.Message}");
        //     }
        // }
    }

}
