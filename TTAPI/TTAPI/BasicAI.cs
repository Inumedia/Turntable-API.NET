using System;
using TTAPI.Recv;

namespace TTAPI
{
    public static class BasicAI
    {
        [Handles("registered")]
        public static void RegisteredUser(TTClient instance, Registered registeredUsers)
        {
            User first = registeredUsers.user[0];
            if (first.userid == instance.userId)
            {
                instance.name = first.name;
                instance.currentPoints = first.points;
                instance.JoinedRoom();
            }
            else
            {
                if (instance.usersInRoom.ContainsKey(first.userid))
                    instance.usersInRoom.Remove(first.userid);
                instance.usersInRoom.Add(first.userid, first);
            }
        }

        [Handles("deregistered")]
        public static void DeregisteredUser(TTClient instance, DeRegistered deregisteredUsers)
        {
            User user = deregisteredUsers.user[0];
            if (user.userid == instance.userId)
            {
                Console.WriteLine("What the shit?");
                throw new InvalidOperationException();
            }
            instance.usersInRoom.Remove(user.userid);
        }

        [Handles("newsong")]
        public static void NewSong(TTClient instance, NewSong yayNewSong)
        {
            instance.UpdateRoomInformation(yayNewSong.room);
        }
    }
}
