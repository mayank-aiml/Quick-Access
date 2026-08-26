using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using QuickAccessHub.Services;

namespace QuickAccessHub
{
    public partial class App : System.Windows.Application
    {
        private const string MutexName = "QuickAccessHub_SingleInstance_Mutex_98765";
        private static Mutex? _mutex;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Single Instance Check
            try
            {
                _mutex = new Mutex(true, MutexName, out bool isNewInstance);
                if (!isNewInstance)
                {
                    if (!_mutex.WaitOne(TimeSpan.Zero, false))
                    {
                        System.Windows.MessageBox.Show("Quick Access Hub is already running in the system tray.", "Quick Access Hub", MessageBoxButton.OK, MessageBoxImage.Information);
                        Shutdown();
                        return;
                    }
                }
            }
            catch (AbandonedMutexException)
            {
                // Previous instance crashed, safe to continue
            }

            base.OnStartup(e);

            // 2. Global Exception Handling
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                System.Windows.Forms.MessageBox.Show($"Unexpected error: {ex?.Message}", "Quick Access Hub Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            };

            // 3. Initialize Main Window
            _mainWindow = new MainWindow();

            // 4. Create System Tray Icon
            SetupSystemTray();

            // 5. Check autostart argument
            bool startMinimized = false;
            foreach (var arg in e.Args)
            {
                if (arg.Equals("--autostart", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
                {
                    startMinimized = true;
                    break;
                }
            }

            if (!startMinimized)
            {
                _mainWindow.ShowLauncher();
            }
        }

        private void SetupSystemTray()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Quick Access Hub (Ctrl + Space)"
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                _mainWindow?.ShowLauncher();
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var itemOpen = new System.Windows.Forms.ToolStripMenuItem("Open Quick Access Hub");
            itemOpen.Font = new Font(itemOpen.Font, System.Drawing.FontStyle.Bold);
            itemOpen.Click += (s, e) => _mainWindow?.ShowLauncher();

            var itemSettings = new System.Windows.Forms.ToolStripMenuItem("Settings...");
            itemSettings.Click += (s, e) =>
            {
                _mainWindow?.ShowLauncher();
                var db = new DatabaseService();
                var hotkeyMgr = new HotkeyManager();
                var settingsWin = new SettingsWindow(db, hotkeyMgr) { Owner = _mainWindow };
                settingsWin.ShowDialog();
                _mainWindow?.RegisterCurrentHotkey();
            };

            var itemStartup = new System.Windows.Forms.ToolStripMenuItem("Start with Windows")
            {
                Checked = StartupManager.IsStartWithWindowsEnabled()
            };
            itemStartup.Click += (s, e) =>
            {
                bool targetState = !itemStartup.Checked;
                if (StartupManager.SetStartWithWindows(targetState, out string? error))
                {
                    itemStartup.Checked = targetState;
                    new DatabaseService().SaveSetting("StartWithWindows", targetState ? "1" : "0");
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show($"Failed to update startup setting: {error}", "Startup Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
            };

            var itemExit = new System.Windows.Forms.ToolStripMenuItem("Exit");
            itemExit.Click += (s, e) =>
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _mainWindow?.ExitApplication();
                Shutdown();
            };

            contextMenu.Items.Add(itemOpen);
            contextMenu.Items.Add(itemSettings);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(itemStartup);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            contextMenu.Items.Add(itemExit);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
            base.OnExit(e);
        }
    }
}
