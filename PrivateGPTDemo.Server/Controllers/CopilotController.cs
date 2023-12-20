using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using PrivateGPTDemo.Server.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PrivateGPTDemo.Server.Controllers
{
    [ApiController]
    [Route("api/copilot/v1")]
    public class CopilotController : ControllerBase
    {
        private readonly IOpenAIClientFactory _openAIClientFactory;

        public CopilotController(IOpenAIClientFactory openAIClientFactory)
        {
            _openAIClientFactory = openAIClientFactory;
        }


        [HttpPost]
        [Route("send")]
        public async Task<ActionResult> SendMessage(string message, CancellationToken ct = default)
        {

            var client = _openAIClientFactory.GetClient();


            var serializationOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var funcs = new List<FunctionDefinition>() {
                new()
                {

                    Name = "get_time_in_city",
                    Description= "Get the current time in a given city in Europe",
                    Parameters = BinaryData.FromObjectAsJson(new {
                        Type = "object",
                        Properties = new {

                            City = new
                            {
                                Type = "string",
                                Description = "The city and state in Europe"
                            }

                        },
                        Required = new []{ "location" }

                    }, serializationOptions)
                }
            };


            var deploymentName = "gpt-35-turbo";


            var completions = await client.GetChatCompletionsAsync(
                new ChatCompletionsOptions(deploymentName, new List<ChatRequestMessage>()
                {
                    new ChatRequestUserMessage("What time is it in Amsterdam?")
                })
                {
                    Functions = funcs
                }, ct);


            foreach (var choice in completions.Value.Choices)
            {

                if (choice.Message.FunctionCall != null)
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(choice.Message.FunctionCall.Arguments);

                    var city = payload["city"];
                    var timeZone = await GetCurrentTime(city, ct);


                    var completions1 = await client.GetChatCompletionsAsync(new ChatCompletionsOptions(deploymentName,
                        new List<ChatRequestMessage>()
                    {

                        new ChatRequestUserMessage("What time is it in Amsterdam?"),
                        new ChatRequestFunctionMessage("get_time_in_city", timeZone)

                    }), ct);


                    var comp = completions1.Value.Choices.First();
                    Console.WriteLine(comp.Message.FunctionCall);

                }
                else
                {
                    Console.WriteLine(choice.Message.Content);
                }

            }


            return Ok();
        }

        private async Task<string> GetCurrentTime(string city, CancellationToken ct = default)
        {
            using var httpClient = new HttpClient();

            try
            {

                string url = $"http://worldtimeapi.org/api/timezone/Europe/{city}";

                var response = await httpClient.GetAsync(url, ct);

                var responseBody = await response.Content.ReadAsStringAsync(ct);

                var jsonResponse = JsonNode.Parse(responseBody);

                return responseBody;

            }
            catch (HttpRequestException e)
            {

                Console.WriteLine($"Message = {e.Message}");
                return null;
            }
        }
    }
}
