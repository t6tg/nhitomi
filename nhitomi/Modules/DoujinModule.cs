// Copyright (c) 2018-2019 chiya.dev
//
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Discord.Parsing;
using nhitomi.Interactivity;

namespace nhitomi.Modules
{
    [Module("doujin", IsPrefixed = false)]
    public class DoujinModule
    {
        readonly IMessageContext _context;
        readonly AppSettings _settings;
        readonly IDatabase _database;
        readonly InteractiveManager _interactive;

        public DoujinModule(IMessageContext context, IOptions<AppSettings> options, IDatabase database,
            InteractiveManager interactive)
        {
            _context = context;
            _settings = options.Value;
            _database = database;
            _interactive = interactive;
        }

        [Command("get")]
        public async Task GetAsync(string source, string id)
        {
            using (_context.BeginTyping())
            {
                var doujin = await _database.GetDoujinAsync(source, id);

                if (doujin == null)
                {
                    await _context.ReplyAsync("messages.doujinNotFound");
                    return;
                }

                await _interactive.SendInteractiveAsync(new DoujinMessage(doujin), _context);
            }
        }

        [Command("from")]
        public async Task FromAsync(string source)
        {
            using (_context.BeginTyping())
            {
                IAsyncEnumerable<Doujin> enumerate(IDatabase db, int offset) => db.EnumerateDoujinsAsync(x =>
                {
                    if (!string.IsNullOrEmpty(source))
                        x = x.Where(d => d.Source == source);

                    // todo: ascending option
                    x = x.OrderByDescending(d => d.UploadTime);

                    return x.Skip(offset).Take(50);
                });

                await _interactive.SendInteractiveAsync(new DoujinListMessage(enumerate), _context);
            }
        }

        [Command("search"), Binding("[query+]")]
        public async Task SearchAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                await _context.ReplyAsync("messages.invalidQuery");
                return;
            }

            using (_context.BeginTyping())
            {
                IAsyncEnumerable<Doujin> enumerate(IDatabase db, int offset) => db.EnumerateDoujinsAsync(x =>
                    x.FullTextSearch(_database, query,
                        d => d.OriginalName,
                        d => d.PrettyName));

                await _interactive.SendInteractiveAsync(new DoujinListMessage(enumerate), _context);
            }
        }

        [Command("download", Aliases = new[] {"dl"})]
        public async Task DownloadAsync(string source, string id)
        {
            Doujin doujin;

            using (_context.BeginTyping())
            {
                // allow downloading only for users of guild
                if (!_settings.Doujin.AllowNonGuildMemberDownloads)
                {
                    var guild = await _context.Client.GetGuildAsync(_settings.Discord.Guild.GuildId);

                    // guild user is null; user is not in guild
                    if (await guild.GetUserAsync(_context.User.Id) == null)
                    {
                        await _context.ReplyAsync("messages.joinForDownload");
                        return;
                    }
                }

                doujin = await _database.GetDoujinAsync(source, id);

                if (doujin == null)
                {
                    await _context.ReplyAsync("messages.doujinNotFound");
                    return;
                }
            }

            await _interactive.SendInteractiveAsync(new DownloadMessage(doujin), _context);
        }
    }
}