using Discord;
using Discord.WebSocket;
using Knock.Models;
using Knock.Schedules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knock.Scenarios
{
    public class AccountRegistrationScenario : ChannelScenarioBase
    {
        public AccountRegistrationScenario(
            SocketGuild guild, SocketUser user, SocketCategoryChannel category) 
            : base(guild, user, category, "acc-reg")
        {
            Models.Add(new ScenarioModel("create-server-register-minecraft-account-button", ClickRegisterAccountButton));
            Models.Add(new ScenarioModel("register-account-modal", SubmitRegisterAccountModal));
            Models.Add(new ScenarioModel("create-server-register-minecraft-account-button-confirm", ClickRegisterConfirmButton));
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
            else
            {
                EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.account_registration.already.title"))
                    .WithColor(Color["success"]);

                ScheduleBase schedule = new UnregisterScenarioSchedule(ScenarioId, 10);
                Schedule.Resister(schedule);

                EmbedBuilder deleteEmbedBuilder = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.channel_delete.title"))
                    .WithColor(Color["success"]);

                await TextChannel.SendMessageAsync(
                    embeds: Utils.ArrayOf(embed.Build(), deleteEmbedBuilder.Build()));
            }
        }

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

            await RemoveMessage("register-account-found");
            await arg.DeferAsync();

            EmbedBuilder embed = new EmbedBuilder()
                    .WithTitle(Locale.Get("embed.account_registration.already.title"))
                    .WithColor(Color["success"]);

            ScheduleBase schedule = new UnregisterScenarioSchedule(ScenarioId, 10);
            Schedule.Resister(schedule);

            EmbedBuilder deleteEmbedBuilder = new EmbedBuilder()
                .WithTitle(Locale.Get("embed.channel_delete.title"))
                .WithColor(Color["success"]);

            await TextChannel.SendMessageAsync(
                embeds: Utils.ArrayOf(embed.Build(), deleteEmbedBuilder.Build()));
        }
    }
}
