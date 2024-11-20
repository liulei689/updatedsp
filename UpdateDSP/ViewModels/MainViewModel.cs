using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rubyer;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using UpdateDSP.Views;

namespace UpdateDSP.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private ObservableCollection<ViewItem> viewItems;

        [ObservableProperty]
        private ViewItem currentViewItem;

        [ObservableProperty]
        private ObservableCollection<ThemeColorInfo> themeColors;

        public static MainViewModel Instance { get; set; }
        public MainViewModel()
        {
            Instance = this;
            Title = "在线升级";

            ViewItems =
            [
               new("65在线升级程序", "适配65 BOOTLOAD",new UpdateDsp65(), IconType.Home2Line),
               new("通用在线升级程序", "适配天义厂原始 BOOTLOAD", new FC(), IconType.ComputerLine),
            ];

            CurrentViewItem = ViewItems.First();

            ThemeColors =
            [
                new ThemeColorInfo
                {
                    Name = "默认蓝",
                    Url = @"pack://application:,,,/UpdateDSP;component/Themes/BlueColor.xaml",
                    Primary = new SolidColorBrush(Color.FromRgb(0x21,0x96,0xF3)),
                    IsSeleted =true
                },
                new ThemeColorInfo
                {
                    Name = "酷安绿",
                    Url = @"pack://application:,,,/UpdateDSP;component/Themes/GreenColor.xaml",
                    Primary = new SolidColorBrush(Color.FromRgb(0x0B,0xA3,0x61)),
                    IsSeleted = false
                },
                new ThemeColorInfo
                {
                    Name = "网易红",
                    Primary = new SolidColorBrush(Color.FromRgb(0xE5,0x39,0x35)),
                    Url = @"pack://application:,,,/UpdateDSP;component/Themes/RedColor.xaml",
                    IsSeleted =false
                },
                new ThemeColorInfo
                {
                    Name = "妹妹紫",
                    Primary =new SolidColorBrush( Color.FromRgb(0x6A,0x1B,0x9A)),
                    Url = @"pack://application:,,,/UpdateDSP;component/Themes/PurpleColor.xaml",
                    IsSeleted =false
                },
                new ThemeColorInfo
                {
                    Name = "哔哩粉",
                    Primary = new SolidColorBrush(Color.FromRgb(0xFB,0x72,0x99)),
                    Url = @"pack://application:,,,/UpdateDSP;component/Themes/PinkColor.xaml",
                    IsSeleted =false
                },
            ];
        }

        [RelayCommand]
        private void ChangeThemeColor(ThemeColorInfo info)
        {
            if (info.IsSeleted)
            {
                return;
            }

            ThemeManager.ApplyThemeColor(info.Url);

            foreach (var item in ThemeColors)
            {
                item.IsSeleted = false;
            }

            info.IsSeleted = true;
        }

        [RelayCommand]
        private async Task OpenAboutDialog()
        {
            var content = new About();
            await Dialog.Show(content, title: "关于");
        }

    }
}