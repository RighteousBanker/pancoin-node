using System;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Panode.API
{
    [DataContract]
    public class Settings
    {
        [DataMember]
        public bool accessible { get; set; }

        [DataMember]
        public bool mine { get; set; }

        [DataMember]
        public string minerAddr { get; set; }

        [DataMember]
        public string url { get; set; }

        [DataMember]
        public string defaultNode { get; set; }

        private static string GetSampleSettings() => $"{{{Environment.NewLine}\"accessible\": false,{Environment.NewLine}\"mine\": false,{Environment.NewLine}\"minerAddr\": \"0x1\",{Environment.NewLine}\"url\": \"http://127.0.0.1:8125/\",{Environment.NewLine}\"defaultNode\": \"http://127.0.0.1:8125/\"{Environment.NewLine}}}";

        public Settings()
        {

        }

        public Settings(string path)
        {
            string fileContent;

            if (File.Exists(path))
            {
                fileContent = File.ReadAllText(path);
            }
            else
            {
                fileContent = GetSampleSettings();
                File.WriteAllText(path, fileContent);
                
                Console.WriteLine($"Settings file created with default settings");
            }

            var settings = JsonConvert.DeserializeObject<Settings>(fileContent);

            accessible = settings.accessible;
            defaultNode = settings.defaultNode;
            mine = settings.mine;

            mine = false; //miner is permanently disabled in node

            if (settings.url != null)
            {
                if (settings.url[settings.url.Length - 1] != '/')
                {
                    url += '/';
                }

                url = settings.url;
            }
        }
    }
}
