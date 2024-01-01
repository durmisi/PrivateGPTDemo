using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;

namespace PrivateGPTDemo.Server.Services
{

    public interface IOpenAIClientFactory
    {
        public OpenAIClient GetClient();
        string GetDeploymentName(string name);
    }


    public class OpenAIClientFactory : IOpenAIClientFactory
    {
        private readonly IConfiguration _configuration;

        public OpenAIClientFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public OpenAIClient GetClient()
        {
            var endpoint = _configuration.GetValue<string>("OpenAI:Endpoint");

            if (string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            var key = _configuration.GetValue<string>("OpenAI:Key");

            if (!string.IsNullOrEmpty(key))
            {
                return new OpenAIClient(
                  new Uri(endpoint),
                  new AzureKeyCredential(key));
            }

            return new OpenAIClient(
                  new Uri(endpoint),
                  new DefaultAzureCredential());
        }

        public string GetDeploymentName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }

            return _configuration.GetValue<string>($"OpenAI:Deployments:{name}")!;
        }
    }
}
