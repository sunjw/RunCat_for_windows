// Copyright 2020 Takuto Nakamura
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

using RunCat365.Properties;
using System.ComponentModel;

namespace RunCat365
{
    internal class ContextMenuManager
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly CustomToolStripMenuItem runnerMenu = new();
        private readonly CustomToolStripMenuItem themeMenu = new();
        private readonly CustomToolStripMenuItem fpsMaxLimitMenu = new();
        private readonly CustomToolStripMenuItem startupMenu = new();
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> icons = [];
        private int current = 0;

        internal ContextMenuManager(
            Func<Runner> getRunner,
            Action<Runner> setRunner,
            Func<Theme> getSystemTheme,
            Func<Theme> getManualTheme,
            Action<Theme> setManualTheme,
            Func<FPSMaxLimit> getFPSMaxLimit,
            Action<FPSMaxLimit> setFPSMaxLimit,
            Func<bool> getStartup,
            Func<bool, bool> toggleStartup,
            Action onExit
        )
        {
            systemInfoMenu.Text = "-\n-\n-\n-\n-";
            systemInfoMenu.Enabled = false;

            runnerMenu.SetupMenuFromEnum<Runner>(
                "Runners",
                r => r.GetString(),
                (sender, e) =>
                {
                    HandleMenuItemSelection<Runner>(
                        sender,
                        runnerMenu,
                        (string? s, out Runner r) => Enum.TryParse(s, out r),
                        r => setRunner(r)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                r => getRunner() == r,
                r => GetRunnerThumbnailBitmap(getSystemTheme(), r)
            );

            themeMenu.SetupMenuFromEnum<Theme>(
                "Theme",
                t => t.GetString(),
                (sender, e) =>
                {
                    HandleMenuItemSelection<Theme>(
                        sender,
                        themeMenu,
                        (string? s, out Theme t) => Enum.TryParse(s, out t),
                        t => setManualTheme(t)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                t => getManualTheme() == t,
                _ => null
            );

            fpsMaxLimitMenu.SetupMenuFromEnum<FPSMaxLimit>(
                "FPS Max Limit",
                f => f.GetString(),
                (sender, e) =>
                {
                    HandleMenuItemSelection<FPSMaxLimit>(
                        sender,
                        fpsMaxLimitMenu,
                        (string? s, out FPSMaxLimit f) => FPSMaxLimitExtension.TryParse(s, out f),
                        f => setFPSMaxLimit(f)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                f => getFPSMaxLimit() == f,
                _ => null
            );

            startupMenu.Text = "Startup at launch";
            startupMenu.Checked = getStartup();
            startupMenu.Click += (sender, e) => HandleStartupMenuClick(toggleStartup);

            var appVersionMenu = new CustomToolStripMenuItem(
                $"{Application.ProductName} v{Application.ProductVersion}"
            )
            {
                Enabled = false
            };

            var exitMenu = new CustomToolStripMenuItem("Exit");
            exitMenu.Click += (sender, e) => onExit();

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                systemInfoMenu,
                new ToolStripSeparator(),
                runnerMenu,
                themeMenu,
                fpsMaxLimitMenu,
                startupMenu,
                new ToolStripSeparator(),
                appVersionMenu,
                new ToolStripSeparator(),
                exitMenu
            );
            contextMenuStrip.Renderer = new ContextMenuRenderer();

            SetIcons(getSystemTheme(), getManualTheme(), getRunner());

            notifyIcon.Text = "-";
            notifyIcon.Icon = icons[0];
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private static void HandleMenuItemSelection<T>(
            object? sender,
            ToolStripMenuItem parentMenu,
            CustomTryParseDelegate<T> tryParseMethod,
            Action<T> assignValueAction
        )
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem childItem in parentMenu.DropDownItems)
            {
                childItem.Checked = false;
            }
            item.Checked = true;
            if (tryParseMethod(item.Text, out T parsedValue))
            {
                assignValueAction(parsedValue);
            }
        }

        private static Bitmap? GetRunnerThumbnailBitmap(Theme systemTheme, Runner runner)
        {
            var iconName = $"{systemTheme.GetString()}_{runner.GetString()}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            return obj is Icon icon ? icon.ToBitmap() : null;
        }

        internal void SetIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var prefix = (manualTheme == Theme.System ? systemTheme : manualTheme).GetString();
            var runnerName = runner.GetString();
            var rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{prefix}_{runnerName}_{i}".ToLower();
                var icon = rm.GetObject(iconName);
                if (icon is null) continue;
                list.Add((Icon)icon);
            }
            icons.Clear();
            icons.AddRange(list);
        }

        private void HandleStartupMenuClick(Func<bool, bool> toggleStartup)
        {
            if (toggleStartup(startupMenu.Checked))
            {
                startupMenu.Checked = !startupMenu.Checked;
            }
        }

        internal void ShowBalloonTip()
        {
            var message = "App has launched. " +
                "If the icon is not on the taskbar, it has been omitted, " +
                "so please move it manually and pin it.";
            notifyIcon.ShowBalloonTip(5000, "RunCat 365", message, ToolTipIcon.Info);
        }

        internal void AdvanceFrame()
        {
            if (icons.Count <= current) current = 0;
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Count;
        }

        internal void SetSystemInfoMenuText(string text)
        {
            systemInfoMenu.Text = text;
        }

        internal void SetNotifyIconText(string text)
        {
            notifyIcon.Text = text;
        }

        internal void HideNotifyIcon()
        {
            notifyIcon.Visible = false;
        }

        private delegate bool CustomTryParseDelegate<T>(string? value, out T result);
    }
}
