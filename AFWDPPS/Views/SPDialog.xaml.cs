using Rubyer;

namespace 导引头上位机程序.Views
{
    public partial class SPDialog : RubyerWindow
    {
        public SPDialog(string title, object v)
        {
            InitializeComponent();

            this.Content = v;

            // 设置其他属性如大小、标题等
            this.Title = title;
            this.Width = 1200;
            this.Height = 900;
        }
    }
}
