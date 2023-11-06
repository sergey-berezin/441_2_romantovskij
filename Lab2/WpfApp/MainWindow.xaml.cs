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
using System.Security.Cryptography;

namespace WpfApp
{


    public partial class MainWindow : Window
    {
        ViewData viewData = new ViewData();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewData;
            listViewChat.ItemsSource = viewData.Chat;
            viewData.DownloadAsync();
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            btnSend.IsEnabled = false;
            try
            {
                string question = textBoxEntry.Text;
                textBoxEntry.Clear();
                viewData.Chat.Add(question);
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
                        viewData.UpdateText(openFileDialog.FileName);
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
                    string answer;

                    if (viewData.Answers[viewData.textHash].ContainsKey(question))
                    {
                        answer = viewData.Answers[viewData.textHash][question];
                    }
                    else
                    {
                        answer = await viewData.llm.GetAnswerAsync(viewData.text, question);
                    }
                    if (answer != null)
                    {
                        if(answer != "The operation was canceled.")
                            viewData.Answers[viewData.textHash][question] = answer;
                        viewData.Chat.Add("Answer: " + answer);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            listViewChat.Items.Refresh();
            btnSend.IsEnabled = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => viewData.cts.Cancel();

        private void btnDelete_Click(object sender, RoutedEventArgs e) 
        {
            viewData.DeleteChat();
            listViewChat.Items.Refresh();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) => viewData.SaveChat();

    }
}
