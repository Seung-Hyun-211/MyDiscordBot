using Discord;
using Discord.Audio;
using Discord.Commands;


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

            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync("ㅎㅇ");
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

        public async void Reaction(SocketCommandContext context, string text)
        {
            var lastContext = DiscordBot.lastContext;

            // 기존 Context의 메시지를 삭제
            if (lastContext != null)
            {
                await lastContext.Message.DeleteAsync();
            }
            var botMessage = await context.Channel.SendMessageAsync(text);

            //var newContext = new SocketCommandContext(context.Client, botMessage);
            // 새로운 Context 업데이트 (필요한 경우)
            //DiscordBot.lastContext = newContext;

            context.Message.DeleteAsync();
        }
    }
}