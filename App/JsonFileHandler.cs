using Newtonsoft.Json;

namespace App
{

    public static class JsonFileHandler
    {
        public static T Read<T>(string filePath)
        {
            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
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
    }
}