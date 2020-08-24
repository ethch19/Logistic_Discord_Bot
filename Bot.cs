using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Linq;
using System.Net.Http.Headers;
using System.Drawing;
using Google;
using Logistic_Bot.Commands;
using DSharpPlus.Interactivity;

namespace Logistic_Bot
{
    public class Bot
    {
        public DiscordClient Client { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "LogisticsLogger";
        static SheetsService service;
        SettingJson structJson;
        AdminCacheJson adminJson;
        public bool running = false;
        public List<string> playerCooldown = new List<string>();
        public async Task RunAsync()
        {
            //StartUp Goodgle Sheets
            UserCredential credential;
            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            //Discord Bot Json
            var json = string.Empty;
            using (var fs = File.OpenRead("config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false); //ConfigureAwait so after reading file is finished, the program doesn't have to use the same thread

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };
            Client = new DiscordClient(config);

            Client.Ready += OnClientReady; //When bot is ready, fires the OnClientReady function
            Client.MessageCreated += WaitOnWebhook;

            Client.UseInteractivity(new InteractivityConfiguration());
            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { configJson.Prefix },
                EnableDms = false,
                EnableMentionPrefix = false,
                DmHelp = true,
            };
            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.RegisterCommands<AdminCommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1);//Make sure bot stays online after loaded
        }
        private Task OnClientReady(ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }
        private async Task WaitOnWebhook(MessageCreateEventArgs e)
        {
            while (running == true)
            {
                await Task.Delay(500);
            }
            running = true;
            var adminCache = string.Empty;
            using (var fs = File.OpenRead("adminCache.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                adminCache = await sr.ReadToEndAsync().ConfigureAwait(false);
            adminJson = JsonConvert.DeserializeObject<AdminCacheJson>(adminCache);
            if (adminJson.enabled == false || adminJson.setUp == false)
            {
                running = false;
                return;
            }
            var settingJson = string.Empty;
            using (var fs = File.OpenRead("settings.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                settingJson = await sr.ReadToEndAsync().ConfigureAwait(false); //ConfigureAwait so after reading file is finished, the program doesn't have to use the same thread

            structJson = JsonConvert.DeserializeObject<SettingJson>(settingJson);
            if ((e.Author.Id == structJson.TrainingWebhookId || e.Author.Id == structJson.AttendWebHookId) && e.Author.IsBot == true)
            {
                string trainingType = e.Message.Embeds[0].Fields[0].Value;
                string tempColumn = string.Empty;
                string hostName = e.Message.Embeds[0].Fields[1].Value;
                string cohostName = string.Empty;
                string supervisorName = string.Empty;
                try
                {
                    if (trainingType == "Attendance")
                    {
                        cohostName = string.Empty;
                        supervisorName = string.Empty;
                    }
                    else
                    {
                        if (e.Message.Embeds[0].Fields[2].Name == "Co-host: (If none, leave it blank)")
                        {
                            cohostName = e.Message.Embeds[0].Fields[2].Value;
                        }
                        if (e.Message.Embeds[0].Fields[2].Name == "Supervisor: (If none, leave it blank)")
                        {
                            supervisorName = e.Message.Embeds[0].Fields[2].Value;
                        }
                        if (e.Message.Embeds[0].Fields[3].Name == "Supervisor: (If none, leave it blank)" && cohostName != string.Empty)
                        {
                            supervisorName = e.Message.Embeds[0].Fields[3].Value;
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Error");
                }
                if ((cohostName == hostName || supervisorName == hostName || supervisorName == cohostName) && trainingType != "Attendance" && (supervisorName != string.Empty && cohostName != string.Empty))
                {
                    await e.Message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
                    await e.Message.RespondAsync(":red_square: Rejection: Found Malicious Form; Same names for fields");
                    Console.WriteLine(":red_square: Rejection: Found Malicious Form; Same names for fields");
                    running = false;
                    return;
                }
                switch (trainingType)
                {
                    case "RT":
                        tempColumn = structJson.RTColumn;
                        break;
                    case "PT":
                        tempColumn = structJson.PTColumn;
                        break;
                    case "CT":
                        tempColumn = structJson.CTColumn;
                        break;
                    case "AT":
                        tempColumn = structJson.ATColumn;
                        break;
                    case "LT":
                        tempColumn = structJson.LTColumn;
                        break;
                    case "Patrol":
                        tempColumn = structJson.PatrolColumn;
                        break;
                    case "Inspection":
                        tempColumn = structJson.InspectColumn;
                        break;
                    case "Attendance":
                        tempColumn = structJson.AttendColumn;
                        break;
                }
                var range = $"{structJson.Sheet}!{structJson.Range}";
                var request = service.Spreadsheets.Values.Get(structJson.SpreadsheetId, range);
                var response = request.Execute();
                var values = response.Values;
                if (values != null && values.Count > 0)
                {
                    string[] cells = new string[3];
                    string[] names = new string[3];
                    string appendCell = string.Empty;
                    string points = string.Empty;
                    int currentAppend = 0;
                    for (int i = 0; i < values.Count; i++)
                    {
                        try
                        {
                            if (values[i][0].ToString() == hostName && tempColumn == structJson.AttendColumn)
                            {
                                string targetCell = $"{structJson.Sheet}!{tempColumn}" + (i + structJson.AppendNumber).ToString();
                                appendCell = targetCell;
                                var cache1 = structJson.AppendNumber ?? default(int);
                                currentAppend = i + cache1;
                                break;
                            }
                            if (values[i][0].ToString() == hostName)
                            {
                                string targetCell = $"{structJson.Sheet}!{tempColumn}" + (i + structJson.AppendNumber).ToString();
                                cells[0] = targetCell;
                                names[0] = hostName;
                                var cache1 = structJson.AppendNumber ?? default(int);
                                currentAppend = i + cache1;
                            }
                            if (cohostName != string.Empty && values[i][0].ToString() == cohostName)
                            {
                                string targetCell = $"{structJson.Sheet}!{structJson.CohostColumn}" + (i + structJson.AppendNumber).ToString();
                                cells[1] = targetCell;
                                names[1] = cohostName;
                            }
                            if (supervisorName != string.Empty && values[i][0].ToString() == supervisorName)
                            {
                                string targetCell = $"{structJson.Sheet}!{structJson.SuperColumn}" + (i + structJson.AppendNumber).ToString();
                                cells[2] = targetCell;
                                names[2] = supervisorName;
                            }
                        }
                        catch (ArgumentOutOfRangeException arg)
                        {
                            continue;
                        }
                    }
                    if (tempColumn == structJson.AttendColumn)
                    {
                        if (appendCell == string.Empty)
                        {
                            await e.Message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
                            await e.Message.RespondAsync("Rejection: Some names not found on sheet");
                            Console.WriteLine(":red_square: Rejection: Some names not found on sheet");
                            running = false;
                            return;
                        }
                        UpdateCell(appendCell, hostName);
                        await Reply(e, currentAppend, hostName, true);
                        running = false;
                    }
                    else if (playerCooldown.Contains(names[0]) == true || playerCooldown.Contains(names[1]) == true || playerCooldown.Contains(names[2]) == true)
                    {
                        await e.Message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
                        await e.Message.RespondAsync("Rejection: Some names still on cooldown: 1 minute");
                        Console.WriteLine(":red_square: Rejection: Some names still on cooldown: 1 minute");
                        running = false;
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (string.IsNullOrEmpty(cells[i]) == true || string.IsNullOrEmpty(names[i]) == true)
                            {
                                switch (i)
                                {
                                    case 0:
                                        if (string.IsNullOrEmpty(hostName) == false)
                                        {
                                            await e.Message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
                                            await e.Message.RespondAsync("Rejection: Some names not found on sheet");
                                            Console.WriteLine(":red_square: Rejection: Some names not found on sheet");
                                            return;
                                        }
                                        break;
                                    case 1:
                                        if (string.IsNullOrEmpty(cohostName) == false)
                                        {
                                            await e.Message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
                                            await e.Message.RespondAsync("Rejection: Some names not found on sheet");
                                            Console.WriteLine(":red_square: Rejection: Some names not found on sheet");
                                            return;
                                        }
                                        break;
                                    case 2:
                                        if (string.IsNullOrEmpty(supervisorName) == false)
                                        {
                                            await e.Message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
                                            await e.Message.RespondAsync("Rejection: Some names not found on sheet");
                                            Console.WriteLine(":red_square: Rejection: Some names not found on sheet");
                                            return;
                                        }
                                        break;
                                }
                            }
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            if (string.IsNullOrEmpty(cells[i]) == true || string.IsNullOrEmpty(names[i]) == true)
                            {
                                continue;
                            }
                            UpdateCell(cells[i], names[i]);
                        }
                        await Reply(e, currentAppend, hostName, false);
                        running = false;
                    }
                }
                else
                {
                    Console.WriteLine("Rejection: Sheets values are null or empty");
                    await e.Message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":x:"));
                    await e.Message.RespondAsync(":red_square: Rejection: Sheets values are null or empty");
                    running = false;
                }
            }
            running = false;
            return;
        }
        private async Task Reply(MessageCreateEventArgs c, int currentAppend, string playerName, bool attendance)
        {
            var range = $"{structJson.TotalColumn}{currentAppend}";
            var request = service.Spreadsheets.Values.Get(structJson.SpreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;
            string points = values[0][0].ToString();
            await c.Message.CreateReactionAsync(DiscordEmoji.FromName(Client, ":white_check_mark:"));
            if (attendance == false)
            {
                if (points == "1")
                {
                    await c.Message.RespondAsync($":green_square: Completion: Training logged successfully, {playerName}'s currently point is " + points.ToString());
                }
                else
                {
                    await c.Message.RespondAsync($":green_square: Completion: Training logged successfully, {playerName}'s currently points are " + points.ToString());
                }
                Console.WriteLine("Completion: Training logged successfully");
            }
            else if (attendance == true)
            {
                if (points == "1")
                {
                    await c.Message.RespondAsync($":green_square: Completion: Attendance logged successfully, {playerName}'s currently point is " + points.ToString());
                }
                else
                {
                    await c.Message.RespondAsync($":green_square: Completion: Attendance logged successfully, {playerName}'s currently points are " + points.ToString());
                }
                Console.WriteLine("Completion: Attendance logged successfully");
            }
        }

        private bool UpdateCell(string range, string playerName)
        {
            if (playerCooldown.Contains(playerName)==true)
            {
                return false;
            }
            //Get old value
            try
            {
                string newValue = string.Empty;
                var request = service.Spreadsheets.Values.Get(structJson.SpreadsheetId, range);
                var response = request.Execute();
                var values = response.Values;
                for (int i = 0; i < values.Count; i++)
                {
                    newValue = values[i][i].ToString();
                }
                newValue = (Int32.Parse(newValue) + 1).ToString();
                //Change value
                var valueRange = new ValueRange();
                var objectList = new List<object>() { newValue };
                valueRange.Values = new List<IList<object>> { objectList };
                var updateRequest = service.Spreadsheets.Values.Update(valueRange, structJson.SpreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var updateResponse = updateRequest.Execute();
                Cooldown(playerName);
                return true;
            }
            catch (GoogleApiException ex)
            {
                Console.WriteLine(ex);
                return false;
            }
        }
        private async Task Cooldown(string PlayerName)
        {
            if (playerCooldown.Contains(PlayerName) == true)
            {
                return;
            }
            else
            {
                playerCooldown.Add(PlayerName);
                await Task.Delay(60000);
                playerCooldown.Remove(PlayerName);
                Console.WriteLine(PlayerName + " cooldown removed");
                return;
            }
        }
    }
}
