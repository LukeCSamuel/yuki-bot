using Discord;
using Discord.WebSocket;
using NTextCat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YukiBot.Services;

namespace YukiBot.Handlers.Message
{
	internal class BanFrench : IMessageHandler
	{
		Regex Connections { get; } = new Regex(@"Connections(?:\n|\r\n)Puzzle #\d+", RegexOptions.Multiline);
		RankedLanguageIdentifier Identifier { get; }

		public BanFrench ()
		{
			var factory = new RankedLanguageIdentifierFactory();
			Identifier = factory.Load("Core14.profile.xml");
		}

		public async Task OnMessageReceiveAsync (SocketMessage msg)
		{
			// Don't try to identify the language if the message is too short, it's wildly inaccurate
			var tooShort = msg.CleanContent.Length < 20;
			var isConnections = Connections.IsMatch(msg.CleanContent);
			if (tooShort || isConnections)
			{
				return;
			}

			var languages = Identifier.Identify(msg.Content);
			if (languages.FirstOrDefault() is ({ Iso639_3: "fra" }, _))
			{
				if (msg.Author is IGuildUser author)
				{
					try
					{
						await author.SetTimeOutAsync(
							new TimeSpan(0, 10, 0),
							new RequestOptions
							{
								AuditLogReason = "Speaking French"
							}
						);
					}
					catch { }
				}
				await msg.Channel.SendMessageAsync(
					$"<@{msg.Author.Id}>, please do not use foul language ):",
					messageReference: new MessageReference(msg.Id)
				);
			}
		}
	}
}
