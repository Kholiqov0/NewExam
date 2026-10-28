using Domain.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Services;

public static class EventCache
{
    private const string VersionKey = "events:version";

    public static string ByIdKey(int id) => $"events:id:{id}";

    public static string ListKey(string version, EventFilter filter)
        => $"events:list:{version}:search={filter.Search?.Trim().ToLower()}:category={filter.CategoryId}:min={filter.MinPrice}:max={filter.MaxPrice}:sort={filter.SortBy}:desc={filter.Desc}:page={filter.Page}:size={filter.PageSize}";

    public static DistributedCacheEntryOptions Options() => new DistributedCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(2),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
    };

    public static async Task<string> GetVersionAsync(IDistributedCache cache)
    {
        var version = await cache.GetStringAsync(VersionKey);

        if (version is null)
        {
            version = Guid.NewGuid().ToString("N");
            await cache.SetStringAsync(VersionKey, version, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
        }

        return version;
    }

    public static async Task InvalidateAsync(IDistributedCache cache, int eventId)
    {
        await cache.RemoveAsync(ByIdKey(eventId));
        await cache.SetStringAsync(VersionKey, Guid.NewGuid().ToString("N"), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });
    }
}
