using System;
using TTAPI.Recv;

namespace TTAPI
{
    public static class BasicAI
    {
        [Handles("registered")]
        public static void RegisteredUser(TTClient instance, Registered registeredUsers)
        {
            for (int i = 0; i < registeredUsers.user.Length; ++i)
                instance.RegisterUser(registeredUsers.user[i]);
            /*if (first.userid == instance.userId)
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
            }*/
        }

        [Handles("deregistered")]
        public static void DeregisteredUser(TTClient instance, DeRegistered deregisteredUsers)
        {
            for (int i = 0; i < deregisteredUsers.user.Length; ++i)
                instance.DeregisterUser(deregisteredUsers.user[i]);
            /*User user = deregisteredUsers.user[0];
            if (user.userid == instance.userId)
            {
                Console.WriteLine("What the shit?");
                throw new InvalidOperationException();
            }
            instance.usersInRoom.Remove(user.userid);*/
        }

        [Handles("newsong")]
        public static void NewSong(TTClient instance, SongChange roomInfo)
        {
            instance.UpdateRoomInformation(roomInfo.room);
        }

        [Handles("nosong")]
        public static void NoSong(TTClient instance, SongChange roomInfo)
        {
            instance.UpdateRoomInformation(roomInfo.room);
        }
    }
}
