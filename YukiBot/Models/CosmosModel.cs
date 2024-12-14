using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukiBot.Models
{
	internal abstract class CosmosModel
	{
		public virtual string id { get; set; }
		public virtual string type
		{
			get => GetType().Name;
			set { }
		}
	}
}
