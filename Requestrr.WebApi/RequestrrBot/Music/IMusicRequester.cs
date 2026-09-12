using System.Threading.Tasks;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public interface IMusicRequester
    {
        Task<MusicRequestResult> RequestMusicAsync(MusicRequest request, MusicArtist music);

        Task<MusicRequestResult> RequestMusicAlbumAsync(MusicRequest request, MusicAlbum album);
    }


    public class MusicRequestResult
    {
        public bool WasDenied { get; set; }

        /// <summary>
        /// Set only for denials with a specific, user-facing explanation (e.g. Lidarr's
        /// missing-cover-art constraint bug) - null falls back to the generic denied message.
        /// </summary>
        public string DenialReason { get; set; }
    }
}
