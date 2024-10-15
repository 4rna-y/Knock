using CmlLib.Core.Installer.Forge.Versions;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Fleck;
using Knock.Enums;
using Knock.Models;
using Knock.Models.Response;
using Knock.Schedules;
using Knock.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Knock.Scenarios
{
    public class CreateServerScenario : ChannelScenarioBase
    {
        private int versionPage = 0;
        public CreateServerScenario(SocketGuild guild, SocketUser user, SocketCategoryChannel category) : 
            base(guild, user, category, "create-server")
        {
            Models.Add(new ScenarioModel("create-server-register-minecraft-account-button", ClickRegisterAccountButton));
            Models.Add(new ScenarioModel("register-account-modal", SubmitRegisterAccountModal));
            Models.Add(new ScenarioModel("create-server-register-minecraft-account-button-confirm", ClickRegisterConfirmButton));
            Models.Add(new ScenarioModel("send-server-type-name", StartServerSetUp));
            Models.Add(new ScenarioModel("create-server-type-name", ClickTypeNameButton));
            Models.Add(new ScenarioModel("type-name-modal", SubmitServerNameModal));
            Models.Add(new ScenarioModel("server-app-selectmenu", SelectServerApplication));
            Models.Add(new ScenarioModel("server-version-selectmenu", SelectServerVersion));
            Models.Add(new ScenarioModel("server-memory-amount", SelectServerMemory));
            Models.Add(new ScenarioModel("select-server", SelectServer));
            Models.Add(new ScenarioModel("create-server-confirm", ClickConfirmButton));

            Models.Add(new ScenarioModel(
                "select-server-version-back", async arg => await SwapBackSelectMenuItem(arg, "select-server-version")));
            Models.Add(new ScenarioModel(
                "select-server-version-next", async arg => await SwapNextSelectMenuItem(arg, "select-server-version")));

            Models.Add(new ScenarioModel("backto-type-name", BackToTypeName));
            Models.Add(new ScenarioModel("backto-select-server-app", BackToSelectServerApplication));
            Models.Add(new ScenarioModel("backto-select-server-version", BackToServerVersion));
            Models.Add(new ScenarioModel("backto-select-server-memory", BackToServerMemory));
            Models.Add(new ScenarioModel("backto-select-server", BackToSelectServer));
        }

        public override async Task Start()
        {
            MinecraftAccounts accounts = Data.Get<MinecraftAccounts>("mcinfo");
            MinecraftAccount mc = accounts.Accounts.FirstOrDefault(x => x.DiscordId.Equals(User.Id) && x.IsConfirmed);
            if (mc is null)
            {
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.create_server.not_registered_account.title"))
                    .WithDescription(Locale.Get("embed.create_server.not_registered_account.description"))
                    .WithColor(Color["warning"]);

                ComponentBuilder component = new ComponentBuilder()
                    .WithButton(
                        Locale.Get("button.register_minecraft_account"),
                        $"scenario.{ScenarioId}.create-server-register-minecraft-account-button",
                        ButtonStyle.Primary,
                        new Emoji("📝"));

                IMessage msg = await TextChannel.SendMessageAsync(embed: embed.Build(), components: component.Build());
                MessageIds.Add("register-account", msg.Id);

                return;
            }

            await StartServerSetUp(null);
        }

        #region RegisterMinecraftAccount
        private async Task ClickRegisterAccountButton(SocketInteraction arg)
        {
            SocketMessageComponent component = arg as SocketMessageComponent;
            if (component is null) return;

            ModalBuilder modal = new ModalBuilder()
                .WithTitle(Locale.Get("modal.register_account.title"))
                .WithCustomId($"scenario.{ScenarioId}.register-account-modal")
                .AddTextInput(
                    new TextInputBuilder(
                        Locale.Get("modal.register_account.minecraft_name"),
                        "minecraft-name",
                        TextInputStyle.Short,
                        placeholder: Locale.Get("modal.register_account.minecraft_name_placeholder"),
                        required: true))
                ;

            await component.RespondWithModalAsync(modal.Build());
        }

        private async Task SubmitRegisterAccountModal(SocketInteraction arg)
        {
            SocketModal modal = arg as SocketModal;
            if (modal is null) return;

            await RemoveMessage("register-account");
            await RemoveMessage("register-account-not-found");
            await RemoveMessage("register-account-found");

            string username = modal.Data.Components.FirstOrDefault(x => x.CustomId.Equals("minecraft-name")).Value;

            HttpResponseModel<JsonDocument> responseModel = 
                await Http.Get($"https://api.mojang.com/users/profiles/minecraft/{username}");

            if (responseModel.Code == System.Net.HttpStatusCode.NotFound) 
            {
                EmbedBuilder notFoundEmbed = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.create_server.minecraft_username_not_found.title"))
                    .WithDescription(Locale.Get("embed.create_server.minecraft_username_not_found.description"))
                    .WithColor(Color["error"]);

                ComponentBuilder component = new ComponentBuilder()
                    .WithButton(
                        Locale.Get("button.register_minecraft_account"),
                        $"scenario.{ScenarioId}.create-server-register-minecraft-account-button",
                        ButtonStyle.Primary,
                        new Emoji("📝"));

                await modal.RespondAsync(embed: notFoundEmbed.Build(), components: component.Build());
                IMessage notFoundMsg = await modal.GetOriginalResponseAsync();
                MessageIds.Add("register-account-not-found", notFoundMsg.Id);

                return;
            }

            JsonElement root = responseModel.Result.RootElement;

            string name = root.GetProperty("name").GetString();
            string uuid = root.GetProperty("id").GetString();

            Data.Set<MinecraftAccounts>("mcinfo", x =>
            {
                MinecraftAccount acc = new MinecraftAccount(modal.User.Id, uuid, name, false);
                int i = x.Accounts.FindIndex(y => y.DiscordId.Equals(acc.DiscordId));
                if (i < 0) x.Accounts.Add(acc);
                else x.Accounts[i] = acc;
                return x;
            });

            EmbedBuilder confirmEmbed = new EmbedBuilder()
                .WithTitle(name)
                .WithDescription(Locale.Get("embed.create_server.minecraft_username_found.description"))
                .WithThumbnailUrl($"https://mc-heads.net/avatar/{uuid}")
                .WithColor(Color["success"]);
                

            ComponentBuilder confirmButton = new ComponentBuilder()
                    .WithButton(
                        Locale.Get("button.register_minecraft_account_confirm"),
                        $"scenario.{ScenarioId}.create-server-register-minecraft-account-button-confirm",
                        ButtonStyle.Success,
                        new Emoji("✅"))
                    .WithButton(
                        Locale.Get("button.register_minecraft_account_retry"),
                        $"scenario.{ScenarioId}.create-server-register-minecraft-account-button",
                        ButtonStyle.Secondary,
                        new Emoji("❌"));

            await modal.RespondAsync(embed: confirmEmbed.Build(), components: confirmButton.Build());

            IMessage FoundMsg = await modal.GetOriginalResponseAsync();
            MessageIds.Add("register-account-found", FoundMsg.Id);

            return;
        }

        private async Task ClickRegisterConfirmButton(SocketInteraction arg)
        {
            SocketMessageComponent component = arg as SocketMessageComponent;
            if (component is null) return;

            Data.Set<MinecraftAccounts>("mcinfo", x =>
            {
                int i = x.Accounts.FindIndex(y => y.DiscordId.Equals(component.User.Id));
                if (i < 0) return x;
                else x.Accounts[i].IsConfirmed = true;
                return x;
            });

            await StartServerSetUp(null);
        }
        #endregion

        private async Task StartServerSetUp(SocketInteraction arg)
        {
            EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.create_server.type_name.title"))
                    .WithDescription(Locale.Get("embed.create_server.type_name.description"))
                    .WithColor(Color["question"]);

            ComponentBuilder component = new ComponentBuilder()
                    .WithButton(
                        Locale.Get("button.server_setup.type_name"),
                        $"scenario.{ScenarioId}.create-server-type-name",
                        ButtonStyle.Primary,
                        new Emoji("📝"));

            IMessage msg = await TextChannel.SendMessageAsync(embed: embed.Build(), components: component.Build());

            Add("type-name", msg.Id);
            AddComponent("type-name", msg.Id);
        }

        private async Task BackToTypeName(SocketInteraction arg)
        {
            await RemoveMessage("select-server-app");
            await ToggleMessage("type-name", false);
        }

        private async Task ClickTypeNameButton(SocketInteraction arg)
        {
            SocketMessageComponent component = arg as SocketMessageComponent;
            if (component is null) return;

            ModalBuilder modal = new ModalBuilder()
                .WithTitle(Locale.Get("modal.type_name.title"))
                .WithCustomId($"scenario.{ScenarioId}.type-name-modal")
                .AddTextInput(
                    new TextInputBuilder(
                        Locale.Get("modal.type_name.label"),
                        "server-type-name",
                        TextInputStyle.Short,
                        placeholder: Locale.Get("modal.type_name.placeholder"),
                        required: true));

            await component.RespondWithModalAsync(modal.Build());
        }

        private async Task SubmitServerNameModal(SocketInteraction arg)
        {
            SocketModal modal = arg as SocketModal;
            if (modal is null) return;

            await ToggleMessage("type-name", true);

            string name = modal.Data.Components.FirstOrDefault(x => x.CustomId.Equals("server-type-name")).Value;

            await Scenario.ChangeChannelName(this, Locale.Get("channel_name.create_server") + name);

            Server.CreateBuilder(ScenarioId);
            Server.SetBuilder(ScenarioId, x => x.WithName(name));

            EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.create_server.select_app.title"))
                    .WithColor(Color["default"])
                    .WithDescription(Locale.Get("embed.create_server.select_app.description"));

            ComponentBuilder component = new ComponentBuilder()
                .WithSelectMenu(
                    $"scenario.{ScenarioId}.server-app-selectmenu",
                    new List<SelectMenuOptionBuilder>()
                    {
                        new SelectMenuOptionBuilder(
                            Locale.Get("selectmenu.select_app.options.paper.title"),
                            "0",
                            Locale.Get("selectmenu.select_app.options.paper.description"),
                            new Emoji("🪶")
                            ),
                        //new SelectMenuOptionBuilder(
                        //    Locale.Get("selectmenu.select_app.options.forge.title"),
                        //    "1",
                        //    Locale.Get("selectmenu.select_app.options.forge.description"),
                        //    new Emoji("🛠️")),
                    },
                    Locale.Get("selectmenu.select_app.label"))
                .WithButton(
                    Locale.Get("button.server_setup.back"),
                    $"scenario.{ScenarioId}.backto-type-name",
                    ButtonStyle.Secondary,
                    new Emoji("◀️"));


            await modal.RespondAsync(embed: embed.Build(), components: component.Build());
            IMessage msg = await modal.GetOriginalResponseAsync();

            Add("select-server-app", msg.Id);
            AddComponent("select-server-app", msg.Id);
        }

        private async Task BackToSelectServerApplication(SocketInteraction arg)
        {
            await RemoveMessage("select-server-version");
            await ToggleMessage("select-server-app", false);
        }

        private async Task SelectServerApplication(SocketInteraction arg)
        {
            SocketMessageComponent componentArg = arg as SocketMessageComponent;
            if (componentArg is null) return;

            await ToggleMessage("select-server-app", true);

            versionPage = 0;
            string text = string.Join(", ", componentArg.Data.Values);

            await componentArg.DeferAsync();

            using (componentArg.Channel.EnterTypingState())
            {
                Server.SetBuilder(
                    ScenarioId,
                    x => x.WithServerApplication((ServerApplication)int.Parse(text)));

                MultiPageableSelectMenus.Remove("select-server-version");
                MultiPageableSelectMenus.Add(
                    "select-server-version",
                    new MultiPageableSelectMenu(await GetVersions(Server.GetBuilder(ScenarioId).Application)));

                EmbedBuilder embed = new EmbedBuilder()
                        .WithTitle(Locale.Get("embed.create_server.select_version.title"))
                        .WithColor(Color["default"])
                        .WithDescription(Locale.Get("embed.create_server.select_version.description"));

                SelectMenuBuilder menu = new SelectMenuBuilder()
                    .WithCustomId($"scenario.{ScenarioId}.server-version-selectmenu")
                    .WithPlaceholder(Locale.Get("selectmenu.select_version.label"));

                foreach (SelectMenuItem item in MultiPageableSelectMenus["select-server-version"].GetCurrentSegmentedItems())
                {
                    menu.AddOption(item.Label, item.Value);
                }

                ComponentBuilder component = new ComponentBuilder()
                    .WithSelectMenu(menu)
                    .WithButton(
                        Locale.Get("button.server_setup.back"),
                        $"scenario.{ScenarioId}.select-server-version-back",
                        ButtonStyle.Secondary,
                        new Emoji("◀️"),
                        disabled: true,
                        row: 1)
                    .WithButton(
                        Locale.Get("button.server_setup.next"),
                        $"scenario.{ScenarioId}.select-server-version-next",
                        ButtonStyle.Secondary,
                        new Emoji("▶️"),
                        disabled: MultiPageableSelectMenus["select-server-version"].Items.Count < 25,
                        row: 1)
                    .WithButton(
                        Locale.Get("button.server_setup.back"),
                        $"scenario.{ScenarioId}.backto-select-server-app",
                        ButtonStyle.Secondary,
                        new Emoji("◀️"),
                        row: 2);

                IMessage msg = await componentArg.Channel.SendMessageAsync(embed: embed.Build(), components: component.Build());
                MessageIds.Add("select-server-version", msg.Id);
                ComponentMessageIds.Add("select-server-version", msg.Id);
            }
        }

        private async Task BackToServerVersion(SocketInteraction arg)
        {
            await RemoveMessage("select-server-memory");
            await ToggleMessage("select-server-version", false);
        }

        private async Task SelectServerVersion(SocketInteraction arg)
        {
            SocketMessageComponent componentArg = arg as SocketMessageComponent;
            if (componentArg is null) return;

            await ToggleMessage("select-server-version", true);

            await componentArg.DeferAsync();

            using (componentArg.Channel.EnterTypingState())
            {
                string version = string.Join(", ", componentArg.Data.Values);

                if (Server.GetBuilder(ScenarioId).Application.Equals(ServerApplication.Paper))
                {
                    HttpResponseModel<JsonDocument> buildRes = await Http.GetPaperBuildNumber(version);
                    if (buildRes.Code != System.Net.HttpStatusCode.OK) return;
                    List<int> builds = buildRes.Result.RootElement.GetProperty("builds").Deserialize<List<int>>();
                    int build = builds.Last();

                    string dlLink = await Http.GetPaperDownloadLink(version, build.ToString());
                    if (dlLink is null) return;

                    Server.SetBuilder(
                        ScenarioId,
                        x => x.WithVersion(version)
                            .WithApplicationFullName($"paper-{version}-{build}")
                            .WithDownloadLink(dlLink));
                }
                else
                {
                    ForgeVersion forgeVersion = await Http.GetForgeVersion(version);
                    if (forgeVersion is null) return;

                    Server.SetBuilder(
                        ScenarioId,
                        x => x.WithVersion(version)
                            .WithApplicationFullName($"forge-{forgeVersion.MinecraftVersionName}-{forgeVersion.ForgeVersionName}")
                            .WithDownloadLink(forgeVersion.GetInstallerFile().DirectUrl));
                }

                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.create_server.select_memory_amount.title"))
                    .WithDescription(Locale.Get("embed.create_server.select_memory_amount.description"))
                    .WithColor(Color["default"]);

                ComponentBuilder component = new ComponentBuilder()
                    .WithSelectMenu(
                        new SelectMenuBuilder()
                            .WithPlaceholder(Locale.Get("selectmenu.select_memory_amount.label"))
                            .WithCustomId($"scenario.{ScenarioId}.server-memory-amount")
                            .AddOption(
                                Locale.Get("selectmenu.select_memory_amount.options.amount_2G.title"),
                                "2",
                                Locale.Get("selectmenu.select_memory_amount.options.amount_2G.description"),
                                new Emoji("👨‍👦‍👦"))
                            .AddOption(
                                Locale.Get("selectmenu.select_memory_amount.options.amount_4G.title"),
                                "4",
                                Locale.Get("selectmenu.select_memory_amount.options.amount_4G.description"),
                                new Emoji("💪"))
                            .AddOption(
                                Locale.Get("selectmenu.select_memory_amount.options.amount_8G.title"),
                                "8",
                                Locale.Get("selectmenu.select_memory_amount.options.amount_8G.description"),
                                new Emoji("⚖️")))
                    .WithButton(
                        Locale.Get("button.server_setup.back"),
                        $"scenario.{ScenarioId}.backto-select-server-version",
                        ButtonStyle.Secondary,
                        new Emoji("◀️"),
                        row: 1);

                IMessage msg = await componentArg.Channel.SendMessageAsync(embed: embedBuilder.Build(), components: component.Build());
                MessageIds.Add("select-server-memory", msg.Id);
                ComponentMessageIds.Add("select-server-memory", msg.Id);
            }
        }

        private async Task BackToServerMemory(SocketInteraction arg)
        {
            await RemoveMessage("create-server-select-server");
            await ToggleMessage("select-server-memory", false);
        }

        private async Task SelectServerMemory(SocketInteraction arg)
        {
            SocketMessageComponent componentArg = arg as SocketMessageComponent;
            if (componentArg is null) return;

            await ToggleMessage("select-server-memory", true);

            string amount = string.Join(", ", componentArg.Data.Values);

            Server.SetBuilder(ScenarioId, x => x.WithMemoryAmount(amount));

            using (componentArg.Channel.EnterTypingState())
            {
                SelectMenuBuilder selectMenuBuilder = new SelectMenuBuilder()
                            .WithCustomId($"scenario.{ScenarioId}.select-server")
                            .WithPlaceholder(Locale.Get("selectmenu.select_server.label"));

                for (int i = 0; i < Request.GetConnections().Count; i++)
                {
                    selectMenuBuilder.AddOption($"Server{i + 1}", $"{i}");
                }

                ComponentBuilder componentBuilder = new ComponentBuilder()
                    .WithSelectMenu(selectMenuBuilder)
                    .WithButton(
                        Locale.Get("button.server_setup.back"),
                        $"scenario.{ScenarioId}.backto-select-server-memory",
                        ButtonStyle.Secondary,
                        new Emoji("◀️"),
                        row: 1);
                EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.create_server.select_server.title"))
                    .WithDescription(Locale.Get("embed.create_server.select_server.description"))
                    .WithColor(Color["default"]);

                await componentArg.RespondAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
            }

            IMessage msg = await componentArg.GetOriginalResponseAsync();
            MessageIds.Add("create-server-select-server", msg.Id);
            ComponentMessageIds.Add("create-server-select-server", msg.Id);
        }

        private async Task BackToSelectServer(SocketInteraction arg)
        {
            await RemoveMessage("create-server-confirm");
            await ToggleMessage("create-server-select-server", false);
        }

        public async Task SelectServer(SocketInteraction arg)
        {
            SocketMessageComponent componentArg = arg as SocketMessageComponent;
            if (componentArg is null) return;

            await ToggleMessage("create-server-select-server", true);

            string location = string.Join(", ", componentArg.Data.Values);
            int serverIdx = int.Parse(location);
            IWebSocketConnectionInfo info = Request.GetConnections()[serverIdx].ConnectionInfo;

            Server.SetBuilder(ScenarioId, x => x
                .WithLastAccessDate(DateTime.Now)
                .WithStoredLocation($"{info.ClientIpAddress}:{info.ClientPort}")
                .WithOwners(new List<ulong>() { { componentArg.User.Id } })
            );

            ServerContainerBuilder b = Server.GetBuilder(ScenarioId);

            EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.create_server.confirmation.title"))
                    .WithDescription(Locale.Get("embed.create_server.confirmation.description"))
                    .WithColor(Color["default"])
                    .AddField(Locale.Get("embed.create_server.confirmation.field.server_name"), b.Name)
                    .AddField(Locale.Get("embed.create_server.confirmation.field.server_app"), b.Application)
                    .AddField(Locale.Get("embed.create_server.confirmation.field.server_version"), b.Version)
                    .AddField(Locale.Get("embed.create_server.confirmation.field.server_memory"), b.MemoryAmount + "GB")
                    .AddField(Locale.Get("embed.create_server.confirmation.field.stored_location"), $"Server{serverIdx + 1}");

            ComponentBuilder component = new ComponentBuilder()
                .WithButton(
                        Locale.Get("button.server_setup.back"),
                        $"scenario.{ScenarioId}.backto-select-server",
                        ButtonStyle.Secondary,
                        new Emoji("◀️"))
                .WithButton(
                        Locale.Get("button.server_setup.confirm"),
                        $"scenario.{ScenarioId}.create-server-confirm",
                        ButtonStyle.Success,
                        new Emoji("🙆"));

            await componentArg.RespondAsync(embed: embedBuilder.Build(), components: component.Build());
            IMessage msg = await componentArg.GetOriginalResponseAsync();
            MessageIds.Add("create-server-confirm", msg.Id);
            ComponentMessageIds.Add("create-server-confirm", msg.Id);
        }

        public async Task ClickConfirmButton(SocketInteraction arg)
        {
            SocketMessageComponent componentArg = arg as SocketMessageComponent;
            if (componentArg is null) return;

            await ToggleMessage("create-server-confirm", true);

            await componentArg.DeferAsync();

            ServerContainer container = Server.CreateContainer(ScenarioId);

            EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.create_server.server_create_waiting.title"))
                    .WithDescription(Locale.Get("embed.create_server.server_create_waiting.description"))
                    .WithColor(Color["default"]);

            await componentArg.Channel.SendMessageAsync(embed: embedBuilder.Build());

            IDisposable typingState = componentArg.Channel.EnterTypingState();

            bool res = await Request.CreateServerContainer(container.Id);
                        
            typingState.Dispose();

            EmbedBuilder resultEmbedBuilder = new EmbedBuilder()
                .WithTitle(Locale.Get($"embed.create_server.server_create_{(res ? "success" : "failed")}.title"))
                .WithDescription(Locale.Get($"embed.create_server.server_create_{(res ? "success" : "failed")}.description"))
                .WithColor(Color[res ? "success" : "error"]);

            ScheduleBase schedule = new UnregisterScenarioSchedule(ScenarioId, 10);
            Schedule.Resister(schedule);

            EmbedBuilder deleteEmbedBuilder = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.channel_delete.title"))
                    .WithColor(Color["default"]);

            if (res)
            {
                ChannelScenarioBase scenario = new ManageServerScenario(container.Id, Guild, User, Category);
                await Scenario.Register(scenario);

                IMessage msg = await scenario.TextChannel.GetMessageAsync(scenario.MentionMessageId);
                deleteEmbedBuilder.WithDescription(
                        string.Format(Locale.Get("embed.channel_delete.description"),
                        msg.GetJumpUrl()));
            }
            else
            {
                Server.RemoveContainer(container.Id);
            }

            await componentArg.Channel.SendMessageAsync(
                embeds: new Embed[] { resultEmbedBuilder.Build(), deleteEmbedBuilder.Build() });
        }

        private async Task<List<SelectMenuItem>> GetVersions(ServerApplication app)
        {
            if (app.Equals(ServerApplication.Paper))
            {
                HttpResponseModel<JsonDocument> versions = await Http.GetPaperVersions();
                JsonDocument json = versions.Result;
                JsonElement elem = json.RootElement;
                JsonElement version = elem.GetProperty("versions");
                List<string> list = version.Deserialize<List<string>>();
                list.Reverse();
                List<SelectMenuItem> dest = new List<SelectMenuItem>();

                for (int i = 0; i < list.Count; i++)
                {
                    string item = list[i + (versionPage * 25)];
                    dest.Add(new SelectMenuItem(item, item));
                }
                return dest;
            }
            else
            {
                List<string> list = await Http.GetForgeVersions();
                List<SelectMenuItem> dest = new List<SelectMenuItem>();

                for (int i = 0; i < list.Count; i++)
                {
                    string item = list[i + (versionPage * 25)];
                    dest.Add(new SelectMenuItem(item, item));
                }

                return dest;
            }
        }
    }
}
