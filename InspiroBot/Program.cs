using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace Inspirobot
{
    class Program
    {
        const string pathOfConfig = "config.json";
        private Configuration Config = null;
        private Random random = new Random();

        private DiscordSocketClient Client;
        static void Main(string[] args) => new Program().EntryPointAsync().GetAwaiter().GetResult();

        public async Task EntryPointAsync()
        {
            //Load the config
            Config = ConfigParser.LoadConfig(pathOfConfig);

            //Create a new client object
            DiscordSocketConfig config = new DiscordSocketConfig() { MessageCacheSize = 100, LogLevel = LogSeverity.Verbose };
            Client = new DiscordSocketClient(config);

            //Add event handlers
            Client.MessageReceived += MessageReceived;
            Client.GuildMemberUpdated += GuildMemberUpdated;
            Client.Ready += async () =>
            {
                Console.WriteLine("Connected");
                await Client.SetActivityAsync(new Game("for ?inspire or ?bulkinspire", ActivityType.Watching));
            };

            //Start bot
            await Client.LoginAsync(TokenType.Bot, Config.Token);
            await Client.StartAsync();

            //Wait indefinitely
            await WaitForKeypress();
        }

        private async Task WaitForKeypress()
        {
            Console.ReadLine();
            await CloseConnection();
        }

        private async Task CloseConnection()
        {
            await Client.LogoutAsync();
            ConfigParser.SaveConfig(pathOfConfig, Config);
            Console.WriteLine("Logged out");
            Environment.Exit(0);
        }

        private async Task<IUserMessage> EditOrSendNewMessage(EmbedBuilder embedToSend, IUserMessage messageToEdit)
        {
            IMessage result = await messageToEdit.Channel.GetMessageAsync(messageToEdit.Id);

            //If the message no longer exists
            if (result == null)
            {
                return await messageToEdit.Channel.SendMessageAsync(embed: embedToSend.Build());
            }
            //If the message does still exist
            else
            {
                await messageToEdit.ModifyAsync(delegate (MessageProperties properties)
                {
                    properties.Embed = embedToSend.Build();
                });

                return messageToEdit;
            }
        }

        private ChannelPermissions GetBotPerms(SocketGuildChannel channel)
        {
            //Get the perms for the bot
            SocketGuildUser botUser = channel.Guild.GetUser(Client.CurrentUser.Id);

            //Return its perms
            return botUser.GetPermissions(channel);
        }

        private bool HasSufficientTextPerms(SocketGuildChannel channel)
        {
            SocketGuildChannel guildChannel = (SocketGuildChannel)channel;

            ChannelPermissions perms = GetBotPerms(guildChannel);

            //Return true if we have the perms ViewChannel and SendMessages and EmbedLinks and AttachFiles
            return perms.ViewChannel && perms.SendMessages && perms.EmbedLinks && perms.AttachFiles;
        }

        private string GetImageURL()
        {
            return "http://generated.inspirobot.me/" + random.Next(1, 87).ToString("D3") + "/aXm" + random.Next(1000, 9001) + "xjU.jpg";
        }

        private ulong nextFileUuid = 0;

        private async Task<string> DownloadNewImage()
        {
            string fileName = nextFileUuid + ".jpg";
            nextFileUuid += 1;

            using (WebClient webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(new Uri(GetImageURL()), fileName);
            }

            return fileName;
        }

        private async Task MessageReceived(SocketMessage message)
        {
            //Check that this message is sent from a human
            if (message.Source != MessageSource.User) return;

            //Check that the channel is correct
            if (message.Channel.Id != Config.ChannelId) return;

            //Get the channel
            SocketGuildChannel channel = (SocketGuildChannel)message.Channel;

            //Check that the perms are up to scratch
            if (!HasSufficientTextPerms(channel)) return;

            //Inspire command
            if (message.Content.StartsWith(Config.CommandPrefix + "inspire"))
            {
                string path = await DownloadNewImage();
                await message.Channel.SendFileAsync(path);
                System.IO.File.Delete(path);
            }
            //Bulk inspire command
            else if (message.Content.StartsWith(Config.CommandPrefix + "bulkinspire"))
            {
                _ = Task.Run(async () =>
                {
                    string[] urls = new string[10];

                    IUserMessage bulkMessage = null;

                    for (int i = 0; i < 10; i++)
                    {
                        //Get a new url
                        string thisUrl = GetImageURL();
                        urls[i] = thisUrl;

                        //Get embed
                        EmbedBuilder embed = new EmbedBuilder
                        {
                            Title = "Inspirational Quote " + (i + 1) + "/10",
                            Url = thisUrl,
                            ImageUrl = thisUrl,
                            Color = new Color(25, 69, 10)
                        };

                        //If this is the first edit, send a new message. Otherwise, edit our existing message
                        if (bulkMessage == null)
                        {
                            bulkMessage = await message.Channel.SendMessageAsync(embed: embed.Build());
                        }
                        else
                        {
                            bulkMessage = await EditOrSendNewMessage(embed, bulkMessage);
                        }

                        //Wait 5 secs
                        await Task.Delay(5000);
                    }

                    //Edit to show list of urls
                    bulkMessage = await EditOrSendNewMessage(new EmbedBuilder
                    {
                        Title = "Inspirational Quotes",
                        Description = string.Join("\n", urls),
                        Color = new Color(25, 69, 10)
                    }, bulkMessage);
                });
            }
            //Reload config command
            else if (message.Content.StartsWith(Config.CommandPrefix + "reloadconfig"))
            {
                //Check that piggey sent it
                if (message.Author.Id != Config.OwnerId) return;

                //Reload the config
                Config = ConfigParser.LoadConfig(pathOfConfig);
            }
        }

        private async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            //Is the user in the dictionary
            if (Config.UserToStickyRoles.ContainsKey(after.Id))
            {
                //Iterate over every role this user is supposed to have
                foreach(ulong roleTheyShouldHaveId in Config.UserToStickyRoles[after.Id])
                {
                    //Continue if they have this role
                    bool hasRole = false;
                    foreach(SocketRole roleTheyHave in after.Roles)
                    {
                        if (roleTheyHave.Id == roleTheyShouldHaveId) hasRole = true;
                    }
                    if (hasRole) continue;

                    //Right, they don't have the role. Firstly check that we have perms to add the role, then figure out who removed the role, then add the role, then announce it

                    SocketGuildUser botUser = after.Guild.GetUser(Client.CurrentUser.Id);
                    SocketRole roleTheyShouldHave = botUser.Guild.GetRole(roleTheyShouldHaveId);

                    //Check we have the manage roles permission
                    if (!botUser.GuildPermissions.ManageRoles) break;

                    //Check that the role they should have is lower in the hierarchy than our role
                    if (botUser.Roles.Count == 0) continue;
                    SocketRole[] ourRoles = botUser.Roles.ToArray();
                    SocketRole ourHighestRole = ourRoles[ourRoles.Length - 1];
                    if (ourHighestRole.Position < roleTheyShouldHave.Position) continue;

                    //Find out who did it, 100 entries max
                    SocketGuildUser culprit = null;
                    if (botUser.GuildPermissions.ViewAuditLog)
                    {
                        IEnumerable<RestAuditLogEntry> auditLog = await botUser.Guild.GetAuditLogsAsync(100).FlattenAsync();

                        foreach (RestAuditLogEntry entry in auditLog)
                        {
                            //Check that the action is a member role update
                            if (entry.Action != ActionType.MemberRoleUpdated) continue;

                            MemberRoleAuditLogData data = (MemberRoleAuditLogData)entry.Data;

                            //Check that the user who's role was updated matches what we need
                            if (data.Target.Id != after.Id) continue;

                            //Check that the role that was updated matches what we need
                            bool containRoleWeNeed = false;
                            foreach(MemberRoleEditInfo info in data.Roles)
                            {
                                if (info.RoleId == roleTheyShouldHave.Id) containRoleWeNeed = true;
                            }
                            if (!containRoleWeNeed) continue;

                            //Ok, this entry is the one we need
                            culprit = botUser.Guild.GetUser(entry.User.Id);
                            break;
                        }
                    }

                    //Add the role
                    await after.AddRoleAsync(roleTheyShouldHave);

                    //Announce it
                    SocketTextChannel channelToPostIn = (SocketTextChannel)botUser.Guild.GetChannel(Config.ChannelId);
                    if (culprit == null)
                    {
                        await channelToPostIn.SendMessageAsync("Somebody just tried to remove the role " + roleTheyShouldHave.Name + " from " + MentionUtils.MentionUser(after.Id) + "\nNice try");
                    }
                    else if (culprit.Id == after.Id)
                    {
                        await channelToPostIn.SendMessageAsync(MentionUtils.MentionUser(after.Id) + " just tried to remove the role " + roleTheyShouldHave.Name + " from themself\nNice try");
                    }
                    else
                    {
                        await channelToPostIn.SendMessageAsync(MentionUtils.MentionUser(culprit.Id) + " just tried to remove the role " + roleTheyShouldHave.Name + " from " + MentionUtils.MentionUser(after.Id) + "\nNice try");
                    }
                }
            }
        }
    }
}
