using Discord;
using Discord.WebSocket;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
                await message.Channel.SendMessageAsync("도움이 필요하신가요?\n❓현재 사용 가능한 명령어는 test, 제피르, 엘리스, 엘리프,Ala(ala), 벨리스가 있어요!\n📩기타 내용이나 문의는 주인님에게 문의해주세요!");
            }
            if (command == "제피르")
            {
                await message.Channel.SendMessageAsync("바보똥멍청이변태지만좋은누나");
            }
            if (command == "제피르(희망편)")
            {
                await message.Channel.SendMessageAsync("세상에서 제일이쁘고 착하고 타인의 모범이 되는 누나");
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
                await message.Channel.SendMessageAsync("My best polish friend met in roblox 6 or 5 idk months");
            }
            if (command == "벨리스")
            {
                await message.Channel.SendMessageAsync("제 동생이에요! 요즘엔 바빠서 잘 못 만나지만.. 다음엔 꼭 사탕을 잔뜩 사갈거에요! 벨리스는 사탕을 아주 좋아하거든요.");
            }
            if (command == "EmbedTest")
            {
                await message.Channel.SendMessageAsync("이건 임베드 메시지입니다!", embed: new EmbedBuilder
                {
                    Title = "임베드 제목",
                    Description = "임베드 설명",
                    Color = Color.Blue
                }
                .Build());
            }
            /*
            if (command == "Rand")
            {
                // 명령어 뒤 숫자 (min,max)에 따라 랜덤 숫자 생성
                await message.Channel.SendMessageAsync($"랜덤으로 {}와 {}사이의 숫자를 뽑아 드릴게요! 잠시만요..");
                // 주사위 굴리는 중... 이라고 메시지 보내기
                var loading = await message.Channel.SendMessageAsync("주사위 굴리는 중.. 🎲");
                // 3초 후에 랜덤 숫자 보내기(버튼 누르면 바로 랜덤 숫자 보내기)
                await loading.ModifyAsync(m => m.Content = $"{result}");
            }
            static int Rnum(int min, int max)
            {
                var random = new Random();
                return random.Next(min, max + 1);
            }
            */
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}