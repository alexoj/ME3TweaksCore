using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FontAwesome5;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Services.Backup;

namespace ME3TweaksCoreWPF.Services.Backup
{
    /// <summary>
    /// ME3Tweaks Backup Service, with WPF extensions (for icons)
    /// </summary>
    public class BackupServiceWPF : BackupService
    {
        private static EFontAwesomeIcon _me1ActivityIcon = EFontAwesomeIcon.Solid_TimesCircle;
        public static EFontAwesomeIcon ME1ActivityIcon
        {
            get => _me1ActivityIcon;
            private set => SetProperty(ref _me1ActivityIcon, value);
        }

        private static EFontAwesomeIcon _me2ActivityIcon = EFontAwesomeIcon.Solid_TimesCircle;
        public static EFontAwesomeIcon ME2ActivityIcon
        {
            get => _me2ActivityIcon;
            private set => SetProperty(ref _me2ActivityIcon, value);
        }

        private static EFontAwesomeIcon _me3ActivityIcon = EFontAwesomeIcon.Solid_TimesCircle;
        public static EFontAwesomeIcon ME3ActivityIcon
        {
            get => _me3ActivityIcon;
            private set => SetProperty(ref _me3ActivityIcon, value);
        }

        private static EFontAwesomeIcon _le1ActivityIcon = EFontAwesomeIcon.Solid_TimesCircle;
        public static EFontAwesomeIcon LE1ActivityIcon
        {
            get => _le1ActivityIcon;
            private set => SetProperty(ref _le1ActivityIcon, value);
        }

        private static EFontAwesomeIcon _le2ActivityIcon = EFontAwesomeIcon.Solid_TimesCircle;
        public static EFontAwesomeIcon LE2ActivityIcon
        {
            get => _le2ActivityIcon;
            private set => SetProperty(ref _le2ActivityIcon, value);
        }

        private static EFontAwesomeIcon _le3ActivityIcon = EFontAwesomeIcon.Solid_TimesCircle;
        public static EFontAwesomeIcon LE3ActivityIcon
        {
            get => _le3ActivityIcon;
            private set => SetProperty(ref _le3ActivityIcon, value);
        }

        public static void SetIcon(MEGame game, EFontAwesomeIcon p1)
        {
            switch (game)
            {
                case MEGame.ME1:
                    ME1ActivityIcon = p1;
                    break;
                case MEGame.ME2:
                    ME2ActivityIcon = p1;
                    break;
                case MEGame.ME3:
                    ME3ActivityIcon = p1;
                    break;
                case MEGame.LE1:
                    LE1ActivityIcon = p1;
                    break;
                case MEGame.LE2:
                    LE2ActivityIcon = p1;
                    break;
                case MEGame.LE3:
                    LE3ActivityIcon = p1;
                    break;
            }
        }

        public static void ResetIcon(MEGame game)
        {
            SetIcon(game, EFontAwesomeIcon.Solid_TimesCircle);
        }
    }
}
