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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using Nett;

namespace Agatha2
{
	internal class ModuleTwitch : BotModule
	{

		private Dictionary<String, Boolean> streamStatus;
		private Dictionary<String, String> streamIDtoUserName;
		private Dictionary<String, String> streamIDToDisplayName;
		internal Dictionary<String, String> streamNametoID;
		private List<String> streamers;
		private Dictionary<UInt64, UInt64> streamChannelIds = new Dictionary<UInt64, UInt64>();
		internal string streamAPIClientID;

		internal ModuleTwitch()
		{
			moduleName = "Twitch";
			description = "A module for watching for and looking up Twitch streamers.";
		}

		internal override void LoadConfig()
		{
			if(File.Exists(@"modules\twitch\data\config.tml"))
			{
				TomlTable configTable = Toml.ReadFile(@"modules\twitch\data\config.tml");
				streamAPIClientID = configTable.Get<string>("StreamAPIClientID");
			}
			if(File.Exists(@"modules\twitch\data\channel_ids.json"))
			{
				foreach(KeyValuePair<string, string> guildAndChannel in JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(@"modules\twitch\data\channel_ids.json")))
				{
					try
					{
						streamChannelIds.Add((UInt64)Convert.ToInt64(guildAndChannel.Key), (UInt64)Convert.ToInt64(guildAndChannel.Value));
					}
					catch(Exception e)
					{
						Console.WriteLine($"Exception when loading stream channel config: {e.Message}");
					}
				}
			}
		}
		internal override void StartModule()
		{
			Console.WriteLine("Starting Twitch polling.");
			var logFile = File.ReadAllLines(@"modules\twitch\data\streamers.txt");
			try
			{
				foreach(String streamer in new List<string>(logFile))
				{
					JToken jData = RetrieveUserIdFromUserName(streamer);
					string streamerID = jData["id"].ToString();
					streamers.Add(streamerID);
					streamStatus.Add(streamerID, false);
					streamIDtoUserName.Add(streamerID, streamer);
					streamNametoID.Add(streamer, streamerID);
					streamIDToDisplayName.Add(streamerID, jData["display_name"].ToString());
				}
				Console.WriteLine("Done loading streamers."); /* Initializing poll timer.");
				IObservable<long> pollTimer = Observable.Interval(TimeSpan.FromMinutes(5));
				CancellationTokenSource source = new CancellationTokenSource();
				Action action = (() => 
				{
					PollStreamers(null);
				}
				);
				pollTimer.Subscribe(x => { Task task = new Task(action); task.Start();}, source.Token);
				Console.WriteLine("Stream poller initialized.");
				*/
			}
			catch(Exception e)
			{
				Console.WriteLine($"Exception when loading Twitch module: {e.Message}");
			}
		}

		internal override bool Register(List<BotCommand> commands)
		{
			streamStatus = new Dictionary<String, Boolean>();
			streamIDtoUserName = new Dictionary<String, String>();
			streamIDToDisplayName = new Dictionary<String, String>();
			streamNametoID = new Dictionary<String, String>();
			streamers = new List<String>();
			commands.Add(new CommandTwitch());
			return true;
		}

		internal EmbedBuilder MakeAuthorEmbed(JToken jData, JToken jsonStream)
		{

			EmbedAuthorBuilder embedAuthor = new EmbedAuthorBuilder();
			embedAuthor.Name = jData["display_name"].ToString();
			embedAuthor.IconUrl = jData["profile_image_url"].ToString();
			embedAuthor.Url = $"http://twitch.tv/{jData["login"].ToString()}";

			EmbedBuilder embedBuilder = new EmbedBuilder();
			embedBuilder.Author = embedAuthor;
			embedBuilder.Description = jData["description"].ToString();
			if(jsonStream != null && jsonStream.HasValues)
			{
				embedBuilder.Title = jsonStream["title"].ToString();
				embedBuilder.ThumbnailUrl = jsonStream["thumbnail_url"].ToString().Replace("{width}x{height}", "1280x720");
			}
			else
			{
				embedBuilder.Title = "Stream offline.";
				if(jData["offline_image_url"].ToString() != "")
				{
					embedBuilder.ThumbnailUrl = jData["offline_image_url"].ToString();
				}
				else
				{
					embedBuilder.ThumbnailUrl = jData["profile_image_url"].ToString();
				}
			}
			return embedBuilder;
		}

		internal async Task PollStreamers(SocketMessage message)
		{
			Console.WriteLine("Polling streamers.");
			foreach(string streamer in streamers)
			{
				try
				{
					Console.WriteLine($"Looking up user id {streamer}");
					var request = (HttpWebRequest)WebRequest.Create($"https://api.twitch.tv/helix/streams?user_id={streamer}");
					request.Method = "Get";
					request.Timeout = 12000;
					request.ContentType = "application/vnd.twitchtv.v5+json";
					request.Headers.Add("Client-ID", streamAPIClientID);

					try
					{
						using (var s = request.GetResponse().GetResponseStream())
						{
							using (var sr = new System.IO.StreamReader(s))
							{
								var jsonObject = JObject.Parse(sr.ReadToEnd());
								JToken jsonStream = jsonObject["data"];
								if(jsonStream.HasValues)
								{
									jsonStream = jsonStream[0];
								}

								JToken jData = RetrieveUserIdFromUserName(streamIDtoUserName[streamer]);
								if(jData != null && jData.HasValues)
								{
									EmbedBuilder embedBuilder = null;
									string streamAnnounce = "";
									if(!streamStatus[streamer] && jsonStream.HasValues)
									{
										streamStatus[streamer] = true;
										streamAnnounce = $"{jData["display_name"].ToString()} has started streaming.";
										embedBuilder = MakeAuthorEmbed(jData, jsonStream);
									}
									else if(streamStatus[streamer] && !jsonStream.HasValues)
									{
										streamStatus[streamer] = false;
										streamAnnounce = $"{jData["display_name"].ToString()} has stopped streaming.";
										embedBuilder = MakeAuthorEmbed(jData, jsonStream);
									}
									if(embedBuilder != null)
									{
										foreach(KeyValuePair<UInt64, UInt64> streamChannel in streamChannelIds)
										{
											IMessageChannel channel = Program.Client.GetChannel(streamChannel.Value) as IMessageChannel;
											await channel.SendMessageAsync(streamAnnounce, false, embedBuilder.Build());
										}
									}
								}
							}
						}
					}
					catch(WebException e)
					{
						Console.WriteLine(e);
					}
				}
				catch(Exception e)
				{
					await message.Channel.SendMessageAsync($"I tried to poll {streamer} for stream info, but I got an exception instead. ({e.Message})");
				}
			}
		}
		internal JToken RetrieveUserIdFromUserName(String streamer)
		{
			Console.WriteLine($"Retrieving streamer info for {streamer}.");
			var request = (HttpWebRequest)WebRequest.Create($"https://api.twitch.tv/helix/users?login={streamer}");
			request.Method = "Get";
			request.Timeout = 12000;
			request.ContentType = "application/vnd.twitchtv.v5+json";
			request.Headers.Add("Client-ID", streamAPIClientID);

			try
			{
				var s = request.GetResponse();
				if(s != null)
				{
					System.IO.Stream sStream = s.GetResponseStream();
					if(sStream != null)
					{
						var sr = new System.IO.StreamReader(sStream);
						if(sr != null)
						{
							var jsonObject = JObject.Parse(sr.ReadToEnd());
							if(jsonObject != null)
							{
								return jsonObject["data"][0];
							}
						}
					}
				}
			}
			catch(WebException e)
			{
				Console.WriteLine(e.ToString());
			}
			return null;
		}
	}
}