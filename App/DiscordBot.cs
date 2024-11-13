using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Audio;
using NAudio.Wave;
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

            Youtube.apiKey = (string)JsonFileHandler.Read<Config>("JsonDatas/config.json").YoutubeToken;

            await client.LoginAsync(TokenType.Bot, (string)JsonFileHandler.Read<Config>("JsonDatas/config.json").DiscordToken);
            await client.StartAsync();

            await commands.AddModuleAsync<CommandModules>(service);

            client.MessageReceived += CommandAsync;
            client.Log += DiscordLog;

            //안꺼지게
            await Task.Delay(-1);
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
            string curPath = $"Audio/{PlayList.Instance.GetPath()}.mp3";
            Console.WriteLine("현재 곡 위치 : " + curPath);

            if (!File.Exists(curPath))
            {
                Console.WriteLine("경로 오류");
            }
            else
            {
                using (var reader = new MediaFoundationReader(curPath))
                using (var output = audioClient.CreatePCMStream(AudioApplication.Music))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;

                    while (nowPlaying && (bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, bytesRead);
                    }

                    await output.FlushAsync();
                }
            }


            nowPlaying = false;

            if (PlayList.Instance.GetListCount() > 0)
            {
                await PlayMusic();
            }

            return;
        }
        public static void Skip()
        {
            nowPlaying = false;
        }
    }

}
