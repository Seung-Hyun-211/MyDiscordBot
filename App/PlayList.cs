using YoutubeExplode.Playlists;

namespace App
{
    public class PlayList
    {
        static string JsonPath = "JsonDatas/Songs.json";
        Dictionary<string, Song> history;
        List<string> curList;
        public static async Task<PlayList> CreateAsync()
        {
            PlayList instance = new PlayList();
            await instance.Init();
            return instance;
        }

        private PlayList()
        {
            history = new Dictionary<string, Song>();
            curList = new List<string>();
            CheckDir();
        }
        private async Task Init()
        {

            List<Song> log = await JsonFileHandler.ReadAsync<Song>(JsonPath);

            foreach (var item in log)
                history.Add(item.url, item);
        }
        void CheckDir()
        {
            Console.WriteLine("Check Dir...");

            if (!Directory.Exists("Audio"))
                Directory.CreateDirectory("Audio");

            if (!Directory.Exists("JsonDatas"))
                Directory.CreateDirectory("JsonDatas");

            Console.WriteLine("Json File Check...");
            if (!File.Exists(JsonPath))
            {
                // Songs.json 파일 생성
                File.WriteAllText(JsonPath, "[\n]");
            }
        }
        public Song? SearchHistory(string fullString)
        {
            if (history.ContainsKey(fullString)) return history[fullString];
            else return null;
        }
        public void AddHistroy(Song s)
        {

        }
        public void AddList(string path)
        {
            Console.WriteLine("리스트 추가됨 " + path);
            curList.Add(path);
        }
        public string GetPath()
        {
            string nextPath = curList.Count() > 0 ? curList[0] : "";
            curList.RemoveAt(0);
            return nextPath;
        }
        public int GetListCount()
        {
            return curList.Count;
        }
        public void RemoveList(int index = 0)
        {
            if (curList.Count() < index)
                index = curList.Count() - 1;

            curList.RemoveAt(index);
            return;
        }
        public void RandomMix()
        {
            List<string> temp = new List<string>();
            int curSize = curList.Count();
            if (curSize <= 1) return;
            var rand = new Random();
            int delIdx = 0;
            for (; curSize > 0; curSize--)
            {
                delIdx = rand.Next() % curSize;
                temp.Add(curList[delIdx]);
                curList.RemoveAt(delIdx);
            }
            curList = temp;
            return;
        }
        public void SearchArtist(string artist)
        {
            var result =
                (from s in history
                 where CompareArtist(s.Value.author, artist)
                 select s.Value.title).ToList();
            // 결과가 비어있지 않으면 curList에 추가
            string pathHead = "Audio/";

            if (result != null && result.Count > 0)
            {
                foreach (var title in result)
                    curList.Add(pathHead + title + ".mp3");
            }
            else
            {
                Console.WriteLine("검색된 아티스트가 없습니다.");
            }
        }
        private bool CompareArtist(string a = "", string b = "")
        {
            a = a.ToLower();
            b = b.ToLower();

            if (a == b || a.Contains(b))
                return true;

            return IsSubsequence(a, b);
        }
        private bool IsSubsequence(string a, string b)
        {
            int aIndex = 0;
            int bIndex = 0;

            while (aIndex < a.Length && bIndex < b.Length)
            {
                if (a[aIndex] == b[bIndex])
                {
                    bIndex++;
                }
                aIndex++;
            }

            return bIndex == b.Length;
        }

        public async Task RecordHistroy()
        {
            await JsonFileHandler.WriteAsync("JsonDatas/Songs.json", history.Values.ToList());
        }
    }

}