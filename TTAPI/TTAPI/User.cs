using System;
using System.Web.Script.Serialization;
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
        [ScriptIgnore]
        public ACL aclReadable
        {
            get
            {
                return (ACL)(int)Math.Round(acl, 0);
            }
        }
        public double acl;
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