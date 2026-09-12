using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public interface IMusicSearcher
    {
        Task<IReadOnlyList<MusicArtist>> SearchMusicForArtistAsync(MusicRequest request, string artistName);
        Task<MusicArtist> SearchMusicForArtistIdAsync(MusicRequest request, string artistId);


        Task<Dictionary<string, MusicArtist>> SearchAvailableMusicArtistAsync(HashSet<string> artistIds, CancellationToken token);


        /// <summary>
        /// Returns every album (existing or not yet added) for a given artist, keyed by MusicBrainz artist id.
        /// </summary>
        Task<IReadOnlyList<MusicAlbum>> SearchAlbumsForArtistIdAsync(MusicRequest request, string artistId);

        Task<MusicAlbum> SearchAlbumForIdAsync(MusicRequest request, string albumId);

        /// <summary>
        /// Free-text album title search, used when an artist-name search finds nothing -
        /// e.g. someone typing a compilation title like "Now That's What I Call Music 50"
        /// instead of an artist name.
        /// </summary>
        Task<IReadOnlyList<MusicAlbum>> SearchAlbumsByTermAsync(MusicRequest request, string term);
    }
}
