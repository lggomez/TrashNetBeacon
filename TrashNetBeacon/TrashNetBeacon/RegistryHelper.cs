namespace TrashNetBeacon
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Win32;

    public static class RegistryHelper
    {
        public static void SetRegistrySetting(params KeyValuePair<string, string>[] keys)
        {
            TryHandleRegistrySetting(keys, TrySetRegistryKeyValue);
        }

        private static void TrySetRegistryKeyValue(
            RegistryKey registrySubKey,
            KeyValuePair<string, string> keyValuePair)
        {
            try
            {
                registrySubKey?.SetValue(keyValuePair.Key, keyValuePair.Value);
            }
            catch (UnauthorizedAccessException)
            {
                // ignored
            }
        }

        public static string GetRegistrySetting(string keyName)
        {
            var keyValues = new List<KeyValuePair<string, string>>();
            keyValues.Add(new KeyValuePair<string, string>(keyName, null));

            return GetRegistrySetting(keyValues.ToArray()).First();
        }

        public static List<string> GetRegistrySettings(params string[] keyNames)
        {
            var keyValues = new List<KeyValuePair<string, string>>();

            foreach (var keyName in keyNames)
            {
                keyValues.Add(new KeyValuePair<string, string>(keyName, null));
            }

            return GetRegistrySetting(keyValues.ToArray());
        }

        public static List<string> GetRegistrySetting(params KeyValuePair<string, string>[] keys)
        {
            var values = new List<string>();

            TryHandleRegistrySetting(
                keys,
                (registrySubKey, keyValuePair) => { values.Add(registrySubKey?.GetValue(keyValuePair.Key) as string); });

            return values;
        }

        public static void TryHandleRegistrySetting(
            KeyValuePair<string, string>[] keys,
            Action<RegistryKey, KeyValuePair<string, string>> keyAction)
        {
            try
            {
                using (var subKey = Registry.CurrentUser.OpenSubKey(Constants.RegistrySubKeyName))
                {
                    if (subKey != null)
                    {
                        foreach (var keyValuePair in keys)
                        {
                            keyAction(subKey, keyValuePair);
                        }
                    }
                    else
                    {
                        using (var newKey = Registry.CurrentUser.CreateSubKey(Constants.RegistrySubKeyName))
                        {
                            foreach (var keyValuePair in keys)
                            {
                                keyAction(newKey, keyValuePair);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ignored
                // Debug.Fail($"Error while trying to manipulate registry settings{Environment.NewLine}\t{ex.Message}{Environment.NewLine}\t{ex.StackTrace}");
            }
        }
    }
}