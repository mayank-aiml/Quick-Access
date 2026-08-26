using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace QuickAccessHub.Models
{
    public class HotkeyConfig
    {
        public bool Control { get; set; } = true;
        public bool Alt { get; set; } = false;
        public bool Shift { get; set; } = false;
        public bool Windows { get; set; } = false;
        public Key Key { get; set; } = Key.Space;

        public override string ToString()
        {
            var parts = new List<string>();
            if (Control) parts.Add("Ctrl");
            if (Alt) parts.Add("Alt");
            if (Shift) parts.Add("Shift");
            if (Windows) parts.Add("Win");
            parts.Add(Key.ToString());
            return string.Join(" + ", parts);
        }

        public static HotkeyConfig Parse(string value)
        {
            var config = new HotkeyConfig();
            if (string.IsNullOrWhiteSpace(value)) return config;

            var tokens = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            config.Control = false;
            config.Alt = false;
            config.Shift = false;
            config.Windows = false;

            foreach (var token in tokens)
            {
                if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || token.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    config.Control = true;
                else if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    config.Alt = true;
                else if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    config.Shift = true;
                else if (token.Equals("Win", StringComparison.OrdinalIgnoreCase) || token.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                    config.Windows = true;
                else if (Enum.TryParse<Key>(token, true, out var keyVal))
                    config.Key = keyVal;
            }

            return config;
        }
    }
}
