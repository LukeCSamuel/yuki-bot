using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukiBot.Handlers.Message;

namespace YukiBot.Client
{
	internal class Client
	{
		internal IClientToken? Token { get; set; }
		IEnumerable<IHandler> Handlers { get; }
		IEnumerable<IJob> Jobs { get; }

		public DiscordSocketClient Socket { get; private set; }

		internal Client (IEnumerable<IHandler> handlers, IEnumerable<IJob> jobs)
		{
			Handlers = handlers;
			Jobs = jobs;

			var unusedIntents = GatewayIntents.GuildScheduledEvents | GatewayIntents.GuildInvites;
			var allIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent;
			var intents = allIntents & ~unusedIntents;
			Socket = new(new DiscordSocketConfig { GatewayIntents = intents });
			
			Socket.Ready += OnReady;
			Socket.Log += Log;
			Socket.MessageReceived += OnMessageReceived;
		}

		public async Task StartAsync ()
		{
			if (Token is null)
			{
				throw new Exception("A Discord Bot token was not provided.");
			}
			await Socket.LoginAsync(Token.Type, Token.Value);
			await Socket.StartAsync();

			// Block this task until the program is closed
			await Task.Delay(-1);
		}

		public void Run ()
		{
			StartAsync().GetAwaiter().GetResult();
		}

		Task OnReady ()
		{
			// Start jobs
			foreach (var job in Jobs)
			{
				job.Scheduled ??= RunJob(job);
			}
			return Task.CompletedTask;
		}

		async Task RunJob (IJob job, CancellationToken cancellationToken = default)
		{
			using PeriodicTimer timer = new(job.Interval);
			await Task.Delay(1, CancellationToken.None);
			while (true)
			{
				try
				{
					await job.OnJobTriggered();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
				await timer.WaitForNextTickAsync(cancellationToken);
			}
		}

		Task Log (LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

		async Task OnMessageReceived (SocketMessage msg)
		{
			if (msg.Author.Id == Socket.CurrentUser.Id)
			{
				// Ignore self!
				return;
			}

			foreach (var handler in Handlers)
			{
				if (handler is IMessageHandler messageHandler)
				{
					await messageHandler.OnMessageReceiveAsync(msg);
				}
			}
		}
	}
}
