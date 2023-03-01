using PlatformX.Common.Behaviours;
using System;

namespace PlatformX.FunctionLayer.Helper
{
    public class SimpleConfigurationHelper : IAppConfig
    {
        public SimpleConfigurationHelper()
        {
        }

        public bool GetBool(string key)
        {
            bool ret;
            GetValue(key, bool.Parse, out ret);
            return ret;
        }

        public int GetInt(string key)
        {
            int ret;
            GetValue(key, int.Parse, out ret);
            return ret;
        }

        public string GetString(string key)
        {
            string ret;
            GetValue(key, itm => itm, out ret);
            return ret;
        }

        private static bool GetValue<T>(string key, Func<string, T> convertor, out T result)
        {
            try
            {
                // Not found so look in the config file
                var settingsVal = Environment.GetEnvironmentVariable(key);
                if (settingsVal != null)
                {
                    result = convertor(settingsVal);
                    return true;
                }
            }
            catch
            {

            }

            result = default(T);
            return false;
        }
    }
}
