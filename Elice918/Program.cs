using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace EliceBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private string _token;
        private string _prefix;

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

            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            Console.WriteLine("Bot started.");
            await Task.Delay(-1);
        }

        private async Task MessageHandler(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            if (!message.Content.StartsWith(_prefix)) return;

            var command = message.Content.Substring(_prefix.Length);

            if (command == "test")
            {
                await message.Channel.SendMessageAsync("테스트 성공이에요!");
            }
            if (command == "도움")
            {
                await message.Channel.SendMessageAsync("도움이 필요하신가요?\n❓현재 사용 가능한 명령어는 test, 제피르, 엘리스, 엘리프,Ala(ala)가 있어요!\n📩기타 내용이나 문의는 주인님에게 문의해주세요!");
            }
            if (command == "제피르")
            {
                await message.Channel.SendMessageAsync("바보똥멍청이변태지만좋은누나");
            }
            if (command == "엘리스")
            {
                await message.Channel.SendMessageAsync("히히~ 저에요!");
            }
            if (command == "엘리프")
            {
                await message.Channel.SendMessageAsync("ご主人様..♡");
            }
            if (command == "Ala" || command == "ala")
            {
                await message.Channel.SendMessageAsync("My best polish friend met in roblox 6 or 5 idk months);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}