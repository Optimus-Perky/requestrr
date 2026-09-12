using System.Collections.Generic;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public interface IMusicUserInterface
    {
        Task ShowMusicArtistSelection(MusicRequest request, IReadOnlyList<MusicArtist> music, string searchTerm);
        Task WarnNoMusicArtistFoundAsync(string musicName);

        Task DisplayMusicArtistDetailsAsync(MusicRequest request, MusicArtist music);
        Task DisplayArtistRequestDeniedAsync(MusicArtist music);
        Task DisplayArtistRequestSuccessAsync(MusicArtist music);

        Task WarnMusicArtistAlreadyAvailableAsync(MusicArtist music);

        /// <summary>
        /// Same warning, but for the main requesting flow (where a categoryId is available) rather
        /// than the notification workflow - "available" only means the artist has *some* music on
        /// disk, not that every album is present, so this still offers a way to browse/request a
        /// specific album they might be missing instead of a dead end.
        /// </summary>
        Task WarnMusicArtistAlreadyAvailableWithAlbumOptionAsync(MusicRequest request, MusicArtist music);

        Task WarnMusicArtistUnavailableAndAlreadyHasNotificationAsync(MusicArtist music);
        Task AskForNotificationArtistRequestAsync(MusicArtist music);
        Task DisplayNotificationArtistSuccessAsync(MusicArtist music);


        /// <summary>
        /// Shown instead of DisplayMusicArtistDetailsAsync when the resolved artist is
        /// "Various Artists" - requesting the whole artist there is nonsensical (it would
        /// mean every compilation album that has ever existed), so the user is forced to
        /// pick explicitly rather than defaulting either way.
        /// </summary>
        Task ShowArtistOrAlbumChoiceAsync(MusicRequest request, MusicArtist music);

        /// <summary>
        /// A Various Artists artist's discography can be huge (every compilation album they've
        /// ever tracked), so rather than browsing a capped dropdown list, prompt for the album
        /// name directly and run it through the same term search used elsewhere.
        /// </summary>
        Task DisplayAlbumSearchModalAsync(MusicRequest request);

        Task ShowMusicAlbumSelection(MusicRequest request, MusicArtist artist, IReadOnlyList<MusicAlbum> albums);

        /// <summary>
        /// Same as above but for the very first response shown (e.g. a compilation title typed
        /// directly into the main search, with no prior artist context/dropdown to preserve).
        /// </summary>
        Task ShowMusicAlbumSelection(MusicRequest request, IReadOnlyList<MusicAlbum> albums);
        Task WarnNoMusicAlbumFoundAsync(string albumName);

        Task DisplayMusicAlbumDetailsAsync(MusicRequest request, MusicAlbum album);
        Task DisplayAlbumRequestDeniedAsync(MusicAlbum album, string reason = null);
        Task DisplayAlbumRequestSuccessAsync(MusicAlbum album);

        Task WarnMusicAlbumAlreadyAvailableAsync(MusicAlbum album);
    }
}
