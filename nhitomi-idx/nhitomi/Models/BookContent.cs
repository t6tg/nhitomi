using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;
using nhitomi.Scrapers;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents the contents of a book.
    /// </summary>
    public class BookContent : BookContentBase, IHasId
    {
        /// <summary>
        /// Content ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// Pages in this content.
        /// </summary>
        [Required, MaxLength(512)]
        public BookImage[] Pages { get; set; }

        /// <summary>
        /// Source from where this content was downloaded.
        /// </summary>
        public ScraperType Source { get; set; }
    }

    public class BookContentBase
    {
        /// <summary>
        /// Content language.
        /// </summary>
        [Required]
        public LanguageType Language { get; set; }
    }
}