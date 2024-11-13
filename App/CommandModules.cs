using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Google.Apis.YouTube.v3.Data;


namespace App
{
    public class CommandModules : ModuleBase<SocketCommandContext>
    {
        IAudioClient audioClient;
        [Command("join", RunMode = RunMode.Async)]
        public async Task JoinChannel(IVoiceChannel channel = null)
        {
            // Get the audio channel
            if (channel == null)
                channel = (Context.User as IGuildUser)?.VoiceChannel;

            if (channel == null)
            {
                await Context.Channel.SendMessageAsync("채널에 입장해 있지 않음."); return;
            }

            await Context.Channel.SendMessageAsync("ㅎㅇ");
            DiscordBot.audioClient = await channel.ConnectAsync();
        }

        [Command("p", RunMode = RunMode.Async)]
        public async Task PlayCommand(params string[] queries)
        {
            if (DiscordBot.audioClient == null)
            {
                Console.WriteLine("first");
                JoinChannel((Context.User as IGuildUser)?.VoiceChannel);
                Task.Delay(1000);
            }


            string fullString = string.Join(" ", queries);
            if (fullString.Contains("playlist?list="))
            {
                await Context.Message.AddReactionAsync(new Emoji("✅"));
                await Context.Channel.SendMessageAsync("플레이 리스트 확인");
                await GetPlayList(queries);
                return;
            }

            try
            {
                Console.WriteLine(fullString);


                Song? search = null;

                await Context.Message.AddReactionAsync(new Emoji("✅"));

                if (string.Compare(fullString.Substring(0, 4), "http") != 0)
                {
                    Console.WriteLine("노래 검색중 ...");
                    search = await Youtube.SearchTitle(fullString);
                    fullString = search.url;
                }
                search = PlayList.Instance.SearchHistory(fullString);
                if (search == null)
                {
                    Console.WriteLine("url 검색중 ...");
                    search = await Youtube.SearchURL(fullString);
                    Console.WriteLine("다운로드중 ... " + fullString);
                    await Youtube.DownloadMp3(fullString);
                    search.title = search.title.Replace('/', '-');
                    PlayList.Instance.AddHistroy(search);
                    PlayList.Instance.RecordHistroy();
                }

                PlayList.Instance.AddList(search);
                await Context.Message.DeleteAsync();
                await Context.Channel.SendMessageAsync("재생 : " + search.title);
                await DiscordBot.PlayMusic();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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

        [Command("random", RunMode = RunMode.Async)]
        public async Task RandomMix(params string[] queries)
        {
            PlayList.Instance.RandomMix();
        }

        [Command("playlist", RunMode = RunMode.Async)]
        public async Task GetPlayList(params string[] queries)
        {

            Console.WriteLine("플리받음" + queries[0]);
            int index = queries[0].IndexOf("list=");  // "playlist?list="의 시작 인덱스를 찾음
            string plID = "";
            if (index >= 0)
            {
                plID = queries[0].Substring(index + 5);
            }
            Console.WriteLine("PlaylistID : " + plID);

            List<String> listURLs = await Youtube.GetVideoUrlsFromPlaylist(plID);
            int count = 0;
            bool first = false;
            if (listURLs.Count > 0)
            {
                foreach (var item in listURLs)
                {
                    Console.WriteLine($"{count++}");
                    var search = PlayList.Instance.SearchHistory(item);
                    if (search == null)
                    {
                        Console.WriteLine("url 검색중 ...");
                        search = await Youtube.SearchURL(item);
                        Console.WriteLine("다운로드중 ... " + item);
                        await Youtube.DownloadMp3(item);
                        search.title = search.title.Replace('/', '-');
                        PlayList.Instance.AddHistroy(search);
                        PlayList.Instance.RecordHistroy();
                    }

                    PlayList.Instance.AddList(search);
                    if (first)
                    {
                        first = false;
                        await Context.Channel.SendMessageAsync("재생 : " + search.title);
                        await DiscordBot.PlayMusic();
                    }
                }
            }
        }

    }
}