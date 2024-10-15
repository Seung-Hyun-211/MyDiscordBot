using System;
using System.Reflection.Metadata;
using DSharpPlus.Entities;
using NAudio.Wave;
using YoutubeExplode.Exceptions;
using System.Linq;
using System.Collections.Generic;

namespace App
{
    public class Song
    {
        public string title { get; set; }
        public string url { get; private set; }
        public string author { get; private set; }
        public TimeSpan duration { get; private set; }
        string[]? tags;

        public Song(string title, string url, string author, TimeSpan duration, string[]? tags = null)
        {
            this.title = title;
            this.url = url;
            this.author = author;
            this.duration = duration;
            this.tags = tags;
        }

        public void AddTag(string tag)
        {
            if (tags == null) tags = new string[0];
            Array.Resize(ref this.tags, this.tags.Length + 1);
            tags[tags.Length - 1] = tag;
        }
        public void Print()
        {
            string str = "";
            str += "title : " + this.title;
            str += "\nurl : " + this.url;
            str += "\nauthor : " + this.author;
            str += "\nduration : " + this.duration.ToString();
            if (tags != null) str += "\ntags : " + string.Join(", ", this.tags);

            Console.WriteLine(str);
        }
    }



    internal class Program
    {
        internal const string DBPath = "JsonDatas/Songs.json";
        public static DiscordBot bot;
        internal static List<Song> history = new List<Song>();
        internal static List<string> curList = new List<string>();
        internal static Dictionary<string, Song> urlToSongDic = new Dictionary<string, Song>();
        public static void Main(string[] args)
        {
            Ready().GetAwaiter().GetResult();
        }

        private static async Task Ready()
        {
            Console.WriteLine("DB Loading...");
            history = await JsonFileHandler.ReadAsync<Song>(DBPath);

            foreach (var item in history)
                urlToSongDic.Add(item.url, item);

            Console.WriteLine("Done");

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
        public static async void AddList(string path)
        {
            Console.WriteLine("리스트 추가됨 " + path);
            curList.Add(path);
            if (!DiscordBot.nowPlaying)
            {
                await DiscordBot.PlayMusic();
            }
        }
        public static string GetPath()
        {
            string nextPath = curList.Count() > 0 ? curList[0] : "";
            curList.RemoveAt(0);
            return nextPath;
        }
        public static void RemoveList(int index = 0)
        {
            if (curList.Count() < index)
                index = curList.Count() - 1;

            curList.RemoveAt(index);
            return;
        }
        public static void RandomMix()
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
        public static async void SearchArtist(string artist)
        {
            var result =
                (from s in history
                 where CompareArtist(s.author, artist)
                 select s.title).ToList();
            // 결과가 비어있지 않으면 curList에 추가
            string pathHead = "Audio/";

            if (result != null && result.Count > 0)
            {
                foreach (var title in result)
                    curList.Add(pathHead + title+".mp3");
            }
            else
            {
                Console.WriteLine("검색된 아티스트가 없습니다.");
            }
            await DiscordBot.PlayMusic();
        }
        private static bool CompareArtist(string a="", string b="")
        {
            a = a.ToLower();
            b = b.ToLower();

            if (a == b || a.Contains(b))
                return true;

            return IsSubsequence(a, b);
        }
        private static bool IsSubsequence(string a, string b)
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
    }

}
