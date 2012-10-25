using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTAPI
{
    public class Song
    {
        public string djid,
                      _id,
                      djname,
                      sourceid,
                      source;
        public bool snaggable;
        public double score;
        public double starttime;
        public SongMetadata metadata;

        public override string ToString()
        {
            if (metadata != null)
                return string.Format("{0} by {1}, uploaded by {2}, played by {3}", metadata.song, metadata.artist, sourceid, djname);
            return string.Format("{0} uploaded by {1}, played by {2}", _id, sourceid, djname);
        }
    }

    public class SongMetadata
    {
        public string album,
                      artist,
                      coverart,
                      song,
                      mnid,
                      genre;
        public int length;
    }
}
