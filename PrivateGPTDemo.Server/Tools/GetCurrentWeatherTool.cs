using Azure;
using Azure.AI.OpenAI;
using PrivateGPTDemo.Server.Services;
using System.Text.Json;

namespace PrivateGPTDemo.Server.Tools
{
    public class GetCurrentWeatherTool : IChatMessageHandler
    {
        private readonly IOpenAIClientFactory _openAIClientFactory;

        public GetCurrentWeatherTool(IOpenAIClientFactory openAIClientFactory)
        {
            _openAIClientFactory = openAIClientFactory;
        }


        private ChatCompletionsFunctionToolDefinition getWeatherTool = new ChatCompletionsFunctionToolDefinition()
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


        public async Task HandleMessage(string message, CancellationToken ct = default)
        {

            var client = _openAIClientFactory.GetClient();
            var deploymentName = _openAIClientFactory.GetDeploymentName("gpt-35-turbo");

            var messages = new List<ChatRequestMessage> {
                new ChatRequestSystemMessage(@"
                    You are a weather information assistant. 
                    It is crucial that you do not generate any responses that involve any programming language, code, or coding instructions. 
                    Stick strictly to providing weather-related information.
                "),
                new ChatRequestUserMessage(message)
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
                Temperature = 0,
                Tools = { getWeatherTool },
                ToolChoice = ChatCompletionsToolChoice.Auto,
                AzureExtensionsOptions = null
            };

            Response<ChatCompletions> response = await client.GetChatCompletionsAsync(chatCompletionsOptions, ct);

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

                Response<ChatCompletions> responseWithToolCall = await client.GetChatCompletionsAsync(chatCompletionsOptions, ct);

                foreach (var choice in responseWithToolCall.Value.Choices)
                {
                    Console.WriteLine(choice.Message.Content);
                }

            }
            else if (responseChoice.FinishReason == CompletionsFinishReason.Stopped)
            {
                foreach (var choice in response.Value.Choices)
                {
                    Console.WriteLine(choice.Message.Content);
                }
            }

            #endregion
        }

    }
}
