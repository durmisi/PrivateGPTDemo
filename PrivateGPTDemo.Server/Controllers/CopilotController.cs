using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PrivateGPTDemo.Server.Controllers
{
    [ApiController]
    public class CopilotController : ControllerBase
    {

        [HttpPost]
        public async Task<ActionResult> SendMessage(string message, CancellationToken ct = default)
        {

            var client = new OpenAIClient(new Uri(""), new DefaultAzureCredential());


            var serializationOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var funcs = new List<FunctionDefinition>() {
                new FunctionDefinition()
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



            var completions = client.GetChatCompletions(
                new ChatCompletionsOptions("gpt-35-turbo", new List<ChatRequestMessage>()
                {
                    new ChatRequestFunctionMessage("test", await GetCurrentTime("Europe/Berlin")),
                    new ChatRequestUserMessage("What time is it in Amsterdam?")
                })
                {
                    Functions = funcs
                });


            foreach (var choice in completions.Value.Choices)
            {

                if (choice.Message.FunctionCall != null)
                {
                    var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(choice.Message.FunctionCall.Arguments);

                    var city = payload["city"];
                    var timeZone = await GetCurrentTime(city);


                    var completions1 = client.GetChatCompletions(new ChatCompletionsOptions("", new List<ChatRequestMessage>()
                    {

                        new ChatRequestUserMessage("What time is it in Amsterdam?"),
                        new ChatRequestFunctionMessage("get_time_in_city", timeZone)

                    }));


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

        private Task<string> GetCurrentTime(string city)
        {
            throw new NotImplementedException();
        }
    }
}
