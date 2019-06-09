using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public class Doujin
    {
        [Key] public int Id { get; set; }

        public DoujinText Text { get; set; }

        /// <summary>
        /// The identifier by which this doujinshi should be referred.
        /// </summary>
        public Guid AccessId { get; set; }

        /// <summary>
        /// Prettified name of the doujinshi.
        /// This is usually English.
        /// </summary>
        [Required, MaxLength(256)]
        public string PrettyName { get; set; }

        /// <summary>
        /// Original name of the doujinshi.
        /// This is usually the original language of the doujinshi (i.e. Japanese).
        /// </summary>
        [Required, MaxLength(256)]
        public string OriginalName { get; set; }

        /// <summary>
        /// The time at which this doujinshi was uploaded.
        /// </summary>
        public DateTime UploadTime { get; set; }

        /// <summary>
        /// The time at which this doujinshi object was created/processed.
        /// </summary>
        public DateTime ProcessTime { get; set; }

        /// <summary>
        /// The source of this doujinshi (e.g. nhentai, hitomi, etc.).
        /// </summary>
        [Required, MaxLength(16)]
        public string Source { get; set; }

        /// <summary>
        /// The identifier used by the source (e.g. gallery ID for nhentai).
        /// </summary>
        [Required, MaxLength(16)]
        public string SourceId { get; set; }

        /// <summary>
        /// Internal data used to store <see cref="IDoujinClient"/>-specific information, such as page information.
        /// </summary>
        [MaxLength(4096)]
        public string Data { get; set; }

        /// <summary>
        /// Number of pages in this doujin.
        /// </summary>
        public int PageCount { get; set; }

        public ICollection<TagRef> Tags { get; set; }

        /// <summary>
        /// Gets the collections that contain this doujin.
        /// This is for navigation only and should not be included in queries.
        /// </summary>
        public ICollection<CollectionRef> Collections { get; set; }

        /// <summary>
        /// Gets the feed channels that sent this doujin.
        /// This is for navigation only and should not be included in queries.
        /// </summary>
        public ICollection<FeedChannel> FeedChannels { get; set; }

        public static void Describe(ModelBuilder model)
        {
            model.Entity<Doujin>(doujin =>
            {
                doujin.HasIndex(d => d.AccessId).IsUnique();

                doujin.HasIndex(d => new {d.Source, d.SourceId});
                doujin.HasIndex(d => d.UploadTime);
                doujin.HasIndex(d => d.ProcessTime);
            });

            model.Entity<DoujinText>(text =>
            {
                text.HasOne(t => t.Doujin)
                    .WithOne(d => d.Text)
                    .HasForeignKey<DoujinText>(t => t.Id);

                text.HasIndex(t => t.Value);
            });
        }
    }

    /// <summary>
    /// A denormalized version of doujinshi for fulltext searching.
    /// </summary>
    public class DoujinText
    {
        [Key] public int Id { get; set; }

        public Doujin Doujin { get; set; }

        [Required, MaxLength(4096)] public string Value { get; set; }

        public DoujinText()
        {
        }

        public DoujinText(Doujin d)
        {
            Doujin = d;

            var parts = new List<string>();

            void add(string str) => parts.AddRange(str.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));

            add(d.PrettyName);

            if (!d.PrettyName.Equals(d.OriginalName, StringComparison.OrdinalIgnoreCase))
                add(d.OriginalName);

            foreach (var t in d.Tags.Select(x => x.Tag))
                add(t.Value);

            Value = string.Join(" ", parts.Distinct());
        }
    }

    public static class DoujinExtensions
    {
        public static Tag GetTag(this Doujin doujin, TagType type) =>
            doujin.Tags.Select(x => x.Tag).FirstOrDefault(x => x.Type == type);

        public static Tag[] GetTags(this Doujin doujin, TagType type) =>
            doujin.Tags.Select(x => x.Tag).Where(t => t.Type == type).ToArray();
    }
}