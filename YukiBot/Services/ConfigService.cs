using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using YukiBot.Client;

namespace YukiBot.Services
{
	internal class ConfigService
	{
		public AppEnvironment AppEnvironment { get; }
		public string CosmosConnectionString { get; }
		public string DiscordBotToken { get; }
		public AppSettings AppSettings { get; }

		public IClientToken Token => new ClientToken { Config = this };
		
		public ConfigService () {
			var appEnv = Environment.GetEnvironmentVariable("APP_ENVIRONMENT");
			AppEnvironment = appEnv is "development" ? AppEnvironment.Development : AppEnvironment.Production;

			CosmosConnectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING")
				?? throw new Exception("A CosmosDB connection string should be provided in the 'COSMOS_CONNECTION_STRING' environment variable.");
			DiscordBotToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")
				?? throw new Exception("A Discord Bot token should be provided in the 'DISCORD_BOT_TOKEN' environment variable.");

			var settingsFile = AppEnvironment is AppEnvironment.Development ? "appSettings.development.json" : "appSettings.json";
			AppSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsFile)) 
				?? new AppSettings() {
					CosmosDatabaseName = "caswell",
					CosmosContainerName = "yuki-bot",
				};
		}

		class ClientToken : IClientToken
		{
			public required ConfigService Config { get; set; }
			public string Value => Config.DiscordBotToken;
		}
	}

	public class AppSettings
	{
		public required string CosmosDatabaseName { get; set; }
		public required string CosmosContainerName { get; set; }
		// TODO: a better way to manage what channel to post to for a job
		//       maybe DB powered?
		public ulong TheaterChannelId { get; set; }
	}

	internal enum AppEnvironment
	{
		Development,
		Production
	}

	static class ConfigServiceExtensions
	{
		public static IServiceCollection AddConfigService (this IServiceCollection services)
		{
			var config = new ConfigService();
			return services
				.AddSingleton(config)
				.AddSingleton(config.Token);
		}
	}
}
