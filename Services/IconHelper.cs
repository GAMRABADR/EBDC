using System;
using System.Collections.Generic;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;

namespace EBDC.Services
{
    public static class IconHelper
    {
        private static readonly Dictionary<string, PackIconKind> _iconMap = new()
        {
            { "app", PackIconKind.Youtube },
            { "pause", PackIconKind.Pause },
            { "resume", PackIconKind.Play },
            { "cancel", PackIconKind.Cancel }
        };

        public static PackIcon GetIcon(string iconName)
        {
            if (!_iconMap.TryGetValue(iconName.ToLower(), out var kind))
            {
                throw new ArgumentException($"Icon {iconName} not found", nameof(iconName));
            }

            return new PackIcon { Kind = kind };
        }
    }
}
