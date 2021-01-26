using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashSharp.Core
{
    class SubscriptionManager : BackgroundService
    {
        private const string FileName = "subscription.yml";
        private readonly ILogger<SubscriptionManager> _logger;
        private readonly string _clashHomePath;

        private readonly HttpClient _httpClient = new();
        private readonly string? _url;
        private readonly TimeSpan _interval;

        public SubscriptionManager(
            ILogger<SubscriptionManager> logger,
            IOptions<SubscriptionOptions> options,
            IOptions<ClashOptions> clashOptions
            )
        {
            _logger = logger;

            _clashHomePath = clashOptions.Value.HomePath;
            var optionsValue = options.Value;
            _url = optionsValue.Url;
            _interval = TimeSpan.FromMinutes(optionsValue.Interval);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_url == null)
            {
                return;
            }

            _logger.LogInformation("Started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckUpdate(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Check subscription failed.");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Stopped.");
        }

        public string SubscriptionPath => Path.Join(_clashHomePath, FileName);

        public async Task CheckUpdate(CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(_url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var remoteTime = response.Headers.Date;
            if (remoteTime.HasValue && File.Exists(SubscriptionPath))
            {
                var localTime = File.GetLastWriteTime(SubscriptionPath);
                if (localTime >= remoteTime.Value.LocalDateTime)
                {
                    _logger.LogInformation("Local config file is newer.");
                    return;
                }
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(SubscriptionPath, bytes, cancellationToken);
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public record SubscriptionOptions
    {
        [Url]
        public string? Url { get; set; }
        /// <summary>
        /// In minutes
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Interval { get; set; } = 12 * 60;
    }
}
