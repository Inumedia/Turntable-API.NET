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
        }

        [Handles("deregistered")]
        public static void DeregisteredUser(TTClient instance, DeRegistered deregisteredUsers)
        {
            for (int i = 0; i < deregisteredUsers.user.Length; ++i)
                instance.DeregisterUser(deregisteredUsers.user[i]);
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
