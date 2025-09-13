using Discord;
using Discord.Audio;
using Discord.Commands;
using Google.Apis.YouTube.v3.Data;
using static System.Net.Mime.MediaTypeNames;


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
            if (DiscordBot.audioClient != null && DiscordBot.audioClient.ConnectionState == ConnectionState.Connected)
            {
                PlayList.Instance.curList.Clear();
                DiscordBot.Skip();
                await DiscordBot.audioClient.StopAsync();
            }
            DiscordBot.audioClient = await channel.ConnectAsync();
            //await Context.Message.DeleteAsync();
        }
        [Command("p", RunMode = RunMode.Async)]
        public async Task PlayCommand(params string[] queries)
        {
            string[] tempQ = new string[queries.Length];
            Array.Copy(queries, tempQ, queries.Length);
            if (DiscordBot.audioClient == null)
            {
                JoinChannel((Context.User as IGuildUser)?.VoiceChannel);
                await Task.Delay(5000);
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
                    await Context.Message.DeleteAsync();
                    await msg.DeleteAsync();
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
                await Context.Message.RemoveReactionAsync(new Emoji("✅"), Context.Client.CurrentUser);
                await Context.Message.AddReactionAsync(new Emoji("❌"));
                MessageReference refrence = new MessageReference(Context.Message.Id);
                await Context.Channel.SendMessageAsync($"{Context.Message.Author.Mention} 검색 기록 없음", messageReference: refrence);
                await Context.Message.DeleteAsync();
                Console.WriteLine(e.Message);
            }
        }

        [Command("first", RunMode = RunMode.Async)]
        public async Task PlayFirst(params string[] queries)
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
                    var msg = await Context.Channel.SendMessageAsync("플레이리스트는 우선순위 불가");
                    await msg.DeleteAsync();
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

                    PlayList.Instance.AddFirst(search);

                    await Context.Message.DeleteAsync();
                    await Context.Channel.SendMessageAsync($"***{Context.Message.Author.GlobalName}*** - 먼저재생 : {search.Snippet.Title}");
                    await DiscordBot.PlayMusic();
                }
            }
            catch (Exception e)
            {
                await Context.Message.RemoveReactionAsync(new Emoji("✅"), Context.Client.CurrentUser);
                await Context.Message.AddReactionAsync(new Emoji("❌"));
                MessageReference refrence = new MessageReference(Context.Message.Id);
                await Context.Channel.SendMessageAsync($"{Context.Message.Author.Mention} 검색 기록 없음", messageReference: refrence);
                await Context.Message.DeleteAsync();
                Console.WriteLine(e.Message);
            }
        }

        [Command("s", RunMode = RunMode.Async)]
        public async Task SkipCommand()
        {
            DiscordBot.Skip();

            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync("넘김");

        }

        [Command("random", RunMode = RunMode.Async)]
        public async Task RandomMix(params string[] queries)
        {
            await Context.Message.DeleteAsync();
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
                    item.Snippet.Title = YT.ReplaceInvalidFileNameChars(item.Snippet.Title);

                    bool haveAudio = true;
                    var search = PlayList.Instance.SearchHistory(item.Id);
                    if (search == null)
                    {
                        Console.WriteLine("다운로드중 ... " + item.Snippet.Title);
                        if (await YT.DownloadMp3(item))
                        {
                            PlayList.Instance.AddHistroy(item);
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
                        PlayList.Instance.AddList(item);
                        DiscordBot.PlayMusic();
                    }
                }
            }
            Context.Channel.SendMessageAsync($"{count - f} 개 추가됨 (실패 : {f})");
        }


        [Command("list", RunMode = RunMode.Async)]
        public async Task PrintList(params string[] queries)
        {
            await Context.Message.DeleteAsync();
            List<Video> videos = PlayList.Instance.GetList();
            string title = "";
            if (PlayList.Instance.curPlay != null)
            {
                title += $"\n**현재**\n```{ PlayList.Instance.curPlay.Snippet.Title}```";
            }
            title += $"**다음**\n";
            string text = "";

            for (int i = 0; i < 5 && i <videos.Count; i++)
            {
                text += $"{i+1} : {videos[i].Snippet.Title}\n";
            }
            text += $"Count : {videos.Count}";
            string full = $"```{text}```";
            await Context.Channel.SendMessageAsync(title + full);
            return;
        }

        [Command("omakase", RunMode = RunMode.Async)]
        public async Task Omakase(params string[] queries)
        {
            string[] tempQ = new string[queries.Length];
            Array.Copy(queries, tempQ, queries.Length);
            string fullString = string.Join(" ", queries);

            if (int.TryParse(fullString, out var count))
            {
                PlayList.Instance.GetRandomVideos(count);
            }
            else
            {
                PlayList.Instance.GetRandomVideos();
            }

            DiscordBot.PlayMusic();
            PrintList();

        }

        [Command("repeat", RunMode = RunMode.Async)]
        public async Task Repeat(params string[] queries)
        {
            DiscordBot.repeat = !DiscordBot.repeat;
            await Context.Message.DeleteAsync();
            string onoff = DiscordBot.repeat ? "On" : "Off";
            await Context.Channel.SendMessageAsync($"반복 {onoff}");
            if (!DiscordBot.repeat)
            {
                PlayList.Instance.Remove(0);
            }
            else
            {
                PlayList.Instance.AddFirst(PlayList.Instance.curPlay);
            }
        }

        [Command("remove", RunMode = RunMode.Async)]
        public async Task Remove(params string[] queries)
        {
            await Context.Message.DeleteAsync();
            if (int.TryParse(queries.First(), out var idx))
            {
                var text = PlayList.Instance.Remove(idx);
                await Context.Channel.SendMessageAsync($"지워짐 {text}");
            }
            else
            {
                await Context.Channel.SendMessageAsync($"잘못넣음");
            }
        }
        [Command("delete", RunMode = RunMode.Async)]
        public async Task Delete(params string[] queries)
        {
            await Context.Message.DeleteAsync();
            Video? video = PlayList.Instance.curPlay;
            
            if (video == null) 
                return;

            DiscordBot.Skip();

            PlayList.Instance.DeleteHistory(video);
            await Context.Channel.SendMessageAsync($"오마카세에서 제거됨");
        }

        [Command("hibana", RunMode = RunMode.Async)]
        public async Task Hibana(params string[] queries)
        {
            await Context.Channel.SendMessageAsync($"火花");
            
            await GetPlayList("https://www.youtube.com/playlist?list=PLp7APC9OqwXmbiSNZLlr0Y0Pe9FWencwf");
            await PrintList();
            DiscordBot.PlayMusic();
            await Context.Message.DeleteAsync();
        }

        [Command("clear", RunMode = RunMode.Async)]
        public async Task Clear(params string[] queries)
        {
            int count = int.Parse(queries[0]);
            var messages = await Context.Channel.GetMessagesAsync(count + 1).FlattenAsync();
            await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);

            await Context.Channel.SendMessageAsync($"{count}개 채팅 제거됨");
        }
    }
}