using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukiBot.Client;
using YukiBot.Services;

namespace YukiBot.Jobs
{
	internal class TheaterEventJob : IJob
	{
		public TimeSpan Interval => TimeSpan.FromDays(1);
		public Task? Scheduled { get; set; }

		readonly TheaterEventService eventService;
		readonly IDiscordClient discord;
		readonly ulong channelId;

		public TheaterEventJob (TheaterEventService eventService, IDiscordClient discord, ConfigService configService)
		{
			this.eventService = eventService;
			this.discord = discord;
			channelId = configService.AppSettings.TheaterChannelId;
		}

		public async Task OnJobTriggered ()
		{
			var channel = await discord.GetChannelAsync(channelId);
			if (channel is ITextChannel textChannel)
			{
				// Update events in the database
				await eventService.UpdateShowData();
				var shows = await eventService.GetAllAsync();
				// Post when event has tickets available
				var needAnnounced = shows
					.Where(s => s.hasAnnouncedEvent is not true
						&& s.ticketsDate is DateTime d
						&& DateTime.Now > d);
				foreach (var show in needAnnounced)
				{
					var embed = show.GetEmbed();
					var bestPrice = await eventService.GetBestPriceAsync(show);
					if (bestPrice is not null)
					{
						embed.AddField("Starting From", bestPrice);
					}
					var components = new ComponentBuilder()
						.WithButton("Buy Tickets", style: ButtonStyle.Link, url: show.deeplink)
						.Build();
					await textChannel.SendMessageAsync(embed: embed.Build(), components: components);
					// Update event to indicate it has been posted about
					show.hasAnnouncedEvent = true;
					await eventService.UpdateAsync(show);
				}
			}
		}
	}
}
