using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QuickAccessHub.Models;
using QuickAccessHub.Services;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace QuickAccessHub
{
    public partial class SettingsWindow : Window
    {
        private readonly DatabaseService _db;
        private readonly HotkeyManager _hotkeyManager;
        private bool _isRecording = false;
        private HotkeyConfig _currentConfig;

        public SettingsWindow(DatabaseService db, HotkeyManager hotkeyManager)
        {
            InitializeComponent();
            _db = db;
            _hotkeyManager = hotkeyManager;

            // Load Hotkey config
            string hotkeyStr = _db.GetSetting("GlobalHotkey", "Ctrl + Space") ?? "Ctrl + Space";
            _currentConfig = HotkeyConfig.Parse(hotkeyStr);
            txtHotkeyDisplay.Text = _currentConfig.ToString();

            // Load Start with Windows
            chkStartWithWindows.IsChecked = StartupManager.IsStartWithWindowsEnabled();

            // Load Categories
            RefreshCategories();
        }

        private void RefreshCategories()
        {
            lstCategories.ItemsSource = _db.GetCategories();
        }

        private void BtnRecordHotkey_Click(object sender, RoutedEventArgs e)
        {
            _isRecording = !_isRecording;
            if (_isRecording)
            {
                btnRecordHotkey.Content = "Listening...";
                btnRecordHotkey.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAB387"));
                txtHotkeyDisplay.Text = "Press key combination...";
                txtHotkeyStatus.Text = "Press Ctrl, Alt, Shift, or Win with a target key.";
                txtHotkeyStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAB387"));
                txtHotkeyDisplay.Focus();
            }
            else
            {
                StopRecording();
            }
        }

        private void StopRecording()
        {
            _isRecording = false;
            btnRecordHotkey.Content = "Change Shortcut";
            btnRecordHotkey.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89B4FA"));
        }

        private void TxtHotkeyDisplay_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isRecording) return;

            e.Handled = true;

            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // Ignore modifier keys by themselves
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            var newConfig = new HotkeyConfig
            {
                Control = Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
                Alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt),
                Shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
                Windows = Keyboard.Modifiers.HasFlag(ModifierKeys.Windows),
                Key = key
            };

            // Require at least one modifier
            if (!newConfig.Control && !newConfig.Alt && !newConfig.Shift && !newConfig.Windows)
            {
                txtHotkeyStatus.Text = "Please include at least one modifier (Ctrl, Alt, Shift, or Win).";
                txtHotkeyStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8"));
                return;
            }

            // Try registering hotkey
            if (_hotkeyManager.Register(newConfig, out string? errorMsg))
            {
                _currentConfig = newConfig;
                string newStr = _currentConfig.ToString();
                txtHotkeyDisplay.Text = newStr;
                _db.SaveSetting("GlobalHotkey", newStr);

                txtHotkeyStatus.Text = $"Successfully registered '{newStr}'!";
                txtHotkeyStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A6E3A1"));
                StopRecording();
            }
            else
            {
                txtHotkeyStatus.Text = errorMsg ?? "Could not register hotkey.";
                txtHotkeyStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8"));
            }
        }

        private void ChkStartWithWindows_Click(object sender, RoutedEventArgs e)
        {
            bool enable = chkStartWithWindows.IsChecked == true;
            if (StartupManager.SetStartWithWindows(enable, out string? error))
            {
                _db.SaveSetting("StartWithWindows", enable ? "1" : "0");
            }
            else
            {
                MessageBox.Show($"Failed to update startup setting: {error}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                chkStartWithWindows.IsChecked = !enable;
            }
        }

        private void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            string catName = txtNewCategory.Text.Trim();
            if (string.IsNullOrWhiteSpace(catName)) return;

            try
            {
                _db.AddCategory(catName);
                txtNewCategory.Text = string.Empty;
                RefreshCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Category may already exist: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (lstCategories.SelectedItem is Category cat)
            {
                if (MessageBox.Show($"Are you sure you want to delete category '{cat.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _db.DeleteCategory(cat.Id);
                    RefreshCategories();
                }
            }
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
