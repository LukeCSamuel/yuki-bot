using Microsoft.Extensions.DependencyInjection;
using YukiBot.Client;
using YukiBot.Handlers.Message;
using YukiBot.Jobs;
using YukiBot.Services;

Console.WriteLine("Starting YukiBot...");

ClientBuilder.Create()
	.ConfigureServices((services) => services
		.AddConfigService()
		.AddHttpClient()
		.AddSingleton<CosmosService>()
		.AddSingleton<TheaterEventService>()
	)
	.AddComponent<DirectMessageHandler>()
	.AddComponent<BanFrench>()
	.AddComponent<NytCongrats>()
	.AddComponent<TheaterEventJob>()
	.Build()
	.Run();
