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
using System.Text.RegularExpressions;
using NAudio.Wave;
using System.Diagnostics;
using System.Net;

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

        public static async Task<bool> DownloadMp3(string url)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer()
                };

                // HttpClient 인스턴스를 생성하여 쿠키 관리
                var httpClient = new HttpClient(handler);

                // YouTube 페이지를 요청하여 쿠키를 발급받습니다.
                var response = await httpClient.GetAsync("https://www.youtube.com");

                // 쿠키 발급 확인
                var cookies = handler.CookieContainer.GetCookies(new Uri("https://www.youtube.com"));
                foreach (Cookie cookie in cookies)
                {
                    Console.WriteLine($"Cookie: {cookie.Name} = {cookie.Value}");
                }

                // YouTube 클라이언트 초기화 및 쿠키 설정
                var youtube = new YoutubeClient(httpClient);

                // 영상 정보 가져오기
                var video = await youtube.Videos.GetAsync(url);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (audioStreamInfo != null)
                {
                    // 오디오 스트림 다운로드
                    var stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo);
                    string path = video.Title;

                    // 파일명에 유효하지 않은 문자 제거
                    path = ReplaceInvalidFileNameChars(path);

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading or converting audio: {ex.Message}");
                return false;
            }
            return true;
        }
        static void ConvertMp3ToOpus(string mp3FilePath, string opusFilePath, int targetSampleRate)
        {
            try
            {
                // FFmpeg을 호출하여 샘플 레이트 변환 및 Opus로 변환
                string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe"; // FFmpeg 경로 설정
                string arguments = $"-i \"{mp3FilePath}\" -c:a libopus -ar {targetSampleRate} -b:a 128k -ac 2 \"{opusFilePath}\"";
                // -c:a libopus : Opus 코덱 사용, -ar {targetSampleRate} : 샘플 레이트 설정, -b:a 128k : 비트레이트 설정, -ac 2 : 스테레오

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"Error converting to Opus: {error}");
                    }
                    else
                    {
                        Console.WriteLine($"Converted to Opus: {opusFilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting audio: {ex.Message}");
            }
        }

        public static async Task<List<string>> GetVideoUrlsFromPlaylist(string playlistId)
        {
            // YouTube 서비스 초기화
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "YouTubeSearchApp"
            });
            var videoUrls = new List<string>();
            string nextPageToken = null;

            do
            {
                // Fetch the playlist items
                var playlistRequest = youtubeService.PlaylistItems.List("snippet");
                playlistRequest.PlaylistId = playlistId;
                playlistRequest.PageToken = nextPageToken;

                var playlistResponse = playlistRequest.Execute();

                // Add the video URLs to the list
                foreach (var playlistItem in playlistResponse.Items)
                {
                    string videoUrl = $"https://www.youtube.com/watch?v={playlistItem.Snippet.ResourceId.VideoId}";
                    videoUrls.Add(videoUrl);
                }

                // Get the next page token if available
                nextPageToken = playlistResponse.NextPageToken;

            } while (nextPageToken != null); // Keep going if there's another page of videos

            return videoUrls;
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

    }

}
