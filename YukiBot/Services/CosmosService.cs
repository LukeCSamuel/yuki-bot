using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using YukiBot.Models;

namespace YukiBot.Services
{
	internal class CosmosService
	{
		readonly CosmosClient _client;
		string DatabaseName { get; }
		string ContainerName { get; }
		Task ServiceReady { get; }

		public CosmosService (ConfigService config)
		{
			DatabaseName = config.AppSettings.CosmosDatabaseName;
			ContainerName = config.AppSettings.CosmosContainerName;

			CosmosClientOptions? options = null;
			if (config.AppEnvironment is AppEnvironment.Development)
			{
				// Disable TLS/SSL in development environment because the emulated DB has an untrusted cert
				options = new()
				{
					HttpClientFactory = () => new HttpClient(new HttpClientHandler()
					{
						ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
					}),
					ConnectionMode = ConnectionMode.Gateway,
				};
			}
			_client = new CosmosClient(config.CosmosConnectionString, options);
			ServiceReady = InitializeDatabase();
		}

		public async Task<T?> GetByIdAsync<T> (string id) where T : CosmosModel
		{
			var container = await GetContainerAsync();
			var queryable = container.GetItemLinqQueryable<T>();
			var type = typeof(T).Name;
			using FeedIterator<T> feed = queryable
				.Where(item => item.id == id && item.type == type)
				.ToFeedIterator();

			var results = await ExtractResults(feed);
			return results.SingleOrDefault();
		}

		public async Task<IEnumerable<T>> GetAllAsync<T> (Expression<Func<T, bool>>? predicate = null) where T : CosmosModel
		{
			var container = await GetContainerAsync();
			var queryable = container.GetItemLinqQueryable<T>();
			var type = typeof(T).Name;

			var query = queryable.Where(item => item.type == type);
			if (predicate is not null)
			{
				query = query.Where(predicate);
			}

			using FeedIterator<T> feed = query.ToFeedIterator();

			return await ExtractResults(feed);
		}

		public async Task UpdateAsync<T> (T model) where T : CosmosModel
		{
			var container = await GetContainerAsync();
			await container.UpsertItemAsync(model);
		}

		private async Task<IEnumerable<T>> ExtractResults<T> (FeedIterator<T> feed)
		{
			var result = new List<T>();
			while (feed.HasMoreResults)
			{
				var response = await feed.ReadNextAsync();
				foreach (var item in response)
				{
					result.Add(item);
				}
			}
			return result;
		}

		private async Task InitializeDatabase ()
		{
			// This is primarily for local development to initialize the emulator
			var res = await _client.CreateDatabaseIfNotExistsAsync(DatabaseName, throughput: 400);
			await res.Database.CreateContainerIfNotExistsAsync(ContainerName, partitionKeyPath: "/id");
		}

		private async Task<Container> GetContainerAsync ()
		{
			await ServiceReady;
			return _client.GetDatabase(DatabaseName).GetContainer(ContainerName);
		}
	}
}
