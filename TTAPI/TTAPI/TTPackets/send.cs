using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace TTAPI.Send
{
    public class APICall
    {
        public int msgid;
        public string api,
                      roomid,
                      clientid,
                      userid,
                      userauth;
        [ScriptIgnore]
        public Type HandlerSerializeTo;

        public APICall(string apiToCall, string roomIdSubject = null, Type serializeTo = null)
        {
            api = apiToCall;
            roomid = roomIdSubject;
            HandlerSerializeTo = serializeTo;
        }
    }

    public class SpeakAPI : APICall
    {
        public string text;
        public SpeakAPI(string msg)
            : base("room.speak")
        {
            text = msg;
        }
    }

    public class ChangeName : APICall
    {
        public string name;
        public ChangeName(string newName)
            : base("user.modify")
        {
            name = newName;
        }
    }

    public class ChangeLaptop : APICall
    {
        public string laptop;
        public ChangeLaptop(string newLaptop)
            : base("user.modify")
        {
            laptop = newLaptop;
        }
    }

    public class SetAvatar : APICall
    {
        public int avatarid;
        public SetAvatar(int newAvatarId)
            : base("user.set_avatar")
        {
            avatarid = newAvatarId;
        }
    }

    public class BecomeFan : APICall
    {
        public string djid;
        public BecomeFan(string userId)
            : base("user.become_fan")
        {
            djid = userid;
        }
    }

    public class RemoveFan : APICall
    {
        public string djid;
        public RemoveFan(string userId)
            : base("user.remove_fan")
        {
            djid = userid;
        }
    }

    public class RoomInfoRequest : APICall
    {
        public bool extended;
        public RoomInfoRequest(bool isextended = false)
            : base("room.info")
        {
            extended = isextended;
        }
    }

    public class BootUser : APICall
    {
        public string target_userid;

        public BootUser(string target)
            : base("room.boot_user")
        {
            target_userid = target;
        }
    }

    public class BootUserReason : BootUser
    {
        public string reason;
        public BootUserReason(string target, string bootReason)
            : base(target)
        {
            reason = bootReason;
        }
    }

    public class PresenceUpdate : APICall
    {
        public string status;
        public PresenceUpdate(string presenceStatus)
            : base("presence.update")
        {
            status = presenceStatus;
        }
    }

    public class ListRooms : APICall
    {
        public int skip;
        public ListRooms(int skipper = 0)
            : base("room.list_rooms")
        {
            skip = skipper;
        }
    }

    public class SearchRooms : APICall
    {
        public SearchRooms()
            : base("room.search")
        {

        }
    }
}
