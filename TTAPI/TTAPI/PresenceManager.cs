using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace TTAPI
{
    public class PresenceManager
    {
        ConcurrentBag<TTSocket> pendingAdds = new ConcurrentBag<TTSocket>();
        ConcurrentBag<TTSocket> pendingRemoves = new ConcurrentBag<TTSocket>();
        Thread manager;
        Queue<TTSocket> subscribers = new Queue<TTSocket>();
        public PresenceManager()
        {
            manager = new Thread(DispatchUpdates);
            manager.Name = "PresenceManager";
            manager.Start();
        }

        private void DispatchUpdates(object obj)
        {
            List<TTSocket> removes = new List<TTSocket>();
            while(true)
            {
                while (pendingAdds.TryTake(out var add))
                    subscribers.Enqueue(add);

                while (pendingRemoves.TryTake(out var pendingRemove)) removes.Add(pendingRemove);

                while (subscribers.TryPeek(out var peek) && peek.Client.NextPresenceUpdateAt < DateTime.UtcNow)
                {
                    var update = subscribers.Dequeue();
                    
                    // If we have a pending remove for this, mark it as processed and don't re-queue it
                    if (removes.Contains(update))
                    {
                        removes.Remove(update);
                        continue;
                    }

                    // Update presence and re-queue it at the back of the line
                    update.Client.UpdatePresence();
                    subscribers.Enqueue(update);
                }

                Thread.Sleep(1);
            }
        }

        public void Subscribe(TTSocket socket)
        {
            socket.Client.UpdatePresence();
            pendingAdds.Add(socket);
        }

        public void Unsubscribe(TTSocket socket)
        {
            pendingRemoves.Add(socket);
        }
    }
}
