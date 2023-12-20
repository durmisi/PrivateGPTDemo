namespace PrivateGPTDemo.Server.Services
{
    public static class ServiceCollectionExtensions
    {
        public static void AddOpenAI(this IServiceCollection services)
        {
            services.AddTransient<IOpenAIClientFactory, OpenAIClientFactory>();
        }
    }
}
