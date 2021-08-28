using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StatusBot.Services
{
    public class StatusBotService : DiscordBotService
    {
        private readonly ILogger<StatusBotService> _logger;
        private readonly IConfiguration _config;

        private readonly Dictionary<Snowflake, string> _customStatuses;
        private readonly StatusRecorderService _recorderService;

        public StatusBotService(ILogger<StatusBotService> logger, IConfiguration config, StatusRecorderService recorderService)
        {
            _logger = logger;
            _config = config;
            _recorderService = recorderService;
            
            _customStatuses = new();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Bot.WaitUntilReadyAsync(stoppingToken);
            _ = _recorderService.SaveAllStatusesAsync();
        }

        protected override async ValueTask OnPresenceUpdated(PresenceUpdatedEventArgs e)
        {
            string status;
            lock (_customStatuses)
            {
                status = (e.NewPresence.Activities.FirstOrDefault(x => x.Type == ActivityType.Custom) as ICustomActivity)?.Text;
                if (string.IsNullOrEmpty(status) || _customStatuses.TryGetValue(e.MemberId, out var oldStatus) && status == oldStatus)
                    return;
                _customStatuses[e.MemberId] = status;
            }

            var user = Bot.GetUser(e.MemberId) as IUser ?? await Bot.FetchUserAsync(e.MemberId);
            _logger.LogInformation($"{user.Tag} updated their status to '{status}'");

            await _recorderService.SaveStatusAsync(e.MemberId, status);
        }
    }
}
