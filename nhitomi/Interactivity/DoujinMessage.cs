using System.Collections.Generic;
using System.Linq;
using Discord;
using nhitomi.Core;
using nhitomi.Globalization;
using nhitomi.Interactivity.Triggers;

namespace nhitomi.Interactivity
{
    public interface IDoujinMessage : IInteractiveMessage
    {
        Doujin Doujin { get; }
    }

    public class DoujinMessage : InteractiveMessage<DoujinMessage.View>, IDoujinMessage
    {
        public Doujin Doujin { get; }

        public DoujinMessage(Doujin doujin)
        {
            Doujin = doujin;
        }

        protected override IEnumerable<IReactionTrigger> CreateTriggers()
        {
            yield return new FavoriteTrigger();
            yield return new DownloadTrigger();
            yield return new DeleteTrigger();
        }

        protected override void InitializeView(View view)
        {
            base.InitializeView(view);

            view.Doujin = Doujin;
        }

        public class View : EmbedViewBase
        {
            public Doujin Doujin;

            protected override Embed CreateEmbed() => CreateEmbed(Doujin, Context.Localization);

            public static Embed CreateEmbed(Doujin doujin, Localization l)
            {
                var path = new LocalizationPath("doujinMessage");

                var embed = new EmbedBuilder()
                    .WithTitle(doujin.OriginalName ?? doujin.PrettyName)
                    .WithDescription(doujin.OriginalName == doujin.PrettyName ? null : doujin.PrettyName)
                    .WithAuthor(a => a
                        .WithName(doujin.Artist.Value ?? doujin.Source)
                        .WithIconUrl(path["sourceIcons"][doujin.Source][l]()))
                    .WithUrl(doujin.GalleryUrl)
                    .WithImageUrl($"https://s.chiya.dev/nhitomi/{doujin.Id}/1.jpeg")
                    .WithColor(Color.Green)
                    .WithFooter($"{doujin.Source}/{doujin.SourceId}");

                AddField(embed, path["language"][l](), doujin.Language?.Value);
                AddField(embed, path["group"][l](), doujin.Group?.Value);
                AddField(embed, path["parodyOf"][l](), doujin.ParodyOf?.Value);
                AddField(embed, path["categories"][l](), doujin.Categories?.Select(x => x.Tag.Value));
                AddField(embed, path["characters"][l](), doujin.Characters?.Select(x => x.Tag.Value));
                AddField(embed, path["tags"][l](), doujin.Tags?.Select(x => x.Tag.Value));
                AddField(embed, path["content"][l](), $"{doujin.PageCount} pages");

                return embed.Build();
            }

            static void AddField(EmbedBuilder builder, string name, string value, bool inline = false)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                builder.AddField(name.Trim(), value.Trim(), inline);
            }

            static void AddField(EmbedBuilder builder, string name, IEnumerable<string> values, bool inline = true)
            {
                var array = values?.ToArray();

                if (array == null || array.Length == 0)
                    return;

                AddField(builder, name, string.Join(", ", array), inline);
            }
        }

        public static bool TryParseDoujinIdFromMessage(IMessage message, out (string source, string id) id)
        {
            var identifier = message.Embeds.FirstOrDefault(e => e is Embed)?.Footer?.Text;

            if (identifier == null)
            {
                id = (null, null);
                return false;
            }

            // source/id
            var parts = identifier.Split('/', 2);

            id = (parts[0], parts[1]);
            return true;
        }
    }
}