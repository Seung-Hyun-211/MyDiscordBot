namespace App
{
    internal class Program
    {
        public static DiscordBot bot;
        public static async Task Main(string[] args)
        {
            try
            {
                if(PlayList.Instance == null) return;
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
