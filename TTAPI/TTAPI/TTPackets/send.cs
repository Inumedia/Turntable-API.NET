using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace TTAPI.Send
{
    public class APICall : IAPICall
    {
        public int msgid { get; set; }
        public string api { get; set; }
        public string roomid { get; set; }
        public string clientid { get; set; }
        public string userid { get; set; }
        public string userauth { get; set; }
        [ScriptIgnore]
        public Type HandlerSerializeTo { get; set; }

        public APICall(string apiToCall, string roomIdSubject = null, Type serializeTo = null)
        {
            api = apiToCall;
            roomid = roomIdSubject;
            HandlerSerializeTo = serializeTo;
        }
    }

    public interface IAPICall
    {
        int msgid { get; set; }
        string api { get; }
        string roomid { get; set; }
        string clientid { get; set; }
        string userid { get; set; }
        string userauth { get; set; }
        [ScriptIgnore]
        Type HandlerSerializeTo { get; set; }
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

    public class GetProfile : APICall
    {
        public string userid;
        public GetProfile(string target = null)
            : base("user.get_profile")
        {
            userid = target;
        }
    }

    public class GetPresence : APICall
    {
        public string uid;
        public GetPresence(string target = null) : base("presence.get")
        {
            uid = target;
        }
    }

    public class GetUserID : APICall
    {
        public string name;
        public GetUserID(string target = null)
            : base("user.get_id")
        {
            name = target;
        }
    }

    public class GetPlaylist : APICall
    {
        public string playlist_name;
        public bool minimal;
        public GetPlaylist(string name = "default", bool isMinimal = false) : base("playlist.all")
        {
            minimal = isMinimal;
            playlist_name = name;
        }
    }

    public class UpdateProfile : UserProfile, IAPICall
    {
        public int msgid { get { return base.msgid; } set { base.msgid = value; } }
        public string api { get { return "user.modify_profile"; } }
        public string roomid { get { return base.roomid; } set { base.roomid = value; } }
        public string userid { get { return base.userid; } set { base.userid = value; } }
        public string clientid { get; set; }
        public string userauth { get; set; }
        [ScriptIgnore]
        public Type HandlerSerializeTo { get; set; }
    }
}
