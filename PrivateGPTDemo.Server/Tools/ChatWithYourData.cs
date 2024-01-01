using Azure;
using Azure.AI.OpenAI;
using PrivateGPTDemo.Server.Services;

namespace PrivateGPTDemo.Server.Tools
{
    public class ChatWithYourData : IChatMessageHandler
    {
        private readonly IOpenAIClientFactory _openAIClientFactory;

        private AzureCognitiveSearchChatExtensionConfiguration _contosoExtensionConfig;

        public ChatWithYourData(IOpenAIClientFactory openAIClientFactory, 
            string searchEndpoint,
            string indexName,
            string apiKey
            )
        {
            _openAIClientFactory = openAIClientFactory;

            _contosoExtensionConfig = new()
            {
                SearchEndpoint = new Uri(searchEndpoint),
                Authentication = new OnYourDataApiKeyAuthenticationOptions(apiKey),
                IndexName = indexName
            };
        }


        public async Task HandleMessage(string message, CancellationToken ct = default)
        {
            var client = _openAIClientFactory.GetClient();
            var deploymentName = _openAIClientFactory.GetDeploymentName("gpt-35-turbo");


            ChatCompletionsOptions chatCompletionsOptions = new()
            {
                DeploymentName = deploymentName,
                Messages =
            {
                new ChatRequestSystemMessage(
                    "You are a helpful assistant that answers questions about our Hotels database."),
                new ChatRequestUserMessage(message)
            },

                // The addition of AzureChatExtensionsOptions enables the use of Azure OpenAI capabilities that add to
                // the behavior of Chat Completions, here the "using your own data" feature to supplement the context
                // with information from an Azure Cognitive Search resource with documents that have been indexed.
                AzureExtensionsOptions = new AzureChatExtensionsOptions()
                {
                    Extensions = { _contosoExtensionConfig }
                }
            };

            Response<ChatCompletions> response = await client.GetChatCompletionsAsync(chatCompletionsOptions, ct);
            ChatResponseMessage chatResponseMessage = response.Value.Choices[0].Message;

            // The final, data-informed response still appears in the ChatMessages as usual
            Console.WriteLine($"{chatResponseMessage.Role}: {chatResponseMessage.Content}");

            // Responses that used extensions will also have Context information that includes special Tool messages
            // to explain extension activity and provide supplemental information like citations.
            Console.WriteLine($"Citations and other information:");

            foreach (ChatResponseMessage contextMessage in chatResponseMessage.AzureExtensionsContext.Messages)
            {
                // Note: citations and other extension payloads from the "tool" role are often encoded JSON documents
                // and need to be parsed as such; that step is omitted here for brevity.
                Console.WriteLine($"{contextMessage.Role}: {contextMessage.Content}");
            }
        }

    }
}
