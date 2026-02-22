using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;   // ✅ 추가
using System.Threading;            // ✅ 추가

namespace EliceBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private string _token;
        private string _prefix;

        // ✅ 메시지(주사위 메시지)마다 진행 중인 애니메이션을 취소하기 위한 저장소
        private readonly Dictionary<ulong, CancellationTokenSource> _randTasks = new();

        static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            _prefix = Environment.GetEnvironmentVariable("DISCORD_PREFIX") ?? ".";

            if (string.IsNullOrWhiteSpace(_token))
            {
                Console.WriteLine("DISCORD_TOKEN 환경변수가 설정되지 않았습니다.");
                return;
            }

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);

            _client.Log += LogAsync;
            _client.MessageReceived += MessageHandler;
            _client.InteractionCreated += InteractionHandler;

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            Console.WriteLine("Bot started.");
            await Task.Delay(-1);
        }

        // ===================== 명령어 =====================
        private async Task MessageHandler(SocketMessage msg)
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
                if (parts.Length < 3 ||
                    !int.TryParse(parts[1], out var min) ||
                    !int.TryParse(parts[2], out var max))
                {
                    await message.Channel.SendMessageAsync("앨리스는 숫자 주사위만 가지고 있어요..");
                    return;
                }

                if (min >= max)
                {
                    await message.Channel.SendMessageAsync("최솟값은 최댓값보다 작아야 해요!");
                    return;
                }

                // ✅ 중요: 여기서 바로 await 하지 않고 별도 Task로 실행
                _ = RunRandAnimationAsync(message, min, max);
                return;
            }
        }

        // ===================== 랜덤 애니메이션 (Gateway 안 막히게 분리) =====================
        private async Task RunRandAnimationAsync(SocketUserMessage message, int min, int max)
        {
            IUserMessage randMsg = null;

            // ✅ 이 애니메이션을 중간에 버튼으로 끊어낼 수 있게 토큰 생성
            var cts = new CancellationTokenSource();

            try
            {
                randMsg = await message.Channel.SendMessageAsync(
                    embed: new EmbedBuilder()
                        .WithTitle("👀 앨리스가 주사위 가져오는 중..")
                        .Build(),
                    components: RandPreComponents(min, max));

                // ✅ 메시지 ID 기준으로 취소 토큰 보관
                _randTasks[randMsg.Id] = cts;

                await Task.Delay(1000, cts.Token);

                await randMsg.ModifyAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("✌️✊✋ 주사위 누가 굴릴지 정하는 중..")
                        .WithDescription("가위.. 바위.. 보!")
                        .Build();
                    m.Components = RandPreComponents(min, max);
                });

                await Task.Delay(2000, cts.Token);

                await randMsg.ModifyAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("🎲 주사위 굴리는 중..")
                        .WithDescription("두구두구두구두구")
                        .Build();
                    m.Components = RandPreComponents(min, max);
                });

                await Task.Delay(500, cts.Token);

                int result = Random.Shared.Next(min, max + 1);

                await randMsg.ModifyAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("🎲 결과가 나왔어요!")
                        .WithDescription($"**{result}**")
                        .Build();
                    m.Components = RandPostComponents(min, max);
                });
            }
            catch (OperationCanceledException)
            {
                // ✅ 버튼(바로 돌리기/닫기)로 애니메이션이 취소된 경우: 아무것도 안 함
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                // ✅ 정리(중복 취소/누수 방지)
                if (randMsg != null && _randTasks.TryGetValue(randMsg.Id, out var stored) && ReferenceEquals(stored, cts))
                {
                    _randTasks.Remove(randMsg.Id);
                }
                cts.Dispose();
            }
        }

        // ===================== 버튼 처리 =====================
        private async Task InteractionHandler(SocketInteraction interaction)
        {
            if (interaction is not SocketMessageComponent comp) return;

            if (comp.Data.CustomId.StartsWith("rand:reveal:"))
            {
                // ✅ 진행 중 애니메이션이 있으면 취소해서 "자동으로 다시 바뀌는 현상" 방지
                if (_randTasks.TryGetValue(comp.Message.Id, out var cts))
                {
                    cts.Cancel();
                    _randTasks.Remove(comp.Message.Id);
                }

                var s = comp.Data.CustomId.Split(':');
                int min = int.Parse(s[2]);
                int max = int.Parse(s[3]);

                int result = Random.Shared.Next(min, max + 1);

                await comp.UpdateAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithTitle("👊 앨리스의 주사위를 뺏어서 돌렸어요.")
                        .WithDescription($"**꼭 그렇게 해야 됐을까요..? 결과는 {result}이에요.**")
                        .Build();
                    m.Components = RandPostComponents(min, max);
                });
                return;
            }

            if (comp.Data.CustomId == "rand:close")
            {
                // 진행 중 애니메이션이 있으면 취소
                if (_randTasks.TryGetValue(comp.Message.Id, out var cts))
                {
                    cts.Cancel();
                    _randTasks.Remove(comp.Message.Id);
                }

                await comp.UpdateAsync(m =>
                {
                    m.Content = "🧹 앨리스가 주사위를 정리했어요."; // ← 여기 네 문구로 바꿔
                    m.Embed = null; // 임베드 제거
                    m.Components = new ComponentBuilder().Build(); // 버튼 제거
                });

                return;
            }
        }

        // ===================== 버튼 UI =====================
        private MessageComponent RandPreComponents(int min, int max)
        {
            return new ComponentBuilder()
                .WithButton("기다릴 시간 없어!!", $"rand:reveal:{min}:{max}", ButtonStyle.Success)
                .WithButton("그만두기", "rand:close", ButtonStyle.Danger)
                .Build();
        }

        private MessageComponent RandPostComponents(int min, int max)
        {
            return new ComponentBuilder()
                .WithButton("다시 돌리기", $"rand:reveal:{min}:{max}", ButtonStyle.Primary)
                .WithButton("그만두기", "rand:close", ButtonStyle.Danger)
                .Build();
        }

        // ===================== 로그 =====================
        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}