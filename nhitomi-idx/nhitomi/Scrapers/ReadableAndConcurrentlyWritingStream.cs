using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

// https://gist.github.com/phosphene47/d6dca076ce8d8f5916bffb536817e6c7
namespace nhitomi.Scrapers
{
    /// <summary>
    /// A readonly <see cref="Stream"/> that reads from an underlying stream on read, and concurrently writes the read data to multiple other independent writable streams.
    /// </summary>
    public class ReadableAndConcurrentlyWritingStream : Stream
    {
        readonly Stream _source;
        readonly Stream[] _destinations;
        readonly Stream[] _streams; // all streams, source preceding

        public ReadableAndConcurrentlyWritingStream(Stream source, params Stream[] destinations)
        {
            _source       = source;
            _destinations = destinations;

            _streams    = new Stream[_destinations.Length + 1];
            _streams[0] = _source;

            Array.Copy(_destinations, 0, _streams, 1, _destinations.Length);
        }

        public override bool CanRead => _source.CanRead;
        public override bool CanSeek => _source.CanSeek;
        public override bool CanWrite => _source.CanWrite;
        public override long Length => _source.Length;

        public override long Position
        {
            get => _source.Position;
            set => _source.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = _source.Read(buffer, offset, count);

            if (result != 0)
                foreach (var dest in _destinations)
                    dest.Write(buffer, offset, result);

            return result;
        }

        public override int Read(Span<byte> buffer)
        {
            var result = _source.Read(buffer);

            if (result != 0)
                foreach (var dest in _destinations)
                    dest.Write(buffer.Slice(0, result));

            return result;
        }

        public override int ReadByte()
        {
            var result = _source.ReadByte();

            if (result != -1)
                foreach (var dest in _destinations)
                    dest.WriteByte((byte) result);

            return result;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var result = await _source.ReadAsync(buffer, offset, count, cancellationToken);

            if (result != 0)
                await WhenAllFast(_destinations, s => s.WriteAsync(buffer, offset, result, cancellationToken));

            return result;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var result = await _source.ReadAsync(buffer, cancellationToken);

            if (result != 0)
                await WhenAllFast(_destinations, s => s.WriteAsync(buffer.Slice(0, result), cancellationToken));

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin) => _source.Seek(offset, origin);
        public override void SetLength(long value) => _source.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _source.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _source.Write(buffer);
        public override void WriteByte(byte value) => _source.WriteByte(value);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _source.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken()) => _source.WriteAsync(buffer, cancellationToken);

        public override void Flush()
        {
            var ex = null as Exception;

            foreach (var stream in _streams)
            {
                try
                {
                    stream.Flush();
                }
                catch (Exception e)
                {
                    ex ??= e;
                }
            }

            if (ex != null)
                ExceptionDispatchInfo.Throw(ex);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
            => WhenAllFast(_streams, s => s.FlushAsync(cancellationToken));

        public override void Close()
        {
            var ex = null as Exception;

            foreach (var stream in _streams)
            {
                try
                {
                    stream.Close();
                }
                catch (Exception e)
                {
                    ex ??= e;
                }
            }

            if (ex != null)
                ExceptionDispatchInfo.Throw(ex);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var ex = null as Exception;

                foreach (var stream in _streams)
                {
                    try
                    {
                        stream.Dispose();
                    }
                    catch (Exception e)
                    {
                        ex ??= e;
                    }
                }

                // dispose really should never throw, but since one of the streams threw we'll propagate it nevertheless
                if (ex != null)
                    ExceptionDispatchInfo.Throw(ex);
            }
        }

        public override ValueTask DisposeAsync()
            => WhenAllFast(_streams, s => s.DisposeAsync());

        static Task WhenAllFast(Stream[] streams, Func<Stream, Task> func)
        {
            switch (streams.Length)
            {
                case 0: return Task.CompletedTask;
                case 1: return func(streams[0]);
            }

            var tasks = new Task[streams.Length];
            var count = 0;

            for (var i = 0; i < streams.Length; i++)
            {
                var task = func(streams[i]);

                if (!task.IsCompletedSuccessfully)
                    tasks[count++] = task;
            }

            switch (count)
            {
                case 0:
                    return Task.CompletedTask;

                case 1:
                    return tasks[0];

                default:
                    Array.Resize(ref tasks, count);
                    return Task.WhenAll(tasks);
            }
        }

        static ValueTask WhenAllFast(Stream[] streams, Func<Stream, ValueTask> func)
        {
            switch (streams.Length)
            {
                case 0: return default;
                case 1: return func(streams[0]);
            }

            var tasks = new ValueTask[streams.Length];
            var count = 0;

            for (var i = 0; i < streams.Length; i++)
            {
                var task = func(streams[i]);

                if (!task.IsCompletedSuccessfully)
                    tasks[count++] = task;
            }

            switch (count)
            {
                case 0:
                    return default;

                case 1:
                    return tasks[0];

                default:
                    var slowTasks = new Task[count];

                    for (var i = 0; i < count; i++)
                        slowTasks[i] = tasks[i].AsTask();

                    return new ValueTask(Task.WhenAll(slowTasks));
            }
        }
    }
}