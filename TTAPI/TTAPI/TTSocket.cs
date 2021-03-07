using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TTAPI.Recv;
using TTAPI.Send;

namespace TTAPI
{
    public class TTSocket : IDisposable
    {
        static PresenceManager presenceManager = new PresenceManager();
        static MemoryPool<byte> bufferPool = MemoryPool<byte>.Shared;

        public static Dictionary<string, Handler> handlers;
        public readonly TTClient Client;
        private readonly Uri webSocketPath;
        public ClientWebSocket socket;
        public bool isConnected { get { return socket.State == WebSocketState.Open; } }
        public DateTime LastActivity;
        public DateTime LastHeartbeat;

        Thread networkDispatcher;
        CancellationTokenSource threadCancel;

        ConcurrentDictionary<int, HandlerAndSource> messageCallbacks;
        ConcurrentQueue<string> SendQueue;
        public event Action OnConnected;
        public event Action OnDisconnected;
        EventWaitHandle sendQueued = new EventWaitHandle(false, EventResetMode.ManualReset);
        int sendId = 1;

        static TTSocket()
        {
            handlers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(ass => ass.GetTypes())
                .SelectMany(type => type.GetMethods())
                .Select(method =>
                {
                    var handleAttributes = Attribute.GetCustomAttributes(method, typeof(Handles), false) as Handles[];
                    if (handleAttributes == null || handleAttributes.Length == 0) return null;

                    ParameterInfo[] parameters = method.GetParameters();
                    return handleAttributes.Select(attribute =>
                    {
                        Type actualType = parameters[1].ParameterType;
                        Delegate genericHandler = Delegate.CreateDelegate(typeof(Handler<>).MakeGenericType(actualType), method);
                        Handler hardHandler = new Handler((t, o) =>
                        {
                            if (actualType.IsAssignableFrom(o.GetType()))
                                genericHandler.DynamicInvoke(t, o);
                        });

                        return new KeyValuePair<string, Handler>(attribute.eventName, hardHandler);
                    });
                })
                .Where(handlers => handlers != null)
                .SelectMany(kvps => kvps)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public TTSocket(TTClient client, ChatServerInformation serverInformation)
        {
            Client = client;
            webSocketPath = new Uri(String.Format("wss://{0}:{1}/socket.io/websocket", serverInformation.Address, serverInformation.Port));
            SendQueue = new ConcurrentQueue<string>();
            messageCallbacks = new ConcurrentDictionary<int, HandlerAndSource>();
        }

        public async Task ConnectAsync(CancellationToken cancelToken)
        {
            socket = new ClientWebSocket();
            if (threadCancel != null)
            {
                threadCancel.Cancel();
                threadCancel.Dispose();
            }
            if (networkDispatcher != null) networkDispatcher.Join();

            threadCancel = new CancellationTokenSource();
            // When we cancel, we should trip the send token so there won't be an outstanding task waiting for a signal
            threadCancel.Token.Register(() => sendQueued.Set());
            Console.WriteLine("Connecting {0}", this.Client.userId);
            await socket.ConnectAsync(webSocketPath, cancelToken);
            networkDispatcher = new Thread(NetworkDispatch);
            networkDispatcher.Name = $"{Client.userId}-Dispatcher";
            networkDispatcher.Start();
        }

        private void NetworkDispatch(object obj)
        {
            var ourSocket = socket;
            Task recv = DispatchRecv();
            Task send = DispatchSend();
            while (socket == ourSocket && ourSocket.State != WebSocketState.Closed && ourSocket.State != WebSocketState.CloseReceived && ourSocket.State != WebSocketState.Aborted && !threadCancel.IsCancellationRequested)
            {
                var completed = Task.WhenAny(
                    recv,
                    send
                ).GetAwaiter().GetResult();

                if (ourSocket.State != WebSocketState.Closed && ourSocket.State != WebSocketState.CloseReceived && ourSocket.State != WebSocketState.Aborted)
                {
                    if (completed == recv) recv = DispatchRecv();
                    else if (SendQueue.Count > 0) send = DispatchSend();
                    else send = Task.Run(WaitForSendSignal);
                    completed.Dispose();
                }
            }

            presenceManager.Unsubscribe(this);
            Client._logger.LogWarning("Disconnected {0}", this.Client.userId);
            if (OnDisconnected != null) OnDisconnected();
        }

        void WaitForSendSignal()
        {
            sendQueued.WaitOne();
            sendQueued.Reset();
        }


        private async Task DispatchRecv()
        {
            using var sixteenKb = bufferPool.Rent(16);
            try
            {
                var receiveHeaderResults = await socket.ReceiveAsync(sixteenKb.Memory, threadCancel.Token);

                // If we received no data, just return
                if (receiveHeaderResults.Count == 0) return;

                // Skip the first three bytes as it's '~m~'
                // Followed by the message length
                var totalSize = DetermineMessageLength(sixteenKb.Memory, 3, out Memory<byte> firstChunk);
                var nextChunkSize = totalSize - firstChunk.Length;

                // If there isn't more data, process what we have as a full message
                if (nextChunkSize < 0)
                {
                    await ProcessReceivedData(firstChunk.Slice(0, totalSize));
                    return;
                }

                // Build out a total buffer
                using var remainingMessageMemory = bufferPool.Rent(totalSize);

                // Copy the first chunk into the total buffer
                firstChunk.CopyTo(remainingMessageMemory.Memory);

                // Slicing allows us to direct the writes to that segment of the buffer
                var receivePayloadResults = await socket.ReceiveAsync(
                    remainingMessageMemory.Memory.Slice(firstChunk.Length),
                    threadCancel.Token
                );

                await ProcessReceivedData(remainingMessageMemory.Memory.Slice(0, totalSize));
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private async Task ProcessReceivedData(Memory<byte> payload)
        {
            LastActivity = DateTime.UtcNow;
            var result = Encoding.UTF8.GetString(payload.Span);

            Client._logger.LogDebug("<<< {0}", result);
            // Try/catch here?
            try
            {
                await HandleMessageAsync(result);
            }
            catch (Exception ex)
            {
                Client._logger.LogError(ex, "Error occurred while handling received packet");
            }
        }

        private int DetermineMessageLength(Memory<byte> receivedBuffer, int index, out Memory<byte> remaining)
        {
            if (!MemoryMarshal.TryGetArray(receivedBuffer, out ArraySegment<byte> memArray)) throw new InvalidOperationException("Not sure what's going on here, but that can't be good.");
            int offset = memArray.Offset + index;
            var endingIndex = Array.IndexOf(memArray.Array, (byte)'~', offset);
            int lengthTextLength = endingIndex - offset;
            var lengthText = Encoding.UTF8.GetString(memArray.Array, offset, lengthTextLength);

            // Capture any trailing data
            remaining = receivedBuffer.Slice(index + lengthTextLength);

            // Return the true packet length
            return int.Parse(lengthText) + 3; // Total size doesn't include the payload type header
        }

        public async Task HandleMessageAsync(string eventdata)
        {
            if (eventdata.Equals("~m~no_session"))
            {
                presenceManager.Subscribe(this);
                if (isConnected && OnConnected != null)
                    OnConnected();
                return;
            }

            if (eventdata.StartsWith("~m~~h~"))
            {
                LastHeartbeat = DateTime.UtcNow;
                var heartbeatRaw = $"~m~{eventdata.Length}{eventdata}";

                using var rentedMemory = bufferPool.Rent(heartbeatRaw.Length);
                int sentLength = Encoding.UTF8.GetBytes(heartbeatRaw, rentedMemory.Memory.Span);

                Client._logger.LogDebug(">>> (Heartbeat) {0}", heartbeatRaw);
                // Send the data
                await socket.SendAsync(rentedMemory.Memory, WebSocketMessageType.Text, true, threadCancel.Token);
                return;
            }

            string data = eventdata.Substring(3, eventdata.Length - 3);
            var genericCommand = JsonSerializer.Deserialize<Command>(data);

            // Check if this is in response to a sent command and if there's an explicit type it should deserialize to
            HandlerAndSource has = null;
            Type deserializeTo = null;
            if (messageCallbacks.TryGetValue(genericCommand.msgid, out has))
                deserializeTo = has.source.HandlerSerializeTo;

            // If it doesn't have a specific type set from a handler, parse normally
            if (deserializeTo == null)
                deserializeTo = Command.MapCommandToType(genericCommand.command);

            data = Command.Preprocess(deserializeTo, data);
            if (deserializeTo != null)
                genericCommand = JsonSerializer.Deserialize(data, deserializeTo) as Command;

            if (!string.IsNullOrEmpty(genericCommand.err))
            {
                Client._logger.LogError(genericCommand.err);
                return;
            }

            // Callback route
            if (has != null)
                has.handler(Client, genericCommand);

            if (genericCommand.command != null)
                handlers[genericCommand.command](Client, genericCommand);
        }

        public void Send<K>(K message, Handler callback = null)
            where K : IAPICall
        {
            if (message.msgid == 0) message.msgid = sendId;

            if (callback != null)
                messageCallbacks.TryAdd(sendId, new HandlerAndSource(callback, message));

            string sendingMessage = JsonSerializer.Serialize(message);
            SendRaw(String.Format("~m~{0}~m~{1}", sendingMessage.Length, sendingMessage));

            Interlocked.Increment(ref sendId);
        }
        public void SendRaw(string payload)
        {
            SendQueue.Enqueue(payload);
            sendQueued.Set();
        }

        private async Task DispatchSend()
        {
            if (!SendQueue.TryDequeue(out string send)) return;

            // Build our buffer
            using var rentedMemory = bufferPool.Rent(send.Length);
            int length = Encoding.UTF8.GetBytes(send, rentedMemory.Memory.Span);

            Client._logger.LogDebug(">>> {0}", send);
            // Send the data
            try
            {
                await socket.SendAsync(rentedMemory.Memory, WebSocketMessageType.Text, true, threadCancel.Token);
            }
            catch (Exception ex)
            {
                Client._logger.LogError(ex, "Error sending data to server");
            }
        }

        internal void Close()
        {
            threadCancel?.Cancel();
            networkDispatcher?.Join();
        }

        public void Dispose()
        {
            Close();

            threadCancel?.Dispose();
            sendQueued.Dispose();
        }
    }
}
