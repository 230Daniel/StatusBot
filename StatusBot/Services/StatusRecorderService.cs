using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace StatusBot.Services
{
    public class StatusRecorderService
    {
        private readonly ILogger<StatusRecorderService> _logger;
        private readonly IConfiguration _config;
        private readonly DiscordBotBase _bot;
        private readonly SemaphoreSlim _semaphore;
        
        public StatusRecorderService(ILogger<StatusRecorderService> logger, IConfiguration config, DiscordBotBase bot)
        {
            _logger = logger;
            _config = config;
            _bot = bot;
            _semaphore = new(1, 1);
        }

        public async Task SaveAllStatusesAsync()
        {
            try
            {
                foreach (var guild in _bot.GetGuilds().Values)
                {
                    await _bot.Chunker.ChunkAsync(guild);
                    foreach (var member in guild.Members)
                    {
                        var status = (member.Value.GetPresence()?.Activities.FirstOrDefault(x => x.Type == ActivityType.Custom) as ICustomActivity)?.Text;
                        
                        if (!string.IsNullOrEmpty(status))
                        {
                            _logger.LogInformation("{Member} had the status {Status} on startup", member.Value.Tag, status);
                            await SaveStatusAsync(member.Key, status);
                        }
                            
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception thrown saving all statuses");
            }
        }
        
        public async Task SaveStatusAsync(Snowflake userId, string status)
        {
            // Wrap in double quotes and replace " with ""
            status = $"\"{status.Replace("\"", "\"\"")}\"";
            
            await _semaphore.WaitAsync();

            try
            {
                if (!Directory.Exists(_config["Recording:Directory"]))
                    Directory.CreateDirectory(_config["Recording:Directory"]);
                var filename = $"{_config["Recording:Directory"]}/{userId}.csv";

                var attempts = 0;
                while (attempts < 3)
                {
                    try
                    {
                        if (File.Exists(filename))
                        {
                            var previousStatuses = new List<(DateTime, string)>();
                            using (var reader = new StreamReader(filename))
                            {
                                await reader.ReadLineAsync();
                                while (!reader.EndOfStream)
                                {
                                    var line = await reader.ReadLineAsync();
                                    var values = line.Split(",");
                                    previousStatuses.Add((DateTime.Parse(values[0]), values[1]));
                                }
                            }

                            if (previousStatuses.OrderBy(x => x.Item1).Last().Item2 == status) return;
                        }
                        else
                        {
                            await File.AppendAllTextAsync(filename, "Timestamp,Status\n");
                        }

                        await File.AppendAllTextAsync(filename, $"{DateTime.UtcNow},{status}\n");
                        break;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(1000);
                        attempts++;

                        if (attempts == 3) throw;
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
