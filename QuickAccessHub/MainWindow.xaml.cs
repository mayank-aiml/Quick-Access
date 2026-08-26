using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuickAccessHub.Models;
using QuickAccessHub.Services;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Cursors = System.Windows.Input.Cursors;
using Point = System.Windows.Point;
using Vector = System.Windows.Vector;
using DataObject = System.Windows.DataObject;

namespace QuickAccessHub
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db;
        private readonly ItemExecutionService _executionService;
        private readonly HotkeyManager _hotkeyManager;

        private List<Category> _categories = new();
        private List<QuickItem> _allItems = new();
        private long? _selectedCategoryId = null; // null = All
        private bool _isExplicitExit = false;

        private Point _dragStartPoint;
        private QuickItem? _dragItem;

        public MainWindow()
        {
            InitializeComponent();

            _db = new DatabaseService();
            _executionService = new ItemExecutionService();
            _hotkeyManager = new HotkeyManager();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hotkeyManager.Initialize(this);
            _hotkeyManager.HotkeyTriggered += HotkeyManager_HotkeyTriggered;

            RegisterCurrentHotkey();
            RefreshData();
            CenterWindow();
        }

        public void RegisterCurrentHotkey()
        {
            string hotkeyStr = _db.GetSetting("GlobalHotkey", "Ctrl + Space") ?? "Ctrl + Space";
            var config = HotkeyConfig.Parse(hotkeyStr);
            _hotkeyManager.Register(config, out _);
        }

        private void HotkeyManager_HotkeyTriggered(object? sender, EventArgs e)
        {
            ShowLauncher();
        }

        public void ShowLauncher()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();

            txtSearch.Focus();
            txtSearch.SelectAll();
            RefreshData();
        }

        public void HideLauncher()
        {
            Hide();
        }

        public void ExitApplication()
        {
            _isExplicitExit = true;
            _hotkeyManager.Dispose();
            Close();
        }

        private void CenterWindow()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 3;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void BtnHide_Click(object sender, RoutedEventArgs e)
        {
            HideLauncher();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                e.Cancel = true;
                HideLauncher();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                HideLauncher();
                e.Handled = true;
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (IsActive || OwnedWindows.Count > 0) return;
            HideLauncher();
        }

        private void RefreshData()
        {
            _categories = _db.GetCategories();
            RenderCategoryTabs();

            string search = txtSearch.Text.Trim();
            _allItems = _db.GetItems(search, _selectedCategoryId);

            var files = _allItems.Where(i => i.Type == "File").ToList();
            var folders = _allItems.Where(i => i.Type == "Folder").ToList();
            var links = _allItems.Where(i => i.Type == "Url").ToList();

            lstFiles.ItemsSource = files;
            lstFolders.ItemsSource = folders;
            lstLinks.ItemsSource = links;

            secFiles.Visibility = files.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            secFolders.Visibility = folders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            secLinks.Visibility = links.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            pnlEmptyState.Visibility = (_allItems.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RenderCategoryTabs()
        {
            pnlCategories.Children.Clear();
            pnlCategories.Children.Add(CreateCategoryButton("All", null));

            foreach (var cat in _categories)
            {
                pnlCategories.Children.Add(CreateCategoryButton(cat.Name, cat.Id));
            }
        }

        private Button CreateCategoryButton(string label, long? categoryId)
        {
            bool isSelected = _selectedCategoryId == categoryId;
            var btn = new Button
            {
                Content = label,
                Tag = categoryId,
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 12,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#3B82F6" : "#222234")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isSelected ? "#0F0F17" : "#F3F4F6")),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            var style = new Style(typeof(Border));
            style.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(12)));
            btn.Resources.Add(typeof(Border), style);

            btn.Click += (s, e) =>
            {
                _selectedCategoryId = categoryId;
                RefreshData();
            };

            return btn;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtSearchPlaceholder.Visibility = string.IsNullOrEmpty(txtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
            RefreshData();
        }

        private void TxtSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var firstItem = _allItems.FirstOrDefault();
                if (firstItem != null)
                {
                    ExecuteOpen(firstItem);
                }
                e.Handled = true;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var itemFile = new MenuItem { Header = "📄 Add File" };
            itemFile.Click += (s, ev) => OpenAddDialog("File");

            var itemFolder = new MenuItem { Header = "📁 Add Folder" };
            itemFolder.Click += (s, ev) => OpenAddDialog("Folder");

            var itemLink = new MenuItem { Header = "🔗 Add Link (URL)" };
            itemLink.Click += (s, ev) => OpenAddDialog("Url");

            menu.Items.Add(itemFile);
            menu.Items.Add(itemFolder);
            menu.Items.Add(itemLink);

            menu.PlacementTarget = btnAdd;
            menu.IsOpen = true;
        }

        private void OpenAddDialog(string type)
        {
            var dlg = new AddEditItemDialog(_categories, initialType: type) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _db.AddItem(dlg.Item);
                RefreshData();
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWin = new SettingsWindow(_db, _hotkeyManager) { Owner = this };
            settingsWin.ShowDialog();
            RegisterCurrentHotkey();
            RefreshData();
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuickItem item)
            {
                ExecuteOpen(item);
            }
        }

        private void ExecuteOpen(QuickItem item)
        {
            if (!_executionService.OpenItem(item, out string? error))
            {
                MessageBox.Show(error ?? "Unable to open item.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshData();
            }
            else
            {
                HideLauncher();
            }
        }

        private void BtnLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuickItem item)
            {
                if (!_executionService.OpenLocation(item, out string? error))
                {
                    MessageBox.Show(error ?? "Unable to open location.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RefreshData();
                }
            }
        }

        private void BtnCopyLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuickItem item && !string.IsNullOrEmpty(item.Url))
            {
                if (_executionService.CopyUrlToClipboard(item.Url, out string? error))
                {
                    btn.Content = "✓ Copied";
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

                    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                    timer.Tick += (s, ev) =>
                    {
                        btn.Content = "📋 Copy Link";
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                        timer.Stop();
                    };
                    timer.Start();
                }
                else
                {
                    MessageBox.Show(error ?? "Copy failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuickItem item)
            {
                _db.DeleteItem(item.Id);
                RefreshData();
            }
        }

        #region Drag and Drop OUT of Application
        private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Do not initiate drag if user clicked an action button inside the item card
            if (e.OriginalSource is DependencyObject source && IsDescendantOfButton(source))
            {
                _dragItem = null;
                return;
            }

            if (sender is FrameworkElement element && element.DataContext is QuickItem item)
            {
                _dragStartPoint = e.GetPosition(null);
                _dragItem = item;
            }
        }

        private bool IsDescendantOfButton(DependencyObject current)
        {
            while (current != null)
            {
                if (current is Button) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void Item_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _dragItem != null && sender is FrameworkElement element)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _dragStartPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    QuickItem item = _dragItem;
                    _dragItem = null;

                    if (item.Type == "File" || item.Type == "Folder")
                    {
                        if (!string.IsNullOrEmpty(item.Path) && (File.Exists(item.Path) || Directory.Exists(item.Path)))
                        {
                            var dataObj = new DataObject(DataFormats.FileDrop, new string[] { item.Path });
                            System.Windows.DragDrop.DoDragDrop(element, dataObj, DragDropEffects.Copy | DragDropEffects.Link);
                        }
                    }
                    else if (item.Type == "Url" && !string.IsNullOrEmpty(item.Url))
                    {
                        var dataObj = new DataObject(DataFormats.Text, item.Url);
                        System.Windows.DragDrop.DoDragDrop(element, dataObj, DragDropEffects.Copy);
                    }
                }
            }
        }
        #endregion

        #region Drag and Drop INTO Application
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string path in files)
                {
                    string itemType = Directory.Exists(path) ? "Folder" : "File";
                    var dlg = new AddEditItemDialog(_categories, initialType: itemType, initialValue: path) { Owner = this };
                    if (dlg.ShowDialog() == true)
                    {
                        _db.AddItem(dlg.Item);
                    }
                }
                RefreshData();
            }
            else if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                string text = ((string)e.Data.GetData(DataFormats.Text) ?? (string)e.Data.GetData(DataFormats.UnicodeText) ?? "").Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    bool isUrl = text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                text.StartsWith("www.", StringComparison.OrdinalIgnoreCase);

                    string initialType = isUrl ? "Url" : "File";
                    var dlg = new AddEditItemDialog(_categories, initialType: initialType, initialValue: text) { Owner = this };
                    if (dlg.ShowDialog() == true)
                    {
                        _db.AddItem(dlg.Item);
                        RefreshData();
                    }
                }
            }
        }
        #endregion
    }
}
