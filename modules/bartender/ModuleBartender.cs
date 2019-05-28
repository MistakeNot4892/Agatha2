using Discord;
using Discord.WebSocket;
using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Agatha2
{
	internal class ModuleBartender : BotModule
	{
		internal Dictionary<string, List<string>> _bartending;
		internal Dictionary<string, List<string>> _sandwiches;
		internal List<string> validDrinkFields = new List<string>() {"vessel","beverage","garnish"};
		internal List<string> validSandwichFields = new List<string>() {"plate","bread","filling","garnish"};
		internal Dictionary<string, List<string>> BartendingData { get => _bartending; set => _bartending = value; }
		internal Dictionary<string, List<string>> SandwichData { get => _sandwiches; set => _sandwiches = value; }

		internal ModuleBartender()
		{
			moduleName = "Bartender";
			description = "Provides randomly generated food and drink. May or may not be edible.";
		}

		internal override void StartModule()
		{
		}

		internal override bool Register(List<BotCommand> commands)
		{

			BartendingData = new Dictionary<string, List<string>>();
			BartendingData.Add("vessel",   new List<string>(File.ReadAllLines(@"modules/bartender/data/bartending_vessels.txt")));
			BartendingData.Add("garnish",  new List<string>(File.ReadAllLines(@"modules/bartender/data/bartending_garnishes.txt")));
			BartendingData.Add("beverage", new List<string>(File.ReadAllLines(@"modules/bartender/data/bartending_beverages.txt")));

			SandwichData = new Dictionary<string, List<string>>();
			SandwichData.Add("plate",   new List<string>(File.ReadAllLines(@"modules/bartender/data/sandwich_plates.txt")));
			SandwichData.Add("bread",   new List<string>(File.ReadAllLines(@"modules/bartender/data/sandwich_breads.txt")));
			SandwichData.Add("filling", new List<string>(File.ReadAllLines(@"modules/bartender/data/sandwich_fillings.txt")));
			SandwichData.Add("garnish", new List<string>(File.ReadAllLines(@"modules/bartender/data/sandwich_garnishes.txt")));

			commands.Add(new CommandDrink());
			commands.Add(new CommandDwink());
			commands.Add(new CommandSandwich());

			return true;
		}
 		internal void SaveSandwichData()
		{
			System.IO.File.WriteAllLines(@"modules/bartender/data/sandwich_plates.txt",	SandwichData["plate"]);
			System.IO.File.WriteAllLines(@"modules/bartender/data/sandwich_breads.txt",	SandwichData["bread"]);
			System.IO.File.WriteAllLines(@"modules/bartender/data/sandwich_fillings.txt",  SandwichData["filling"]);
			System.IO.File.WriteAllLines(@"modules/bartender/data/sandwich_garnishes.txt", SandwichData["garnish"]);

		}
		internal void SaveBartendingData()
		{
			System.IO.File.WriteAllLines(@"modules/bartender/data/bartending_vessels.txt",   BartendingData["vessel"]);
			System.IO.File.WriteAllLines(@"modules/bartender/data/bartending_garnishes.txt", BartendingData["garnish"]);
			System.IO.File.WriteAllLines(@"modules/bartender/data/bartending_beverages.txt", BartendingData["beverage"]);
		}
	}
}