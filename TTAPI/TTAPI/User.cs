using System;
using System.Text.Json.Serialization;
using TTAPI.Recv;

namespace TTAPI
{
    public class User : Command
    {
        public string name { get; set; }
        public string userid { get; set; }
        public string laptop { get; set; }
        public string laptop_version { get; set; }
        public string status { get; set; }
        public string fbid { get; set; }
        public string twitterid { get; set; }
        [JsonIgnore]
        public ACL aclReadable
        {
            get
            {
                return (ACL)(int)Math.Round(acl, 0);
            }
        }
        public double acl { get; set; }
        public int fans { get; set; }
        public int points { get; set; }
        public int avatarid { get; set; }
        public double created { get; set; }
        public bool has_tt_password { get; set; }

        public override string ToString()
        {
            return name;
        }
    }

    public class UserProfile : User
    {
        public string about { get; set; }
        public string website { get; set; }
        public string topartists { get; set; }
        public string hangout { get; set; }
    }

    public enum ACL
    {
        User = 0,
        SuperUser = 1,
        GateKeeper = 2
    }

    public class AvatarRequirements
    {
        public int[] avatarids { get; set; }
        public int min { get; set; }
        public int acl { get; set; }
    }

    public class UserPresence
    {
        public string status { get; set; }

        public string userid { get; set; }
    }
}