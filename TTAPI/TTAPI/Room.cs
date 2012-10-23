using System;
using System.Web.Script.Serialization;
using TTAPI.Recv;

namespace TTAPI
{
    public class Room
    {
        public string name,
                      shortcut,
                      name_lower,
                      roomid,
                      description;
        public double created;
        public RoomMetadata metadata;

        [ScriptIgnore]
        public ChatServerInformation ServerInformation;
        string[] chatServerInformation;
        public string[] chatserver
        {
            get
            {
                return chatServerInformation;
            }
            set
            {
                chatServerInformation = value;
                ServerInformation = new ChatServerInformation()
                {
                    chatserver = value
                };
            }
        }

        public override string ToString()
        {
            return name;
        }
    }

    public class RoomMetadata
    {
        public bool dj_full;
        public string[] djs,
                        moderator_id;
        public int upvotes,
                   downvotes,
                   listeners,
                   djcount;
        public double random,
                      max_djs,
                      max_size,
                      djthreshold;
        public string privacy,
                      userid,
                      current_dj,
                      genre,
                      netloc;
        public User creator;

        [ScriptIgnore]
        public UserVote[] VoteLog;
        string[][] voteloginternal;
        public string[][] votelog
        {
            get
            {
                return voteloginternal;
            }
            set
            {
                voteloginternal = value;
                SetVoteLog(value);
            }
        }

        public Song current_song;
        public Song[] songlog;
        public SyncInformation sync;

        public void SetVoteLog(string[][] log)
        {
            VoteLog = new UserVote[log.Length];
            for (int i = 0; i < log.Length; ++i)
            {
                string[] uservote = log[i];
                if (uservote.Length != 2) continue;
                Vote voted = Vote.up;
                if (Enum.TryParse<Vote>(uservote[1], out voted))
                    VoteLog[i] = new UserVote()
                    {
                        userid = uservote[0],
                        vote = voted
                    };
            }
        }
    }

    public class StreamInformation
    {
        public string file;
        public int first_seg_id;
        public double start_time;
    }

    public class UserVote
    {
        public string userid;
        public Vote vote;

        public override string ToString()
        {
            return string.Format("{0} voted {1}", userid, vote);
        }
    }

    public class SyncInformation
    {
        public int current_seg,
                   tstamp;
    }

    public enum Vote
    {
        up,
        down
    }
}
