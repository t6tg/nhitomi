using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using nhitomi.Controllers;
using nhitomi.Database;
using nhitomi.Models.Queries;
using nhitomi.Storage;
using IElasticClient = nhitomi.Database.IElasticClient;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace nhitomi.Scrapers
{
    public interface IBookScraper : IScraper
    {
        /// <summary>
        /// Finds a book in the database given a book URL recognized by this scraper.
        /// Setting strict to false will allow multiple matches in the string; otherwise, the entire string will be attempted as one match.
        /// </summary>
        IAsyncEnumerable<(IDbEntry<DbBook>, DbBookContent)> FindBookByUrlAsync(string url, bool strict, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the image of a page of the given book content as a stream.
        /// </summary>
        Task<Stream> GetImageAsync(DbBook book, DbBookContent content, int index, CancellationToken cancellationToken = default);
    }

    public abstract class BookScraperBase : ScraperBase, IBookScraper
    {
        readonly IElasticClient _client;
        readonly ILogger<BookScraperBase> _logger;

        protected BookScraperBase(IServiceProvider services, IOptionsMonitor<ScraperOptions> options, ILogger<BookScraperBase> logger) : base(services, options, logger)
        {
            _client = services.GetService<IElasticClient>();
            _logger = logger;
        }

        /// <summary>
        /// Scrapes new books without adding them to the database.
        /// </summary>
        protected abstract IAsyncEnumerable<DbBook> ScrapeAsync(CancellationToken cancellationToken = default);

        protected override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            // it's better to fully enumerate scrape results before indexing them
            var books = await ScrapeAsync(cancellationToken).ToArrayAsync(cancellationToken);

            // index books one-by-one for effective merging
            foreach (var book in books)
                await IndexAsync(book, cancellationToken);
        }

        sealed class SimilarQuery : IQueryProcessor<DbBook>
        {
            readonly DbBook _book;

            public SimilarQuery(DbBook book)
            {
                _book = book;
            }

            public SearchDescriptor<DbBook> Process(SearchDescriptor<DbBook> descriptor)
                => descriptor.Take(1)
                             .MultiQuery(q => q.SetMode(QueryMatchMode.All)
                                               .Nested(qq => qq.SetMode(QueryMatchMode.Any)
                                                               .Text($"\"{_book.PrimaryName}\"", b => b.PrimaryName) // quotes for phrase query
                                                               .Text($"\"{_book.EnglishName}\"", b => b.EnglishName))
                                               .Nested(qq => qq.SetMode(QueryMatchMode.Any)
                                                               .Text(new TextQuery { Values = _book.TagsArtist?.ToArray(s => $"\"{s}\""), Mode = QueryMatchMode.Any }, b => b.TagsArtist)
                                                               .Text(new TextQuery { Values = _book.TagsCircle?.ToArray(s => $"\"{s}\""), Mode = QueryMatchMode.Any }, b => b.TagsCircle)))
                              //.Filter(new FilterQuery<string> { Values = _book.TagsCharacter, Mode = QueryMatchMode.Any }, b => b.TagsCharacter))
                             .MultiSort(() => (SortDirection.Descending, null));
        }

        protected async Task IndexAsync(DbBook book, CancellationToken cancellationToken = default)
        {
            book = ModelSanitizer.Sanitize(book);

            // the database is structured so that "books" are containers of "contents" which are containers of "pages"
            // we consider two books to be the same if they have:
            // - matching primary or english name
            // - matching artist or circle
            // // - at least one matching character (temporarily disabled 2020/04/29)
            IDbEntry<DbBook> entry;

            if (book.TagsArtist?.Length > 0 || book.TagsCircle?.Length > 0) // && book.TagsCharacter?.Length > 0)
            {
                var result = await _client.SearchEntriesAsync(new SimilarQuery(book), cancellationToken);

                if (result.Items.Length != 0)
                {
                    // merge with similar
                    entry = result.Items[0];

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogInformation($"Merging {Type} book '{book.PrimaryName}' into similar book {entry.Id} '{entry.Value.PrimaryName}'.");

                    do
                    {
                        if (entry.Value == null)
                        {
                            await IndexAsync(book, cancellationToken);
                            return;
                        }

                        entry.Value.MergeFrom(book);
                    }
                    while (!await entry.TryUpdateAsync(cancellationToken));

                    return;
                }
            }

            // no similar books, so create a new one
            entry = _client.Entry(book);

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogInformation($"Creating unique {Type} book {book.Id} '{book.PrimaryName}'.");

            await entry.CreateAsync(cancellationToken);
        }

        sealed class SourceQuery : IQueryProcessor<DbBook>
        {
            readonly ScraperType _type;
            readonly string _id;

            public SourceQuery(ScraperType type, string id)
            {
                _type = type;
                _id   = id;
            }

            public SearchDescriptor<DbBook> Process(SearchDescriptor<DbBook> descriptor)
                => descriptor.Take(1)
                             .MultiQuery(q => q.Filter((FilterQuery<ScraperType>) _type, b => b.Sources)
                                               .Filter((FilterQuery<string>) _id, b => b.SourceIds))
                             .MultiSort(() => (SortDirection.Descending, null));
        }

        public async IAsyncEnumerable<(IDbEntry<DbBook>, DbBookContent)> FindBookByUrlAsync(string url, bool strict, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (UrlRegex == null)
                yield break;

            foreach (var id in UrlRegex.MatchIds(url, strict))
            {
                var result = await _client.SearchEntriesAsync(new SourceQuery(Type, id), cancellationToken);

                if (result.Items.Length == 0)
                    continue;

                var entry   = result.Items[0];
                var content = entry.Value.Contents?.FirstOrDefault(c => c.Source == Type && c.SourceId == id);

                if (content == null)
                    continue;

                yield return (entry, content);
            }
        }

        public abstract Task<Stream> GetImageAsync(DbBook book, DbBookContent content, int index, CancellationToken cancellationToken = default);
    }

    public class BookScraperImageResult : ScraperImageResult
    {
        readonly DbBook _book;
        readonly DbBookContent _content;
        readonly int _index;

        /// <summary>
        /// True to generate thumbnail.
        /// </summary>
        public bool Thumbnail { get; set; }

        protected string FileNamePrefix => $"books/{_book.Id}/contents/{_content.Id}";
        protected override string ReadFileName => Thumbnail ? $"{FileNamePrefix}/thumbs/{_index}" : WriteFileName;
        protected override string WriteFileName => $"{FileNamePrefix}/pages/{_index}";

        public BookScraperImageResult(DbBook book, DbBookContent content, int index)
        {
            _book    = book;
            _content = content;
            _index   = index;
        }

        protected override Task<Stream> GetImageAsync(ActionContext context)
        {
            var scrapers = context.HttpContext.RequestServices.GetService<IScraperService>();

            if (!scrapers.GetBook(_content.Source, out var scraper))
                throw new NotSupportedException($"Scraper {scraper} is not supported.");

            return scraper.GetImageAsync(_book, _content, _index, context.HttpContext.RequestAborted);
        }

        protected override Task<byte[]> PostProcessAsync(ActionContext context, byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (Thumbnail)
                return PostProcessGenerateThumbnailAsync(context, buffer, cancellationToken);

            return base.PostProcessAsync(context, buffer, cancellationToken);
        }

        protected async Task<byte[]> PostProcessGenerateThumbnailAsync(ActionContext context, byte[] buffer, CancellationToken cancellationToken = default)
        {
            buffer = await base.PostProcessAsync(context, buffer, cancellationToken);

            var storage   = context.HttpContext.RequestServices.GetService<IStorage>();
            var processor = context.HttpContext.RequestServices.GetService<IImageProcessor>();
            var options   = context.HttpContext.RequestServices.GetService<IOptionsSnapshot<BookServiceOptions>>().Value.CoverThumbnail;

            // generate thumbnail
            buffer = processor.GenerateThumbnail(buffer, options);

            // save to storage
            using (var file = new StorageFile
            {
                Name      = ReadFileName,
                MediaType = processor.FormatToMediaType(options.Format),
                Stream    = new MemoryStream(buffer)
            })
            {
                StorageFileResult.SetHeaders(context, file);

                await storage.WriteAsync(file, cancellationToken);
            }

            return buffer;
        }
    }
}