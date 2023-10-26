using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGetQA;

namespace WpfApp
{
    public class ViewData
    {
        public LLM llm;
        public bool isDownloaded = false;
        string modelPath = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
        public CancellationTokenSource cts;
        public string text = "";
        public List<string> chat = new List<string>();

        public ViewData()
        {
            cts = new CancellationTokenSource();
            llm = new LLM(modelPath, cts.Token);

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
    }
}
