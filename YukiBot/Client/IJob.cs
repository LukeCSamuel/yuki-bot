using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukiBot.Client
{
	internal interface IJob : IComponent
	{
		public TimeSpan Interval { get; }
		public Task OnJobTriggered ();
		public Task? Scheduled { get; set; }
	}
}
