using Azure;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using YukiBot.Models;

using static ReverseMarkdown.Config;

namespace YukiBot.Services
{
	internal partial class TheaterEventService
	{
		const string kyartsDomain = @"https://www.kentuckyperformingarts.org";
		const string wikipediaDomain = @"https://en.wikipedia.org";

		readonly CosmosService cosmos;
		readonly HttpClient http;
		readonly HtmlWeb web = new();
		readonly ReverseMarkdown.Converter markdown = new(new ReverseMarkdown.Config()
		{
			UnknownTags = UnknownTagsOption.Drop,
			RemoveComments = true,
			SmartHrefHandling = true,
		});

		public TheaterEventService (CosmosService cosmosService, HttpClient httpClient)
		{
			cosmos = cosmosService;
			http = httpClient;
		}

		public Task<IEnumerable<TheaterEvent>> GetAllAsync ()
		{
			return cosmos.GetAllAsync<TheaterEvent>();
		}

		public Task UpdateAsync (TheaterEvent theaterEvent)
		{
			return cosmos.UpdateAsync(theaterEvent);
		}

		public async Task<string?> GetBestPriceAsync (TheaterEvent show)
		{
			const string api = @"https://tickets.kentuckyperformingarts.org/api/syos/GetScreens";
			double bestest = double.PositiveInfinity;
			if (show.performanceIds is not null)
			{
				foreach (var id in show.performanceIds)
				{
					try
					{
						var prices = await http.GetFromJsonAsync<List<TheaterEvent.PriceInfo>>($"{api}?performanceId={id}");
						var best = prices?.Where(p => p.Available)
							.Select(p => double.TryParse(p.MinPrice, out double minPrice) ? minPrice : double.PositiveInfinity)
							.Min();
						if (best is double min && min < bestest)
						{
							bestest = min;
						}
					}
					catch { }
				}
			}
			return bestest == double.PositiveInfinity ? null : $"${bestest:N2}";
		}

		public async Task UpdateShowData ()
		{
			const string page = @"/all-shows";
			var document = await web.LoadFromWebAsync(kyartsDomain + page);
			var eventNodes = document.DocumentNode.SelectNodes(@"//div[contains(@class, 'card-featured')]");

			var existingEvents = await cosmos.GetAllAsync<TheaterEvent>();
			var interestingShows = await GetInterestingShowsAsync();
			var discoveredEvents = eventNodes.ToAsyncEnumerable().Select(ExtractTheaterEventFromNode);

			// Get only events that are interesting
			var newEvents = discoveredEvents
				.Distinct(discovered => discovered.title)
				.Where(discovered => interestingShows.Any(interesting =>
				{
					var isMatch = interesting.Title == discovered.title;
					if (isMatch && interesting.DeepLink is not null)
					{
						discovered.wikipediaLink = $"{wikipediaDomain}{interesting.DeepLink}";
					}
					return isMatch;
				}))
				// Check if the event already exists and use the existing id
				.Select(discovered =>
				{
					if (existingEvents.FirstOrDefault(existing => existing.title == discovered.title) is TheaterEvent existing)
					{
						discovered.CopyFromExisting(existing);
					}
					return discovered;
				});

			// Continue collecting data about new events
			newEvents = newEvents.SelectAwait(ExtractAdditionalInfo);

			// Add/update events in DB
			await foreach(var show in newEvents)
			{
				await UpdateAsync(show);
			}
		}

		TheaterEvent ExtractTheaterEventFromNode (HtmlNode node)
		{
			// title
			var title = node.SelectSingleNode(@".//h4[contains(@class, 'card-title')]")
				.InnerText;
			// showDates
			var showDates = node.SelectSingleNode(@".//p[contains(@class, 'show-dates')]")
				.InnerText;
			// venue
			var venue = node.SelectSingleNode(@".//div[contains(@class, 'venue-icon')]")
				.GetClasses()
				.First(c => c != "venue-icon");
			// deeplink
			var deeplink = kyartsDomain + node.SelectSingleNode(@".//a[@href]")
				.GetAttributeValue("href", "");
			// imageUrl
			var style = node.SelectSingleNode(@".//div[contains(@class, 'card-header') and @style]")
					.GetAttributeValue("style", "");
			var imageUrl = CssUrlRegex().Match(style).Groups[1]?.Value;


			return new TheaterEvent(Guid.NewGuid().ToString())
			{
				title = title,
				showDates = showDates,
				venue = venue,
				deeplink = deeplink,
				imageUrl = imageUrl,
				hasAnnouncedEvent = false,
			};
		}

		async ValueTask<TheaterEvent> ExtractAdditionalInfo (TheaterEvent show)
		{
			try
			{
				// The first page loaded by the deeplink is a generic redirect page that uses
				//   client-side redirection i.e. "window.location = ..."
				var intermediate = await http.GetAsync(show.deeplink);
				var redirect = WindowLocationRegex().Match(await intermediate.Content.ReadAsStringAsync()).Groups[1]?.Value;

				// The second page loaded is some kind of anti-bot / javascript verification page
				// It also uses client-side redirection i.e. "document.href = decodeURIComponent(...)"
				var doc = await http.GetAsync(redirect);
				var actualUrl = doc.RequestMessage?.RequestUri;
				if (actualUrl?.ToString() != redirect)
				{
					var queueitRedirect = DocumentHrefRegex().Match(await doc.Content.ReadAsStringAsync()).Groups[1]?.Value;
					queueitRedirect = $"https://{actualUrl?.Host}{HttpUtility.UrlDecode(queueitRedirect)}";

					// Finally, the deeplink page we were looking for!
					doc = await http.GetAsync(queueitRedirect);
				}


				// Now we can parse the document for the data we want
				var body = await doc.Content.ReadAsStringAsync();
				// ticketsDate
				var ticketsDateMatch = TicketsDateRegex().Match(body);
				if (ticketsDateMatch.Success)
				{
					try
					{
						show.ticketsDate = DateTime.ParseExact(ticketsDateMatch.Groups[1].Value, "MMMM d @ h:mm tt", CultureInfo.InvariantCulture);
					} catch { }
				}
				// performanceIds
				var performanceIdMatches = PerformanceIdRegex().Matches(body);
				var performanceIds = new HashSet<string>();
				await foreach (var match in performanceIdMatches.ToAsyncEnumerable())
				{
					if (match.Success)
					{
						performanceIds.Add(match.Groups[1].Value);
					}
				}
				show.performanceIds = [.. performanceIds];

				// Get info from wikipedia page
				var wikipediaPage = await web.LoadFromWebAsync(show.wikipediaLink);
				// Wikipedia has (imo) better images, so we can override the image link as well
				var ogImage = wikipediaPage.DocumentNode.SelectSingleNode(@"//meta[@property = 'og:image']");
				var ogImageUrl = ogImage.GetAttributeValue("content", null);
				show.imageUrl = ogImageUrl ?? show.imageUrl;
				// description
				var paragraph = wikipediaPage.DocumentNode.SelectSingleNode(@"//p[string-length(normalize-space(text())) > 0]");
				show.description = markdown.Convert(paragraph.InnerHtml);
				show.description = MarkdownLinkRegex().Replace(show.description, match => $"{wikipediaDomain}{match.Groups[0]}");
			}
			catch { }

			// Default the tickets date to now, if it couldn't be populated
			show.ticketsDate ??= DateTime.Now;
			return show;
		}

		async ValueTask<IEnumerable<InterestingShowReference>> GetInterestingShowsAsync ()
		{
			const string page = @"/wiki/List_of_the_longest-running_Broadway_shows";
			try
			{
				var document = await web.LoadFromWebAsync(wikipediaDomain + page);
				var anchors = document.DocumentNode.SelectNodes(@"//table[1]//tr//th//a[1]");
				return anchors
					.Select(a => new InterestingShowReference(a.InnerText, a.GetAttributeValue("href", null)))
					.ToList();
			}
			catch { }
			return [];
		}

		[GeneratedRegex(@"url\(([^)]*)\)")]
		private static partial Regex CssUrlRegex ();

		[GeneratedRegex(@"window\.location = ""([^""]+)""")]
		private static partial Regex WindowLocationRegex ();

		[GeneratedRegex(@"href = decodeURIComponent\('([^']+)'\)")]
		private static partial Regex DocumentHrefRegex ();

		[GeneratedRegex(@"TICKETS GO ON SALE (\w+\s\d+\s@\s\d+:\d+\s\w+)")]
		private static partial Regex TicketsDateRegex ();

		[GeneratedRegex(@"https://tickets\.kentuckyperformingarts\.org/\d+/(\d+)")]
		private static partial Regex PerformanceIdRegex ();

		[GeneratedRegex(@"/wiki/[^\s]+")]
		private static partial Regex MarkdownLinkRegex ();

		record InterestingShowReference(string Title, string DeepLink);
	}
}
