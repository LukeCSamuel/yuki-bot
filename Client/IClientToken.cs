using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukiBot.Client
{
	internal interface IClientToken
	{
		string Value { get; }
		TokenType Type => TokenType.Bot;
	}
}
