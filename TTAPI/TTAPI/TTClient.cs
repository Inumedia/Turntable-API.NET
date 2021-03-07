using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TTAPI.Recv;
using TTAPI.Send;

namespace TTAPI
{
    public class TTClient
    {
        static HttpClient _httpClient = new HttpClient();
        public DateTime syncTime { get; protected set; }

        public string userId,
                authId,
                roomId,
                name,
                clientId;
        public int currentPoints,
                msgId;
        private TTSocket TTSock;
        public DateTime lastHeartbeat;

        public SyncInformation syncInformation { get; protected set; }
        public List<User> usersOnDeck { get; protected set; }
        public Dictionary<string, User> usersInRoom { get; protected set; }
        public ChatServerInformation serverInformation { get; protected set; }
        public Room roomInformation { get; protected set; }
        public DateTime? NextPresenceUpdateAt { get; private set; }
        public TimeSpan? ExpectedPresenceUpdateInterval { get; private set; }

        public event StreamSync StreamsToSync;
        public event Action OnJoinedRoom;
        public event Action<Room> OnUpdateRoom;
        public event Action<RoomInfo> OnUpdateRoomInfo;
        public event Action<User> OnUserRegistered;
        public event Action<User> OnUserDeregistered;
        public string DesiredPresence = "available";
        public ILogger _logger { get; private set; }
        public bool IsConnected { get => TTSock != null && TTSock.isConnected; }

        private readonly ILoggerFactory _logFactory;
        private Random random;

        public TTClient(ILoggerFactory logFactory) => _logFactory = logFactory;

        public TTClient(string userid, string authid, string roomid) { }

        public async Task<bool> ConnectAsync(string userId, string authId, string roomId, CancellationToken cancelToken)
        {
            cancelToken.ThrowIfCancellationRequested();

            this.userId = userId;
            if (int.TryParse(this.userId.Substring(0, 8), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int seed))
                random = new Random(seed);
            else random = new Random(DateTime.UtcNow.Second);
            this._logger = _logFactory.CreateLogger($"TT({userId})");
            this.authId = authId;
            this.roomId = roomId;

            usersInRoom = new Dictionary<string, User>();
            usersOnDeck = new List<User>();

            this.clientId = String.Format("{0}-0.59633534294921572", DateTime.Now.ToBinary().ToString());

            try
            {
                await SelectServerAsync();
                TTSock = new TTSocket(this, serverInformation);
                TTSock.OnConnected += () =>
                {
                    Send(new APICall("room.register", roomId));
                    Send(new ChangeLaptop("pc"));
                    Send(new RoomInfoRequest());
                };
                TTSock.OnDisconnected += () => ConnectAsync(userId, authId, roomId, cancelToken).GetAwaiter().GetResult();
                await TTSock.ConnectAsync(cancelToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't connect");
                return false;
            }

            return TTSock.isConnected;
        }

        public void Vote(string vote)
        {
            Send(new RoomVote(vote, roomId, roomInformation.metadata.current_song._id, random));
        }

        public virtual void JoinedRoom()
        {
            if (OnJoinedRoom != null) OnJoinedRoom();
            UpdateRoomInformation();
        }

        public async Task SelectServerAsync()
        {
            var getRoomChat = await _httpClient.GetAsync(String.Format("http://turntable.fm/api/room.which_chatserver?roomid={0}", roomId));
            getRoomChat.EnsureSuccessStatusCode();
            var resp = await getRoomChat.Content.ReadAsStringAsync();

            int start = resp.IndexOf(", ")+2,
                end = resp.LastIndexOf("]");
            string successful = resp.Substring(1, start - 3);
            if (successful.Equals("true"))
            {
                string serverInformationJSON = resp.Substring(start, end - start);
                serverInformation = JsonSerializer.Deserialize<ChatServerInformation>(serverInformationJSON);
            }
        }

        //public void StartAuthentication(Handler callback = null)
        //{
        //    Send(new APICall("user.authenticate"), callback);
        //}
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
            this.DesiredPresence = status;
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

        public void Send<K>(K message, Handler callback = null, bool needsProcessing = true)
            where K : IAPICall
        {
            if (needsProcessing)
            {
                if (message.clientid == null) message.clientid = clientId;
                if (message.userid == null) message.userid = userId;
                if (message.userauth == null) message.userauth = authId;
                if (message.roomid == null) message.roomid = roomId;
            }

            TTSock.Send(message, callback);
        }

        public void Close()
        {
            TTSock.Close();
        }

        public void UpdatePresence(Handler callback = null)
        {
            if (NextPresenceUpdateAt.HasValue)
            {
                var difference = DateTime.UtcNow - NextPresenceUpdateAt.Value;
                // If the next update is expected in 5 seconds or more, return. Prevent us from spamming.
                if (difference.TotalSeconds < -5) return;
            }

            if (ExpectedPresenceUpdateInterval.HasValue)
                NextPresenceUpdateAt = DateTime.UtcNow.Add(ExpectedPresenceUpdateInterval.Value);

            _logger.LogDebug("{0} Sending update presence, next expected at {1}", userId, NextPresenceUpdateAt);

            Send(new PresenceUpdate(DesiredPresence), (client, command) =>
            {
                client.NextPresenceUpdateAt = DateTime.UtcNow.AddSeconds(command.interval);
                ExpectedPresenceUpdateInterval = TimeSpan.FromSeconds(command.interval);
                if (callback != null)
                    callback(client, command);
            });
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
            if (OnUpdateRoomInfo != null)
                OnUpdateRoomInfo(info);
        }

        public virtual void UpdateRoomInformation(Room info)
        {
            roomInformation = info;
            syncInformation = roomInformation.metadata.sync;
            syncTime = DateTime.Now;
            roomId = info.roomid;
            ResyncStream();
            if (OnUpdateRoom != null)
                OnUpdateRoom(info);
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

        public virtual void RegisterUser(User registered)
        {
            if (registered.userid == this.userId)
            {
                this.name = registered.name;
                this.currentPoints = registered.points;
                this.JoinedRoom();
            }
            else
            {
                if (this.usersInRoom.ContainsKey(registered.userid))
                    this.usersInRoom.Remove(registered.userid);
                this.usersInRoom.Add(registered.userid, registered);
            }
            if (OnUserRegistered != null)
                OnUserRegistered(registered);
        }
        public virtual void DeregisterUser(User deregistered)
        {
            if (deregistered.userid == this.userId)
            {
                _logger.LogError("What the shit?");
                throw new InvalidOperationException();
            }
            this.usersInRoom.Remove(deregistered.userid);
            if (OnUserDeregistered != null)
                OnUserDeregistered(deregistered);
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
