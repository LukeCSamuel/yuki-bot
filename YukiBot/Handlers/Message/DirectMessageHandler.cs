using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukiBot.Services;

namespace YukiBot.Handlers.Message
{
	internal class DirectMessageHandler : IMessageHandler
	{
		readonly TheaterEventService eventService;

		public DirectMessageHandler (TheaterEventService eventService)
		{
			this.eventService = eventService;
		}

		public async Task OnMessageReceiveAsync (SocketMessage msg)
		{
			if (msg.Channel.GetChannelType() == ChannelType.DM)
			{
				Console.WriteLine("Received DM");
				await eventService.UpdateShowData();
				var shows = await eventService.GetAllAsync();
				Console.WriteLine($"Found {shows.Count()} shows");
				foreach (var show in shows)
				{
					var embed = show.GetEmbed();
					await msg.Channel.SendMessageAsync("Woof!", embed: embed.Build());
				}
				//await msg.Channel.SendMessageAsync("Woof!");
			}
		}
	}
}
