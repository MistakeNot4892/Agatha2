using Discord.WebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Agatha2
{
	internal class CommandRoll : BotCommand
	{
        public CommandRoll()
        {
            usage = "roll [1-100]d[1-100]<+/-[modifier]>";
            description = "Rolls dice in a 'standard' schema (d6, d20, etc).";
            aliases = new List<string>(new string[] {"roll", "dice", "d"});
        }
        public override async Task ExecuteCommand(SocketMessage message)
		{
			string responseMessage =  "";
			foreach(Match m in Regex.Matches(message.Content.Substring(6), "(\\d*)(#*)d(\\d+)([+-]\\d+)*"))
			{
				DicePool dice = new DicePool(m);
				responseMessage = $"{responseMessage}```{dice.SummarizeStandardRoll()}```\n";
			}
			if(responseMessage.Equals(""))
			{
				responseMessage = $"Dice syntax is `{Program.CommandPrefix}roll [1-100]d[1-100]<+/-[modifier]>` separated by spaces or commas. Separate dice count from number of sides with `#` for individual rolls.";
			}
			await message.Channel.SendMessageAsync($"{message.Author.Mention}: {responseMessage}");			
        }
    }
}