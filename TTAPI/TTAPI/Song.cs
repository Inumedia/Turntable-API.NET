using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTAPI
{
    public class Song
    {
        public string djid { get; set; }
        public string _id { get; set; }
        public string djname { get; set; }
        public string sourceid { get; set; }
        public string source { get; set; }
        public bool snaggable;
        public double score { get; set; }
        public double starttime { get; set; }
        public SongMetadata metadata { get; set; }

        public override string ToString()
        {
            if (metadata != null)
                return string.Format("{0} by {1}, uploaded by {2}, played by {3}", metadata.song, metadata.artist, sourceid, djname);
            return string.Format("{0} uploaded by {1}, played by {2}", _id, sourceid, djname);
        }
    }

    public class SongMetadata
    {
        public string album { get; set; }
        public string artist { get; set; }
        public string coverart { get; set; }
        public string song { get; set; }
        public string mnid { get; set; }
        public string genre { get; set; }
        public int length { get; set; }
    }
}
