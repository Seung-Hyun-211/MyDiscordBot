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
        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            // Get the audio channel
            channel = (Context.User as IGuildUser)?.VoiceChannel;
            if (channel == null) { await Context.Channel.SendMessageAsync("채널에 입장해 있지 않음."); return; }

            DiscordBot.audioClient = await channel.ConnectAsync();
        }

        [Command("p", RunMode = RunMode.Async)]
        public async Task PlayCommand(params string[] queries)
        {
            try
            {

                string fullString = string.Join(" ", queries);
                Console.WriteLine(fullString);
                Song? search = null;

                if (string.Compare(fullString.Substring(0, 4), "http") != 0)
                {
                    Console.WriteLine("노래 검색중 ...");
                    search = await Youtube.SearchTitle(fullString);
                    fullString = search.url;
                }
                search = PlayList.Instance.SearchHistory(fullString);
                Console.WriteLine(search.title);
                if (search == null)
                {
                    Console.WriteLine("url 검색중 ...");
                    search = await Youtube.SearchURL(fullString);
                    Console.WriteLine("처음 듣는 노래를 다운로드중" + fullString);
                    await Youtube.DownloadMp3(fullString);
                    PlayList.Instance.AddHistroy(search);
                    Console.WriteLine("노래 정보를 기록하는중");
                    PlayList.Instance.RecordHistroy();

                }
                PlayList.Instance.AddList(search);
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
            PlayList.Instance.SearchArtist(fullString);
            await DiscordBot.PlayMusic();
        }

    }
}