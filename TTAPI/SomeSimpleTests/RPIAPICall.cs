using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TTAPI;
using TTAPI.Send;
using TTAPI.Recv;

namespace SomeSimpleTests
{
    [TestClass]
    public class RPIAPICall
    {
        [TestMethod]
        public void TestMethod1()
        {
            APICall request = new GetUserID("Inumedia");
            TTWebInterface.Request(request);
            //TTAPI.TTWebInterface.Request(new TTAPI.Send.APICall("http://turntable.fm/api/user.get_id?name=Inumedia"
        }

        [TestMethod]
        public void TestingCasting()
        {
            APICall request = new GetUserID("Inumedia");
            UserID results = TTWebInterface.Request<UserID>(request);
        }

        [TestMethod]
        public void AuthTest()
        {
            APICall auth = new EmailLogin("inumediaclubwk@inumedia.net", "P0k3mon1");
            string results = TTWebInterface.Request(auth, "GET");
            Console.WriteLine(results);
        }
    }
}
