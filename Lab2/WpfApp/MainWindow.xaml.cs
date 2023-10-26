using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;

namespace WpfApp
{


    public partial class MainWindow : Window
    {
        ViewData viewData = new ViewData();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewData;
            listViewChat.ItemsSource = viewData.chat;
            viewData.DownloadAsync();
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string question = textBoxEntry.Text;
                //textBoxEntry.Clear();
                viewData.chat.Add(question);
                listViewChat.Items.Refresh();
                if (question.StartsWith("/load"))
                {
                    var openFileDialog = new OpenFileDialog()
                    {
                        Title = "File",
                        Filter = "Text Document (*.txt) | *.txt",
                        FileName = ""
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        viewData.text = File.ReadAllText(openFileDialog.FileName);
                        viewData.chat.Add(viewData.text);
                    }
                }
                else if (!viewData.isDownloaded)
                {
                    MessageBox.Show("Wait while the model is loading...");
                }
                else if (viewData.text == "")
                {
                    MessageBox.Show("Enter command /load to select text");
                }
                else
                {
                    var answer = await viewData.llm.GetAnswerAsync(viewData.text, question);
                    if (answer != null)
                    {
                        viewData.chat.Add("Answer: " + answer);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            listViewChat.Items.Refresh();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            viewData.cts.Cancel();
        }
    }
}
