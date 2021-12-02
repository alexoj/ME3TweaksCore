using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ME3TweaksCore.Targets;
using ME3TweaksCoreWPF.Targets;

namespace ME3TweaksCoreWPF.Converters
{
    /// <summary>
    /// Converts GameTargetWPF and GameTarget
    /// </summary>
    public class GameTargetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value; // ??
            if (value is GameTargetWPF gtwpf)
                return gtwpf;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value; // WE DONT NEED THIS
            if (value is GameTarget gt)
                return gt;

            return value;
        }
    }
}
