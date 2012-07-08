using System;
using WebSocketSharp;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace TurntableAPI
{
    public class TTClient
    {
        static JavaScriptSerializer jss;
        static Dictionary<string, Handler> messageHandlers;

        public WebSocket webSocket;
        WebClient webClient;
        public Dictionary<string, Handler> specifiedHandlers;

        public string userId,
                authId,
                roomId,
                name,
                server,
                clientId,
                currentStatus,
                lastHeartbeat;
        public int server_port,
                currentPoints,
                msgId;

        public bool isMaster;
        public bool isConnected { get { return webSocket.ReadyState == WsState.OPEN; } }

        DateTime lastActivity;

        Dictionary<int, Handler> messageCallbacks;

        static TTClient()
        {
            jss = new JavaScriptSerializer();
            messageHandlers = new Dictionary<string, Handler>();
            List<Tuple<Handles, Handler>> handlers = Utilities.Reflector.FindAllMethods<Handles, Handler>();
            foreach (Tuple<Handles, Handler> handlerCallback in handlers)
                messageHandlers.Add(handlerCallback.Item1.eventName, handlerCallback.Item2);
        }

        public TTClient(string userid, string authid, string roomid, bool masterBot = false)
        {
            isMaster = masterBot;
            webClient = new WebClient();
            specifiedHandlers = new Dictionary<string, Handler>();

            this.userId = userid;
            this.authId = authid;
            this.roomId = roomid;
        }

        public virtual void Connect(Action<WebSocket> preprocess = null)
        {
            this.currentStatus = "available";
            this.clientId = String.Format("{0}-0.59633534294921572", DateTime.Now.ToBinary().ToString());
            Select_Server();
            webSocket = new WebSocket(String.Format("ws://{0}:{1}/socket.io/websocket", server, server_port));

            if (preprocess != null) preprocess(webSocket);

            messageCallbacks = new Dictionary<int, Handler>();
            MessageEventHandler no_session = null;
            no_session = new MessageEventHandler((o, eventdata) =>
            {
                if (!eventdata.Equals("~m~10~m~no_session"))
                {
                    webSocket.OnMessage -= no_session;
                    return;
                }
                StartAuthentication((i, res) =>
                {
                    if (webSocket.ReadyState != WsState.OPEN)
                    {
                        Console.WriteLine("Couldn't authenticate.");
                        return;
                    }
                    var joinRoom = String.Format("{{api: 'room.register', roomid: '{0}'}}", roomId);
                    Send(joinRoom);
                });
            });
            webSocket.OnMessage += no_session;
            webSocket.OnMessage += new MessageEventHandler(HandleMessage);
            webSocket.Connect();

            Initialize();
        }

        public virtual void Initialize() { }
        public virtual void JoinedRoom() { }

        public virtual void HandleMessage(object sender, string eventdata)
        {
            if (Regex.Match(eventdata, "~h~[0-9]+").Length > 0)
            {
                SendRaw(eventdata);
                UpdatePresence();
                return;
            }

            lastActivity = DateTime.Now;

            string[] eventDataSplit = eventdata.Split(new char[] { '~' }, 5);
            int length = int.Parse(eventDataSplit[2]);
            string data = eventDataSplit[4];
            try
            {
                var response = jss.Deserialize<dynamic>(data);
                if (response.ContainsKey("msgid") && messageCallbacks.ContainsKey(response["msgid"]))
                    messageCallbacks[response["msgid"]](this, response);
                if (response.ContainsKey("command") && messageHandlers.ContainsKey(response["command"]))
                    messageHandlers[response["command"]](this, response);
                if (response.ContainsKey("command") && specifiedHandlers.ContainsKey(response["command"]))
                    specifiedHandlers[response["command"]](this, response);
            }
            catch (ArgumentException) { }
        }

        public void Select_Server()
        {
            string resp = webClient.DownloadString(String.Format("http://turntable.fm/api/room.which_chatserver?roomid={0}", roomId));
            dynamic respJSON = jss.Deserialize<dynamic>(resp);
            if (respJSON[0])
            {
                server = respJSON[1]["chatserver"][0];
                server_port = respJSON[1]["chatserver"][1];
            }
        }

        public void ExplicitlyHandle(string eventName, Handler callback)
        {
            this.specifiedHandlers.Add(eventName, callback);
        }

        public void StartAuthentication(Handler callback = null)
        {
            Send("{api: 'user.authenticate'}", callback);
        }
        public void GetFanOf(Handler callback = null)
        {
            Send("{api: 'user.get_fan_of'}", callback);
        }
        public void Speak(string msg, Handler callback = null)
        {
            Send(String.Format("{{api: 'room.speak', roomid: '{0}', text: '{1}'}}", roomId, msg.ToString()));
        }
        public void RoomInfo(bool extendedInfo = false, Handler callback = null)
        {
            Send(String.Format("{{api: 'room.info', roomid: '{0}', extended: {1}}}", roomId, extendedInfo.ToString().ToLower()), callback);
        }
        public void ChangeName(string name, Handler callback = null)
        {
            Send(String.Format("{{api: 'user.modify', name: '{0}'}}", name), callback);
        }
        public void ModifyLaptop(string laptop = "chrome", Handler callback = null)
        {
            Send(String.Format("{{ api: 'user.modify', laptop: '{0}' }}", laptop), callback);
        }
        public void SetAvatar(int avatarId, Handler callback = null)
        {
            Send(String.Format("{{ api: 'user.set_avatar', avatarid: {0} }}", avatarId), callback);
        }
        public void BecomeFan(string userId, Handler callback = null)
        {
            Send(String.Format("{{ api: 'user.become_fan', djid: '{0}' }}", userId), callback);
        }
        public void RemoveFan(string userId, Handler callback = null)
        {
            Send(String.Format("{{ api: 'user.remove_fan', djid: '{0}' }}", userId), callback);
        }
        public void AddDJ(Handler callback = null)
        {
            Send(String.Format("{{ api: 'room.add_dj', roomid: '{0}' }}", roomId), callback);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId">If null, is the bot's userid.</param>
        /// <param name="callback"></param>
        public void RemoveDJ(string userId = null, Handler callback = null)
        {
            Send(String.Format("{{ api: 'room.rem_dj', roomid: {0}{1} }}", roomId, userId != null ? String.Format(", djid: {0}", userId) : ""));
        }
        public void StopSong(Handler callback = null)
        {
            Send(String.Format("{{ api: 'room.stop_song', roomid: '{0}' }}", roomId), callback);
        }
        public void BootUser(string userId, string reason = null, Handler callback = null)
        {
            Send(String.Format("{{ api: 'room.boot_user', roomid: '{0}', target_userid: '{1}', reason: '{2}' }}", roomId, userId, reason), callback);
        }
        public void GetFavorites(Handler callback = null)
        {
            Send("{ api: 'room.get_favorites' }", callback);
        }
        public void AddFavorite(string roomId, Handler callback = null)
        {
            Send(String.Format("{{ api: 'room.add_favorite', roomid: '{0}' }}", roomId), callback);
        }
        public void RemoveFavorite(string roomId, Handler callback = null)
        {
            Send(String.Format("{{ api: 'room.rem_favorite', roomid: '{0}' }}", roomId), callback);
        }

        public void SetStatus(string status)
        {
            this.currentStatus = status;
            UpdatePresence();
        }

        public void Send(dynamic message, Handler callback = null, bool needsProcessing = true)
        {
            bool isString = message.GetType() == typeof(String);
            if (isString)
                message = jss.Deserialize<dynamic>(message);

            if (needsProcessing)
            {
                message.Add("msgid", msgId); //message.msgid = msgId;
                message.Add("clientid", clientId); //message.clientid = clientId;
                if (!message.ContainsKey("userid"))
                    message.Add("userid", userId);
                message.Add("userauth", authId); ///.userauth = authId;
            }
            string sendingMessage = jss.Serialize(message);
            webSocket.Send(String.Format("~m~{0}~m~{1}", sendingMessage.Length, sendingMessage));

            if (callback != null)
                messageCallbacks.Add(msgId, callback);

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
            Send(String.Format("{{ api: 'presence.update', status: '{0}' }}", this.currentStatus), callback);
        }
    }
}
