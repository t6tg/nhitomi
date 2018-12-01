﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace nhitomi
{
    public interface IDoujinClient : IDisposable
    {
        string Name { get; }
        string Url { get; }
        string IconUrl { get; }

        Regex GalleryRegex { get; }

        Task<IDoujin> GetAsync(string id);
        Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query);

        Task UpdateAsync();
    }

    public static class DoujinClient
    {
        sealed class SynchronizedClient : IDoujinClient
        {
            readonly SemaphoreSlim _semaphore;
            readonly IDoujinClient _impl;

            public SynchronizedClient(IDoujinClient impl)
            {
                _impl = impl;
            }

            public string Name => _impl.Name;
            public string Url => _impl.Url;
            public string IconUrl => _impl.IconUrl;

            public Regex GalleryRegex => _impl.GalleryRegex;

            public async Task<IDoujin> GetAsync(string id)
            {
                await _semaphore.WaitAsync(); try
                {
                    return await _impl.GetAsync(id);
                }
                finally { _semaphore.Release(); }
            }

            public async Task<IAsyncEnumerable<IDoujin>> SearchAsync(string query)
            {
                await _semaphore.WaitAsync(); try
                {
                    return await _impl.SearchAsync(query);
                }
                finally { _semaphore.Release(); }
            }

            public async Task UpdateAsync()
            {
                await _semaphore.WaitAsync(); try
                {
                    await _impl.UpdateAsync();
                }
                finally { _semaphore.Release(); }
            }

            public void Dispose() => _semaphore.Dispose();
        }

        public static IDoujinClient Synchronized(this IDoujinClient client) => new SynchronizedClient(client);
    }
}
