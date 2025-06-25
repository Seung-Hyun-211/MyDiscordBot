using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Audio;
using System.Diagnostics;

namespace App
{
    class DiscordBot
    {
        internal DiscordSocketClient client;
        public static IAudioClient? audioClient;
        internal IVoiceChannel voiceChannel;

        private CommandService commands;
        private IServiceProvider service;
        public static bool nowPlaying;
        public static SocketCommandContext lastContext;
        public Queue<Action> requestQueue = new Queue<Action>();
        public static bool repeat;
        public async Task StartBotAsync()
        {
            nowPlaying = false;
            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates,
                HandlerTimeout = 10000
            });
            commands = new CommandService(new CommandServiceConfig() { LogLevel = LogSeverity.Verbose });

            YT.SetApi((string)JsonFileHandler.Read<Config>("JsonDatas/config.json").YoutubeToken);

            await client.LoginAsync(TokenType.Bot, (string)JsonFileHandler.Read<Config>("JsonDatas/config.json").DiscordToken);
            await client.StartAsync();

            await commands.AddModuleAsync<CommandModules>(service);

            client.MessageReceived += CommandAsync;
            client.Log += DiscordLog;

            //안꺼지게
            
            while(true)
            {
                await Task.Delay(1000);
                if(requestQueue.Count > 0)
                    requestQueue.Dequeue().Invoke();
            }
            repeat = false;
        }
        private async Task CommandAsync(SocketMessage msg)
        {
            Console.WriteLine("get msg");
            var userMsg = msg as SocketUserMessage;
            var context = new SocketCommandContext(client, userMsg);
            if (userMsg == null || userMsg.Author.IsBot) return;

            int argPos = 0;
            if (userMsg.HasStringPrefix("!", ref argPos))
            {
                var result = await commands.ExecuteAsync(context, argPos, service);
                if (!result.IsSuccess) Console.WriteLine("error ... ?");
            }
        }
        private static Task DiscordLog(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }
        public static async Task PlayMusic()
        {
            if (nowPlaying) return;
            nowPlaying = true;

            string curPath = $"Audio/{PlayList.Instance.GetPath(repeat)}.opus"; // Opus 파일 경로
            Console.WriteLine("현재 곡 위치 : " + curPath);

            if (!File.Exists(curPath))
            {
                Console.WriteLine("경로 오류");
            }
            else
            {
                try
                {
                    // Opus -> PCM 변환을 위해 임시 PCM 파일 생성
                    string pcmFilePath = $"{curPath}.pcm";  // PCM 파일 경로

                    // FFmpeg을 사용하여 Opus -> PCM 변환하고 메모리 스트림으로 처리
                    using (var pcmStream = ConvertOpusToPcm(curPath))
                    {
                        // PCM 스트리밍을 Discord로 전달
                            using (var output = audioClient.CreatePCMStream(AudioApplication.Music))
                            {
                                    byte[] buffer = new byte[4096];
                                    int bytesRead;

                                    while (nowPlaying && (bytesRead = await pcmStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        await output.WriteAsync(buffer, 0, bytesRead);

                                        if (bytesRead < buffer.Length)
                                        {
                                            await Task.Delay(20);
                                        }
                                    }

                                    await output.FlushAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error playing Opus file: {ex.Message}");
                }
            }

            nowPlaying = false;

            if (PlayList.Instance.GetListCount() > 0)
            {
                await PlayMusic();  // 다음 곡 재생
            }

            return;
        }

        // Opus -> PCM 변환 함수 (메모리 스트림 사용)
        static Stream ConvertOpusToPcm(string opusFilePath)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe",  // FFmpeg 경로
                    Arguments = $"-i \"{opusFilePath}\" -ar 48000 -f s16le -ac 2 pipe:1", // pipe:1로 출력 스트림을 직접 받아옴
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    throw new Exception("FFmpeg process failed to start.");
                }

                // FFmpeg에서 출력된 PCM 데이터를 메모리 스트림으로 받아옴
                var memoryStream = new MemoryStream();
                process.StandardOutput.BaseStream.CopyTo(memoryStream);
                memoryStream.Position = 0;  // 스트림 처음으로 리셋

                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Error converting Opus to PCM: {process.StandardError.ReadToEnd()}");
                }

                return memoryStream;  // 메모리 스트림 반환
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Opus to PCM: {ex.Message}");
                throw;
            }
        }
        public static void Skip()
        {
            nowPlaying = false;
        }
    }

}
