
using System.Configuration;
using System.Windows;

namespace LabTool
{
    /// <summary>
    /// InputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InputDialog : Window
    {
        public InputDialog()
        {
            InitializeComponent();

            // 读取参数
            string yourParameter = ConfigurationManager.AppSettings["path"];
            inputTextBox.Text = yourParameter;

        }

        public string UserInput { get; private set; }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            UserInput = inputTextBox.Text;
            DialogResult = true;

            // 获取用户输入
            string userInput = inputTextBox.Text;

            // 存储参数
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["path"].Value = userInput;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

}
