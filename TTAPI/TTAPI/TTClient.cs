using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using TTAPI.Recv;
using TTAPI.Send;
using WebSocketSharp;

namespace TTAPI
{
    public class TTClient
    {
        static JavaScriptSerializer jss;
        static Dictionary<string, Handler> messageHandlers;
        static Dictionary<string, Type> receivables;
        static Dictionary<Type, MethodInfo> preProcessable;

        public WebSocket webSocket;
        WebClient webClient;
        public Dictionary<string, Handler> specifiedHandlers;
        public bool isConnected { get { return webSocket.ReadyState == WsState.OPEN; } }
        DateTime lastHeartbeat;
        DateTime lastActivity;
        Dictionary<int, HandlerAndSource> messageCallbacks;
        public DateTime syncTime { get; protected set; }

        public string userId,
                authId,
                roomId,
                name,
                clientId,
                currentStatus;
        public int currentPoints,
                msgId;

        public SyncInformation syncInformation { get; protected set; }
        public List<User> usersOnDeck { get; protected set; }
        public Dictionary<string, User> usersInRoom { get; protected set; }
        public ChatServerInformation serverInformation { get; protected set; }
        public Room roomInformation { get; protected set; }

        public event StreamSync StreamsToSync;
        public event Action<TTClient> OnJoinedRoom;

        static TTClient()
        {
            jss = new JavaScriptSerializer();
            messageHandlers = new Dictionary<string, Handler>();
            receivables = new Dictionary<string, Type>();
            preProcessable = new Dictionary<Type, MethodInfo>();
            //List<Tuple<Handles,Handler>> handlers = Utilities.Reflector.FindAllMethods<Handles, Handler>();
            //foreach (Tuple<Handles, Handler> handlerCallback in handlers)
            //    messageHandlers.Add(handlerCallback.Item1.eventName, handlerCallback.Item2);

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

                ///This is for making generic wrappers to a generic delegate to a function that handles a command.
                MethodInfo[] methods = type.GetMethods();
                foreach (MethodInfo method in methods)
                {
                    Handles[] attribute = Attribute.GetCustomAttributes(method, typeof(Handles), false) as Handles[];
                    if (attribute.Length == 0) continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    foreach (Handles handle in attribute)
                    {
                        Type actualType = parameters[1].ParameterType;
                        Delegate genericHandler = Delegate.CreateDelegate(typeof(Handler<>).MakeGenericType(actualType), method);
                        Handler hardHandler = new Handler((t, o) =>
                        {
                            if (o.GetType() == actualType)
                                genericHandler.DynamicInvoke(t, o);
                        });
                        messageHandlers.Add(handle.eventName, hardHandler);
                    }
                }

                if (!type.IsSubclassOf(CommandType))
                    continue;

                MethodInfo preproc = type.GetMethod("PreProcess", new Type[] { typeof(string) });
                if (preproc != null && preproc.ReturnType == typeof(string))
                    preProcessable.Add(type, preproc);
            }
        }

        public TTClient(string userid, string authid, string roomid)
        {
            webClient = new WebClient();
            specifiedHandlers = new Dictionary<string, Handler>();

            this.userId = userid;
            this.authId = authid;
            this.roomId = roomid;

            usersInRoom = new Dictionary<string, User>();
            usersOnDeck = new List<User>();
        }

        public virtual bool Connect(Action<WebSocket> preprocess = null)
        {
            this.currentStatus = "available";
            this.clientId = String.Format("{0}-0.59633534294921572", DateTime.Now.ToBinary().ToString());
            try
            {
                Select_Server();
                webSocket = new WebSocket(String.Format("ws://{0}:{1}/socket.io/websocket", serverInformation.Address, serverInformation.Port));
            }
            catch (WebException)
            {
                return false;
            }

            if (preprocess != null) preprocess(webSocket);

            messageCallbacks = new Dictionary<int, HandlerAndSource>();
            MessageEventHandler no_session = null;
            no_session = new MessageEventHandler((o, eventdata) =>
            {
                if (!eventdata.Equals("~m~10~m~no_session"))
                {
                    webSocket.OnMessage -= no_session;
                    return;
                }
                webSocket.OnMessage += new MessageEventHandler(HandleMessage);
                StartAuthentication((i, res) =>
                {
                    if (webSocket.ReadyState != WsState.OPEN)
                    {
                        Console.WriteLine("Couldn't authenticate.");
                        return;
                    }

                    Send(new APICall("room.register", roomId));
                });
            });
            webSocket.OnMessage += no_session;
            webSocket.Connect();

            Initialize();

            return webSocket.ReadyState == WsState.OPEN;
        }

        public virtual void Initialize() { }
        public virtual void JoinedRoom()
        {
            UpdateRoomInformation();
            if (OnJoinedRoom != null) OnJoinedRoom(this);
        }

        public virtual void HandleMessage(object sender, string eventdata)
        {
            if (Regex.Match(eventdata, "~h~[0-9]+").Length > 0)
            {
                lastHeartbeat = DateTime.Now;
                SendRaw(eventdata);
                UpdatePresence();
                return;
            }

            lastActivity = DateTime.Now;

            string[] eventDataSplit = eventdata.Split(new char[]{'~'}, 5);
            int length = int.Parse(eventDataSplit[2]);
            string data = eventDataSplit[4];

            Match commandMatch = Regex.Match(data, "\"command\": \"([a-z]*)\"");
            Match msgIdMatch = Regex.Match(data, "\"msgid\": ([0-9]*)");
            HandlerAndSource has = null;
            string command = null;
            int msgId = -1;
            if (commandMatch.Groups.Count > 1)
                command = commandMatch.Groups[1].Value;
            if (msgIdMatch.Groups.Count > 1)
                if (int.TryParse(msgIdMatch.Groups[1].Value, out msgId) && messageCallbacks.ContainsKey(msgId))
                    has = messageCallbacks[msgId];
            Type serializeTo = null;
            if (command != null && receivables.ContainsKey(command))
                serializeTo = receivables[command];
            if (has != null)
                serializeTo = has.source.HandlerSerializeTo;

            try
            {
                Command response;
                if (serializeTo != null && preProcessable.ContainsKey(serializeTo))
                    data = preProcessable[serializeTo].Invoke(null, new object[] { data }) as string;
                if (serializeTo != null)
                    response = jss.Deserialize(data, serializeTo) as Command;
                else
                    response = jss.Deserialize<Command>(data);
                if (!String.IsNullOrEmpty(response.err))
                {
                    Console.WriteLine("[Error!]{0}", response.err);
                    return;
                }
                //Command basicresponse = jss.Deserialize<Command>(data);
                if (has != null)
                    has.handler(this, response);
                if (response.command != null && messageHandlers.ContainsKey(response.command))
                    messageHandlers[response.command](this, response);
                if (response.command != null && specifiedHandlers.ContainsKey(response.command))
                    specifiedHandlers[response.command](this, response);
            }
            catch (ArgumentException) { }
        }

        public void Select_Server()
        {
            string resp = webClient.DownloadString(String.Format("http://turntable.fm/api/room.which_chatserver?roomid={0}", roomId));
            int start = resp.IndexOf(", ")+2,
                end = resp.LastIndexOf("]");
            string successful = resp.Substring(1, start - 3);
            if (successful.Equals("true"))
            {
                string serverInformationJSON = resp.Substring(start, end - start);
                serverInformation = jss.Deserialize<ChatServerInformation>(serverInformationJSON);
            }
            /*if (respJSON[0].Equals(true))
            {
                server = respJSON[1]["chatserver"][0];
                server_port = respJSON[1]["chatserver"][1];
            }*/
        }

        public void ExplicitlyHandle(string eventName, Handler callback)
        {
            this.specifiedHandlers.Add(eventName, callback);
        }

        public void ExplicitlyHandle<T>(string eventName, Handler<T> callback) where T : Command
        {
            ExplicitlyHandle(eventName, new Handler((c, o) =>
            {
                if (o is T)
                    callback(c, o as T);
            }));
        }

        public void StartAuthentication(Handler callback = null)
        {
            Send(new APICall("user.authenticate"), callback);
        }
        public void GetFanOf(Handler<FanOf> callback = null)
        {
            Send(new APICall("user.get_fan_of"), callback, true);
            //Send("{api: ''}", callback);
        }
        public void Speak(string msg, Handler callback = null)
        {
            Send(new SpeakAPI(msg), callback);
        }
        public void RoomInfo(bool extendedInfo = false, Handler<RoomInfo> callback = null)
        {
            Send(new RoomInfoRequest(extendedInfo), callback);
        }
        public void ChangeName(string name, Handler callback = null)
        {
            Send(new ChangeName(name), callback);
        }
        public void ModifyLaptop(string laptop = "chrome", Handler callback = null)
        {
            Send(new ChangeLaptop(laptop), callback);
        }
        public void SetAvatar(int avatarId, Handler callback = null)
        {
            Send(new SetAvatar(avatarId), callback);
        }
        public void BecomeFan(string userId, Handler callback = null)
        {
            Send(new BecomeFan(userId), callback);
        }
        public void RemoveFan(string userId, Handler callback = null)
        {
            //Send(new APICall("user.remove_fan")
            Send(new RemoveFan(userId), callback);
        }
        public void AddDJ(Handler callback = null)
        {
            Send(new APICall("room.add_dj", roomId), callback);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId">If null, is the bot's userid.</param>
        /// <param name="callback"></param>
        public void RemoveDJ(string userId = null, Handler callback = null)
        {
            Send(new APICall("room.rem_dj", roomId), callback);
        }
        public void StopSong(Handler callback = null)
        {
            Send(new APICall("room.stop_song", roomId), callback);
        }
        public void BootUser(string userId, string reason = null, Handler callback = null)
        {
            Send(reason == null ? new BootUser(userId) : new BootUserReason(userId, reason), callback);
        }
        public void GetFavorites(Handler callback = null)
        {
            Send(new APICall("room.get_favorites"), callback);
        }
        public void AddFavorite(string roomId, Handler callback = null)
        {
            Send(new APICall("room.add_favorite", roomId), callback);
        }
        public void RemoveFavorite(string roomId, Handler callback = null)
        {
            Send(new APICall("room.rem_favorite", roomId), callback);
        }
        public void RoomList(int skipper = 0, Handler<Rooms> callback = null)
        {
            Send(new ListRooms(skipper), callback);
        }
        public void RoomGraph(Handler<Rooms> callback = null)
        {
            Send(new APICall("room.directory_graph"), callback);
        }
        public void UserInfo(Handler<User> callback)
        {
            Send(new APICall("user.info"), callback);
        }
        public void AvailableAvatars(Handler<AvatarList> callback)
        {
            Send(new APICall("user.available_avatars"), callback);
        }
        public void GetProfile(string userid = null, Handler<UserProfile> callback = null)
        {
            Send(new GetProfile(userid), callback);
        }
        public void GetPresence(string userid = null, Handler<Presence> callback = null)
        {
            Send(new GetPresence(userid), callback);
        }
        public void GetUserID(string username = null, Handler<UserID> callback = null)
        {
            Send(new GetUserID(username), callback);
        }
        public void GetPlaylsit(string playlistName = "default", bool minimal = false, Handler<Playlist> callback = null)
        {
            Send(new GetPlaylist(playlistName, minimal), callback);
        }

        public void SetStatus(string status)
        {
            this.currentStatus = status;
            UpdatePresence();
        }

        public void Send<T>(IAPICall message, Handler<T> callback, bool needsProcessing = true) where T : Command
        {
            if (message.HandlerSerializeTo == null)
            {
                Type[] handlerParams = callback.GetType().GetGenericArguments();
                if (handlerParams.Length == 1)
                    message.HandlerSerializeTo = handlerParams[0];
            }
            Send(message, new Handler((c, o) =>
            {
                if (o is T)
                    callback(c, o as T);
            }), needsProcessing);
        }

        public void Send(IAPICall message, Handler callback = null, bool needsProcessing = true)
        {
            if (needsProcessing)
            {
                message.msgid = msgId;
                message.clientid = clientId;
                message.userid = userId;
                message.userauth = authId;
                message.roomid = roomId;
            }

            if (callback != null)
                messageCallbacks.Add(msgId, new HandlerAndSource(callback, message));

            string sendingMessage = jss.Serialize(message);
            webSocket.Send(String.Format("~m~{0}~m~{1}", sendingMessage.Length, sendingMessage));

            ++msgId;
        }

        public void SendRaw(string message)
        {
            lastActivity = DateTime.Now;

            webSocket.Send(message);
            ++msgId;
        }

        public void Close()
        {
            webSocket.Close();
        }

        public void UpdatePresence(Handler callback = null)
        {
            Send(new PresenceUpdate(currentStatus), callback);
        }

        public void UpdateRoomInformation(RoomInfo info)
        {
            UpdateRoomInformation(info.room);
            usersInRoom = new Dictionary<string, User>();
            string[] djIDs = roomInformation.metadata.djs;

            User[] djs = new User[djIDs.Length];
            for (int i = 0; i < info.users.Length; ++i)
            {
                User currentUser = info.users[i];
                usersInRoom.Add(currentUser.userid, currentUser);
                int djIndex = -1;
                if ((djIndex = Array.FindIndex(djIDs, (o) => o == currentUser.userid)) != -1)
                    djs[djIndex] = currentUser;
            }
        }

        public void UpdateRoomInformation(Room info)
        {
            roomInformation = info;
            syncInformation = roomInformation.metadata.sync;
            syncTime = DateTime.Now;
            roomId = info.roomid;
            ResyncStream();
        }

        public void UpdateRoomInformation()
        {
            RoomInfo(false, new Handler<RoomInfo>((t, i) => UpdateRoomInformation(i)));
        }

        public void ResyncStream()
        {
            if (StreamsToSync != null && syncInformation != null && roomInformation != null)
            {
                int timeOffset = (int)(DateTime.Now - syncTime).TotalMilliseconds;
                StreamsToSync(roomInformation.metadata.netloc + roomInformation.roomid, syncInformation.current_seg, syncInformation.tstamp - timeOffset, 500);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class Handles : Attribute
    {
        public string eventName;
        public Handles(string handlesEvent)
        {
            eventName = handlesEvent;
        }
    }
    public delegate void Handler<T>(TTClient instance, T eventToHandle) where T : Command;
    public delegate void Handler(TTClient instance, Command eventToHandle);
    public class HandlerAndSource
    {
        public Handler handler;
        public IAPICall source;

        public HandlerAndSource(Handler handle, IAPICall callingSource)
        {
            handler = handle;
            source = callingSource;
        }
    }
    public delegate void StreamSync(string key, int segment, int timeStamp, int timeout);
}
