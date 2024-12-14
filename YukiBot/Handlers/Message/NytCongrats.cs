using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YukiBot.Handlers.Message
{
	internal class NytCongrats : IMessageHandler
	{
		Regex Wordle { get; } = new Regex(@"Wordle [\d,]+ (\d)/6");

		public NytCongrats () { }

		public async Task OnMessageReceiveAsync (SocketMessage msg)
		{
			var match = Wordle.Match(msg.Content);
			if (match.Success && (match.Groups[1].Value == "1" || match.Groups[1].Value == "2"))
			{
				await msg.AddReactionAsync(new Emoji("\U0001f929"));
			}
		}
	}
}
