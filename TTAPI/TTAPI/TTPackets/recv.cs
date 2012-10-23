using System;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using TTAPI;

namespace TTAPI.Recv
{
    public class Command
    {
        public int msgid;
        public string command,
                      roomid,
                      err;
        public bool success;

        public Command()
        {
            msgid = -1;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandName : Attribute
    {
        public string Name;
        public CommandName(string usedname)
        {
            Name = usedname;
        }
        public bool Equals(Command obj)
        {
            return Name == obj.command;
        }
    }

    public class ChatServerInformation
    {
        [ScriptIgnore]
        public int Port;
        [ScriptIgnore]
        public string Address;
        string[] serverInformation;
        public string[] chatserver
        {
            get
            {
                return serverInformation;
            }
            set
            {
                serverInformation = value;

                if (value.Length == 2)
                {
                    Port = -1;
                    if (!int.TryParse(value[1], out Port)) return;
                    Address = value[0];
                }
            }
        }
    }

    [CommandName("registered")]
    public class Registered : Command
    {
        public User[] user;
    }

    [CommandName("deregistered")]
    public class DeRegistered : Command
    {
        public User[] user;
    }

    [CommandName("add_dj")]
    public class AddDJ : Command
    {
        public User[] user;
    }

    [CommandName("rem_dj")]
    public class RemoveDJ : Command
    {
        public User[] user;
    }

    [CommandName("endsong")]
    public class EndSong : Command
    {
        public Room room;
    }

    /// <summary>
    /// Used for getting who we are a fan of.
    /// </summary>
    /// <remarks>Can only be obtained by explicitly typing</remarks>
    public class FanOf : Command
    {
        public string[] fanof;
    }

    /// <summary>
    /// Used for getting the favorite rooms.
    /// </summary>
    /// <remarks>Can only be obtained by explicitly typing</remarks>
    public class Favorites : Command
    {
        public string[] list;
    }

    /// <summary>
    /// Gets a list of people who are fans of you.  ( a feature that TT.FM doesn't show! ;) )
    /// </summary>
    /// <remarks>Can only be obtained by explicitly typing</remarks>
    public class GetFans : Command
    {
        public string[] fans;
    }

    public class RoomInfo : Command
    {
        public Room room;
        public User[] users;
    }

    [CommandName("speak")]
    public class Speak : Command
    {
        public string userid,
                      name,
                      text;
    }

    [CommandName("newsong")]
    public class NewSong : Command
    {
        public Room room;
    }

    [CommandName("nosong")]
    public class NoSong : Command
    {
        Room room;
    }

    [CommandName("new_moderator")]
    public class NewModerator : Command
    {
        public string userid;
    }

    [CommandName("rem_moderator")]
    public class RemoveModerator : Command
    {
        public string userid;
    }

    public class PMHistory : Command
    {
        public PrivateMessage[] history;
    }

    public class Rooms : Command
    {
        static JavaScriptSerializer serializer = new JavaScriptSerializer();

        public RoomUserPair[] rooms;

        public class RoomUserPair
        {
            public Room room;
            public User[] users;
            public string this[int i]
            {
                get
                {
                    /// I'm not even sure if this will work.  :<
                    return null;
                }
                set
                {
                    if (i == 0)
                    {
                        room = serializer.Deserialize<Room>(value);
                    }
                    else
                    {
                        users = serializer.Deserialize<User[]>(value);
                    }
                }
            }
        }

        public static string PreProcess(string input)
        {
            string data = input;//.Replace("]}}, [", "]}}, [")
            int i = 24, breakingPoint = -1;
            StringBuilder builder = new StringBuilder(input.Substring(0,i-1));
            while (i >= 24)
            {
                if (i != 24) builder.Append(", ");
                breakingPoint = data.IndexOf("}, [", i);
                string roomData = data.Substring(i, breakingPoint - i+1);
                i = data.IndexOf("], [{", i);
                string userData = "";
                if (i != -1)
                    userData = data.Substring(breakingPoint + 4, i - breakingPoint - 5);
                builder.AppendFormat("{{\"room\": {0}, \"users\": [{1}]}}", roomData, userData);
                i += 4;
            }
            builder.Append(input.Substring(breakingPoint + 6));
            //int breakingPoint = data.IndexOf("]}}, [", 0) + 3;
            //while(breakingPoint
            //string roomDataBroken = data.Substring(1, breakingPoint);
            //return input.Replace("]}}, [{", "]}}, \"users\": [ {").Replace("[{", "{\"room\":{").Replace("}]], [{", "}]}").Replace("[ {", "[{");
            return builder.ToString();
        }
    }

    [CommandName("snagged")]
    public class Snagged : Command
    {
        // We don't have to do anything specific for this, since it's all already done :D
        // public string command, userid;
    }

    [CommandName("pmed")]
    public class PrivateMessage : Command
    {
        public string text, userid, senderid, command;
        public double time;
    }
}
