using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace TTAPI.Send
{
    public class APICall : IAPICall
    {
        public int msgid { get; set; }
        public string api { get; set; }
        public string roomid { get; set; }
        public string clientid { get; set; }
        public string client { get; set; } = "web";
        public string userid { get; set; }
        public string userauth { get; set; }
        [JsonIgnore]
        public Type HandlerSerializeTo { get; set; }
        [JsonIgnore]
        public string CustomInterfaceAddress { get; set; }

        public APICall(string apiToCall, string roomIdSubject = null, Type serializeTo = null, string customAddress = null)
        {
            api = apiToCall;
            roomid = roomIdSubject;
            HandlerSerializeTo = serializeTo;
            CustomInterfaceAddress = customAddress;
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
        [JsonIgnore]
        Type HandlerSerializeTo { get; set; }
    }

    public class SpeakAPI : APICall
    {
        public string text { get; set; }
        public SpeakAPI(string msg)
            : base("room.speak")
        {
            text = msg;
        }
    }

    public class ChangeName : APICall
    {
        public string name { get; set; }
        public ChangeName(string newName)
            : base("user.modify")
        {
            name = newName;
        }
    }

    public class ChangeLaptop : APICall
    {
        public string laptop { get; set; }
        public ChangeLaptop(string newLaptop)
            : base("user.modify")
        {
            laptop = newLaptop;
        }
    }

    public class SetAvatar : APICall
    {
        public int avatarid { get; set; }
        public SetAvatar(int newAvatarId)
            : base("user.set_avatar")
        {
            avatarid = newAvatarId;
        }
    }

    public class BecomeFan : APICall
    {
        public string djid { get; set; }
        public BecomeFan(string userId)
            : base("user.become_fan")
        {
            djid = userid;
        }
    }

    public class RemoveFan : APICall
    {
        public string djid { get; set; }
        public RemoveFan(string userId)
            : base("user.remove_fan")
        {
            djid = userid;
        }
    }

    public class RoomInfoRequest : APICall
    {
        public bool extended { get; set; }
        public RoomInfoRequest(bool isextended = false)
            : base("room.info")
        {
            extended = isextended;
        }
    }

    public class BootUser : APICall
    {
        public string target_userid { get; set; }

        public BootUser(string target)
            : base("room.boot_user")
        {
            target_userid = target;
        }
    }

    public class BootUserReason : BootUser
    {
        public string reason { get; set; }
        public BootUserReason(string target, string bootReason)
            : base(target)
        {
            reason = bootReason;
        }
    }

    public class PresenceUpdate : APICall
    {
        public string status { get; set; }
        public PresenceUpdate(string presenceStatus)
            : base("presence.update")
        {
            status = presenceStatus;
        }
    }

    public class ListRooms : APICall
    {
        public string userid { get; set; }
        public string userauth { get; set; }
        public int skip { get; set; }
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
        public string userid { get; set; }
        public GetProfile(string target = null)
            : base("user.get_profile")
        {
            userid = target;
        }
    }

    public class GetPresence : APICall
    {
        public string uid { get; set; }
        public GetPresence(string target = null) : base("presence.get")
        {
            uid = target;
        }
    }

    public class GetUserID : APICall
    {
        public string name { get; set; }
        public GetUserID(string target = null)
            : base("user.get_id")
        {
            name = target;
        }
    }

    public class GetPlaylist : APICall
    {
        public string playlist_name { get; set; }
        public bool minimal { get; set; }
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
        [JsonIgnore]
        public Type HandlerSerializeTo { get; set; }
    }

    public class EmailLogin : APICall
    {
        public string email { get; set; }
        public string password { get; set; }
        public string fingerprint { get; set; } = BitConverter.ToString(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes($"{System.Net.Dns.GetHostName()}-{DateTime.UtcNow.ToString()}"))).Replace("-", "").ToLower();
        public EmailLogin(string loginemail, string loginpassword)
            : base("user.email_login")
        {
            email = loginemail;
            password = loginpassword;
        }
    }

    public class GetFavorites : APICall
    {
        public string userid { get; set; }
        
        public string userauth { get; set; }
        public GetFavorites() : base("room.get_favorites") { }
    }

    public class AddDJ : APICall
    {
        public AddDJ() : base("room.add_dj") { }
    }

    public class RemoveDJ : APICall
    {
        public RemoveDJ() : base("room.rem_dj") { }
    }

    public class StopSong : APICall
    {
        public StopSong() : base("room.stop_song") { }
    }

    public class AddFavorite : APICall
    {
        public AddFavorite(string target)
            : base("room.add_favorite")
        {
            roomid = target;
        }
    }

    public class RemoveFavorite : APICall
    {
        public RemoveFavorite(string target)
            : base("room.rem_favorite")
        {
            roomid = target;
        }
    }

    public class GetUserInformation : APICall
    {
        public GetUserInformation(string target)
            : base("user.info")
        {
            userid = target;
        }
    }

    public class GetAvailableAvatars : APICall
    {
        public GetAvailableAvatars() : base("user.available_avatars") { }
    }

    public class GetFansOf : APICall
    {
        public GetFansOf() : base("user.get_fan_of") { }
    }

    public class AttemptAuthentication : APICall
    {
        public AttemptAuthentication() : base("user.authenticate") { }
    }

    public class RoomVote : APICall
    {
        public string vh { get; set; }
        public string th { get; set; }
        public string ph { get; set; }
        public string val { get; set; }

        public RoomVote(string vote, string roomId, string currentSongId, Random random) : base("room.vote", roomId)
        {
            val = vote;

            using var sha = SHA1.Create();
            vh = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes($"{roomId}{vote}{currentSongId}"))).Replace("-", "").ToLower();
            byte[] randomBuffer = new byte[16];
            random.NextBytes(randomBuffer);
            th = BitConverter.ToString(sha.ComputeHash(randomBuffer)).Replace("-", "").ToLower();
            random.NextBytes(randomBuffer);
            ph = BitConverter.ToString(sha.ComputeHash(randomBuffer)).Replace("-", "").ToLower();
        }
    }
}
