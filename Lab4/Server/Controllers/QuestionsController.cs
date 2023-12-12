using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json;
using NuGetQA;

[Route("api/questions")]
[ApiController]
public class QuestionsController : ControllerBase
{
    public LLM llm;
    string modelPath = "bert-large-uncased-whole-word-masking-finetuned-squad.onnx";
    public CancellationTokenSource cts;

    public QuestionsController()
    {
        cts = new CancellationTokenSource();
        llm = new LLM(modelPath, cts.Token);
        llm.DownloadModel(modelPath);
    }


    [HttpGet]
    public IActionResult Index()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "../questions.html");
        return Content(System.IO.File.ReadAllText(filePath), "text/html");
    }
    private async Task<string> Answer(string text, List<string> questions)
    {
        List<string> answers = new List<string>();
        foreach (var question in questions)
        {
            string answer = await llm.GetAnswerAsync(text, question);
            answers.Add(answer);
        }
        return JsonSerializer.Serialize(new { Answers = answers });
    }

    [HttpPost]
    public async Task<ActionResult<string>> Post([FromBody] QuestionRequest request) // string request
    {
        Console.WriteLine(request);
        var data = request;// JsonSerializer.Deserialize<QuestionRequest>(request);
        string text = data.Text;
        List<string> questions = data.Questions;
        Console.WriteLine(questions);
        string result = await Answer(text, questions);
        return Ok(result);
    }
}

public class QuestionRequest
{
    public string Text { get; set; }
    public List<string> Questions { get; set; }
}
