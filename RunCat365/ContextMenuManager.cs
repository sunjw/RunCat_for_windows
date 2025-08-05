﻿// Copyright 2020 Takuto Nakamura
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
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> icons = [];
        private int current = 0;
        private EndlessGameForm? endlessGameForm;

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
            Action openRepository,
            Action onExit
        )
        {
            systemInfoMenu.Text = "-\n-\n-\n-\n-";
            systemInfoMenu.Enabled = false;

            var runnersMenu = new CustomToolStripMenuItem("Runners");
            runnersMenu.SetupSubMenusFromEnum<Runner>(
                r => r.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Runner>(
                        parent,
                        sender,
                        (string? s, out Runner r) => Enum.TryParse(s, out r),
                        r => setRunner(r)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                r => getRunner() == r,
                r => GetRunnerThumbnailBitmap(getSystemTheme(), r)
            );

            var themeMenu = new CustomToolStripMenuItem("Theme");
            themeMenu.SetupSubMenusFromEnum<Theme>(
                t => t.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Theme>(
                        parent,
                        sender,
                        (string? s, out Theme t) => Enum.TryParse(s, out t),
                        t => setManualTheme(t)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                t => getManualTheme() == t,
                _ => null
            );

            var fpsMaxLimitMenu = new CustomToolStripMenuItem("FPS Max Limit");
            fpsMaxLimitMenu.SetupSubMenusFromEnum<FPSMaxLimit>(
                f => f.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<FPSMaxLimit>(
                        parent,
                        sender,
                        (string? s, out FPSMaxLimit f) => FPSMaxLimitExtension.TryParse(s, out f),
                        f => setFPSMaxLimit(f)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                f => getFPSMaxLimit() == f,
                _ => null
            );

            var startupMenu = new CustomToolStripMenuItem("Startup at launch")
            {
                Checked = getStartup()
            };
            startupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleStartup);

            var settingsMenu = new CustomToolStripMenuItem("Settings");
            settingsMenu.DropDownItems.AddRange(
                themeMenu,
                fpsMaxLimitMenu,
                startupMenu
            );

            var endlessGameMenu = new CustomToolStripMenuItem("Endless Game");
            endlessGameMenu.Click += (sender, e) => ShowOrActivateGameWindow(getSystemTheme);

            var appVersionMenu = new CustomToolStripMenuItem(
                $"{Application.ProductName} v{Application.ProductVersion}"
            )
            {
                Enabled = false
            };

            var repositoryMenu = new CustomToolStripMenuItem("Open Repository");
            repositoryMenu.Click += (sender, e) => openRepository();

            var informationMenu = new CustomToolStripMenuItem("Information");
            informationMenu.DropDownItems.AddRange(
                appVersionMenu,
                repositoryMenu
            );

            var exitMenu = new CustomToolStripMenuItem("Exit");
            exitMenu.Click += (sender, e) => onExit();

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                systemInfoMenu,
                new ToolStripSeparator(),
                runnersMenu,
                new ToolStripSeparator(),
                settingsMenu,
                informationMenu,
                endlessGameMenu,
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
            ToolStripMenuItem parentMenu,
            object? sender,
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
            var iconName = $"{runner.GetString()}_0".ToLower();
            if (runner.HasTheme())
            {
                iconName = $"{systemTheme.GetString()}_{iconName}".ToLower();
            }
            var obj = Resources.ResourceManager.GetObject(iconName);
            return obj is Icon icon ? icon.ToBitmap() : null;
        }

        internal void SetIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var prefix = (manualTheme == Theme.System ? systemTheme : manualTheme).GetString() + "_";
            if (!runner.HasTheme())
            {
                prefix = "";
            }
            var runnerName = runner.GetString();
            var rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{prefix}{runnerName}_{i}".ToLower();
                var icon = rm.GetObject(iconName);
                if (icon is null) continue;
                list.Add((Icon)icon);
            }
            icons.ForEach(icon => icon.Dispose());
            icons.Clear();
            icons.AddRange(list);
        }

        private static void HandleStartupMenuClick(object? sender, Func<bool, bool> toggleStartup)
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            if (toggleStartup(item.Checked))
            {
                item.Checked = !item.Checked;
            }
        }

        private void ShowOrActivateGameWindow(Func<Theme> getSystemTheme)
        {
            if (endlessGameForm is null)
            {
                endlessGameForm = new EndlessGameForm(getSystemTheme());
                endlessGameForm.FormClosed += (sender, e) =>
                {
                    endlessGameForm = null;
                };
                endlessGameForm.Show();
            }
            else
            {
                endlessGameForm.Activate();
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
