using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukiBot.Client;

namespace YukiBot.Services
{
	internal class ConfigService
	{
		public AppEnvironment AppEnvironment { get; }
		public string CosmosConnectionString { get; }
		public string CosmosDatabaseName { get; }
		public string CosmosContainerName { get; }
		public string DiscordBotToken { get; }

		public IClientToken Token => new ClientToken { Config = this };
		
		public ConfigService () {
			var appEnv = Environment.GetEnvironmentVariable("APP_ENVIRONMENT");
			AppEnvironment = appEnv is "development" ? AppEnvironment.Development : AppEnvironment.Production;

			CosmosConnectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING")
				?? throw new Exception("A CosmosDB connection string should be provided in the 'COSMOS_CONNECTION_STRING' environment variable.");
			DiscordBotToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")
				?? throw new Exception("A Discord Bot token should be provided in the 'DISCORD_BOT_TOKEN' environment variable.");

			CosmosDatabaseName = Environment.GetEnvironmentVariable("COSMOS_DATABASE_NAME")
				?? "caswell";
			CosmosContainerName = Environment.GetEnvironmentVariable("COSMOS_CONTAINER_NAME")
				?? "yuki-bot";
		}

		private class ClientToken : IClientToken
		{
			public required ConfigService Config { get; set; }
			public string Value => Config.DiscordBotToken;
		}
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
