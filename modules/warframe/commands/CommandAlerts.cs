using Discord;
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

namespace Agatha2
{
	internal class CommandAlerts : BotCommand
	{
		internal CommandAlerts()
		{
			usage = "alerts";
			description = "Get a list of current alerts from Warframe.";
			aliases = new List<string>() {"alerts"};
		}

		internal override async Task ExecuteCommand(SocketMessage message, GuildConfig guild)
		{
			ModuleWarframe wf = (ModuleWarframe)parent;
			if(wf.alerts.Count <= 0)
			{
				await Program.SendReply(message, "There are no mission alerts available currently, Tenno.");
			}
			else
			{
				EmbedBuilder embedBuilder = new EmbedBuilder();
				foreach(KeyValuePair<string, Dictionary<string, string>> alertInfo in wf.alerts)
				{
					if(alertInfo.Value["Expires"] != "unknown")
					{
						embedBuilder.AddField($"{alertInfo.Value["Header"]} - {alertInfo.Value["Mission Type"]} ({alertInfo.Value["Faction"]})", $"{alertInfo.Value["Level"]}. Expires in {alertInfo.Value["Expires"]}.\nRewards:{alertInfo.Value["Rewards"]}");
					}
				}
				await Program.SendReply(message, embedBuilder);
			}
		}
	}
}