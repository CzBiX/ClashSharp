using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashSharp.Core
{
    class SubscriptionManager
    {
        private const string FileName = "subscription.yml";
        private readonly ILogger<SubscriptionManager> _logger;
        private readonly string _clashHomePath;

        private readonly HttpClient _httpClient = new();
        private readonly string? _url;
        private readonly TimeSpan _interval;

        public event Action? Updated;

        public SubscriptionManager(
            ILogger<SubscriptionManager> logger,
            IOptions<SubscriptionOptions> options,
            IOptions<ClashOptions> clashOptions
        )
        {
            _logger = logger;

            _clashHomePath = Path.GetFullPath(clashOptions.Value.HomePath);
            var optionsValue = options.Value;
            _url = optionsValue.Url;
            _interval = TimeSpan.FromMinutes(optionsValue.Interval);
        }

        public bool HasSubscription => _url != null;

        public async void UpdateSubscription(CancellationToken stoppingToken)
        {
            if (!HasSubscription)
            {
                return;
            }

            _logger.LogInformation("Started.");
            _logger.LogInformation($"Check interval: {_interval}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (await CheckUpdate(stoppingToken))
                    {
                        Updated?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Check subscription failed.");
                }

                var nextCheckTime = DateTime.Now + _interval;
                _logger.LogInformation($"Next check will be at {nextCheckTime}");
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Stopped.");
        }

        public string SubscriptionPath => Path.Join(_clashHomePath, FileName);

        private async Task<bool> CheckUpdate(CancellationToken cancellationToken)
        {
            var hasLocalData = File.Exists(SubscriptionPath);
            var request = new HttpRequestMessage(HttpMethod.Get, _url);

            if (hasLocalData)
            {
                request.Headers.IfModifiedSince = File.GetLastWriteTime(SubscriptionPath);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                _logger.LogInformation("Remote config not changed.");
                return false;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Remote config changed.");

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(SubscriptionPath, bytes, cancellationToken);

            var lastModified = response.Content.Headers.LastModified;
            if (lastModified.HasValue)
            {
                File.SetLastWriteTime(SubscriptionPath, lastModified.Value.LocalDateTime);
            }

            return true;
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public record SubscriptionOptions
    {
        [Url] public string? Url { get; set; }

        /// <summary>
        /// In minutes
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Interval { get; set; } = 12 * 60;
    }
}
