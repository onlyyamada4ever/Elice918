using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EliceBot.Utility
{
    public class Help
    {
        private readonly string _prefix;

        // 카테고리 데이터(대충)
        private static readonly Dictionary<string, (string title, string desc, (string n, string v, bool inlineField)[] fields)> _map
            = new()
            {
                ["home"] = ("📘 도움말", "카테고리를 선택해 주세요.", new[]
            {
                (".help / .도움말", "도움말", false),
                (".rand <최대> / .rand <최소> <최대>", "랜덤 숫자", false),
            }),
                ["utility"] = ("🛠 Utility", "유틸 명령어", new[]
            {
                (".rand <최대>", "1부터 최댓값까지", false),
                (".rand <최소> <최대>", "범위 랜덤", false),
            }),
                ["admin"] = ("🛡 Admin", "관리자 명령어", new[]
            {
                ("(준비중)", "나중에 추가", false),
            }),
            };

        public Help(string prefix)
        {
            _prefix = prefix;
        }

        // ===== MessageReceived에서 호출 =====
        public async Task HandleMessageAsync(SocketMessage msg)
        {
            if (msg is not SocketUserMessage message) return;
            if (message.Author.IsBot) return;
            if (!message.Content.StartsWith(_prefix)) return;

            var parts = message.Content[_prefix.Length..]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) return;

            var cmd = parts[0].ToLower();
            if (cmd != "help" && cmd != "도움말") return;

            await message.Channel.SendMessageAsync(
                embed: BuildEmbed("home"),
                components: BuildComponents(message.Author.Id, "home"),
                messageReference: new MessageReference(message.Id));
        }

        // ===== InteractionCreated에서 호출 =====
        public async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            if (interaction is not SocketMessageComponent comp) return;

            // help:select:<ownerId>
            if (!comp.Data.CustomId.StartsWith("help:select:")) return;

            var s = comp.Data.CustomId.Split(':');
            ulong ownerId = ulong.Parse(s[2]);

            if (comp.User.Id != ownerId)
            {
                await comp.RespondAsync("이 도움말은 요청한 사람만 조작 가능해요.\n-# Tip : .help / .도움말을 입력해 주세요!", ephemeral: true);
                return;
            }

            var category = comp.Data.Values.FirstOrDefault() ?? "home";

            await comp.UpdateAsync(m =>
            {
                m.Embed = BuildEmbed(category);
                m.Components = BuildComponents(ownerId, category);
            });
        }

        // ===== 내부 유틸 =====
        private static Embed BuildEmbed(string key)
        {
            if (!_map.TryGetValue(key, out var data))
                data = _map["home"];

            var eb = new EmbedBuilder()
                .WithTitle(data.title)
                .WithDescription(data.desc);

            foreach (var f in data.fields)
                eb.AddField(f.n, f.v, f.inlineField);

            return eb.Build();
        }

        private static MessageComponent BuildComponents(ulong ownerId, string currentKey)
        {
            var select = new SelectMenuBuilder()
                .WithCustomId($"help:select:{ownerId}")
                .WithPlaceholder("카테고리를 선택해 주세요")
                .AddOption("홈", "home", isDefault: currentKey == "home")
                .AddOption("Utility", "utility", isDefault: currentKey == "utility")
                .AddOption("Admin", "admin", isDefault: currentKey == "admin");

            return new ComponentBuilder()
                .WithSelectMenu(select)
                .WithButton("서포트 서버", style: ButtonStyle.Link, url: "https://discord.gg/5XfVt3dhte")
                .WithButton("대시보드", style: ButtonStyle.Link, url: "https://www.youtube.com/watch?v=AuKR2fQbMBk")
                .Build();
        }
    }
}