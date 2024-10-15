using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Audio;
using NAudio.Wave;

using Microsoft.Extensions.DependencyInjection;


using Newtonsoft.Json;
using Discord.Rest;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AngleSharp.Common;
using System.Security.Authentication.ExtendedProtection;
using YoutubeExplode.Playlists;
using System.Net.Security;

namespace App
{
    class DiscordBot
    {
        internal const string SongsPath = "JsonDatas/Songs.json";
        internal DiscordSocketClient client;
        public static IAudioClient? audioClient;
        internal IVoiceChannel voiceChannel;


        private const string Token = "";
        private CommandService commands;
        private IServiceProvider service;


        List<String> curPlayList = new List<string>();
        public static bool nowPlaying;

        public async Task StartBotAsync()
        {
            nowPlaying = false;
            client = new DiscordSocketClient(new DiscordSocketConfig() { LogLevel = LogSeverity.Verbose,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildVoiceStates,
                HandlerTimeout = 10000
            });
            commands = new CommandService(new CommandServiceConfig() { LogLevel = LogSeverity.Verbose });
            

            //자 드가자
            await client.LoginAsync(TokenType.Bot, Token);
            await client.StartAsync();

            await commands.AddModuleAsync<CommandModules>(service);

            client.MessageReceived += CommandAsync;
            client.Log += DiscordLog;
            client.Ready += OnReady;

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
                // 커맨드 실행
                var result = await commands.ExecuteAsync(context, argPos, service);
                if (!result.IsSuccess) Console.WriteLine("error ... ?");
            }
        }
        private static Task DiscordLog(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }
        private static Task OnReady()
        {
            Console.WriteLine("bot ready");
            return Task.CompletedTask;
        }

        public static async Task PlayMusic()
        {
            if (nowPlaying) return;
            nowPlaying = true;
            string curPath = Program.GetPath();
            Console.WriteLine("현재 곡 위치 : " + curPath);
            // MediaFoundationReader를 사용하여 MP3 파일 읽기
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


            nowPlaying = false; // 노래 재생 완료 후 상태 변경

            if (Program.curList.Count > 0)
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
