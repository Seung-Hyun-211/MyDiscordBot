using Google.Apis.Util;
using Google.Apis.YouTube.v3.Data;
using System.IO;
namespace App
{
    public class PlayList
    {
        private static PlayList _instance;
        public static PlayList Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PlayList();
                }

                return _instance;
            }
        }

        static string JsonPath = "JsonDatas/Songs.json";
        Dictionary<string, Video> history;
        List<Video> curList;

        public Video? curPlay;
        private PlayList()
        {
            history = new Dictionary<string, Video>();
            curList = new List<Video>();
            CheckDir();
            Init();
        }

        void Init()
        {
            List<Video> log = JsonFileHandler.Read<List<Video>>(JsonPath);

            foreach (var item in log)
            {
                history.Add(item.Id, item);
            }
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
        public Video? SearchHistory(string videoId)
        {
            Console.WriteLine("search histroy " + videoId + history.ContainsKey(videoId));
            if (history.ContainsKey(videoId))
                return history[videoId];
            else
                return null;
        }
        public void AddHistroy(Video s)
        {
            if (!history.ContainsKey(s.Id))
                history.Add(s.Id, s);
        }
        public void DeleteHistory(Video s)
        {
            string path = s.Snippet.Title;

            var opusFilePath = $"Audio/{path}.opus"; // Opus 파일로 바로 저장

            if (history.ContainsKey(s.Id))
                history.Remove(s.Id);

            RecordHistroy();

            if (File.Exists(opusFilePath))
            {
                try
                {
                    File.Delete(opusFilePath);
                }
                catch (IOException e)
                {
                    Console.WriteLine("파일 제거 오류 : " + s.Snippet.Title);
                }
            }

        }
        public void AddList(Video s)
        {
            curList.Add(s);
        }
        public void AddFirst(Video s)
        {
            if (s != null)
            {
                curList.Insert(0, s);
            }
        }
        public string GetPath(bool repeat)
        {
            string nextPath = curList.Count() > 0 ? curList[0].Snippet.Title : "";
            curPlay = curList.Count() > 0 ? curList[0] : null;
            if (!repeat)
            {
                curList.RemoveAt(0);
            }
            return nextPath;
        }
        public string Remove(int idx)
        {
            if (curList.Count() >= idx)
            {
                string title = curList[idx - 1].Snippet.Title;
                curList.RemoveAt(idx - 1);
                return title;
            }
            return "";
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
            List<Video> temp = new List<Video>();
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
        public void RecordHistroy()
        {
            JsonFileHandler.Write<List<Video>>("JsonDatas/Songs.json", history.Values.ToList());
        }

        public List<Video> GetList()
        {
            return curList;
        }

        public void GetRandomVideos(int count = 50)
        {
            count = count > history.Count ? history.Count : count;

            List<Video> videos = new List<Video>();
            List<int> indexes = new List<int>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                int idx = random.Next() % history.Count();
                if (!indexes.Contains(idx))
                {
                    curList.Add(history.ToList()[idx].Value);
                    indexes.Add(idx);
                }
                else
                {
                    i--;
                }
            }


        }
        // public int SearchArtist(string artist)
        // {
        //     var result =
        //         (from s in history
        //          where CompareArtist(s.Value.author, artist)
        //          select s.Value).ToList();
        //     // 결과가 비어있지 않으면 curList에 추가
        //     int cnt = result.Count();
        //     if (result != null && result.Count > 0)
        //     {
        //         foreach (var song in result)
        //             curList.Add(song);
        //     }
        //     else
        //     {
        //         Console.WriteLine("검색된 아티스트가 없습니다.");
        //     }
        //     return cnt;
        // }
        // private bool CompareArtist(string a = "", string b = "")
        // {
        //     a = a.ToLower();
        //     b = b.ToLower();

        //     if (a == b || a.Contains(b))
        //         return true;

        //     return IsSubsequence(a, b);
        // }
        // private bool IsSubsequence(string a, string b)
        // {
        //     int aIndex = 0;
        //     int bIndex = 0;

        //     while (aIndex < a.Length && bIndex < b.Length)
        //     {
        //         if (a[aIndex] == b[bIndex])
        //         {
        //             bIndex++;
        //         }
        //         aIndex++;
        //     }

        //     return bIndex == b.Length;
        // }


    }

}