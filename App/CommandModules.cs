using Discord;
using Discord.Audio;
using Discord.Commands;
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
            string[] tempQ = new string[queries.Length];
            Array.Copy(queries, tempQ, queries.Length);
            if (DiscordBot.audioClient == null)
            {
                JoinChannel((Context.User as IGuildUser)?.VoiceChannel);
                Task.Delay(2000);
            }

            string fullString = string.Join(" ", queries);

            await Context.Message.AddReactionAsync(new Emoji("✅"));
            try
            {
                if (fullString.Contains("playlist?list="))
                {
                    var msg = await Context.Channel.SendMessageAsync("플레이 리스트 확인");
                    await msg.AddReactionAsync(new Emoji("🔃"));

                    await GetPlayList(queries);
                    Thread.Sleep(5000);
                    return;
                }
                else
                {
                    var search = await YT.GetVideoAsync(fullString);

                    if (PlayList.Instance.SearchHistory(search.Id) == null)
                    {
                        await Context.Message.RemoveReactionAsync(new Emoji("✅"), Context.Client.CurrentUser);
                        await Context.Message.AddReactionAsync(new Emoji("🔃"));
                        if (await YT.DownloadMp3(search) == false)
                        {
                            await Context.Message.RemoveReactionAsync(new Emoji("🔃"), Context.Client.CurrentUser);
                            await Context.Message.AddReactionAsync(new Emoji("❌"));

                            return;
                        }
                        await Context.Message.RemoveReactionAsync(new Emoji("🔃"), Context.Client.CurrentUser);
                        PlayList.Instance.AddHistroy(search);
                        PlayList.Instance.RecordHistroy();
                    }

                    PlayList.Instance.AddList(search);

                    await Context.Message.DeleteAsync();
                    await Context.Channel.SendMessageAsync($"***{Context.Message.Author.GlobalName}*** - 재생 : {search.Snippet.Title}");
                    await DiscordBot.PlayMusic();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        [Command("s", RunMode = RunMode.Async)]
        public async Task SkipCommand()
        {
            DiscordBot.Skip();
            Context.Channel.SendMessageAsync("넘김");

        }

        [Command("random", RunMode = RunMode.Async)]
        public async Task RandomMix(params string[] queries)
        {
            PlayList.Instance.RandomMix();
            Context.Channel.SendMessageAsync("랜덤");
        }

        [Command("playlist", RunMode = RunMode.Async)]
        public async Task GetPlayList(params string[] queries)
        {
            string url = string.Join(" ", queries);
            List<Video> listURLs = await YT.GetListVideoAsync(url);
            Console.WriteLine(" 리스트 url cnt : " + listURLs.Count);
            int count = 0;
            int f = 0;
            if (listURLs.Count > 0)
            {
                foreach (var item in listURLs)
                {
                    Console.WriteLine($"{count++}");
                    bool haveAudio = true;
                    var search = PlayList.Instance.SearchHistory(item.Id);
                    if (search == null)
                    {
                        Console.WriteLine("다운로드중 ... " + item);
                        if (await YT.DownloadMp3(item))
                        {
                            PlayList.Instance.AddHistroy(search);
                            PlayList.Instance.RecordHistroy();
                        }
                        else
                        {
                            f++;
                            haveAudio = false;
                        }
                    }
                    if (haveAudio)
                    {
                        PlayList.Instance.AddList(search);
                        DiscordBot.PlayMusic();
                    }
                }
            }
            Context.Channel.SendMessageAsync($"{count-f} 개 추가됨 (실패 : {f})");
        }
    }

}