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
		// TODO: a better way to manage what channel to post to for a job
		//       maybe DB powered?
		const ulong channelId = 579436919964958725ul;
		//const ulong channelId = 1235114114628587601ul;

		public TimeSpan Interval => TimeSpan.FromDays(1);
		public Task? Scheduled { get; set; }

		readonly TheaterEventService eventService;
		readonly IDiscordClient discord;

		public TheaterEventJob (TheaterEventService eventService, IDiscordClient discord)
		{
			this.eventService = eventService;
			this.discord = discord;
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
