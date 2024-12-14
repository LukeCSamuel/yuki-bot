using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukiBot.Models
{
	internal class TheaterEvent : CosmosModel
	{
		public string? title { get; set; }
		public string? showDates { get; set; }
		public string? venue { get; set; }
		public string? deeplink { get; set; }
		public string? imageUrl { get; set; }

		public string? wikipediaLink { get; set; }
		public string? description { get; set; }
		public DateTime? ticketsDate { get; set; }
		public List<string>? performanceIds { get; set; }

		public bool? hasAnnouncedEvent { get; set; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public string? ReadableVenue => venue switch
		{
			"venue-kentucky-center" => "KCPA Whitney Hall",
			"venue-brown-theatre" => "Brown Theatre",
			"venue-paristown" => "Paristown Hall",
			_ => null,
		};

		public TheaterEvent ()
		{
			id = "";
		}

		public TheaterEvent (string id)
		{
			this.id = id;
		}

		public void CopyFromExisting (TheaterEvent existing)
		{
			// Properties which should not be updated automatically
			// TODO: better way to do this?  With attributes?
			id = existing.id;
			hasAnnouncedEvent = existing.hasAnnouncedEvent;
		}

		public EmbedBuilder GetEmbed ()
		{
			var embed = new EmbedBuilder()
			{
				Title = "🎟️ Tickets Available Now!",
				Description = description,
				ImageUrl = imageUrl,
				Url = deeplink,
				Color = Color.DarkPurple,
			};

			var descBuilder = new StringBuilder();
			descBuilder.AppendLine($"# {title}");
			if (description is not null)
			{
				descBuilder.AppendLine($"-# from [Wikipedia]({wikipediaLink}):");
				descBuilder.AppendLine(description);
			}
			embed.Description = descBuilder.ToString();

			if (ReadableVenue is string venueName)
			{
				embed.AddField("Venue", venueName, true);
			}
			if (showDates is not null)
			{
				embed.AddField("Show Dates", showDates, true);
			}

			return embed;
		}

		/// <summary>
		/// A model containing data from KCPA api
		/// </summary>
		public class PriceInfo
		{
			public string MinPrice { get; set; }
			public string MaxPrice { get; set; }
			public bool Available { get; set; }
		}
	}
}
