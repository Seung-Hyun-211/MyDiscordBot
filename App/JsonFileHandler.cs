using Newtonsoft.Json;

namespace App
{

    public static class JsonFileHandler
    {
        public static async Task<List<T>> ReadAsync<T>(string filePath)
        {
            using (StreamReader r = new StreamReader(filePath))
            {
                string json = await r.ReadToEndAsync();
                return JsonConvert.DeserializeObject<List<T>>(json);
            }
        }

        public static async Task WriteAsync<T>(string filePath, T item)
        {
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(json);
            }
            return;
        }
        public static async Task WriteAsync<T>(string filePath, List<T> items)
        {
            string json = JsonConvert.SerializeObject(items, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(json);
            }
            return;
        }

    }
}