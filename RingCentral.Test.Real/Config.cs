﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Diagnostics;
using System.IO;

namespace RingCentral.Test
{
    class Config
    {
        private static Config instance = null;
        private Config() { }

        public static Config Instance
        {
            get
            {
                if (instance == null)
                {
                    try
                    {
                        using (var sr = new StreamReader("config.json"))
                        {
                            var jsonData = sr.ReadToEnd();
                            instance = JsonConvert.DeserializeObject<Config>(jsonData);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        Debug.WriteLine("config.json doesn't exist");
                    }
                }
                return instance;
            }
        }

        [JsonProperty("RC_APP_KEY")]
        public string AppKey = "";

        [JsonProperty("RC_APP_SECRET")]
        public string AppSecret = "";

        [JsonProperty("RC_APP_SERVER")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SDK.Server Server = SDK.Server.Sandbox;

        [JsonProperty("RC_USERNAME")]
        public string Username = "";

        [JsonProperty("RC_EXTENSION")]
        public string Extension = "";

        [JsonProperty("RC_PASSWORD")]
        public string Password = "";

        [JsonProperty("RC_RECEIVER")]
        public string Receiver = "";
    }
}
