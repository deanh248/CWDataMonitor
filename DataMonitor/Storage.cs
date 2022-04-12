using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Windows;
using System.ComponentModel;

namespace DataMonitor
{
    class Storage
    {
        
        private static readonly string BaseRegistryKey = @"SOFTWARE\CWDataMonitor";
        private static readonly byte[] encryptionIVSeed = new byte[] { 6,9,1,2,3,4,5 };

        public static string Username{
            get {
                byte[] data = GetRegistryKey("User",new byte[]{},RegistryValueKind.Binary) as byte[];
                if (data.Length < 1)
                    return "";
                return Encoding.ASCII.GetString(ProtectedData.Unprotect(data,encryptionIVSeed, DataProtectionScope.CurrentUser));
            }

            set {
                byte[] data = Encoding.ASCII.GetBytes(value);
                byte[] encrypted = ProtectedData.Protect(data, encryptionIVSeed, DataProtectionScope.CurrentUser);
                SetRegistryKey("User", encrypted,RegistryValueKind.Binary);
            }
        }
        public static string Password{
            get{
                byte[] data = GetRegistryKey("Secret", new byte[] { }, RegistryValueKind.Binary) as byte[];
                if (data.Length < 1)
                    return "";
                return Encoding.ASCII.GetString(ProtectedData.Unprotect(data, encryptionIVSeed, DataProtectionScope.CurrentUser));
            }
            set {
                byte[] data = Encoding.ASCII.GetBytes(value);
                byte[] encrypted = ProtectedData.Protect(data, encryptionIVSeed, DataProtectionScope.CurrentUser);
                SetRegistryKey("Secret", encrypted, RegistryValueKind.Binary);
            }
        }

        public static bool RunAtStartUp
        {
            get
            {
                return (bool)bool.Parse(GetRegistryKey("RunAtStartup", false.ToString()));
            }
            set
            {
                SetRegistryKey("RunAtStartup", value.ToString());
                SetStartupState(value);
            }
        }

        public static bool ResetSessionDaily
        {
            get
            {
                return (bool)bool.Parse(GetRegistryKey("ResetSessionDaily", true.ToString()));
            }
            set
            {
                SetRegistryKey("ResetSessionDaily", value.ToString());
                SetStartupState(value);
            }
        }


        // Date since we started tracking.
        public static DateTime CurrentSessionDateStarted = DateTime.MinValue;
        public static long CurrentSessionDataCounter = 0;

        public static void Load()
        {
            Console.WriteLine("Loading storage..");

            CurrentSessionDateStarted = DateTime.Parse(GetRegistryKey("SessionStartDate", DateTime.MinValue.ToString()));
            int val = 0;
            int.TryParse(GetRegistryKey("SessionDataCounter", "0"), out val);
            CurrentSessionDataCounter = val;
        }

        public static void Save()
        {
            Console.WriteLine("Saving storage..");

            SetRegistryKey("SessionStartDate", CurrentSessionDateStarted.ToString());
            SetRegistryKey("SessionDataCounter", CurrentSessionDataCounter.ToString());
        }

        public static string GetRegistryKey(string key, string defaultValue){
            return GetRegistryKey(key, defaultValue,RegistryValueKind.String) as string;
        }

        public static void SetRegistryKey(string key, string defaultValue){
            SetRegistryKey(key, defaultValue, RegistryValueKind.String);
        }

        public static object GetRegistryKey(string key, object defaultValue,RegistryValueKind registryValueKind){

            RegistryKey k = Registry.CurrentUser.CreateSubKey(BaseRegistryKey); 
            object val = k.GetValue(key);
            if(val == null){
                k.SetValue(key, defaultValue, registryValueKind);
                return defaultValue;
            }
            return val;
        }

        public static void SetRegistryKey(string key, object defaultValue, RegistryValueKind registryValueKind){
            RegistryKey k = Registry.CurrentUser.CreateSubKey(BaseRegistryKey); 
            k.SetValue(key, defaultValue, registryValueKind);
        }

        public static void SetStartupState(bool state)
        {
            // https://stackoverflow.com/questions/674628/how-do-i-set-a-program-to-launch-at-startup
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (state)
            {
                string executablePath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + System.AppDomain.CurrentDomain.FriendlyName;
                rk.SetValue("CWDataMonitor", executablePath);
            }
            else
            {
                rk.DeleteValue("CWDataMonitor", false);   
            }
        }

        public static bool GetStartupState()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return rk.GetValue("CWDataMonitor", null) == null ? false : true;
        }

    }
}
