using System;
using System.Collections.Generic;
using Utilities;

namespace TurntableAPI
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class Handles : Attribute { public string eventName; public Handles(string handlesEvent) { eventName = handlesEvent; } }
    public delegate void Handler(TTClient instance, Dictionary<string, dynamic> eventToHandle);

    static class BotAI
    {
        [Handles("registered")]
        public static void RegisteredUser(TTClient instance, Dictionary<string, dynamic> d)
        {
            Dictionary<string, dynamic> user = d["user"][0];
            if (user["userid"] == instance.userId)
            {
                instance.name = user["name"];
                instance.currentPoints = user["points"];
                instance.JoinedRoom();
            }
        }

        [Handles("deregistered")]
        public static void DeregisteredUser(TTClient instance, Dictionary<string, dynamic> d)
        {
            Dictionary<string, dynamic> user = d["user"][0];
        }
    }
}
