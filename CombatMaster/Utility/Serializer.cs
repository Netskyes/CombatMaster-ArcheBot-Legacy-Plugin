using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace CombatMaster
{
    public static class Serializer
    {
        public static bool Save(object obj, string path)
        {
            try
            {
                XmlSerializer writer = new XmlSerializer(obj.GetType());
                if (!File.Exists(path)) File.Create(path).Close();

                using (var stream = new FileStream(path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    writer.Serialize(stream, obj);
                }

                return true;
            }
            catch { }
            return false;
        }

        public static T Load<T>(T obj, string path)
        {
            try
            {
                if(Validate(obj.GetType(), path))
                {
                    XmlSerializer reader = new XmlSerializer(obj.GetType());

                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        return (T)reader.Deserialize(stream);
                    }
                }
            }
            catch { }
            return default(T);
        }

        private static bool Validate(Type type, string path)
        {
            XmlDocument xml = new XmlDocument();

            try
            {
                xml.Load(path);
                XmlSerializer reader = new XmlSerializer(type);

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    return (reader.Deserialize(stream) != null);
                }
            }
            catch { }
            return false;
        }

        public static string ToJsonString(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static T ToJsonObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static bool ToJsonString(object obj, string path)
        {
            var serializer = new JsonSerializer();

            try
            {
                using (var sw = new StreamWriter(path))
                using (var writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, obj);
                }

                return true;
            }
            catch { }
            return false;
        }

        public static T FileToJsonObject<T>(string path)
        {
            try
            {
                using (var sr = new StreamReader(path))
                {
                    return JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
                }
            }
            catch { }
            return default(T);
        }
    }
}
