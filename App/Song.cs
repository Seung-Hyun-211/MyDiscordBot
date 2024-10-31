namespace App
{
    public class Song
    {
        public string title { get; set; }
        public string url { get; private set; }
        public string author { get; private set; }
        public TimeSpan duration { get; private set; }
        public string[]? tags;

        public Song(string title, string url, string author, TimeSpan duration, string[]? tags = null)
        {
            this.title = title;
            this.url = url;
            this.author = author;
            this.duration = duration;
            if (tags == null)
                this.tags = new string[] { };
            else
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
}