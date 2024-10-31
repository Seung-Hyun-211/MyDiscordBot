using System;
using System.Reflection.Metadata;
using DSharpPlus.Entities;
using NAudio.Wave;
using YoutubeExplode.Exceptions;
using System.Linq;
using System.Collections.Generic;

namespace App
{
    internal class Program
    {
        public static DiscordBot bot;
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("봇 생성중");
                new DiscordBot().StartBotAsync().GetAwaiter().GetResult();
                await Task.Delay(-1);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine(" 취소됨 ");
            }
        }
    }

}
