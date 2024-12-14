using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukiBot.Services
{
	static class HttpService
	{
		public static IServiceCollection AddHttpClient (this IServiceCollection services)
		{
			var httpHandler = new HttpClientHandler()
			{
				AllowAutoRedirect = true,
				CookieContainer = new System.Net.CookieContainer(),
				UseCookies = true,
			};
			var httpClient = new HttpClient(httpHandler);
			return services.AddSingleton(httpClient);
		}
	}
}
