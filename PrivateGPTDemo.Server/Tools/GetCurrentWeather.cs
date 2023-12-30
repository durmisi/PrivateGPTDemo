using Azure;
using Azure.AI.OpenAI;
using PrivateGPTDemo.Server.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PrivateGPTDemo.Server.Tools
{
    public class GetCurrentWeather : IChatMessageHandler
    {
        private readonly IOpenAIClientFactory _openAIClientFactory;

        public GetCurrentWeather(IOpenAIClientFactory openAIClientFactory)
        {
            _openAIClientFactory = openAIClientFactory;
        }


        public async Task HandleMessage(string message, CancellationToken ct = default)
        {
            var deploymentName = "gpt-35-turbo-0613";


            var client = _openAIClientFactory.GetClient();


            #region Snippet:ChatTools:DefineTool
            var getWeatherTool = new ChatCompletionsFunctionToolDefinition()
            {
                Name = "get_current_weather",
                Description = "Get the current weather in a given location",
                Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Location = new
                        {
                            Type = "string",
                            Description = "The city and state, e.g. San Francisco, CA",
                        },
                        Unit = new
                        {
                            Type = "string",
                            Enum = new[] { "celsius", "fahrenheit" },
                        }
                    },
                    Required = new[] { "location" },
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            };
            #endregion



            #region Snippet:ChatTools:RequestWithFunctions


            var messages = new List<ChatRequestMessage> {
                new ChatRequestUserMessage("What's the weather like in Boston?")
            };

            foreach (var item in messages)
            {
                if (item is ChatRequestUserMessage userMessage)
                {
                    Console.WriteLine(userMessage.Content);
                }
            }


            var chatCompletionsOptions = new ChatCompletionsOptions(deploymentName, messages)
            {
                Tools = { getWeatherTool },
                ToolChoice = ChatCompletionsToolChoice.Auto,
                AzureExtensionsOptions = null
            };

            Response<ChatCompletions> response = client.GetChatCompletions(chatCompletionsOptions);

            #endregion


            #region Snippet:ChatTools:HandleToolCalls
            // Purely for convenience and clarity, this standalone local method handles tool call responses.
            ChatRequestToolMessage GetToolCallResponseMessage(ChatCompletionsToolCall toolCall)
            {
                var functionToolCall = toolCall as ChatCompletionsFunctionToolCall;
                if (functionToolCall?.Name == getWeatherTool.Name)
                {
                    // Validate and process the JSON arguments for the function call
                    string unvalidatedArguments = functionToolCall.Arguments;
                    var functionResultData = (object)null; // GetYourFunctionResultData(unvalidatedArguments);
                                                           // Here, replacing with an example as if returned from "GetYourFunctionResultData"
                    functionResultData = "31 celsius";
                    return new ChatRequestToolMessage(functionResultData.ToString(), toolCall.Id);
                }
                else
                {
                    // Handle other or unexpected calls
                    throw new NotImplementedException();
                }
            }
            #endregion

            #region Snippet:ChatTools:HandleResponseWithToolCalls
            ChatChoice responseChoice = response.Value.Choices[0];
            if (responseChoice.FinishReason == CompletionsFinishReason.ToolCalls)
            {
                // Add the assistant message with tool calls to the conversation history
                ChatRequestAssistantMessage toolCallHistoryMessage = new(responseChoice.Message);
                chatCompletionsOptions.Messages.Add(toolCallHistoryMessage);

                // Add a new tool message for each tool call that is resolved
                foreach (ChatCompletionsToolCall toolCall in responseChoice.Message.ToolCalls)
                {
                    chatCompletionsOptions.Messages.Add(GetToolCallResponseMessage(toolCall));
                }

                // Now make a new request with all the messages thus far, including the original

                Response<ChatCompletions> response2 = client.GetChatCompletions(chatCompletionsOptions);

                foreach (var choice in response2.Value.Choices)
                {
                    Console.WriteLine(choice.Message.Content);
                }

            }

            #endregion

            #region Snippet:ChatTools:UseToolChoice
            chatCompletionsOptions.ToolChoice = ChatCompletionsToolChoice.Auto; // let the model decide
            chatCompletionsOptions.ToolChoice = ChatCompletionsToolChoice.None; // don't call tools
            chatCompletionsOptions.ToolChoice = getWeatherTool; // only use the specified tool
            #endregion

        }

    }
}
