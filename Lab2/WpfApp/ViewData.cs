using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using NuGetQA;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.ComponentModel;

namespace WpfApp
{
    public class ViewData
    {
        public LLM llm;
        public bool isDownloaded = false;
        string modelPath = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        public CancellationTokenSource cts;
        public string text;
        public string textHash;
        public List<string> Chat { get; set; }
        public Dictionary<string, Dictionary<string, string>> Answers { get; set; }  // {hash(text): {question: answer}}

        public string chatHistoryFileName = "chat_history";

        public ViewData()
        {
            cts = new CancellationTokenSource();
            llm = new LLM(modelPath, cts.Token);
            text = "";
            textHash = "";
            Chat = new List<string>();
            Answers = new Dictionary<string, Dictionary<string, string>>();
        }

        public void SaveChat()
        {
            try
            {
                string jsonString = JsonConvert.SerializeObject(
                    new Tuple<List<string>, Dictionary<string, Dictionary<string, string>>>(Chat, Answers));
                string tempFilePath = chatHistoryFileName + ".temp";

                File.WriteAllText(tempFilePath, jsonString);
                File.Replace(tempFilePath, chatHistoryFileName, null);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении файла: {ex.Message}");
            }
        }

        public void LoadChat() 
        {
            try
            {
                if (File.Exists(chatHistoryFileName))
                {
                    string jsonString = File.ReadAllText(chatHistoryFileName);
                    var data = JsonConvert.DeserializeObject<Tuple<List<string>, Dictionary<string, Dictionary<string, string>>>>(jsonString);
                    Chat = data.Item1;
                    Answers = data.Item2;
                    for (int i = 0; i < Chat.Count; i++)
                    {
                        if (Chat[i].StartsWith("/load"))
                        {
                            text = Chat[i + 1];
                            textHash = GetHash(text);
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                throw new Exception($"Ошибка при загрузке файла: {ex.Message}");
            }
        }

        public void UpdateText(string fileName) 
        {
            text = File.ReadAllText(fileName);
            textHash = GetHash(text);
            Chat.Add(text);
            if (!Answers.ContainsKey(textHash))
                Answers[textHash] = new Dictionary<string, string>();

        }

        public void DeleteChat() 
        {
            File.Delete(chatHistoryFileName);
            Chat.Clear();
            Answers.Clear();
            text = "";
            textHash = "";
        }


        public Task DownloadAsync()
        {
            return Task.Factory.StartNew(() => {

                try
                {
                    llm.DownloadModel(modelPath);
                    isDownloaded = true;
                    return;
                }
                catch { }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public string GetHash(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < 4; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
