using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleSharp.TL;
using TLSharp.Core;

namespace KrakenClientConsole
{
    class Telegram
    {
        public static void SendMessage(string message)
        {
            Task.Run(async () =>
            {
                await Telegram.SendAsync(message);
            }).GetAwaiter().GetResult();
        }
        static async Task SendAsync(string message)
        {
            //string userNumber = "+35799206710";
            int apiId = 96178;
            var apiHash = "9d3d03922e4c820065691e4dc957a592";

            //string userNumber = "+35796135048";
            //int apiId = 139484;
            //var apiHash = "4e0bd82af97a1c9ec4cc2603ecc93ddc";


            var client = new TelegramClient(apiId, apiHash);
            await client.ConnectAsync();

            // Authentication
            // Uncomment once you need to do so
            //var hash = await client.SendCodeRequestAsync(userNumber);
            //var code = "66063"; // you can change code in debugger
            //var user = await client.MakeAuthAsync(userNumber, hash, code);


            //get available contacts
            var result = await client.GetContactsAsync();

            //find recipient in contacts
            var user1 = result.Users.Where(x => x.GetType() == typeof(TLUser))
                .Cast<TLUser>()
                .FirstOrDefault(x => x.Phone == "35799206710");

            ////send message
            await client.SendMessageAsync(new TLInputPeerUser() { UserId = user1.Id }, message);


            // User should be already authenticated!

            //    var store = new FileSessionStore();
            //    //var client = new TelegramClient(store, "session");
            //    await client.Connect();

            //    Assert.IsTrue(client.IsUserAuthorized());

            //    var res = await client.ImportContactByPhoneNumber(NumberToSendMessage);

            //    Assert.IsNotNull(res);

            //    await client.SendMessage(res.Value, "Test message from TelegramClient");
            //}
        }
    }
}
