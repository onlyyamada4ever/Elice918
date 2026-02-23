// Commands/RandCommand.cs
using Discord;
using Discord.WebSocket;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Elice918.Utillity.Games
{
    class RandCommand
    {
        private readonly string _prefix;
        private readonly Dictionary<ulong, CancellationTokenSource> _randTasks = new();

        public RandCommand(string prefix)
        {
            _prefix = prefix;
        }

        // ===================== 명령어 =====================
        public async Task MessageHandler(SocketMessage msg)
        {
            if (msg is not SocketUserMessage message) return;
            if (message.Author.IsBot) return;
            if (!message.Content.StartsWith(_prefix)) return;

            var parts = message.Content[_prefix.Length..]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) return;

            var command = parts[0].ToLower();

            if (command == "rand")
            {
                int min;
                int max;

                if (parts.Length == 2 &&
                    int.TryParse(parts[1], out max))
                {
                    min = 1;
                }
                else if (parts.Length < 3 ||
                    !int.TryParse(parts[1], out min) ||
                    !int.TryParse(parts[2], out max))
                {
                    await message.Channel.SendMessageAsync(
                        "앨리스는 숫자 주사위만 가지고 있어요.." +
                        "\n-# Tip : .rand <최소> <최대>, 혹은 .rand <최대>로 적어야 돼요! (예시: .rand 9 18 / .rand 918)",
                        messageReference: new MessageReference(message.Id));
                    return;
                }
                else if (min >= max)
                {
                    await message.Channel.SendMessageAsync(
                        "최솟값은 최댓값보다 작아야 해요!" +
                        "\n-# Tip : .rand <최소> <최대>, 혹은 .rand <최대>로 적어야 돼요! (예시: .rand 9 18 / .rand 10)",
                        messageReference: new MessageReference(message.Id));
                    return;
                }

                _ = RunRandAnimationAsync(message, min, max);
                return;
            }
        }

        // ===================== 랜덤 애니메이션 =====================
        private async Task RunRandAnimationAsync(SocketUserMessage message, int min, int max)
        {
            IUserMessage randMsg = null;
            var cts = new CancellationTokenSource();

            try
            {
                randMsg = await message.Channel.SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("👀 앨리스가 주사위 가져오는 중..")
                        .Build(),
                    components: RandPreComponents(message.Author.Id, min, max),
                    messageReference: new MessageReference(message.Id)); // ✅ 답장 형식

                _randTasks[randMsg.Id] = cts;

                int result = Random.Shared.Next(min, max + 1);

                await Task.Delay(1000, cts.Token);
                await randMsg.ModifyAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("✌️✊✋ 주사위 누가 굴릴지 정하는 중..")
                        .WithDescription("가위.. 바위.. 보!")
                        .Build();
                    m.Components = RandPreComponents(message.Author.Id, min, max);
                });

                await Task.Delay(2000, cts.Token);
                await randMsg.ModifyAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("🎲 주사위 굴리는 중..")
                        .WithDescription("과연..!!")
                        .Build();
                    m.Components = RandPreComponents(message.Author.Id, min, max);
                });

                await Task.Delay(500, cts.Token);
                await randMsg.ModifyAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("🎲 결과가 나왔어요!")
                        .WithDescription($"**{result}**")
                        .Build();
                    m.Components = RandPostComponents(message.Author.Id, min, max);
                });
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (randMsg != null)
                    _randTasks.Remove(randMsg.Id);
                cts.Dispose();
            }
        }

        // ===================== 버튼 처리 =====================
        public async Task InteractionHandler(SocketInteraction interaction)
        {
            if (interaction is not SocketMessageComponent comp) return;

            var parts = comp.Data.CustomId.Split(':');

            ulong ownerId = 0;

            if (parts.Length >= 3 && parts[0] == "rand")
            {
                ownerId = ulong.Parse(parts[2]);

                if (comp.User.Id != ownerId)
                {
                    await comp.RespondAsync("이 주사위는 시작한 사람만 조작 가능해요. \n-# Tip : 주사위를 굴리고 싶으시다면, .rand 명령어를 쓰면 돼요!", ephemeral: true);
                    return;
                }
            }

            if (comp.Data.CustomId.StartsWith("rand:quick:"))
            {
                if (_randTasks.TryGetValue(comp.Message.Id, out var cts))
                {
                    cts.Cancel();
                    _randTasks.Remove(comp.Message.Id);
                }

                int min = int.Parse(parts[3]);
                int max = int.Parse(parts[4]);

                int result = Random.Shared.Next(min, max + 1);

                await comp.UpdateAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("엘리스의 주사위를 뺏어서 바로 굴렸어요.")
                        .WithDescription($"**너무해요.. 😢 그럴 필욘 없었잖아요! 결과는 {result}이에요..**")
                        .Build();
                    m.Components = RandPostComponents(ownerId, min, max);
                });
                return;
            }

            if (comp.Data.CustomId.StartsWith("rand:reroll:"))
            {
                int min = int.Parse(parts[3]);
                int max = int.Parse(parts[4]);
                int result = Random.Shared.Next(min, max + 1);

                await comp.UpdateAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("주사위를 다시 돌렸어요.")
                        .WithDescription($"**이번에는 {result}이 나왔어요!**")
                        .Build();
                    m.Components = RandPostComponents(ownerId, min, max);
                });
                return;
            }

            if (comp.Data.CustomId.StartsWith("rand:close:"))
            {
                if (_randTasks.TryGetValue(comp.Message.Id, out var cts))
                {
                    cts.Cancel();
                    _randTasks.Remove(comp.Message.Id);
                }

                await comp.UpdateAsync(m =>
                {
                    m.Content = "🧹 엘리스가 주사위를 정리했어요.";
                    m.Embed = null;
                    m.Components = new ComponentBuilder().Build();
                });
                return;
            }
        }

        // ===================== 버튼 UI =====================
        private MessageComponent RandPreComponents(ulong ownerId, int min, int max)
        {
            return new ComponentBuilder()
                .WithButton("바로 돌리기", $"rand:quick:{ownerId}:{min}:{max}", ButtonStyle.Success)
                .WithButton("그만두기", $"rand:close:{ownerId}", ButtonStyle.Danger)
                .Build();
        }

        private MessageComponent RandPostComponents(ulong ownerId, int min, int max)
        {
            return new ComponentBuilder()
                .WithButton("다시 돌리기", $"rand:reroll:{ownerId}:{min}:{max}", ButtonStyle.Primary)
                .WithButton("그만두기", $"rand:close:{ownerId}", ButtonStyle.Danger)
                .Build();
        }
    }
}