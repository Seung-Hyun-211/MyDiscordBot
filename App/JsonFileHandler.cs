using Newtonsoft.Json;

namespace App
{

    public static class JsonFileHandler
    {
        public static List<T> Read<T>(string filePath)
        {
            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<List<T>>(json);
            }
        }

        public static void Write<T>(string filePath, T item)
        {
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(json);
            }
        }

        public static void Write<T>(string filePath, List<T> items)
        {
            string json = JsonConvert.SerializeObject(items, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(json);
            }
        }
    }
}