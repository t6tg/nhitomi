using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChiyaFlake;
using Microsoft.Extensions.Logging;
using Nest;
using nhitomi.Models;
using nhitomi.Scrapers;

namespace nhitomi.Database.Migrations
{
    /// <summary>
    /// Initial migration that does not do anything.
    /// </summary>
    public class Migration202001010000 : MigrationBase
    {
        public Migration202001010000(IServiceProvider services, ILogger<Migration202001010000> logger) : base(services, logger) { }

        public override Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public class DbBook : IDbObject
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Date(Name = "Tc")]
            public DateTime CreatedTime { get; set; }

            [Date(Name = "Tu")]
            public DateTime UpdatedTime { get; set; }

            [Text(Name = "np")]
            public string PrimaryName { get; set; }

            [Text(Name = "ne")]
            public string EnglishName { get; set; }

            [Text(Name = "tg")]
            public string[] TagsGeneral { get; set; }

            [Text(Name = "ta")]
            public string[] TagsArtist { get; set; }

            [Text(Name = "tp")]
            public string[] TagsParody { get; set; }

            [Text(Name = "tc")]
            public string[] TagsCharacter { get; set; }

            [Text(Name = "tco")]
            public string[] TagsConvention { get; set; }

            [Text(Name = "ts")]
            public string[] TagsSeries { get; set; }

            [Text(Name = "tci")]
            public string[] TagsCircle { get; set; }

            [Text(Name = "tm")]
            public string[] TagsMetadata { get; set; }

            [Keyword(Name = "ca", DocValues = false)]
            public BookCategory Category { get; set; }

            [Keyword(Name = "ra", DocValues = false)]
            public MaterialRating Rating { get; set; }

            [Object(Name = "co", Enabled = false)]
            public DbBookContent[] Contents { get; set; }

#region Cached

            [Number(Name = "pc"), DbCached]
            public int[] PageCount { get; set; }

            [Number(Name = "nc"), DbCached]
            public int[] NoteCount { get; set; }

            [Number(Name = "tC"), DbCached]
            public int TagCount { get; set; }

            [Keyword(Name = "ln", DocValues = false), DbCached]
            public LanguageType[] Language { get; set; }

            [Keyword(Name = "sr", DocValues = false), DbCached]
            public ScraperType[] Sources { get; set; }

            [Keyword(Name = "si", DocValues = false), DbCached]
            public string[] SourceIds { get; set; }

            [Completion(Name = "sug", PreserveSeparators = false, PreservePositionIncrements = false), DbCached]
            public CompletionField Suggest { get; set; }

            public void UpdateCache(IServiceProvider services)
            {
                // auto-set content ids
                if (Contents != null)
                    foreach (var content in Contents)
                        content.Id ??= Snowflake.New;

                var tags = new Dictionary<BookTag, string[]>
                {
                    [BookTag.Tag]        = TagsGeneral,
                    [BookTag.Artist]     = TagsArtist,
                    [BookTag.Parody]     = TagsParody,
                    [BookTag.Character]  = TagsCharacter,
                    [BookTag.Convention] = TagsConvention,
                    [BookTag.Series]     = TagsSeries,
                    [BookTag.Circle]     = TagsCircle,
                    [BookTag.Metadata]   = TagsMetadata
                };

                PageCount = Contents?.ToArray(c => c.PageCount);
                NoteCount = Contents?.ToArray(c => c.Notes?.Values.Sum(x => x?.Length ?? 0) ?? 0);
                TagCount  = tags.Values.Sum(x => x?.Length ?? 0);
                Language  = Contents?.ToArray(c => c.Language);
                Sources   = Contents?.ToArray(c => c.Source);
                SourceIds = Contents?.ToArray(c => c.SourceId);

                Suggest = new CompletionField
                {
                    Input = SuggestionFormatter.Format(new Dictionary<int, string[]>
                    {
                        [-1] = new[] { PrimaryName },
                        [-2] = new[] { EnglishName }
                    }.Chain(d =>
                    {
                        foreach (var (key, value) in tags)
                            d[(int) key] = value;
                    }))
                };
            }

#endregion
        }

        public class DbBookContent
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Keyword(Name = "pg", DocValues = false)]
            public int PageCount { get; set; }

            [Keyword(Name = "la", DocValues = false)]
            public LanguageType Language { get; set; }

            [Object(Name = "no", Enabled = false)]
            public Dictionary<int, DbImageNote[]> Notes { get; set; }

            [Keyword(Name = "sr", DocValues = false)]
            public ScraperType Source { get; set; }

            [Keyword(Name = "si", DocValues = false)]
            public string SourceId { get; set; }

            [Keyword(Name = "da", Index = false)]
            public string Data { get; set; }
        }

        public class DbCollection : IDbObject
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Text(Name = "na")]
            public string Name { get; set; }

            [Text(Name = "nd")]
            public string Description { get; set; }

            [Date(Name = "Tc")]
            public DateTime CreatedTime { get; set; }

            [Date(Name = "Tu")]
            public DateTime UpdatedTime { get; set; }

            [Keyword(Name = "ow", DocValues = false)]
            public string[] OwnerIds { get; set; }

            [Keyword(Name = "x", DocValues = false)]
            public ObjectType Type { get; set; }

            [Keyword(Name = "it", Index = false)]
            public string[] Items { get; set; }

            public void UpdateCache(IServiceProvider services) { }
        }

        public class DbImageNote
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Number(Name = "x")]
            public int X { get; set; }

            [Number(Name = "y")]
            public int Y { get; set; }

            [Number(Name = "w")]
            public int Width { get; set; }

            [Number(Name = "h")]
            public int Height { get; set; }

            [Text(Name = "c")]
            public string Content { get; set; }
        }

        public class DbSnapshot : IDbObject
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Date(Name = "T")]
            public DateTime CreatedTime { get; set; }

            [Keyword(Name = "s", DocValues = false)]
            public SnapshotSource Source { get; set; }

            [Keyword(Name = "e", DocValues = false)]
            public SnapshotEvent Event { get; set; }

            [Keyword(Name = "b", DocValues = false)]
            public string RollbackId { get; set; }

            [Keyword(Name = "c", DocValues = false)]
            public string CommitterId { get; set; }

            [Keyword(Name = "x", DocValues = false)]
            public ObjectType Target { get; set; }

            [Keyword(Name = "z", DocValues = false)]
            public string TargetId { get; set; }

            [Text(Name = "r")]
            public string Reason { get; set; }

            [Keyword(Name = "d", Index = false)]
            public string Data { get; set; }

            public void UpdateCache(IServiceProvider services) { }
        }

        public class DbUser : IDbObject
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Date(Name = "Tc")]
            public DateTime CreatedTime { get; set; }

            [Date(Name = "Tu")]
            public DateTime UpdatedTime { get; set; }

            [Keyword(Name = "un")]
            public string Username { get; set; }

            [Keyword(Name = "em")]
            public string Email { get; set; }

            [Object(Name = "re", Enabled = false)]
            public DbUserRestriction[] Restrictions { get; set; }

            [Keyword(Name = "pe", DocValues = false)]
            public UserPermissions[] Permissions { get; set; }

            [Keyword(Name = "ln", DocValues = false)]
            public LanguageType Language { get; set; } = LanguageType.English;

            [Object(Name = "cd", Enabled = false)]
            public DbUserDiscordConnection DiscordConnection { get; set; }

            [Keyword(Name = "Cs", Index = false)]
            public bool AllowSharedCollections { get; set; } = true;

            [Object(Name = "Cp", Enabled = false)]
            public Dictionary<ObjectType, Dictionary<SpecialCollection, string>> SpecialCollections { get; set; }

#region Cached

            /// <summary>
            /// This is a cached property for querying.
            /// </summary>
            [Keyword(Name = "cdi", DocValues = false), DbCached]
            public ulong? DiscordId { get; set; }

            public void UpdateCache(IServiceProvider services)
            {
                DiscordId = DiscordConnection?.Id;
            }

#endregion
        }

        public class DbVote : IDbObject
        {
            [Keyword(Name = "id", Index = false)]
            public string Id { get; set; }

            [Keyword(Name = "y", DocValues = false)]
            public VoteType Type { get; set; }

            [Keyword(Name = "u", DocValues = false)]
            public string UserId { get; set; }

            [Keyword(Name = "x", DocValues = false)]
            public ObjectType Target { get; set; }

            [Keyword(Name = "e", DocValues = false)]
            public string TargetId { get; set; }

            public void UpdateCache(IServiceProvider services) { }
        }
    }
}