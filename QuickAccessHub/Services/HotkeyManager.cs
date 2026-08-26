using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using QuickAccessHub.Models;

namespace QuickAccessHub.Services
{
    public class HotkeyManager : IDisposable
    {
        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _hWnd = IntPtr.Zero;
        private HwndSource? _hwndSource;
        private bool _isRegistered;

        public event EventHandler? HotkeyTriggered;

        public bool IsRegistered => _isRegistered;

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hWnd = helper.Handle;

            if (_hWnd == IntPtr.Zero)
            {
                helper.EnsureHandle();
                _hWnd = helper.Handle;
            }

            _hwndSource = HwndSource.FromHwnd(_hWnd);
            _hwndSource?.AddHook(HwndHook);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyTriggered?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public bool Register(HotkeyConfig config, out string? errorMessage)
        {
            errorMessage = null;

            if (_hWnd == IntPtr.Zero)
            {
                errorMessage = "Window handle is not initialized.";
                return false;
            }

            Unregister();

            uint modifiers = MOD_NOREPEAT;
            if (config.Control) modifiers |= MOD_CONTROL;
            if (config.Alt) modifiers |= MOD_ALT;
            if (config.Shift) modifiers |= MOD_SHIFT;
            if (config.Windows) modifiers |= MOD_WIN;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(config.Key);

            if (vk == 0)
            {
                errorMessage = "Invalid key configuration.";
                return false;
            }

            bool success = RegisterHotKey(_hWnd, HOTKEY_ID, modifiers, vk);
            if (success)
            {
                _isRegistered = true;
                return true;
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                errorMessage = $"Failed to register shortcut ({config}). It may already be in use by another application (Error code {errorCode}).";
                _isRegistered = false;
                return false;
            }
        }

        public void Unregister()
        {
            if (_isRegistered && _hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hWnd, HOTKEY_ID);
                _isRegistered = false;
            }
        }

        public void Dispose()
        {
            Unregister();
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(HwndHook);
                _hwndSource = null;
            }
        }
    }
}
