using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using QuickAccessHub.Models;

namespace QuickAccessHub
{
    public partial class AddEditItemDialog : Window
    {
        public QuickItem Item { get; private set; }
        private readonly List<Category> _categories;

        public AddEditItemDialog(List<Category> categories, QuickItem? existingItem = null, string? initialType = null, string? initialValue = null)
        {
            InitializeComponent();
            _categories = categories;
            cmbCategory.ItemsSource = _categories;

            if (existingItem != null)
            {
                Item = existingItem;
                txtDialogTitle.Text = "Edit Item";
                txtName.Text = existingItem.Name;
                txtPathOrUrl.Text = existingItem.Type == "Url" ? existingItem.Url : existingItem.Path;

                if (existingItem.Type == "Folder") rbFolder.IsChecked = true;
                else if (existingItem.Type == "Url") rbUrl.IsChecked = true;
                else rbFile.IsChecked = true;

                if (existingItem.CategoryId.HasValue)
                {
                    cmbCategory.SelectedValue = existingItem.CategoryId.Value;
                }
            }
            else
            {
                Item = new QuickItem();
                if (cmbCategory.Items.Count > 0)
                {
                    cmbCategory.SelectedIndex = 0;
                }

                if (!string.IsNullOrEmpty(initialType))
                {
                    if (initialType == "Folder") rbFolder.IsChecked = true;
                    else if (initialType == "Url") rbUrl.IsChecked = true;
                    else rbFile.IsChecked = true;
                }

                if (!string.IsNullOrEmpty(initialValue))
                {
                    txtPathOrUrl.Text = initialValue;
                    if (string.IsNullOrEmpty(txtName.Text))
                    {
                        if (rbUrl.IsChecked == true)
                        {
                            txtName.Text = GetUrlDisplayName(initialValue);
                        }
                        else
                        {
                            txtName.Text = Path.GetFileName(initialValue);
                            if (string.IsNullOrEmpty(txtName.Text))
                            {
                                txtName.Text = initialValue;
                            }
                        }
                    }
                }
            }

            UpdateTypeUI();
        }

        private string GetUrlDisplayName(string url)
        {
            try
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }
                var uri = new Uri(url);
                return uri.Host.Replace("www.", "");
            }
            catch
            {
                return url;
            }
        }

        private void Type_Checked(object sender, RoutedEventArgs e)
        {
            UpdateTypeUI();
        }

        private void UpdateTypeUI()
        {
            if (lblPathOrUrl == null || btnBrowse == null) return;

            if (rbUrl.IsChecked == true)
            {
                lblPathOrUrl.Text = "Web Link (URL)";
                btnBrowse.Visibility = Visibility.Collapsed;
            }
            else if (rbFolder.IsChecked == true)
            {
                lblPathOrUrl.Text = "Folder Path";
                btnBrowse.Visibility = Visibility.Visible;
            }
            else
            {
                lblPathOrUrl.Text = "File Path";
                btnBrowse.Visibility = Visibility.Visible;
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (rbFolder.IsChecked == true)
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtPathOrUrl.Text = dlg.SelectedPath;
                    if (string.IsNullOrWhiteSpace(txtName.Text))
                    {
                        txtName.Text = Path.GetFileName(dlg.SelectedPath);
                    }
                }
            }
            else
            {
                using var dlg = new OpenFileDialog();
                dlg.Filter = "All Files (*.*)|*.*";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtPathOrUrl.Text = dlg.FileName;
                    if (string.IsNullOrWhiteSpace(txtName.Text))
                    {
                        txtName.Text = Path.GetFileName(dlg.FileName);
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = string.Empty;

            string name = txtName.Text.Trim();
            string pathOrUrl = txtPathOrUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                txtError.Text = "Please enter a display name.";
                return;
            }

            if (string.IsNullOrWhiteSpace(pathOrUrl))
            {
                txtError.Text = "Please enter a valid path or URL.";
                return;
            }

            Item.Name = name;
            if (rbUrl.IsChecked == true)
            {
                Item.Type = "Url";
                Item.Url = pathOrUrl;
                Item.Path = null;
            }
            else if (rbFolder.IsChecked == true)
            {
                Item.Type = "Folder";
                Item.Path = pathOrUrl;
                Item.Url = null;
            }
            else
            {
                Item.Type = "File";
                Item.Path = pathOrUrl;
                Item.Url = null;
            }

            if (cmbCategory.SelectedValue != null)
            {
                Item.CategoryId = Convert.ToInt64(cmbCategory.SelectedValue);
            }
            else
            {
                Item.CategoryId = null;
            }

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
