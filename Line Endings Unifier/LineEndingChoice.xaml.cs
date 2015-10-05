using System.Windows;
using System.Windows.Controls;

namespace JakubBielawa.LineEndingsUnifier
{
    public partial class LineEndingChoice : Window
    {
        private LineEndingsChanger.LineEndings lineEndings = LineEndingsChanger.LineEndings.None;

        public LineEndingChoice()
        {
            InitializeComponent();
        }

        public LineEndingChoice(string fileName)
        {
            InitializeComponent();
            this.Title = fileName;
        }

        public LineEndingsChanger.LineEndings LineEndings
        {
            get
            {
                return lineEndings;
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            
            if (button.Content.ToString().Contains("Windows"))
            {
                this.lineEndings = LineEndingsChanger.LineEndings.Windows;
            }
            else if (button.Content.ToString().Contains("Linux"))
            {
                this.lineEndings = LineEndingsChanger.LineEndings.Linux;
            }
            else if (button.Content.ToString().Contains("Macintosh"))
            {
                this.lineEndings = LineEndingsChanger.LineEndings.Macintosh;
            }
            else
            {
                this.lineEndings = LineEndingsChanger.LineEndings.None;
            }
        }

        private void Change_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.lineEndings = LineEndingsChanger.LineEndings.None;
            this.DialogResult = false;
            this.Close();
        }
    }
}
