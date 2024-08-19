using CommunityToolkit.Mvvm.ComponentModel;

namespace UpdateDSP.ViewModels
{
    /// <summary>
    /// 文本框
    /// </summary>
    public partial class TextBoxViewModel : ObservableObject
    {
        [ObservableProperty]
        private int? number;
    }
}
