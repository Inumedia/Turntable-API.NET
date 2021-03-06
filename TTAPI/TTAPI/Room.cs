using System;
using TTAPI.Recv;

namespace TTAPI
{
    public class Room
    {
        public string name                                { get; set; }
        public string               shortcut              { get; set; }
        public string               name_lower            { get; set; }
        public string               roomid                { get; set; }
        public string description { get; set; }
        public double created { get; set; }
        public RoomMetadata metadata { get; set; }

        public ChatServerInformation ServerInformation { get; set; }
        object[] chatServerInformation { get; set; }
        public object[] chatserver
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
        public bool dj_full { get; set; }
        public bool featured { get; set; }
        public string[] djs { get; set; }
        public string[] moderator_id { get; set; }
        public int upvotes { get; set; }
        public int downvotes { get; set; }
        public int listeners { get; set; }
        public int djcount { get; set; }
        public double random { get; set; }
        public double max_djs { get; set; }
        public double max_size { get; set; }
        public double djthreshold { get; set; }
        public string privacy { get; set; }
        public string userid { get; set; }
        public string current_dj { get; set; }
        public string genre { get; set; }
        public string netloc { get; set; }
        public User creator { get; set; }

        public UserVote[] VoteLog { get; set; }
        string[][] voteloginternal { get; set; }
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

        public Song current_song { get; set; }
        public Song[] songlog { get; set; }
        public SyncInformation sync { get; set; }

        public void SetVoteLog(string[][] log)
        {
            if (log == null)
            {
                VoteLog = new UserVote[0];
                return;
            }

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
        public string file { get; set; }
        public int first_seg_id { get; set; }
        public double start_time { get; set; }
    }

    public class UserVote
    {
        public string userid { get; set; }
        public Vote vote { get; set; }

        public override string ToString()
        {
            return string.Format("{0} voted {1}", userid, vote);
        }
    }

    public class SyncInformation
    {
        public int current_seg { get; set; }
        public int tstamp { get; set; }
}

    public enum Vote
    {
        up,
        down
    }
}
