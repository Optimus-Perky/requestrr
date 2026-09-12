using DSharpPlus;
using DSharpPlus.Entities;
using Requestrr.WebApi.RequestrrBot.Locale;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Requestrr.WebApi.RequestrrBot.ChatClients.Discord
{
    public class DiscordMusicUserInterface : IMusicUserInterface
    {
        private readonly DiscordInteraction _interactionContext;
        private readonly IMusicSearcher _musicSearcher;

        public DiscordMusicUserInterface(
            DiscordInteraction interactionContext,
            IMusicSearcher musicSearcher)
        {
            _interactionContext = interactionContext;
            _musicSearcher = musicSearcher;
        }


        public async Task ShowMusicArtistSelection(MusicRequest request, IReadOnlyList<MusicArtist> music, string searchTerm)
        {
            List<DiscordSelectComponentOption> options = music.Take(15).Select(x => new DiscordSelectComponentOption(GetFormattedMusicArtistName(x), $"{request.CategoryId}/{x.ArtistId}")).ToList();
            DiscordSelectComponent select = new DiscordSelectComponent($"MuRSA/{_interactionContext.User.Id}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandMusicArtistRequestHelpDropdown), options);

            // Lidarr's artist search is fuzzy enough to return something for almost any text, so
            // a compilation title typed here (e.g. "Now That's What I Call Music 50") often still
            // matches a handful of irrelevant artists rather than zero - always offer the album
            // search as an escape hatch rather than only when the artist search is fully empty.
            DiscordButtonComponent searchAlbumsButton = new DiscordButtonComponent(ButtonStyle.Secondary, $"muralterm/{_interactionContext.User.Id}/{request.CategoryId}/{LimitStringSize(searchTerm, 80)}", Language.Current.DiscordCommandMusicRequestSpecificAlbumButton);

            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddComponents(select).AddComponents(searchAlbumsButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestHelp));
        }



        public async Task DisplayMusicArtistDetailsAsync(MusicRequest request, MusicArtist musicArtist)
        {
            string message = Language.Current.DiscordCommandMusicArtistRequestConfirm;
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MuRCA/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton);
            DiscordButtonComponent browseAlbumsButton = new DiscordButtonComponent(ButtonStyle.Secondary, $"murbr/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}", Language.Current.DiscordCommandMusicRequestSpecificAlbumButton);

            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton, browseAlbumsButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        /// <summary>
        /// Shown instead of DisplayMusicArtistDetailsAsync for "Various Artists" - requesting the
        /// whole artist there would mean every compilation album that has ever existed, so the user
        /// is forced to choose explicitly rather than defaulting either way.
        /// </summary>
        public async Task ShowArtistOrAlbumChoiceAsync(MusicRequest request, MusicArtist musicArtist)
        {
            DiscordButtonComponent requestWholeArtistButton = new DiscordButtonComponent(ButtonStyle.Danger, $"MuRCA/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}", Language.Current.DiscordCommandMusicRequestWholeArtistButton);

            // Various Artists' discography can be huge, so prompt for the album name directly
            // (via a modal) instead of a normal artist's capped browse-list. The trailing
            // "/Modal" segment is required - it tells DiscordComponentInteractionCreatedHandler
            // to skip its usual auto-ack so this button's own response can open the modal instead.
            DiscordButtonComponent searchAlbumButton = new DiscordButtonComponent(ButtonStyle.Primary, $"murvasearch/{_interactionContext.User.Id}/{request.CategoryId}/Modal", Language.Current.DiscordCommandMusicRequestSpecificAlbumButton);

            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist))))
                .AddComponents(requestWholeArtistButton, searchAlbumButton)
                .WithContent(Language.Current.DiscordCommandMusicVariousArtistsChoicePrompt);

            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task DisplayAlbumSearchModalAsync(MusicRequest request)
        {
            DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();

            TextInputComponent textBox = new TextInputComponent(
                Language.Current.DiscordCommandMusicAlbumSearchModalLabel,
                $"muvam/{_interactionContext.User.Id}/{request.CategoryId}",
                Language.Current.DiscordCommandMusicAlbumSearchModalPlaceholder,
                string.Empty,
                true,
                TextInputStyle.Short,
                0,
                null
            );

            builder.AddComponents(textBox);
            builder.WithCustomId("MUVAM");
            builder.WithTitle(Language.Current.DiscordCommandMusicAlbumSearchModalTitle);

            await _interactionContext.CreateResponseAsync(InteractionResponseType.Modal, builder);
        }


        public async Task ShowMusicAlbumSelection(MusicRequest request, MusicArtist artist, IReadOnlyList<MusicAlbum> albums)
        {
            List<DiscordSelectComponentOption> options = albums.Take(15).Select(x => new DiscordSelectComponentOption(GetFormattedMusicAlbumName(x), x.AlbumId)).ToList();
            DiscordSelectComponent select = new DiscordSelectComponent($"muralsel/{_interactionContext.User.Id}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandMusicAlbumRequestHelpDropdown), options);

            var builder = (await AddPreviousDropdownsAsync(artist, new DiscordWebhookBuilder())).AddComponents(select).WithContent(Language.Current.DiscordCommandMusicAlbumRequestHelp);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task ShowMusicAlbumSelection(MusicRequest request, IReadOnlyList<MusicAlbum> albums)
        {
            List<DiscordSelectComponentOption> options = albums.Take(15).Select(x => new DiscordSelectComponentOption(GetFormattedMusicAlbumName(x), x.AlbumId)).ToList();
            DiscordSelectComponent select = new DiscordSelectComponent($"muralsel/{_interactionContext.User.Id}/{request.CategoryId}", LimitStringSize(Language.Current.DiscordCommandMusicAlbumRequestHelpDropdown), options);

            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddComponents(select).WithContent(Language.Current.DiscordCommandMusicAlbumRequestHelp));
        }


        public async Task WarnNoMusicAlbumFoundAsync(string albumName)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandMusicAlbumNotFound.ReplaceTokens(LanguageTokens.MusicAlbumTitle, albumName)));
        }


        public async Task DisplayMusicAlbumDetailsAsync(MusicRequest request, MusicAlbum album)
        {
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"muralconf/{_interactionContext.User.Id}/{request.CategoryId}/{album.AlbumId}", Language.Current.DiscordCommandRequestButton);

            var builder = (await AddPreviousDropdownsAsync(album, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(album)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicAlbumRequestConfirm);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task WarnMusicAlbumAlreadyAvailableAsync(MusicAlbum album)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"0/1/0", Language.Current.DiscordCommandAvailableButton, true);
            var builder = (await AddPreviousDropdownsAsync(album, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(album)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicAlbumAlreadyAvailable);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task DisplayAlbumRequestSuccessAsync(MusicAlbum album)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(album, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(album)))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicAlbumRequestSuccess.ReplaceTokens(album));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task DisplayAlbumRequestDeniedAsync(MusicAlbum album, string reason = null)
        {
            DiscordButtonComponent deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);
            string message = string.IsNullOrWhiteSpace(reason) ? Language.Current.DiscordCommandMusicAlbumRequestDenied : reason;
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(album, new DiscordWebhookBuilder().AddEmbed(GenerateMusicAlbumDetails(album)))).AddComponents(deniedButton).WithContent(message);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public static DiscordEmbed GenerateMusicAlbumDetails(MusicAlbum album)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(album.Title)
                .WithTimestamp(DateTime.Now)
                .WithUrl($"https://musicbrainz.org/release-group/{album.AlbumId}")
                .WithFooter("Powered by Requestrr")
                .AddField($"__Artist__", album.ArtistName, true);

            if (!string.IsNullOrWhiteSpace(album.Overview))
                embedBuilder.WithDescription(album.Overview.Substring(0, Math.Min(album.Overview.Length, 255)) + "(...)");

            if (!string.IsNullOrWhiteSpace(album.PosterPath) && album.PosterPath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                embedBuilder.WithImageUrl(album.PosterPath);

            if (album.ReleaseDate.HasValue)
                embedBuilder.AddField($"__Released__", album.ReleaseDate.Value.ToString("yyyy-MM-dd"), true);

            return embedBuilder.Build();
        }


        public async Task WarnMusicArtistAlreadyAvailableAsync(MusicArtist musicArtist)
        {
            var requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandAvailableButton, true);
            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicArtistAlreadyAvailable);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task WarnMusicArtistAlreadyAvailableWithAlbumOptionAsync(MusicRequest request, MusicArtist musicArtist)
        {
            var availableButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandAvailableButton, true);
            var browseAlbumsButton = new DiscordButtonComponent(ButtonStyle.Secondary, $"murbr/{_interactionContext.User.Id}/{request.CategoryId}/{musicArtist.ArtistId}", Language.Current.DiscordCommandMusicRequestSpecificAlbumButton);

            var builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist))))
                .AddComponents(availableButton, browseAlbumsButton)
                .WithContent(Language.Current.DiscordCommandMusicArtistAlreadyAvailable);

            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task WarnNoMusicArtistFoundAsync(string musicArtistName)
        {
            await _interactionContext.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent(Language.Current.DiscordCommandMusicArtistNotFound.ReplaceTokens(LanguageTokens.MusicArtistName, musicArtistName)));
        }



        public static DiscordEmbed GenerateMusicArtistDetails(MusicArtist musicArtist)
        {
            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(musicArtist.ArtistName)
                .WithTimestamp(DateTime.Now)
                .WithUrl($"https://musicbrainz.org/artist/{musicArtist.ArtistId}")
                .WithFooter("Powered by Requestrr");

            if (!string.IsNullOrWhiteSpace(musicArtist.Overview))
                embedBuilder.WithDescription(musicArtist.Overview.Substring(0, Math.Min(musicArtist.Overview.Length, 255)) + "(...)");

            if (!string.IsNullOrWhiteSpace(musicArtist.PosterPath) && musicArtist.PosterPath.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                embedBuilder.WithImageUrl(musicArtist.PosterPath);

            if (!string.IsNullOrWhiteSpace(musicArtist.Quality))
                embedBuilder.AddField($"__{Language.Current.DiscordEmbedMusicQuality}__", $"{musicArtist.Quality}", true);

            if (!string.IsNullOrWhiteSpace(musicArtist.PlexUrl))
                embedBuilder.AddField($"__Plex__", $"[{Language.Current.DiscordEmbedMusicListenNow}]({musicArtist.PlexUrl})", true);

            if (!string.IsNullOrWhiteSpace(musicArtist.EmbyUrl))
                embedBuilder.AddField($"__Emby__", $"[{Language.Current.DiscordEmbedMusicListenNow}]({musicArtist.EmbyUrl})", true);

            return embedBuilder.Build();
        }


        public async Task DisplayArtistRequestSuccessAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandRequestButtonSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestSuccess.ReplaceTokens(musicArtist));            
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task DisplayArtistRequestDeniedAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent deniedButton = new DiscordButtonComponent(ButtonStyle.Danger, $"0/1/0", Language.Current.DiscordCommandRequestButtonDenied);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(deniedButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestDenied);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task WarnMusicArtistUnavailableAndAlreadyHasNotificationAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent requestButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MMU/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton, true);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(requestButton).WithContent(Language.Current.DiscordCommandMusicArtistRequestAlreadyExistNotified);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }



        public async Task AskForNotificationArtistRequestAsync(MusicArtist musicArtist)
        {
            var notificationButton = new DiscordButtonComponent(ButtonStyle.Primary, $"MuNR/{_interactionContext.User.Id}/{musicArtist.ArtistId}", Language.Current.DiscordCommandRequestButton, false, new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🔔")));
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(notificationButton).WithContent(Language.Current.DiscordCommandMusicArtistNotificationRequest);
            await _interactionContext.EditOriginalResponseAsync(builder);
        }


        public async Task DisplayNotificationArtistSuccessAsync(MusicArtist musicArtist)
        {
            DiscordButtonComponent successButton = new DiscordButtonComponent(ButtonStyle.Success, $"0/1/0", Language.Current.DiscordCommandNotifyMeSuccess);
            DiscordWebhookBuilder builder = (await AddPreviousDropdownsAsync(musicArtist, new DiscordWebhookBuilder().AddEmbed(GenerateMusicArtistDetails(musicArtist)))).AddComponents(successButton).WithContent(Language.Current.DiscordCommandMusicArtistNotificationSuccess.ReplaceTokens(musicArtist));
            await _interactionContext.EditOriginalResponseAsync(builder);
        }





        private string GetFormattedMusicArtistName(MusicArtist music)
        {
            return LimitStringSize(music.ArtistName);
        }
        private string GetFormattedMusicAlbumName(MusicAlbum album)
        {
            return LimitStringSize(album.ReleaseDate.HasValue ? $"{album.Title} ({album.ReleaseDate.Value.Year})" : album.Title);
        }
        private string LimitStringSize(string value, int limit = 100)
        {
            return value.Count() > limit ? value[..(limit - 3)] + "..." : value;
        }


        private Task<DiscordWebhookBuilder> AddPreviousDropdownsAsync(MusicArtist music, DiscordWebhookBuilder builder)
        {
            return AddPreviousDropdownsAsync(GetFormattedMusicArtistName(music), builder);
        }


        private Task<DiscordWebhookBuilder> AddPreviousDropdownsAsync(MusicAlbum album, DiscordWebhookBuilder builder)
        {
            return AddPreviousDropdownsAsync(GetFormattedMusicAlbumName(album), builder);
        }


        /// <summary>
        /// Re-adds every select menu already on the message so the interaction trail stays visible
        /// (e.g. the artist dropdown stays shown once the user has moved on to picking an album from
        /// it) - only the most recently interacted-with one is relabeled to show the new selection,
        /// any older ones keep their existing placeholder text untouched.
        /// </summary>
        private async Task<DiscordWebhookBuilder> AddPreviousDropdownsAsync(string mostRecentSelectionLabel, DiscordWebhookBuilder builder)
        {
            List<DiscordSelectComponent> previousSelectors = (await _interactionContext.GetOriginalResponseAsync()).FilterComponents<DiscordSelectComponent>().ToList();

            for (int i = 0; i < previousSelectors.Count; i++)
            {
                DiscordSelectComponent previous = previousSelectors[i];
                bool isMostRecent = i == previousSelectors.Count - 1;
                DiscordSelectComponent selector = new DiscordSelectComponent(previous.CustomId, isMostRecent ? LimitStringSize(mostRecentSelectionLabel) : previous.Placeholder, previous.Options);
                builder.AddComponents(selector);
            }

            return builder;
        }
    }
}
