﻿using NuGetQA;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {

        string text = File.ReadAllText(args[0]);

        var cts = new CancellationTokenSource();
        string modelPath = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        var llm = new LLM(modelPath, cts.Token);
        llm.DownloadModel(modelPath);
        var taskList = new List<Task>();
        Console.Write("Write questions:\n");
        while (!cts.Token.IsCancellationRequested)
        {
            string question = Console.ReadLine();

            if (question == "")
                cts.Cancel();

            var task = llm.GetAnswerAsync(text, question).ContinueWith(task => { Console.WriteLine("\n" + question + " : " + task.Result); });
            taskList.Add(task);

        }
        await Task.WhenAll(taskList);
    }
}