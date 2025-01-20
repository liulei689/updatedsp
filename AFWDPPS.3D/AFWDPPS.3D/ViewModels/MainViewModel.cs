using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Reflection;
using System.Windows.Threading;

namespace WpfApp3D.ViewModels
{
    internal class MainViewModel : ObservableObject
    {
        private DateTime _currentTime = DateTime.Now;

        public DateTime CurrentTime
        {
            get { return _currentTime; }
            set { SetProperty(ref _currentTime, value); }
        }

        private object _pageView;

        public object PageView
        {
            get { return _pageView; }
            set { SetProperty<object>(ref _pageView, value); }
        }

        public RelayCommand<string> NavCommand { get; set; }

        DispatcherTimer timer = new DispatcherTimer();
        public MainViewModel()
        {
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += Timer_Tick;
            timer.Start();


            NavCommand = new RelayCommand<string>(DoNavigation);
            this.DoNavigation("Dome1");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CurrentTime = DateTime.Now;
        }

        private void DoNavigation(string page)
        {
            // 拿到了字符串的页面名称 
            // 创建对应的页面了   进行显示 
            // 反射
            Type type = Assembly.GetExecutingAssembly().GetType("WpfApp3D.View." + page);
            if (type != null)
            {
                object instance = Activator.CreateInstance(type);// 对应的页面

                PageView = instance;
                return;
            }


            type = Assembly.GetExecutingAssembly().GetType("WpfApp3D." + page);
            if (type != null)
            {
                object instance = Activator.CreateInstance(type);// 对应的页面

                PageView = instance;
                return;
            }
        }
    }
}
