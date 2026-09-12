using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class MusicRequestingWorkflow
    {
        private static readonly Regex MusicBrainzIdPattern = new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);

        private readonly int _categoryId;
        private readonly MusicUserRequester _user;
        private readonly IMusicSearcher _musicSearcher;
        private readonly IMusicRequester _requester;
        private readonly IMusicUserInterface _userInterface;
        private readonly IMusicNotificationWorkflow _notificationWorkflow;


        public MusicRequestingWorkflow(
            MusicUserRequester user,
            int categoryId,
            IMusicSearcher searcher,
            IMusicRequester requester,
            IMusicUserInterface userInterface,
            IMusicNotificationWorkflow notificationWorkflow
        )
        {
            _categoryId = categoryId;
            _user = user;
            _musicSearcher = searcher;
            _requester = requester;
            _userInterface = userInterface;
            _notificationWorkflow = notificationWorkflow;
        }


        public async Task SearchMusicForArtistAsync(string artistName)
        {
            string trimmedName = artistName.Trim();

            if (string.Equals(trimmedName, "various artists", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmedName, "va", StringComparison.OrdinalIgnoreCase))
            {
                // Typing this exactly means intent is unambiguous - go straight to the choice
                // screen using the well-known Various Artists id, with no Lidarr lookup at all,
                // rather than running it through the fuzzy artist search like anything else.
                MusicArtist variousArtists = new MusicArtist { ArtistId = MusicAlbum.VariousArtistsId, ArtistName = "Various Artists" };
                await _userInterface.ShowArtistOrAlbumChoiceAsync(new MusicRequest(_user, _categoryId), variousArtists);
                return;
            }

            IReadOnlyList<MusicArtist> musicList = await SearchMusicForArtistListAsync(artistName);
            string searchTerm = artistName.Replace(".", " ");

            if (musicList.Any())
            {
                if (musicList.Count > 1)
                {
                    // Lidarr's artist search is fuzzy enough to return *something* for almost
                    // any text, so a compilation title (e.g. "Now That's What I Call Music 50")
                    // typed here will often still match a handful of irrelevant artists rather
                    // than zero - always offer a way to search albums instead rather than only
                    // when the artist search comes back completely empty.
                    await _userInterface.ShowMusicArtistSelection(new MusicRequest(_user, _categoryId), musicList, searchTerm);
                }
                else if (musicList.Count == 1)
                {
                    MusicArtist music = musicList.Single();
                    await HandleMusicSelectionAsync(music);
                }
            }
            else
            {
                await HandleAlbumTermSearchAsync(searchTerm);
            }
        }


        public Task ShowAlbumSearchModalAsync()
        {
            return _userInterface.DisplayAlbumSearchModalAsync(new MusicRequest(_user, _categoryId));
        }


        /// <summary>
        /// Reached either when an artist-name search finds nothing, or when the user explicitly
        /// asks to search albums instead from the artist-selection screen. A pasted MusicBrainz
        /// release-group URL/id resolves directly and sidesteps Lidarr's free-text album search,
        /// which has real gaps for compilation series - verified empirically: some well-known
        /// compilations don't surface via title search at all but resolve perfectly by id.
        /// </summary>
        public async Task HandleAlbumTermSearchAsync(string term)
        {
            Match idMatch = MusicBrainzIdPattern.Match(term);

            if (idMatch.Success)
            {
                MusicAlbum albumById = await _musicSearcher.SearchAlbumForIdAsync(new MusicRequest(_user, _categoryId), idMatch.Value);
                await HandleMusicAlbumSelectionAsync(albumById);
                return;
            }

            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchAlbumsByTermAsync(new MusicRequest(_user, _categoryId), term);

            if (albums.Any())
                await _userInterface.ShowMusicAlbumSelection(new MusicRequest(_user, _categoryId), albums);
            else
                await _userInterface.WarnNoMusicAlbumFoundAsync(term);
        }


        public async Task<IReadOnlyList<MusicArtist>> SearchMusicForArtistListAsync(string artistName)
        {
            IReadOnlyList<MusicArtist> music = Array.Empty<MusicArtist>();

            artistName = artistName.Replace(".", " ");
            music = await _musicSearcher.SearchMusicForArtistAsync(new MusicRequest(_user, _categoryId), artistName);

            return music;
        }


        public async Task HandleMusicArtistSelectionAsync(string musicArtistId)
        {
            await HandleMusicSelectionAsync(await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), musicArtistId));
        }


        private async Task HandleMusicSelectionAsync(MusicArtist musicArtist)
        {
            if (musicArtist.IsVariousArtists)
            {
                // Requesting the whole "Various Artists" artist would mean every
                // compilation album that has ever existed, so never default into
                // DisplayMusicArtistDetailsAsync for it - make the user choose.
                await _userInterface.ShowArtistOrAlbumChoiceAsync(new MusicRequest(_user, _categoryId), musicArtist);
                return;
            }

            if (CanBeRequested(musicArtist))
            {
                await _userInterface.DisplayMusicArtistDetailsAsync(new MusicRequest(_user, _categoryId), musicArtist);
            }
            else
            {
                if (musicArtist.Available)
                {
                    await _userInterface.WarnMusicArtistAlreadyAvailableWithAlbumOptionAsync(new MusicRequest(_user, _categoryId), musicArtist);
                }
                else
                {
                    await _notificationWorkflow.NotifyForExistingRequestAsync(_user.UserId, musicArtist);
                }
            }
        }


        /// <summary>
        /// Fetches and displays the given artist's albums, so the user can request one
        /// specifically instead of the whole artist. Reached both from a normal artist's
        /// "Request Specific Album" button and from the Various Artists choice prompt.
        /// </summary>
        public async Task HandleBrowseAlbumsAsync(string artistId)
        {
            IReadOnlyList<MusicAlbum> albums = await _musicSearcher.SearchAlbumsForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            MusicArtist artist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);

            if (albums.Any())
            {
                await _userInterface.ShowMusicAlbumSelection(new MusicRequest(_user, _categoryId), artist, albums);
            }
            else
            {
                await _userInterface.WarnNoMusicAlbumFoundAsync(artist?.ArtistName ?? artistId);
            }
        }


        public async Task HandleMusicAlbumSelectionAsync(string albumId)
        {
            MusicAlbum album = await _musicSearcher.SearchAlbumForIdAsync(new MusicRequest(_user, _categoryId), albumId);
            await HandleMusicAlbumSelectionAsync(album);
        }


        private async Task HandleMusicAlbumSelectionAsync(MusicAlbum album)
        {
            if (album.Available)
            {
                await _userInterface.WarnMusicAlbumAlreadyAvailableAsync(album);
            }
            else
            {
                // Shown whether it's a fresh request or already requested-but-pending -
                // unlike artists, albums don't have a "notify me when available" flow.
                await _userInterface.DisplayMusicAlbumDetailsAsync(new MusicRequest(_user, _categoryId), album);
            }
        }


        /// <summary>
        /// Handles the request for a specific album (including Various Artists compilations)
        /// </summary>
        public async Task RequestMusicAlbumAsync(string albumId)
        {
            MusicAlbum album = await _musicSearcher.SearchAlbumForIdAsync(new MusicRequest(_user, _categoryId), albumId);
            MusicRequestResult result = await _requester.RequestMusicAlbumAsync(new MusicRequest(_user, _categoryId), album);

            if (result.WasDenied)
            {
                await _userInterface.DisplayAlbumRequestDeniedAsync(album, result.DenialReason);
            }
            else
            {
                await _userInterface.DisplayAlbumRequestSuccessAsync(album);
            }
        }



        /// <summary>
        /// Handles the request for an artist
        /// </summary>
        /// <param name="artistId"></param>
        /// <returns></returns>
        public async Task RequestMusicArtistAsync(string artistId)
        {
            MusicArtist musicArtist = await _musicSearcher.SearchMusicForArtistIdAsync(new MusicRequest(_user, _categoryId), artistId);
            MusicRequestResult result = await _requester.RequestMusicAsync(new MusicRequest(_user, _categoryId), musicArtist);

            if (result.WasDenied)
            {
                await _userInterface.DisplayArtistRequestDeniedAsync(musicArtist);
            }
            else
            {
                await _userInterface.DisplayArtistRequestSuccessAsync(musicArtist);
                await _notificationWorkflow.NotifyForNewRequestAsync(_user.UserId, musicArtist);
            }
        }



        private static bool CanBeRequested(MusicArtist music)
        {
            return !music.Available && !music.Requested;
        }
    }
}
