using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukiBot.Client;

namespace YukiBot.Handlers.Message
{
	internal interface IMessageHandler : IHandler
	{
		public Task OnMessageReceiveAsync (SocketMessage msg);
	}
}
