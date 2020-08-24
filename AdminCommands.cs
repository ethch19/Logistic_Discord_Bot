using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using System.Diagnostics;

namespace Logistic_Bot.Commands
{
    public class AdminCommands : BaseCommandModule
    {
        AdminCacheJson adminJson;
        SettingJson settingJson;
        Stopwatch stopwatch = new Stopwatch();
        public AdminCommands()
        {
            stopwatch.Start();
            deserialiseJson();
        }
        public async Task deserialiseJson()
        {
            var adminCache = string.Empty;
            using (var fs = File.OpenRead("adminCache.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                adminCache = await sr.ReadToEndAsync().ConfigureAwait(false);
            adminJson = JsonConvert.DeserializeObject<AdminCacheJson>(adminCache);
            var settingCache = string.Empty;
            using (var fs = File.OpenRead("settings.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                settingCache = await sr.ReadToEndAsync().ConfigureAwait(false);
            settingJson = JsonConvert.DeserializeObject<SettingJson>(settingCache);
        }
        public bool serialiseJson(bool setup=false)
        {
            var settingCache = JsonConvert.SerializeObject(settingJson);
            File.WriteAllText("settings.json", settingCache);
            adminJson.setUp = setup;
            var adminCache = JsonConvert.SerializeObject(adminJson);
            File.WriteAllText("adminCache.json", adminCache);
            Console.WriteLine("Serialised");
            return true;
        }
        [Command("enablebot")]
        [Description("Enable the bot to start logging applications")]
        public async Task EnableBot(CommandContext ctx)
        {
            if (adminJson.setUp == false)
            {
                await ctx.RespondAsync(":grey_exclamation: You have to set up your bot first. Do /setup to start.");
                return;
            }
            bool isAdmin = false;
            foreach (DiscordRole role in ctx.Member.Roles)
            {
                if (role.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed)
                {
                    isAdmin = true;
                    break;
                }
            }
            if (ctx.Member.IsOwner == true)
            {
                isAdmin = true;
            }
            var embed = new DiscordEmbedBuilder
            {
                Title = "Webhooks Logging:",
                Color = DiscordColor.CornflowerBlue
            };
            if (ctx.Member.IsBot == false && isAdmin == true)
            {
                adminJson.enabled = (adminJson.enabled == true) ? false : true;
                switch (adminJson.enabled)
                {
                    case true:
                        embed.WithDescription("Enabled");
                        await ctx.Channel.SendMessageAsync(embed: embed);
                        break;
                    case false:
                        embed.WithDescription("Disabled");
                        await ctx.Channel.SendMessageAsync(embed: embed);
                        break;
                }
                serialiseJson(true);
            }
            else
            {
                await ctx.RespondAsync(":no_entry_sign: You do not have permission to use this command. Contact a server administrator.");
                return;
            }
        }
        [Command("setadmin")]
        [Description("Set a role as the bot's admin")]
        public async Task SetAdminRole(CommandContext ctx, [Description("Role's ID")]string roleID)
        {
            if (adminJson.setUp == false)
            {
                await ctx.RespondAsync(":grey_exclamation: You have to set up your bot first. Do /setup to start.");
                return;
            }
            bool isAdmin = false;
            foreach (DiscordRole role in ctx.Member.Roles)
            {
                if (role.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed)
                {
                    isAdmin = true;
                    break;
                }
            }
            if (ctx.Member.IsOwner == true)
            {
                isAdmin = true;
            }
            if (ctx.Member.IsBot == false && isAdmin == true)
            {
                bool successful = false;
                try
                {
                    ulong cache = ulong.Parse(roleID);
                }
                catch
                {
                    await ctx.Channel.SendMessageAsync(":x: Unsuccessful; " + adminJson.adminRoleName +  " is not a vaild ID");
                    return;
                }
                foreach (DiscordRole role in ctx.Guild.Roles.Values)
                {
                    if (role.Id == ulong.Parse(roleID))
                    {
                        adminJson.adminRoleName = role.Name;
                        successful = serialiseJson(true);
                    }
                }
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Bot Admin Role:",
                    Color = DiscordColor.Purple
                };
                if (successful == true)
                {
                    embed.WithDescription("Admin role have been set to "+adminJson.adminRoleName + " successfully");
                    await ctx.Channel.SendMessageAsync(embed:embed);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(":x: Unsuccessful; Could not find " + adminJson.adminRoleName);
                }
            }
            else
            {
                await ctx.RespondAsync(":no_entry_sign: You do not have permission to use this command. Contact a server administrator.");
                return;
            }
        }
        [Command("status")]
        [Description("Checks the current status of the bot")]
        public async Task Status(CommandContext ctx)
        {
            if (ctx.Member.IsBot == false)
            {
                var embed = new DiscordEmbedBuilder();
                embed.WithTitle("Status");
                string cache = (adminJson.enabled == true) ? "Enabled" : "Disabled";
                string cache2 = (string.IsNullOrEmpty(adminJson.adminRoleName) == true) ? "None" : adminJson.adminRoleName;
                string cache3 = "";
                if (stopwatch.Elapsed.TotalSeconds < 60)
                {
                    cache3 = Math.Floor(stopwatch.Elapsed.TotalSeconds).ToString() + " seconds";
                }
                else if (stopwatch.Elapsed.TotalMinutes < 60)
                {
                    cache3 = (Math.Floor(stopwatch.Elapsed.TotalMinutes) == 1) ? Math.Floor(stopwatch.Elapsed.TotalMinutes).ToString() + " minute" : Math.Floor(stopwatch.Elapsed.TotalMinutes).ToString() + " minutes";
                }
                else if (stopwatch.Elapsed.TotalHours < 24)
                {
                    cache3 = (Math.Floor(stopwatch.Elapsed.TotalHours) == 1) ? Math.Floor(stopwatch.Elapsed.TotalHours).ToString() + " hour" : Math.Floor(stopwatch.Elapsed.TotalHours).ToString() + " hours";
                }
                else
                {
                    cache3 = (Math.Floor(stopwatch.Elapsed.TotalDays) == 1) ? Math.Floor(stopwatch.Elapsed.TotalDays).ToString() + " day" : Math.Floor(stopwatch.Elapsed.TotalDays).ToString() + " days";
                }
                embed.AddField("Webhooks logging:", cache);
                embed.AddField("Assigned admin role:", cache2);
                embed.AddField("Latency:", ctx.Client.Ping.ToString() + " ms");
                embed.AddField("Bot Uptime:", cache3);
                embed.WithFooter("Logistic Bot, Made by Eth#7536");
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }
        [Command("resetbot")]
        [Description("Reset all the settings to factory settings")]
        public async Task ResetBot(CommandContext ctx)
        {
            if (adminJson.setUp == false)
            {
                await ctx.RespondAsync(":grey_exclamation: You have to set up your bot first. Do /setup to start.");
                return;
            }
            bool isAdmin = false;
            foreach (DiscordRole role in ctx.Member.Roles)
            {
                if (role.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed)
                {
                    isAdmin = true;
                    break;
                }
            }
            if (ctx.Member.IsOwner == true)
            {
                isAdmin = true;
            }
            if (isAdmin != true)
            {
                await ctx.RespondAsync(":no_entry_sign: You do not have permission to use this command. Contact a server administrator.");
                return;
            }
            else
            {
                var confirmEmbed = new DiscordEmbedBuilder
                {
                    Title = "Confim:",
                    Description = "Say 'y' to continue, 'n' to exit.",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed: confirmEmbed);
                var interactivity = ctx.Client.GetInteractivity();
                var response = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author.Id == ctx.User.Id && (x.Content == "y" || x.Content == "n"), TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                if (response.Result.Content == "n")
                {
                    await ctx.RespondAsync(":no_entry: Reset to factory settings action cancelled. Nothing has been changed or saved.");
                    return;
                }
                var adminFactoryCache = string.Empty;
                using (var fs = File.OpenRead("adminFactory.json"))
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                    adminFactoryCache = await sr.ReadToEndAsync().ConfigureAwait(false);
                var adminFactory = JsonConvert.DeserializeObject<AdminCacheJson>(adminFactoryCache);
                adminJson = adminFactory;
                var settingFactoryCache = string.Empty;
                using (var fs = File.OpenRead("settingsFactory.json"))
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                    settingFactoryCache = await sr.ReadToEndAsync().ConfigureAwait(false);
                var settingFactory = JsonConvert.DeserializeObject<SettingJson>(settingFactoryCache);
                settingJson = settingFactory;
                serialiseJson();
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Reset to Factory Settings:",
                    Description = "Success, "+ ctx.Member.Username +" has reset the bot back to factory settings.\nPlease do /setup again for it to function well.",
                    Color = DiscordColor.SpringGreen
                };
                await ctx.RespondAsync(embed:embed);
                return;
            }

        }
        [Command("setup")]
        [Description("Quick setup for the bot to work purposefully by following simple instructions")]
        public async Task SetUp(CommandContext ctx)
        {
            await deserialiseJson();
            bool isServerAdmin = false;
            foreach (DiscordRole role in ctx.Member.Roles)
            {
                if (role.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed)
                {
                    isServerAdmin = true;
                    break;
                }
            }
            if (ctx.Member.IsOwner == true)
            {
                isServerAdmin = true;
            }
            if (adminJson.setUp == true || isServerAdmin == false)
            {
                await ctx.RespondAsync(":grey_exclamation: You have already set up your bot. To reset to factory settings, do /resetbot.");
                return;
            }
            bool successful = false;
            var descriptionArray = new string[] 
            {
                "Progress have not been saved.",
                "There are a couple things we need for the bot to work purposefully. To get a list of the required fields, say 'setuplist' anytime during this setup.\nSay anything to continue, 'n' to exit set up.",
                "Enter discord event log channel webhook's ID.\nSay 'n' to exit set up.",
                "Enter discord attendance log channel webhook's ID.\nSay 'n' to exit set up.",
                "Add the bot's gmail on the google spreadsheet, as an editor. \nGmail: discordlogisticbot@gmail.com (Click the title to check out a forum for a tutorial about this)\nOnce you're finished, say anything to continue. Or 'n' to exit set up.",
                "Enter the spreadsheet's ID.\nSay 'n' to exit set up.",
                "Enter the target sheet's name inside the spreadsheet (Case-Sensitive).\nSay 'n' to exit set up.",
                "Enter the range of cells. \nFor example: 'B1:F3' which means everything from B1 to F3 will be checked.\nSay 'n' to exit set up.",
                "Enter the append row number. \nFor example, if your range is 'B1:F3', you will enter '1' because the first cell(B1) is on row 1.\nSay 'n' to exit set up.",
                "Enter the column's letter where total points are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where RT amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where PT amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where CT amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where AT amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where LT amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where patrol amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where inspection amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where co-host amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where supervision amounts are stored.\nSay 'n' to exit set up.",
                "Enter the column's letter where attendance are stored.\nSay 'n' to exit set up."
            };
            var interactivity = ctx.Client.GetInteractivity();
            var embedCache = new DiscordEmbedBuilder();
            embedCache.WithAuthor("Setting Up");
            embedCache.WithFooter("Logistic Bot, Made by Eth#7536");
            embedCache.WithColor(DiscordColor.DarkGreen);
            var embedMessage = await ctx.Channel.SendMessageAsync(embed: embedCache).ConfigureAwait(false);
            for (int i=1; i<=descriptionArray.Length-1; i++)
            {
                embedCache.WithTitle("Step " + i + " / 19:");
                embedCache.WithDescription(descriptionArray[i]);
                await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                var response = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                while (response.Result.Content == "setuplist")
                {
                    embedCache.WithTitle("Required Fields");
                    embedCache.WithDescription("Discord: Two Webhooks \n TestNewLine Say 'y' to return back, 'n' to exit setting up.");
                    await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                    await response.Result.DeleteAsync();
                    response = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author.Id == ctx.User.Id && (x.Content == "y" || x.Content == "n"), TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                    if (response.Result.Content == "y")
                    {
                        embedCache.WithTitle("Step " + i + " / 5:");
                        embedCache.WithDescription(descriptionArray[i]);
                        await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                        await response.Result.DeleteAsync();
                        response = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author.Id == ctx.User.Id, TimeSpan.FromMinutes(2)).ConfigureAwait(false);
                    }
                    else
                    {
                        embedCache.WithTitle("Exited");
                        embedCache.WithDescription(descriptionArray[0]);
                        embedCache.WithColor(DiscordColor.Red);
                        await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                        await response.Result.DeleteAsync();
                        return;
                    }
                }
                if (response.Result.Content == "n")
                {
                    embedCache.WithTitle("Exited");
                    embedCache.WithDescription(descriptionArray[0]);
                    embedCache.WithColor(DiscordColor.Red);
                    await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                    await response.Result.DeleteAsync();
                    return;
                }
                else if (response.TimedOut == true)
                {
                    embedCache.WithTitle("Timeout");
                    embedCache.WithDescription(descriptionArray[0]);
                    embedCache.WithColor(DiscordColor.Red);
                    await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                    await response.Result.DeleteAsync();
                    return;
                }
                switch (i)
                {
                    case 1:
                        break;
                    case 2:
                        try
                        {
                            settingJson.TrainingWebhookId = ulong.Parse(response.Result.Content);
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to ulong. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 3:
                        try
                        {
                            settingJson.AttendWebHookId = ulong.Parse(response.Result.Content);
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to ulong. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 4:
                        break;
                    case 5:
                        try
                        {
                            settingJson.SpreadsheetId = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 6:
                        try
                        {
                            settingJson.Sheet = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 7:
                        try
                        {
                            settingJson.Range = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 8:
                        try
                        {
                            settingJson.AppendNumber = Int32.Parse(response.Result.Content);
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to Int32. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 9:
                        try
                        {
                            settingJson.TotalColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 10:
                        try
                        {
                            settingJson.RTColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 11:
                        try
                        {
                            settingJson.PTColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 12:
                        try
                        {
                            settingJson.CTColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 13:
                        try
                        {
                            settingJson.ATColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 14:
                        try
                        {
                            settingJson.LTColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 15:
                        try
                        {
                            settingJson.PatrolColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 16:
                        try
                        {
                            settingJson.InspectColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 17:
                        try
                        {
                            settingJson.CohostColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 18:
                        try
                        {
                            settingJson.SuperColumn = response.Result.Content.ToString();
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            await response.Result.DeleteAsync();
                            return;
                        }
                        break;
                    case 19:
                        try
                        {
                            settingJson.AttendColumn = response.Result.Content.ToString();
                            embedCache.WithTitle("Confirm Settings (Say 'y' to confirm, 'n' to decline and exit)");
                            embedCache.WithDescription("");
                            embedCache.AddField("Event Log Channel Webhook ID:", settingJson.TrainingWebhookId.ToString(), true);
                            embedCache.AddField("Attendance Log Channel Webhook ID:", settingJson.AttendWebHookId.ToString());
                            embedCache.AddField("Spreadsheet ID:", settingJson.SpreadsheetId.ToString());
                            embedCache.AddField("Target Sheet Name:", settingJson.Sheet.ToString());
                            embedCache.AddField("Range:", settingJson.Range.ToString());
                            embedCache.AddField("Append  Row Number:", settingJson.AppendNumber.ToString());
                            embedCache.AddField("Total Points Column:", settingJson.TotalColumn.ToString());
                            embedCache.AddField("RT Column:", settingJson.RTColumn.ToString());
                            embedCache.AddField("PT Column:", settingJson.PTColumn.ToString());
                            embedCache.AddField("CT Column:", settingJson.CTColumn.ToString());
                            embedCache.AddField("AT Column:", settingJson.ATColumn.ToString());
                            embedCache.AddField("LT Column:", settingJson.LTColumn.ToString());
                            embedCache.AddField("Patrol Column:", settingJson.PatrolColumn.ToString());
                            embedCache.AddField("Inspection Column:", settingJson.InspectColumn.ToString());
                            embedCache.AddField("Co-host Column:", settingJson.CohostColumn.ToString());
                            embedCache.AddField("Supervision Column:", settingJson.SuperColumn.ToString());
                            embedCache.AddField("Attendance Column:", settingJson.AttendColumn.ToString());
                            embedCache.WithColor(DiscordColor.DarkGreen);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            response = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author.Id == ctx.User.Id && (x.Content == "y" || x.Content == "n"), TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                            if(response.Result.Content == "n")
                            {
                                embedCache.WithTitle("Exited");
                                embedCache.WithDescription(descriptionArray[0]);
                                embedCache.WithColor(DiscordColor.Red);
                                await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                                await response.Result.DeleteAsync();
                                return;
                            }
                            else if (response.TimedOut == true)
                            {
                                embedCache.WithTitle("Timeout");
                                embedCache.WithDescription(descriptionArray[0]);
                                embedCache.WithColor(DiscordColor.Red);
                                await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                                await response.Result.DeleteAsync();
                                return;
                            }
                            await response.Result.DeleteAsync();
                            successful = serialiseJson(true);
                        }
                        catch
                        {
                            embedCache.WithTitle("Error");
                            embedCache.WithDescription("Cannot convert response to string. Progress have not been saved.");
                            embedCache.WithColor(DiscordColor.Red);
                            await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
                            return;
                        }
                        break;
                }
                await response.Result.DeleteAsync();
            }
            if (successful == true)
            {
                embedCache.ClearFields();
                embedCache.WithTitle("Success");
                embedCache.WithDescription("Set up successful. All progress saved.");
                embedCache.WithColor(DiscordColor.Green);
                await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
            }
            else
            {
                embedCache.WithTitle("Unsuccessful");
                embedCache.WithDescription("Set up unsuccessful. Progress unknown.");
                embedCache.WithColor(DiscordColor.Red);
                await embedMessage.ModifyAsync(embed: (DiscordEmbed)embedCache).ConfigureAwait(false);
            }
        }
    }
}
