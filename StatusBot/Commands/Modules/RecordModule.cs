using Disqord;
using Disqord.Bot;
using Microsoft.Extensions.Configuration;
using Qmmands;

namespace StatusBot.Commands.Modules
{
    public class RecordModule : DiscordGuildModuleBase
    {
        private readonly IConfiguration _config;
        
        public RecordModule(IConfiguration config)
        {
            _config = config;
        }
        
        [Command("statuses")]
        public DiscordCommandResult Statuses([RequireNotBot] IMember member)
            => Response($"{_config["Recording:BaseUrl"]}{member.Id}.csv");
    }
}
