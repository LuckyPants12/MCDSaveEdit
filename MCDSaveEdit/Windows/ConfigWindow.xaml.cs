using System.IO;
using System.Windows;
#nullable enable

namespace MCDSaveEdit
{
    /// <summary>
    /// Interaction logic for GameFilesWindow.xaml
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            InitializeComponent();

            Title = "SETTINGS";
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            EventLogger.logEvent("exitButton_Click");
            this.Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            EventLogger.logEvent("okButton_Click");
            Properties.Settings.Default["AES_KEY"] = pathTextBox.Text;
            Properties.Settings.Default.Save();
            this.Close();
            return;
        }

        private void pathTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}
