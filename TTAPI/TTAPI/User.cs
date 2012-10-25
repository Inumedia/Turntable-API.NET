using System;
using TTAPI.Recv;

namespace TTAPI
{
    public class User : Command
    {
        public string name,
                      userid,
                      laptop,
                      laptop_version,
                      status,
                      fbid,
                      twitterid;
        public ACL acl;
        public int fans,
                   points,
                   avatarid;
        public double created;
        public bool has_tt_password;

        public override string ToString()
        {
            return name;
        }
    }

    public class UserProfile : User
    {
        public string about, website, topartists, hangout;
    }

    public enum ACL
    {
        User = 0,
        SuperUser = 1,
        GateKeeper = 2
    }

    public class AvatarRequirements
    {
        public int[] avatarids;
        public int min;
        public int acl;
    }

    public class UserPresence
    {
        public string status, userid;
    }
}