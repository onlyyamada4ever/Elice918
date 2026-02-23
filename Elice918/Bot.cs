using Discord;
using Discord.WebSocket;
using Elice918.Utillity.Games;
using EliceBot.Utility;
using System;
using System.Threading.Tasks;

namespace EliceBot
{
    class Bot
    {
        private DiscordSocketClient _client;
        private string _token;
        private string _prefix;

        private EliceBot.Utility.Help _help;
        private RandCommand _rand;

        public async Task MainAsync()
        {
            _token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            _prefix = Environment.GetEnvironmentVariable("DISCORD_PREFIX");

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

            // ✅ 명령어 객체 생성
            _help = new EliceBot.Utility.Help(_prefix);
            _rand = new RandCommand(_prefix);

            // ✅ 이벤트 연결(명령어 파일로 위임)
            _client.MessageReceived += _help.HandleMessageAsync;
            _client.InteractionCreated += _help.HandleInteractionAsync;
            _client.MessageReceived += _rand.MessageHandler;
            _client.InteractionCreated += _rand.InteractionHandler;


            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            Console.WriteLine("Bot started.");
            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}