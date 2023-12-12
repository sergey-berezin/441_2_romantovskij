using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using System.Text;
using System.Net.Http.Json;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;

namespace Tests
{
    public class TextControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> factory;
        public TextControllerTests(WebApplicationFactory<Program> factory)
        {
            this.factory = factory;
        }
        [Fact]
        public async Task NumQuestionsTest()
        {
            var client = factory.CreateClient();
            var json = "{\"Text\":\"Exapmle\",\"Questions\":[\"q1\", \"q2\", \"q3\"]}";
            var postResponse = await client.PostAsJsonAsync("api/questions", json);
            postResponse.EnsureSuccessStatusCode();
            var answersJson = await postResponse.Content.ReadAsStringAsync();
            var answers = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(answersJson);
            Assert.Equal(3, answers["Answers"].Count);
        }

        [Fact]
        public async Task AnswerTest()
        {
            var client = factory.CreateClient();
            var json = "{\"Text\":\"Today will be rain\",\"Questions\":[\"What will be today?\"]}";
            var postResponse = await client.PostAsJsonAsync("api/questions", json);
            postResponse.EnsureSuccessStatusCode();
            string answersJson = await postResponse.Content.ReadAsStringAsync();
            var answers = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(answersJson);
            Assert.Equal("rain", answers["Answers"][0]);
        }
    }
}