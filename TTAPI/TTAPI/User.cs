using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTAPI
{
    public class User
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

    public enum ACL
    {
        User = 0,
        SuperUser = 1,
        GateKeeper = 2
    }
}