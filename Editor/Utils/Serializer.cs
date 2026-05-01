using System.Runtime.Serialization;
namespace LumenX.Utils
{
    public static class Serializer
    {
        public static void ToFile<T>(T obj, string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Create);
                var serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(fs, obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing object to file: {ex.Message}");
                //TODO: Log error code
            }
        }

        public static T FromFile<T>(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open);
                var serializer = new DataContractSerializer(typeof(T));
                T instance = (T)serializer.ReadObject(fs);
                return instance;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing object from file: {ex.Message}");
                //TODO: Log error code
                return default(T);
            }
        }
    }
}