using System;

namespace Requestrr.WebApi.RequestrrBot.Music
{
    public class MusicAlbum
    {
        // MusicBrainz models "Various Artists" compilations (e.g. Now That's
        // What I Call Music, Ministry of Sound Annuals) as this one real artist.
        public const string VariousArtistsId = "89ad4ac3-39f7-470e-963a-56509c546377";

        public string DownloadClientId { get; set; }
        public string AlbumId { get; set; }
        public string Title { get; set; }
        public string Disambiguation { get; set; }
        public string Overview { get; set; }
        public string ArtistId { get; set; }
        public string ArtistName { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string AlbumType { get; set; }


        public bool Available { get; set; }
        public bool Monitored { get; set; }
        public string Quality { get; set; }
        public bool Requested { get; set; }


        public string PosterPath { get; set; }


        public bool IsVariousArtists => string.Equals(ArtistId, VariousArtistsId, StringComparison.OrdinalIgnoreCase);
    }
}
