using System.Threading.Channels;
using Microsoft.AspNetCore.Http;

namespace ThirdPerson.Development.Gm.Rollback;

sealed class RelayQueryQueue
{
    readonly Channel<IQuery> m_Queue;
    readonly int m_MaximumPerPump;

    public RelayQueryQueue(int capacity, int maximumPerPump)
    {
        m_Queue = Channel.CreateBounded<IQuery>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        m_MaximumPerPump = maximumPerPump;
    }

    public async Task<T> ReadAsync<T>(Func<T> read, CancellationToken cancellation)
    {
        var query = new Query<T>(read);
        if (!m_Queue.Writer.TryWrite(query))
            throw new BadHttpRequestException("Relay 查询队列已满。", StatusCodes.Status429TooManyRequests);
        using CancellationTokenRegistration registration = cancellation.Register(() => query.Cancel(cancellation));
        return await query.Result;
    }

    public void Pump()
    {
        for (int i = 0; i < m_MaximumPerPump && m_Queue.Reader.TryRead(out IQuery? query); i++)
            query.Execute();
    }

    interface IQuery { void Execute(); }

    sealed class Query<T> : IQuery
    {
        readonly Func<T> m_Read;
        readonly TaskCompletionSource<T> m_Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Query(Func<T> read) => m_Read = read;
        public Task<T> Result => m_Result.Task;
        public void Cancel(CancellationToken cancellation) => m_Result.TrySetCanceled(cancellation);

        public void Execute()
        {
            if (m_Result.Task.IsCompleted)
                return;
            try { m_Result.TrySetResult(m_Read()); }
            catch (Exception exception) { m_Result.TrySetException(exception); }
        }
    }
}
