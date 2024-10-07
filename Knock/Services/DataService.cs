using Knock.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Knock.Services
{
    public class DataService
    {
        private readonly string dataPath = "data";
        private readonly Type jsonModelBasedType = typeof(JsonModelBase);

        private ConcurrentDictionary<string, JsonModelBase> models;

        public DataService()
        {
            models = new ConcurrentDictionary<string, JsonModelBase>();
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath); 
            Initialize();
        }

        public T Get<T>(string key) where T : JsonModelBase
        {
            models.TryGetValue(key, out JsonModelBase value);
            return value as T;
        }

        public void Set<T>(string key, Func<T, T> action) where T : JsonModelBase
        {
            T model = Get<T>(key);
            T res = action(model);
            models.TryUpdate(key, res, model);
        }

        public void Dispose()
        {
            foreach (string key in models.Keys)
            {
                FileInfo file = new FileInfo(Path.Combine(dataPath, key + ".json"));
                Save(key, file);
            }
        }

        private void Initialize()
        {
            IEnumerable<Type> types = GetJsonModelTypes();

            foreach (Type type in types)
            {
                JsonModelBase model = Activator.CreateInstance(type) as JsonModelBase;

                string path = Path.Combine(dataPath, model.Name + ".json");
                FileInfo file = new FileInfo(path);
                if (file.Exists) 
                    Load(type, file);
                else 
                    New(type, file);
            }
        }

        private void Load(Type type, FileInfo file)
        {
            using (TextReader tr = file.OpenText())
            {
                JsonModelBase model = JsonSerializer.Deserialize(tr.ReadToEnd(), type) as JsonModelBase;
                models.TryAdd(Path.GetFileNameWithoutExtension(file.Name), model);
                tr.Close();
            }
        }

        private void New(Type type, FileInfo file)
        {
            using (TextWriter tw = file.CreateText())
            {
                object model = Activator.CreateInstance(type);
                tw.Write(JsonSerializer.Serialize(model, type));
                tw.Flush();
                tw.Close();
            }
        }

        private void Save(string name, FileInfo file)
        {
            IEnumerable<Type> types = GetJsonModelTypes();
            if (file.Exists) file.Delete();

            using (TextWriter tw = file.CreateText())
            {
                Type t = null;
                foreach (Type type in types)
                {
                    JsonModelBase model = Activator.CreateInstance(type) as JsonModelBase;
                    if (model.Name == name)
                    {
                        t = type;
                        break;
                    }
                }
                if (t == null) return;
                tw.Write(JsonSerializer.Serialize(models[name], t));
                tw.Flush();
                tw.Close();
            }
        }

        private IEnumerable<Type> GetJsonModelTypes()
        {
            return Assembly
                .GetAssembly(jsonModelBasedType)
                .GetTypes()
                .Where(t => t.IsSubclassOf(jsonModelBasedType));
        }
    }
}
