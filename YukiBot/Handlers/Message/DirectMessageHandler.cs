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
				await msg.Channel.SendMessageAsync("Woof!");
			}
		}
	}
}
