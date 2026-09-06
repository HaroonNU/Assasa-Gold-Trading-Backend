using GoldTrading.Infrastructure.Repositories;

namespace GoldTrading.Application.Tests.Repositories;

public sealed class TradeSettlementLockTests
{
    [Fact]
    public async Task ExecuteAsync_RunsDelegateAndReturnsResult()
    {
        using var lockSvc = new TradeSettlementLock();
        var result = await lockSvc.ExecuteAsync(() => Task.FromResult(42));
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_SerialisesConcurrentCalls()
    {
        using var lockSvc = new TradeSettlementLock();
        var concurrentCount = 0;
        var maxConcurrent   = 0;
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            lockSvc.ExecuteAsync(async () =>
            {
                var current = Interlocked.Increment(ref concurrentCount);
                // Track the high-water mark
                int observed;
                do
                {
                    observed = maxConcurrent;
                    if (current <= observed) break;
                } while (Interlocked.CompareExchange(ref maxConcurrent, current, observed) != observed);

                await Task.Delay(5);
                Interlocked.Decrement(ref concurrentCount);
                return true;
            }));

        await Task.WhenAll(tasks);

        // The semaphore(1,1) must ensure at most 1 concurrent execution
        Assert.Equal(1, maxConcurrent);
    }

    [Fact]
    public async Task ExecuteAsync_ReleasesLock_WhenDelegateThrows()
    {
        using var lockSvc = new TradeSettlementLock();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            lockSvc.ExecuteAsync<bool>(() => throw new InvalidOperationException("boom")));

        // Lock must be released — this must not deadlock
        var result = await lockSvc.ExecuteAsync(() => Task.FromResult(true));
        Assert.True(result);
    }
}
