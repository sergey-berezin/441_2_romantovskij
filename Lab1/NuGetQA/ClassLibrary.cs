using BERTTokenizers;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Net;

namespace NuGetQA
{
    public class LLM
    {

        static InferenceSession session;
        static CancellationToken cancelToken;
        static string modelPath;
        static string modelUrl = "https://storage.yandexcloud.net/dotnet4/bert-large-uncased-whole-word-masking-finetuned-squad.onnx";


        public LLM(string _modelPath, CancellationToken _cancelToken)
        {
            modelPath = _modelPath;
            cancelToken = _cancelToken;
            session = new InferenceSession(modelPath);
        }


        public void DownloadModel(string modelPath)
        {
            if (File.Exists(modelPath))
                return;

            int retries = 10;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    WebClient myWebClient = new WebClient();
                    myWebClient.DownloadFile(modelUrl, modelPath);
                    return;
                }
                catch (Exception) { }
            }
            throw new Exception("Failed to download model");
        }

        public Task<string> GetAnswerAsync(string text, string question)
        {
            return Task.Factory.StartNew(() => {

                try
                {
                    cancelToken.ThrowIfCancellationRequested();
                    var sentence = "{\"question\": \"" + question + "\", \"context\": \"@CTX\"}".Replace("@CTX", text);

                    // Create Tokenizer and tokenize the sentence.
                    var tokenizer = new BertUncasedLargeTokenizer();

                    // Get the sentence tokens.
                    cancelToken.ThrowIfCancellationRequested();
                    var tokens = tokenizer.Tokenize(sentence);
                    // Console.WriteLine(String.Join(", ", tokens));

                    // Encode the sentence and pass in the count of the tokens in the sentence.
                    cancelToken.ThrowIfCancellationRequested();
                    var encoded = tokenizer.Encode(tokens.Count(), sentence);

                    // Break out encoding to InputIds, AttentionMask and TypeIds from list of (input_id, attention_mask, type_id).
                    cancelToken.ThrowIfCancellationRequested();
                    var bertInput = new BertInput()
                    {
                        InputIds = encoded.Select(t => t.InputIds).ToArray(),
                        AttentionMask = encoded.Select(t => t.AttentionMask).ToArray(),
                        TypeIds = encoded.Select(t => t.TokenTypeIds).ToArray(),
                    };


                    // Create input tensor.
                    cancelToken.ThrowIfCancellationRequested();
                    var input_ids = ConvertToTensor(bertInput.InputIds, bertInput.InputIds.Length);
                    var attention_mask = ConvertToTensor(bertInput.AttentionMask, bertInput.InputIds.Length);
                    var token_type_ids = ConvertToTensor(bertInput.TypeIds, bertInput.InputIds.Length);


                    // Create input data for session.
                    var input = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input_ids", input_ids),
                                                            NamedOnnxValue.CreateFromTensor("input_mask", attention_mask),
                                                            NamedOnnxValue.CreateFromTensor("segment_ids", token_type_ids) };


                    // Run session and send the input data in to get inference output.
                    cancelToken.ThrowIfCancellationRequested();
                    IDisposableReadOnlyCollection<DisposableNamedOnnxValue>? output;

                    lock (session)
                    {
                        output = session.Run(input);
                    }
                    cancelToken.ThrowIfCancellationRequested();
                    // Call ToList on the output.
                    // Get the First and Last item in the list.
                    // Get the Value of the item and cast as IEnumerable<float> to get a list result.
                    List<float> startLogits = (output.ToList().First().Value as IEnumerable<float>).ToList();
                    List<float> endLogits = (output.ToList().Last().Value as IEnumerable<float>).ToList();

                    // Get the Index of the Max value from the output lists.
                    var startIndex = startLogits.ToList().IndexOf(startLogits.Max());
                    var endIndex = endLogits.ToList().IndexOf(endLogits.Max());

                    // From the list of the original tokens in the sentence
                    // Get the tokens between the startIndex and endIndex and convert to the vocabulary from the ID of the token.
                    var predictedTokens = tokens
                                .Skip(startIndex)
                                .Take(endIndex + 1 - startIndex)
                                .Select(o => tokenizer.IdToToken((int)o.VocabularyIndex))
                                .ToList();


                    return String.Join(" ", predictedTokens);
                }
                catch (Exception ex) { return ex.Message; }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        }
        public static Tensor<long> ConvertToTensor(long[] inputArray, int inputDimension)
        {
            // Create a tensor with the shape the model is expecting. Here we are sending in 1 batch with the inputDimension as the amount of tokens.
            Tensor<long> input = new DenseTensor<long>(new[] { 1, inputDimension });

            // Loop through the inputArray (InputIds, AttentionMask and TypeIds)
            for (var i = 0; i < inputArray.Length; i++)
            {
                // Add each to the input Tenor result.
                // Set index and array value of each input Tensor.
                input[0, i] = inputArray[i];
            }
            return input;
        }

    }
    public class BertInput
    {
        public long[] InputIds { get; set; }
        public long[] AttentionMask { get; set; }
        public long[] TypeIds { get; set; }
    }
}