using System.Windows;

namespace LumenXEditor.GameProject
{
    /// <summary>
    /// Interaction logic for ProjectBrowserDialog.xaml
    /// </summary>
    public partial class ProjectBrowserDialog : Window
    {
        public ProjectBrowserDialog()
        {
            InitializeComponent();
        }

		private void ButtonClicked(object sender, RoutedEventArgs e)
		{
            if (sender == openProjectButton)
            {
                if (createProjectButton.IsChecked == true)
                {
                    createProjectButton.IsChecked = false;
					Boarders.Margin = new Thickness(0, 0, 0, 0);
				}
                openProjectButton.IsChecked = true;
            }
            else
            {
                if (openProjectButton.IsChecked == true)
                {
                    openProjectButton.IsChecked = false;
					Boarders.Margin = new Thickness(-800, 0, 0, 0);
				}
                createProjectButton.IsChecked = true;
            }
		}
	}
}
