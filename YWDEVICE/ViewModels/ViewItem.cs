using CommunityToolkit.Mvvm.ComponentModel;
using Rubyer;

namespace YWDEVICE.ViewModels
{
    /// <summary>
    /// 视图项
    /// </summary>
    [INotifyPropertyChanged]
#pragma warning disable MVVMTK0032 // Inherit from ObservableObject instead of using [INotifyPropertyChanged]
    public partial class ViewItem
#pragma warning restore MVVMTK0032 // Inherit from ObservableObject instead of using [INotifyPropertyChanged]
    {
        /// <summary>
        /// 名称
        /// </summary>
        [ObservableProperty]
        private string name;

        /// <summary>
        /// 描述
        /// </summary>
        [ObservableProperty]
        private string description;

        /// <summary>
        /// 图标类型
        /// </summary>
        [ObservableProperty]
        private IconType? iconType;

        /// <summary>
        /// 内容
        /// </summary>
        [ObservableProperty]
        private object content;

        public ViewItem(string name, string description, object content, IconType? iconType = null)
        {
            Name = name;
            Description = description;
            Content = content;
            IconType = iconType;
        }
    }
}
