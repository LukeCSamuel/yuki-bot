using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukiBot.Services;

namespace YukiBot.Client
{
	internal class ClientBuilder
	{
		public IServiceCollection Services { get; }
		List<Type> Components { get; }
		

		private ClientBuilder () {
			Components = new List<Type>();
			Services = new ServiceCollection();
		}

		public ClientBuilder ConfigureServices (Action<IServiceCollection> configure)
		{
			configure(Services);
			return this;
		}

		public ClientBuilder AddComponent (IComponent component)
		{
			var type = component.GetType();
			Components.Add(type);
			Services.AddSingleton(type, component);
			return this;
		}

		public ClientBuilder AddComponent<T> () where T : class, IComponent
		{
			Components.Add(typeof(T));
			Services.AddSingleton<T>();
			return this;
		}

		public Client Build ()
		{
			// Create the bot client and add the socket as a service
			var handlers = new List<IHandler>();
			var jobs = new List<IJob>();
			var client = new Client(handlers, jobs);
			Services.AddTransient<IDiscordClient>(_ => client.Socket);

			// Create a service provider and populate the handlers
			var provider = Services.BuildServiceProvider();
			foreach (var type in Components)
			{
				var component = provider.GetRequiredService(type);
				if (component is IHandler handler)
				{
					handlers.Add(handler);
				}
				else if (component is IJob job)
				{
					jobs.Add(job);
				}
			}

			// Provide the token the client should use to connect
			client.Token = provider.GetRequiredService<IClientToken>();

			return client;
		}

		public static ClientBuilder Create ()
		{
			return new ClientBuilder();
		}
	}
}
