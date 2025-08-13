// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using Microsoft.Win32;
using Windows.ApplicationModel;

namespace RunCat365
{
    internal static class StartupManager
    {

        private static StartupTask? startupTask;

        public static bool GetStartup()
        {
            if (IsRunningAsPackaged())
            {
                return GetStartupAsync_Packed().Result;
            }
            else
            {
                return GetStartup_NotPacked();
            }
        }

        public static bool SetStartup(bool isChecked)
        {
            if (IsRunningAsPackaged())
            {
                return SetStartupAsync_Packed(isChecked).Result;
            }
            else
            {
                return SetStartup_NotPacked(isChecked);
            }
        }

        private static async Task<bool> GetStartupAsync_Packed()
        {
            if (startupTask is null) startupTask = await StartupTask.GetAsync("RunCatStartup");
            if (startupTask is null) return false;
            if (startupTask.State == StartupTaskState.Enabled) return true;
            return false;
        }

        private static bool GetStartup_NotPacked()
        {
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            if (rKey is null) return false;
            var value = (rKey.GetValue(Application.ProductName) is not null);
            rKey.Close();
            return value;
        }

        private static async Task<bool> SetStartupAsync_Packed(bool isChecked)
        {
            var active = !isChecked;
            var changeCheck = false;
            if (startupTask is null) startupTask = await StartupTask.GetAsync("RunCatStartup");
            if (active)
            {
                switch (startupTask.State)
                {
                    case StartupTaskState.Enabled:
                        changeCheck = true;
                        break;
                    case StartupTaskState.Disabled:
                        StartupTaskState newState = await startupTask.RequestEnableAsync();
                        if (newState == StartupTaskState.Enabled)
                        {
                            changeCheck = true;
                        }
                        else
                        {
                            MessageBox.Show("Launch at Startup could not be activated.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            changeCheck = false;
                        }
                        break;
                    case StartupTaskState.DisabledByUser:
                        MessageBox.Show("Launch at startup was disabled by the user, enable it in Task Manager > Startup, search RunCat 365 and enable it.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        changeCheck = false;
                        break;
                    case StartupTaskState.DisabledByPolicy:
                        MessageBox.Show("Launch at startup was disabled by policy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        changeCheck = false;
                        break;
                }
            }
            else
            {
                if (startupTask.State == StartupTaskState.Enabled) startupTask.Disable();
                changeCheck = true;
            }
            return changeCheck;
        }

        private static bool SetStartup_NotPacked(bool isChecked)
        {
            var productName = Application.ProductName;
            if (productName is null) return false;
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName, true);
            if (rKey is null) return false;
            if (isChecked)
            {
                rKey.DeleteValue(productName, false);
            }
            else
            {
                var fileName = Environment.ProcessPath;
                if (fileName != null)
                {
                    rKey.SetValue(productName, fileName);
                }
            }
            rKey.Close();
            return true;
        }

        private static bool IsRunningAsPackaged()
        {
            try
            {
                var _ = Package.Current;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
