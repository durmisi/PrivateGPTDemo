using PrivateGPTDemo.Server.Services;
using PrivateGPTDemo.Server.Tools;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;


// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



//open ai
builder.Services.AddOpenAI();

builder.Services.AddScoped<IChatMessageHandler, GetCurrentWeatherTool>();

var searchEndpoint = configuration.GetValue<string>("OpenAI:SearchService:Endpoint");

if (!string.IsNullOrEmpty(searchEndpoint))
{
    builder.Services.AddScoped<IChatMessageHandler>(sp =>
    {
        var indexName = configuration.GetValue<string>("OpenAI:SearchService:IndexName")!;
        var apiKey = configuration.GetValue<string>("OpenAI:SearchService:ApiKey")!;
        return new ChatWithYourData(sp.GetRequiredService<IOpenAIClientFactory>(), searchEndpoint, indexName, apiKey);
    });
}

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
