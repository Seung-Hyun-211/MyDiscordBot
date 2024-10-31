using Discord;
using Discord.Audio;
using Discord.Commands;
using NAudio.CoreAudioApi;
using System.Diagnostics;



namespace App
{
    public class CommandModules : ModuleBase<SocketCommandContext>
    {
        IAudioClient audioClient;
        PlayList pl;
        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            pl = DiscordBot.pl;
            // Get the audio channel
            channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null) { await Context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

            // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
            DiscordBot.audioClient = await channel.ConnectAsync();
            Console.WriteLine(audioClient.ConnectionState);

        }
        [Command("p", RunMode = RunMode.Async)]
        public async Task PlayCommand(params string[] queries)
        {
            try
            {
                string fullString = string.Join(" ", queries);
                Console.WriteLine(fullString);
                Song? search = null;

                if (fullString.Substring(0, 4) != "http")
                {
                    Console.WriteLine("노래 검색중 ...");
                    search = await Youtube.SearchTitle(fullString);
                    fullString = search.url;
                }
                search = pl.SearchHistory(fullString);

                if (search == null)
                {
                    Console.WriteLine("url 검색중 ...");
                    search = await Youtube.SearchURL(fullString);
                }
                Console.WriteLine("처음 듣는 노래를 다운로드중" + fullString);
                await Youtube.DownloadMp3(fullString);
                pl.AddHistroy(search);
                Console.WriteLine("노래 정보를 기록하는중");
                await pl.RecordHistroy();

                string audioPath = "Audio/" + search.title.Replace('/', '-') + ".mp3";
                search.Print();
                Console.WriteLine("재생 ... " + audioPath);
                pl.AddList(search.url);
                await DiscordBot.PlayMusic();
            }
            catch
            {
            }
        }


        [Command("s", RunMode = RunMode.Async)]
        public async Task SkipCommand()
        {
            Console.WriteLine("노래 넘김");
            DiscordBot.Skip();
        }

        [Command("artist", RunMode = RunMode.Async)]
        public async Task SearchArtist(params string[] queries)
        {
            string fullString = string.Join(" ", queries);
            pl.SearchArtist(fullString);
            await DiscordBot.PlayMusic();
        }



    }
}