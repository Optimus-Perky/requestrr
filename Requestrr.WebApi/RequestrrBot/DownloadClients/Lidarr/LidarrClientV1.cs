using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Requestrr.WebApi.Extensions;
using Requestrr.WebApi.RequestrrBot.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr.LidarrClient;

namespace Requestrr.WebApi.RequestrrBot.DownloadClients.Lidarr
{
    public class LidarrClientV1 : IMusicSearcher, IMusicRequester
    {
        private IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LidarrClient> _logger;
        private LidarrSettingsProvider _lidarrSettingProvider;
        private LidarrSettings _lidarrSettings => _lidarrSettingProvider.Provider();

        private string BaseURL => GetBaseURL(_lidarrSettings);


        public LidarrClientV1(IHttpClientFactory httpClientFactory, ILogger<LidarrClient> logger, LidarrSettingsProvider lidarrSettingsProvider)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _lidarrSettingProvider = lidarrSettingsProvider;
        }



        /// <summary>
        /// Used to test if Lidarr service can be found
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task TestConnectionAsync(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.BaseUrl) && !settings.BaseUrl.StartsWith("/"))
            {
                throw new Exception("Invalid base URL, must start with /");
            }

            var testSuccessful = false;

            try
            {
                var response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/config/host");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new Exception("Invalid api key");
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception("Incorrect api version");
                }

                try
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JObject.Parse(responseString);

                    if (!jsonResponse.urlBase.ToString().Equals(settings.BaseUrl, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new Exception("Base url does not match what is set in Lidarr");
                    }
                }
                catch
                {
                    throw new Exception("Base url does not match what is set in Lidarr");
                }

                testSuccessful = true;
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Error while testing Lidarr connection: " + ex.Message);
                throw new Exception("Invalid host and/or port");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error while testing Lidarr connection: " + ex.Message);

                if (ex.GetType() == typeof(Exception))
                {
                    throw;
                }
                else
                {
                    throw new Exception("Invalid host and/or port");
                }
            }

            if (!testSuccessful)
            {
                throw new Exception("Invalid host and/or port");
            }
        }


        public static async Task<IList<JSONRootPath>> GetRootPaths(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/rootfolder");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<JSONRootPath>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting Lidarr root paths: " + ex.Message);
            }

            throw new Exception("An error occurred while getting Lidarr root paths");
        }


        /// <summary>
        /// Fetches profile information from Lidarr
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<IList<JSONProfile>> GetProfiles(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/qualityprofile");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<JSONProfile>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting Lidarr profiles: " + ex.Message);
            }

            throw new Exception("An error occurred while getting Lidarr profiles");
        }



        /// <summary>
        /// Fetches metadata profile information from Lidarr
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="logger"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<IList<JSONProfile>> GetMetadataProfiles(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/metadataprofile");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<JSONProfile>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting Lidarr metadata profiles: " + ex.Message);
            }

            throw new Exception("An error occurred while getting Lidarr metadata profiles");
        }



        public static async Task<IList<JSONTag>> GetTags(HttpClient httpClient, ILogger<LidarrClient> logger, LidarrSettings settings)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync(httpClient, settings, $"{GetBaseURL(settings)}/tag");
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<IList<JSONTag>>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error while getting Lidarr tags: " + ex.Message);
            }

            throw new Exception("An error occurred while getting Lidarr tags");
        }


        /// <summary>
        /// Handle 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private Task<HttpResponseMessage> HttpGetAsync(string url)
        {
            return HttpGetAsync(_httpClientFactory.CreateClient(), _lidarrSettings, url);
        }


        /// <summary>
        /// Makes a connection to Lidarr and returns a response from API
        /// </summary>
        /// <param name="client"></param>
        /// <param name="settings"></param>
        /// <param name="url">Full URL to the API</param>
        /// <returns>Returns the HttpReponseMessage from the API</returns>
        private static async Task<HttpResponseMessage> HttpGetAsync(HttpClient client, LidarrSettings settings, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Api-Key", settings.ApiKey);

            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                return await client.SendAsync(request, cts.Token);
            }
        }


        /// <summary>
        /// Gets Base URL for Lidarr server
        /// </summary>
        /// <param name="settings">Lidarr Settings</param>
        /// <returns>Returns a string of the URL</returns>
        private static string GetBaseURL(LidarrSettings settings)
        {
            var protocol = settings.UseSSL ? "https" : "http";

            return $"{protocol}://{settings.Hostname}:{settings.Port}{settings.BaseUrl}/api/v{settings.Version}";
        }



        /// <summary>
        /// Handles the fetching of a single query based on Music DB Id
        /// </summary>
        /// <param name="artistId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<MusicArtist> SearchMusicForArtistIdAsync(MusicRequest request, string artistId)
        {
            try
            {
                JSONMusicArtist foundArtistJson = await FindExistingArtistByMusicDbIdAsync(artistId);

                if (foundArtistJson == null)
                {
                    HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/lookup?term=lidarr:{artistId}");
                    await response.ThrowIfNotSuccessfulAsync("LidarrMusicLookup failed", x => x.error);

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    foundArtistJson = JsonConvert.DeserializeObject<List<JSONMusicArtist>>(jsonResponse).First();
                }

                return foundArtistJson != null ? ConvertToMusic(foundArtistJson) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching for music by Id \"{artistId}\" with Lidarr: {ex.Message}");
            }

            throw new Exception("An error occurred while searching for music by Id with Lidarr");
        }



        /// <summary>
        /// Handles the fetching of a 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<IReadOnlyList<MusicArtist>> SearchMusicForArtistAsync(MusicRequest request, string artistName)
        {
            try
            {
                string searchTerm = Uri.EscapeDataString(artistName.ToLower().Trim());
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/lookup?term={searchTerm}");
                await response.ThrowIfNotSuccessfulAsync("LidarrMusicArtistLookup failed", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<JSONMusicArtist> jsonMusic = JsonConvert.DeserializeObject<List<JSONMusicArtist>>(jsonResponse);

                //TODO: Correct this, searching should handle both artist and albums
                return jsonMusic.Where(x => x != null).Select(x => ConvertToMusic(x)).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for music artist with Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while searching for music artist with Lidarr");
        }



        private async Task<JSONMusicArtist> FindExistingArtistByMusicDbIdAsync(string artistId)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist?mbId={artistId}");
                await response.ThrowIfNotSuccessfulAsync("Could not search artist by Id", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JSONMusicArtist[] jsonMusicArtists = JsonConvert.DeserializeObject<List<JSONMusicArtist>>(jsonResponse).ToArray();

                if (jsonMusicArtists.Any())
                    return jsonMusicArtists.First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred finding existing music artist by Id \"{artistId}\" with Lidarr: {ex.Message}");
            }

            return null;
        }



        public async Task<Dictionary<string, MusicArtist>> SearchAvailableMusicArtistAsync(HashSet<string> artistIds, CancellationToken token)
        {
            try
            {
                List<MusicArtist> convertedMusicArtists = new List<MusicArtist>();

                foreach (string artistId in artistIds)
                {
                    JSONMusicArtist existingMusic = await FindExistingArtistByMusicDbIdAsync(artistId);
                    if (existingMusic != null)
                        convertedMusicArtists.Add(ConvertToMusic(existingMusic));
                }

                return convertedMusicArtists.Where(x => x.Available).ToDictionary(x => x.ArtistId, x => x);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching available music artist with Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while searching available music artist with Lidarr");
        }



        /// <summary>
        /// Returns every album for a given artist, by MusicBrainz artist id. Lidarr's API has no way
        /// to preview an unadded artist's discography at all (the metadata source's GetArtistInfo call
        /// that returns it is only ever used internally when actually adding an artist, never exposed
        /// over HTTP) - "lidarr:{id}" on /album/lookup only resolves a single album's own id, not an
        /// artist's, verified empirically. So: if the artist is already in the library, Lidarr has
        /// already indexed their real discography and /album?artistId= is authoritative; otherwise the
        /// best available option is a free-text title search filtered down to that artist's name.
        /// </summary>
        public async Task<IReadOnlyList<MusicAlbum>> SearchAlbumsForArtistIdAsync(MusicRequest request, string artistId)
        {
            try
            {
                JSONMusicArtist existingArtist = await FindExistingArtistByMusicDbIdAsync(artistId);

                if (existingArtist != null)
                {
                    HttpResponseMessage libraryResponse = await HttpGetAsync($"{BaseURL}/album?artistId={existingArtist.Id}");
                    await libraryResponse.ThrowIfNotSuccessfulAsync("LidarrAlbumsByArtist failed", x => x.error);

                    string libraryJson = await libraryResponse.Content.ReadAsStringAsync();
                    List<JSONMusicAlbum> libraryAlbums = JsonConvert.DeserializeObject<List<JSONMusicAlbum>>(libraryJson);

                    return libraryAlbums.Where(x => x != null).Select(x => ConvertToMusicAlbum(x)).ToArray();
                }

                MusicArtist lookedUpArtist = await SearchMusicForArtistIdAsync(request, artistId);
                string searchTerm = Uri.EscapeDataString(lookedUpArtist.ArtistName.ToLower().Trim());
                HttpResponseMessage searchResponse = await HttpGetAsync($"{BaseURL}/album/lookup?term={searchTerm}");
                await searchResponse.ThrowIfNotSuccessfulAsync("LidarrAlbumLookup failed", x => x.error);

                string searchJson = await searchResponse.Content.ReadAsStringAsync();
                List<JSONMusicAlbum> searchAlbums = JsonConvert.DeserializeObject<List<JSONMusicAlbum>>(searchJson);

                return searchAlbums
                    .Where(x => x != null && x.Artist != null && string.Equals(x.Artist.ArtistName, lookedUpArtist.ArtistName, StringComparison.OrdinalIgnoreCase))
                    .Select(x => ConvertToMusicAlbum(x))
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching for albums for artist \"{artistId}\" with Lidarr: {ex.Message}");
            }

            throw new Exception("An error occurred while searching for albums for artist with Lidarr");
        }


        /// <summary>
        /// Free-text album title search - used when an artist-name search finds nothing, e.g. someone
        /// typing a compilation title like "Now That's What I Call Music 50" instead of an artist name.
        /// </summary>
        public async Task<IReadOnlyList<MusicAlbum>> SearchAlbumsByTermAsync(MusicRequest request, string term)
        {
            try
            {
                string searchTerm = Uri.EscapeDataString(term.ToLower().Trim());
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album/lookup?term={searchTerm}");
                await response.ThrowIfNotSuccessfulAsync("LidarrAlbumTitleLookup failed", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                List<JSONMusicAlbum> jsonAlbums = JsonConvert.DeserializeObject<List<JSONMusicAlbum>>(jsonResponse);

                // MusicBrainz frequently has several near-duplicate releases of the same
                // title/artist, and the search backend doesn't rank by relevance to the typed
                // term - so de-dupe and put closer title matches first rather than showing
                // the same unrelated album three times ahead of the one actually being searched for.
                return jsonAlbums
                    .Where(x => x != null)
                    .GroupBy(x => (x.Title, ArtistName: x.Artist?.ArtistName))
                    .Select(g => g.First())
                    .OrderByDescending(x => TitleMatchScore(x.Title, term))
                    .Select(x => ConvertToMusicAlbum(x))
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching for album \"{term}\" with Lidarr: {ex.Message}");
            }

            throw new Exception("An error occurred while searching for album with Lidarr");
        }


        private static int TitleMatchScore(string title, string term)
        {
            if (string.IsNullOrWhiteSpace(title))
                return 0;

            string normalizedTitle = title.Trim();
            string normalizedTerm = term.Trim();

            if (string.Equals(normalizedTitle, normalizedTerm, StringComparison.OrdinalIgnoreCase))
                return 3;

            if (normalizedTitle.StartsWith(normalizedTerm, StringComparison.OrdinalIgnoreCase))
                return 2;

            if (normalizedTitle.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase))
                return 1;

            return 0;
        }


        public async Task<MusicAlbum> SearchAlbumForIdAsync(MusicRequest request, string albumId)
        {
            try
            {
                JSONMusicAlbum foundAlbumJson = await FindExistingAlbumByMusicDbIdAsync(albumId);

                if (foundAlbumJson == null)
                {
                    HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album/lookup?term=lidarr:{albumId}");
                    await response.ThrowIfNotSuccessfulAsync("LidarrAlbumLookup failed", x => x.error);

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    foundAlbumJson = JsonConvert.DeserializeObject<List<JSONMusicAlbum>>(jsonResponse).First();
                }

                return foundAlbumJson != null ? ConvertToMusicAlbum(foundAlbumJson) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while searching for album by Id \"{albumId}\" with Lidarr: {ex.Message}");
            }

            throw new Exception("An error occurred while searching for album by Id with Lidarr");
        }


        private async Task<JSONMusicAlbum> FindExistingAlbumByMusicDbIdAsync(string albumId)
        {
            try
            {
                HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/album?foreignAlbumId={albumId}");
                await response.ThrowIfNotSuccessfulAsync("Could not search album by Id", x => x.error);

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JSONMusicAlbum[] jsonAlbums = JsonConvert.DeserializeObject<List<JSONMusicAlbum>>(jsonResponse).ToArray();

                if (jsonAlbums.Any())
                    return jsonAlbums.First();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred finding existing album by Id \"{albumId}\" with Lidarr: {ex.Message}");
            }

            return null;
        }




        public async Task<MusicRequestResult> RequestMusicAsync(MusicRequest request, MusicArtist music)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(music.DownloadClientId))
                    await CreateMusicInLidarr(request, music);
                else
                    await UpdateExistingMusic(request, music);

                return new MusicRequestResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error while requesting music \"{music.ArtistName}\" from Lidarr: " + ex.Message);
            }

            throw new Exception("An error occurred while requesting a music from Lidarr");
        }



        private async Task CreateMusicInLidarr(MusicRequest request, MusicArtist music)
        {
            LidarrCategory category = null;

            try
            {
                category = _lidarrSettings.Categories.SingleOrDefault(x => x.Id == request.CategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occured while requesting music \"{music.ArtistName}\" from Lidarr, could not find category with id {request.CategoryId}");
                throw new Exception($"An error occurred while requesting music \"{music.ArtistName}\" from Lidarr, could not find category with id {request.CategoryId}");
            }

            MusicArtist jsonMusic = await SearchMusicForArtistIdAsync(request, music.ArtistId);
            HttpResponseMessage response = await HttpPostAsync($"{BaseURL}/artist", JsonConvert.SerializeObject(new
            {
                foreignArtistId = jsonMusic.ArtistId,
                artistName = jsonMusic.ArtistName,
                mbId = jsonMusic.ArtistId,
                qualityProfileId = category.ProfileId,
                metadataProfileId = category.MetadataProfileId,
                monitored = _lidarrSettings.MonitorNewRequests,
                tags = JToken.FromObject(category.Tags),
                rootFolderPath = category.RootFolder,
                addOptions = new
                {
                    // Lidarr's AddArtistService forces the top-level `monitored` flag above back
                    // to false whenever addOptions.monitor resolves to "none" - which is exactly
                    // what happens when this is left unset entirely, silently discarding the
                    // MonitorNewRequests setting on every fresh artist request. Has to be set
                    // explicitly to actually take effect (verified against Lidarr's own source).
                    monitor = _lidarrSettings.MonitorNewRequests ? "all" : "none",
                    searchForMissingAlbums = _lidarrSettings.SearchNewRequests
                }
            }));

            await response.ThrowIfNotSuccessfulAsync("LidarrMusicCreation failed", x => x.error);
        }


        private async Task UpdateExistingMusic(MusicRequest request, MusicArtist music)
        {
            LidarrCategory category = null;
            int lidarrMusicId = int.Parse(music.DownloadClientId);
            HttpResponseMessage response = await HttpGetAsync($"{BaseURL}/artist/{lidarrMusicId}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    await CreateMusicInLidarr(request, music);
                    return;
                }

                await response.ThrowIfNotSuccessfulAsync("LidarrGetMusic failed", x => x.error);
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic lidarrMusic = JObject.Parse(jsonResponse);

            try
            {
                category = _lidarrSettings.Categories.Single(x => x.Id == request.CategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while requesting music \"{music.ArtistName}\" from Lidarr, cound not find category with id {request.CategoryId}");
                throw new Exception($"An error occurred while requesting music \"{music.ArtistName}\" from Lidarr, could not find category with id {request.CategoryId}");
            }

            lidarrMusic.tags = JToken.FromObject(category.Tags);
            lidarrMusic.monitored = _lidarrSettings.MonitorNewRequests;

            response = await HttpPutAsync($"{BaseURL}/artist/{lidarrMusicId}", JsonConvert.SerializeObject(lidarrMusic));
            await response.ThrowIfNotSuccessfulAsync("LidarrUpdateMusic failed", x => x.error);

            if (_lidarrSettings.SearchNewRequests)
            {
                try
                {
                    response = await HttpPostAsync($"{BaseURL}/command", JsonConvert.SerializeObject(new
                    {
                        name = "musicSearch",
                        musicIds = new[] { lidarrMusicId }
                    }));

                    await response.ThrowIfNotSuccessfulAsync("LidarrMusicSearchCommand failed", x => x.error);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error while sending search command for music \"{music.ArtistName}\" to Lidarr: " + ex.Message);
                    throw;
                }
            }
        }



        /// <summary>
        /// Handles the request for a specific album, including Various Artists compilations.
        /// Unlike an artist request, this never monitors/searches anything beyond the one
        /// album - if the artist isn't in the library yet, Lidarr's own POST /album creates
        /// it alongside the album in a single call, added unmonitored so nothing else of
        /// theirs gets pulled in as a side effect.
        /// </summary>
        public async Task<MusicRequestResult> RequestMusicAlbumAsync(MusicRequest request, MusicAlbum album)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(album.DownloadClientId))
                    await CreateAlbumInLidarr(request, album);
                else
                    await MonitorExistingAlbum(request, album);

                return new MusicRequestResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error while requesting album \"{album.Title}\" from Lidarr: " + ex.Message);

                // Confirmed Lidarr-side bug (reproduced directly against its API, not specific
                // to this client): adding an album with no cover art images fails a NOT NULL
                // constraint on Albums.Images server-side. Surface this specifically rather
                // than the generic denied/error message, since there's nothing wrong with the
                // request itself and no client-side fix is possible for a server DB constraint.
                if (ex.Message.Contains("NOT NULL constraint failed: Albums.Images", StringComparison.OrdinalIgnoreCase))
                {
                    return new MusicRequestResult
                    {
                        WasDenied = true,
                        DenialReason = "This album has no cover art in Lidarr's metadata source, which triggers a known Lidarr bug when adding it. This isn't something Requestrr can work around - it would need fixing in Lidarr itself."
                    };
                }
            }

            throw new Exception("An error occurred while requesting an album from Lidarr");
        }


        private async Task CreateAlbumInLidarr(MusicRequest request, MusicAlbum album)
        {
            LidarrCategory category = GetCategoryOrThrow(request.CategoryId, album.Title);
            int metadataProfileId = await GetMetadataProfileIdForAlbumAsync(album, category);

            HttpResponseMessage response = await HttpPostAsync($"{BaseURL}/album", JsonConvert.SerializeObject(new
            {
                foreignAlbumId = album.AlbumId,
                monitored = true,
                profileId = category.ProfileId,
                addOptions = new
                {
                    searchForNewAlbum = _lidarrSettings.SearchNewRequests
                },
                artist = new
                {
                    foreignArtistId = album.ArtistId,
                    qualityProfileId = category.ProfileId,
                    metadataProfileId,
                    rootFolderPath = category.RootFolder,
                    monitored = false,
                    tags = JToken.FromObject(category.Tags),
                    addOptions = new
                    {
                        // Explicit rather than relying on the unset default: Lidarr's
                        // AddArtistService forces `monitored` above to false whenever this
                        // resolves to "none" anyway, which happens to be what's wanted here
                        // too (only the one requested album should be monitored, not the
                        // artist's whole catalog) - see CreateMusicInLidarr for the case
                        // where that same default silently discarded an actual true value.
                        monitor = "none",
                        searchForMissingAlbums = false
                    }
                }
            }));

            await response.ThrowIfNotSuccessfulAsync("LidarrAlbumCreation failed", x => x.error);
        }


        private async Task MonitorExistingAlbum(MusicRequest request, MusicAlbum album)
        {
            int lidarrAlbumId = int.Parse(album.DownloadClientId);

            HttpResponseMessage response = await HttpPutAsync($"{BaseURL}/album/monitor", JsonConvert.SerializeObject(new
            {
                albumIds = new[] { lidarrAlbumId },
                monitored = true
            }));

            await response.ThrowIfNotSuccessfulAsync("LidarrAlbumMonitor failed", x => x.error);

            if (_lidarrSettings.SearchNewRequests)
            {
                try
                {
                    response = await HttpPostAsync($"{BaseURL}/command", JsonConvert.SerializeObject(new
                    {
                        name = "AlbumSearch",
                        albumIds = new[] { lidarrAlbumId }
                    }));

                    await response.ThrowIfNotSuccessfulAsync("LidarrAlbumSearchCommand failed", x => x.error);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error while sending search command for album \"{album.Title}\" to Lidarr: " + ex.Message);
                    throw;
                }
            }
        }


        private LidarrCategory GetCategoryOrThrow(int categoryId, string musicName)
        {
            try
            {
                return _lidarrSettings.Categories.Single(x => x.Id == categoryId);
            }
            catch (Exception)
            {
                _logger.LogError($"An error occurred while requesting music \"{musicName}\" from Lidarr, could not find category with id {categoryId}");
                throw new Exception($"An error occurred while requesting music \"{musicName}\" from Lidarr, could not find category with id {categoryId}");
            }
        }


        /// <summary>
        /// Various Artists compilations behave very differently from a normal artist's discography
        /// (thousands of unrelated releases), so if the Lidarr instance has a metadata profile that
        /// looks like it's meant for that (named "VA"/"Various Artists"), use it instead of the
        /// category's default - falls back to the category's profile if no such profile exists.
        /// </summary>
        private async Task<int> GetMetadataProfileIdForAlbumAsync(MusicAlbum album, LidarrCategory category)
        {
            if (!album.IsVariousArtists)
                return category.MetadataProfileId;

            try
            {
                IList<JSONProfile> profiles = await GetMetadataProfiles(_httpClientFactory.CreateClient(), _logger, _lidarrSettings);
                JSONProfile vaProfile = profiles?.FirstOrDefault(x => x.name.IndexOf("various", StringComparison.OrdinalIgnoreCase) >= 0
                                                                    || x.name.Equals("VA", StringComparison.OrdinalIgnoreCase));

                if (vaProfile != null)
                    return vaProfile.id;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch Lidarr metadata profiles to look for a Various Artists profile, falling back to the category default: " + ex.Message);
            }

            return category.MetadataProfileId;
        }


        private async Task<HttpResponseMessage> HttpPostAsync(string url, string content)
        {
            StringContent postRequest = new StringContent(content);
            postRequest.Headers.Clear();
            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("X-Api-Key", _lidarrSettings.ApiKey);

            HttpClient client = _httpClientFactory.CreateClient();
            return await client.PostAsync(url, postRequest);
        }


        private async Task<HttpResponseMessage> HttpPutAsync(string url, string content)
        {
            StringContent postRequest = new StringContent(content);
            postRequest.Headers.Clear();
            postRequest.Headers.Add("Content-Type", "application/json");
            postRequest.Headers.Add("X-Api-Key", _lidarrSettings.ApiKey);

            HttpClient client = _httpClientFactory.CreateClient();
            return await client.PutAsync(url, postRequest);
        }



        private MusicArtist ConvertToMusic(JSONMusicArtist jsonArtist)
        {
            string downloadClientId = jsonArtist.Id.ToString();

            return new MusicArtist
            {
                DownloadClientId = downloadClientId,
                ArtistId = jsonArtist.ForeignArtistId.ToString(),
                ArtistName = jsonArtist.ArtistName,
                Overview = jsonArtist.Overview,

                Available = (jsonArtist.Statistics?.SizeOnDisk ?? -1) > 0,
                Monitored = jsonArtist.Monitored,
                Quality = string.Empty,
                Requested = !string.IsNullOrWhiteSpace(downloadClientId),

                PlexUrl = string.Empty,
                EmbyUrl = string.Empty,
                PosterPath = GetPosterImageUrl(jsonArtist.Images)
            };
        }


        private MusicAlbum ConvertToMusicAlbum(JSONMusicAlbum jsonAlbum)
        {
            string downloadClientId = jsonAlbum.Id?.ToString() ?? string.Empty;

            return new MusicAlbum
            {
                DownloadClientId = downloadClientId,
                AlbumId = jsonAlbum.ForeignAlbumId.ToString(),
                Title = jsonAlbum.Title,
                Disambiguation = jsonAlbum.Disambiguation,
                Overview = jsonAlbum.Overview,
                ArtistId = jsonAlbum.Artist?.ForeignArtistId.ToString() ?? string.Empty,
                ArtistName = jsonAlbum.Artist?.ArtistName ?? string.Empty,
                ReleaseDate = jsonAlbum.ReleaseDate,
                AlbumType = jsonAlbum.AlbumType,

                Available = (jsonAlbum.Statistics?.SizeOnDisk ?? -1) > 0,
                Monitored = jsonAlbum.Monitored,
                Quality = string.Empty,
                Requested = !string.IsNullOrWhiteSpace(downloadClientId),

                PosterPath = GetPosterImageUrl(jsonAlbum.Images ?? new List<JSONImage>())
            };
        }


        private string GetPosterImageUrl(List<JSONImage> images)
        {
            JSONImage posterImage = images.Where(x => x.CoverType.Equals("poster", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (posterImage != null)
            {
                if (!string.IsNullOrWhiteSpace(posterImage.RemoteUrl))
                    return posterImage.RemoteUrl;

                return posterImage.Url;
            }
            return string.Empty;
        }



        public class JSONLink
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class JSONImage
        {
            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("coverType")]
            public string CoverType { get; set; }

            [JsonProperty("extension")]
            public string Extension { get; set; }

            [JsonProperty("remoteUrl")]
            public string RemoteUrl { get; set; }
        }

        public class JSONRating
        {
            [JsonProperty("votes")]
            public int Votes { get; set; }

            [JsonProperty("value")]
            public float Value { get; set; }
        }

        public class JSONStatistics
        {
            [JsonProperty("albumCount")]
            public int AlbumCount { get; set; }

            [JsonProperty("trackFileCount")]
            public int TrackFileCount { get; set; }

            [JsonProperty("trackCount")]
            public int TrackCount { get; set; }

            [JsonProperty("totalTrackCount")]
            public int TotalTrackCount { get; set; }

            [JsonProperty("sizeOnDisk")]
            public double SizeOnDisk { get; set; }

            [JsonProperty("percentOfTracks")]
            public double PercentOfTracks { get; set; }
        }

        public class JSONMedia
        {
            [JsonProperty("mediumNumber")]
            public int MediumNumber { get; set; }

            [JsonProperty("mediumName")]
            public string mediumName { get; set; }

            [JsonProperty("mediumFormat")]
            public string MediumFormat { get; set; }
        }

        public class JSONReleases
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("albumId")]
            public int AlbumId { get; set; }

            [JsonProperty("foreignReleaseId")]
            public string ForeignReleaseId { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("duration")]
            public int Duration { get; set; }

            [JsonProperty("trackCount")]
            public int TrackCount { get; set;  }

            [JsonProperty("media")]
            public List<JSONMedia> Media { get; set; }

            [JsonProperty("mediumCount")]
            public int MediumCount { get; set; }

            [JsonProperty("disambiguation")]
            public string Disambiguation { get; set; }

            [JsonProperty("country")]
            public List<string> Country { get; set; }

            [JsonProperty("label")]
            public List<string> Label { get; set; }

            [JsonProperty("format")]
            public string Format { get; set; }

            [JsonProperty("monitored")]
            public bool Monitored { get; set;  }

        }


        private class JSONMusicArtist
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("artistMetadataId")]
            public int? ArtistMetadataId { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("ended")]
            public bool Ended { get; set; }

            [JsonProperty("artistName")]
            public string ArtistName { get; set; }

            [JsonProperty("foreignArtistId")]
            public Guid ForeignArtistId { get; set; }

            [JsonProperty("tadbId")]
            public int TadbId { get; set; }

            [JsonProperty("discogsId")]
            public int DiscogsId { get; set; }

            [JsonProperty("overview")]
            public string Overview { get; set; }

            [JsonProperty("artistType")]
            public string ArtistType { get; set; }

            [JsonProperty("disambiguation")]
            public string Disambiguation { get; set; }

            [JsonProperty("links")]
            public List<JSONLink> Links { get; set; }

            [JsonProperty("images")]
            public List<JSONImage> Images { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; } = null;

            [JsonProperty("qualityProfileId")]
            public int QualityProfileId { get; set; }

            [JsonProperty("metadataProfileId")]
            public int MetadataProfileId { get; set; }

            [JsonProperty("monitored")]
            public bool Monitored { get; set; }

            [JsonProperty("monitorNewItems")]
            public string MonitorNewItems { get; set; }

            [JsonProperty("folder")]
            public string Folder { get; set; }

            [JsonProperty("genres")]
            public List<string> Genres { get; set; }

            [JsonProperty("tags")]
            public List<int> Tags { get; set; }

            [JsonProperty("added")]
            public DateTime Added { get; set; }

            [JsonProperty("ratings")]
            public JSONRating Ratings { get; set; }

            [JsonProperty("statistics")]
            public JSONStatistics Statistics { get; set; }
        }


        private class JSONMusicAlbum
        {
            [JsonProperty("id")]
            public int? Id { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("disambiguation")]
            public string Disambiguation { get; set; }

            [JsonProperty("overview")]
            public string Overview { get; set; }

            [JsonProperty("artistId")]
            public int ArtistId { get; set; }

            [JsonProperty("foreignAlbumId")]
            public Guid ForeignAlbumId { get; set; }

            [JsonProperty("monitored")]
            public bool Monitored { get; set; }

            [JsonProperty("anyReleaseOk")]
            public bool AnyReleaseOk { get; set; }

            [JsonProperty("profileId")]
            public int ProfileId { get; set; }

            [JsonProperty("duration")]
            public int Duration { get; set; }

            [JsonProperty("albumType")]
            public string AlbumType { get; set; }

            [JsonProperty("secondaryTypes")]
            public List<string> SecondaryTypes { get; set; }

            [JsonProperty("mediumCount")]
            public int MediumCount { get; set; }

            [JsonProperty("ratings")]
            public JSONRating Ratings { get; set; }

            [JsonProperty("statistics")]
            public JSONStatistics Statistics { get; set; }


            [JsonProperty("releaseDate")]
            public DateTime ReleaseDate { get; set; }

            [JsonProperty("releases")]
            public List<JSONReleases> Releases { get; set; }

            [JsonProperty("genres")]
            public List<string> Genres { get; set; }

            [JsonProperty("media")]
            public List<JSONMedia> Media { get; set; }

            [JsonProperty("artist")]
            public JSONMusicArtist Artist { get; set; }

            [JsonProperty("images")]
            public List<JSONImage> Images { get; set; }

            [JsonProperty("links")]
            public List<JSONLink> Links { get; set; }

            [JsonProperty("remoteCover")]
            public string RemoteCover { get; set; }

            [JsonProperty("grabbed")]
            public bool Grabbed { get; set; }
        }
    }
}
