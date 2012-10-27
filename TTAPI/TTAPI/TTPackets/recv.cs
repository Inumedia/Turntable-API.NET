using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using TTAPI;

namespace TTAPI.Recv
{
    public class Command
    {
        static Dictionary<string, Type> receivables;
        static Dictionary<Type, Converter<string,string>> preProcessable;

        static Command()
        {
            receivables = new Dictionary<string, Type>();
            preProcessable = new Dictionary<Type, Converter<string, string>>();

            Type CommandType = typeof(Command);
            Type[] types = typeof(Command).Assembly.GetTypes();
            foreach (Type type in types)
            {
                /// No point in iterating over all the types twice when we can just throw this in with this iteration.
                /// This is for finding all the command to class convertable types.
                CommandName[] commandAttributes = Attribute.GetCustomAttributes(type, typeof(CommandName), false) as CommandName[];
                if (commandAttributes.Length != 0)
                {
                    foreach (CommandName attribute in commandAttributes)
                        receivables.Add(attribute.Name, type);
                }

                if (!type.IsSubclassOf(CommandType))
                    continue;

                MethodInfo preproc = type.GetMethod("PreProcess", new Type[] { typeof(string) });
                if (preproc != null && preproc.ReturnType == typeof(string))
                    preProcessable.Add(type, Delegate.CreateDelegate(typeof(Converter<string,string>), preproc) as Converter<string,string>);
            }
        }

        /// <summary>
        /// Attempts to preprocess text to allow easier mapping to a native object.
        /// </summary>
        /// <param name="serializeTo">The type that we are planning on serializing to.</param>
        /// <param name="input">The input data that we are preprocessing.</param>
        /// <returns>Processed data that should allow for mapping to <paramref name="serializeTo"/>.  If no preprocessable method exists, returns the <paramref name="input"/></returns>
        public static string Preprocess(Type serializeTo, string input)
        {
            if (serializeTo != null && preProcessable.ContainsKey(serializeTo))
                return preProcessable[serializeTo](input);
            return input;
        }

        public static Type MapCommandToType(string command)
        {
            if (command != null && receivables.ContainsKey(command))
                return receivables[command];
            return null;
        }

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
            int i = 0, breakingPoint = -1;
            i = input.Substring(0, 24).IndexOf("\"rooms\": [")+11;
            int startingPoint = i;
            StringBuilder builder = new StringBuilder(input.Substring(0,i-1));
            while (i >= startingPoint)
            {
                if (i != startingPoint) builder.Append(", ");
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
        public string userid;
    }

    [CommandName("pmed")]
    public class PrivateMessage : Command
    {
        public string text, userid, senderid, command;
        public double time;
    }

    public class AvatarList : Command
    {
        public AvatarRequirements[] avatars;
    }

    public class Presence : Command
    {
        public UserPresence presence;
    }

    public class UserID : Command
    {
        public string userid;
        // Again with the nothing to do.  I just like strong typing this. :D
    }

    public class Playlist : Command
    {
        public Song[] list;
    }

    public class UserAuth : Command
    {
        public string userid, userauth;
    }
}
