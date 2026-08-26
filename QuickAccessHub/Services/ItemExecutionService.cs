using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using QuickAccessHub.Models;

namespace QuickAccessHub.Services
{
    public class ItemExecutionService
    {
        public bool OpenItem(QuickItem item, out string? errorMessage)
        {
            errorMessage = null;

            try
            {
                if (item.Type == "Url")
                {
                    if (string.IsNullOrWhiteSpace(item.Url))
                    {
                        errorMessage = "Invalid or empty URL.";
                        return false;
                    }

                    string url = item.Url;
                    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        url = "https://" + url;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    return true;
                }

                if (item.Type == "File")
                {
                    if (string.IsNullOrWhiteSpace(item.Path) || !File.Exists(item.Path))
                    {
                        item.IsMissing = true;
                        errorMessage = "File not found at specified path.";
                        return false;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.Path,
                        UseShellExecute = true
                    });
                    return true;
                }

                if (item.Type == "Folder")
                {
                    if (string.IsNullOrWhiteSpace(item.Path) || !Directory.Exists(item.Path))
                    {
                        item.IsMissing = true;
                        errorMessage = "Folder not found at specified path.";
                        return false;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.Path,
                        UseShellExecute = true
                    });
                    return true;
                }

                errorMessage = "Unknown item type.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error opening item: {ex.Message}";
                return false;
            }
        }

        public bool OpenLocation(QuickItem item, out string? errorMessage)
        {
            errorMessage = null;

            try
            {
                if (string.IsNullOrWhiteSpace(item.Path))
                {
                    errorMessage = "No path specified for this item.";
                    return false;
                }

                if (item.Type == "File")
                {
                    if (!File.Exists(item.Path))
                    {
                        item.IsMissing = true;
                        errorMessage = "File not found at specified path.";
                        return false;
                    }

                    Process.Start("explorer.exe", $"/select,\"{item.Path}\"");
                    return true;
                }

                if (item.Type == "Folder")
                {
                    if (!Directory.Exists(item.Path))
                    {
                        item.IsMissing = true;
                        errorMessage = "Folder not found at specified path.";
                        return false;
                    }

                    Process.Start("explorer.exe", $"\"{item.Path}\"");
                    return true;
                }

                errorMessage = "Item is not a file or folder.";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error opening location: {ex.Message}";
                return false;
            }
        }

        public bool CopyUrlToClipboard(string url, out string? errorMessage)
        {
            errorMessage = null;
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    errorMessage = "URL is empty.";
                    return false;
                }

                System.Windows.Clipboard.SetText(url);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to copy to clipboard: {ex.Message}";
                return false;
            }
        }
    }
}
