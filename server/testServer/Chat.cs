using Newtonsoft.Json;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    struct ChatMessage
    {
        public int message_id;
        public string nickname;
        public string text;
    }

    class Chat
    {
        List<ChatMessage> messages = new List<ChatMessage>();
        API api = new API();
        int message_id = 1;

        public int WriteToChat(int user_id, string text)
        {
            string nickname = api.GetNick(user_id);
            if (nickname == "-1" || nickname == "-10") return 0;
            ChatMessage tmp = new ChatMessage();
            tmp.message_id = message_id++;
            tmp.nickname = nickname;
            tmp.text = text;
            messages.Add(tmp);
            //храним только 1000 сообщений
            if (messages.Count > 1000) messages = messages.Skip(100).ToList(); //если >1000, удалим первые 100
            return 1;
        }

        public List<ChatMessage> GetMessagesFrom(int message_id)
        {
            if (messages.Count == 0) return null;
            return messages.Where(x => x.message_id > message_id).ToList();
        }

        public int GetLastMessageID()
        {
            if (messages.Count == 0) return 0;
            return messages.Last().message_id;
        }

        public string MakeChatQuery(string query)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(query);
                int user_id = int.Parse(data["user_id"]);
                string session = data["session_id"];
                if (!api.isSession(session, user_id)) return "-1";
                if (data["action"] == "write") return WriteToChat(user_id, data["text"]).ToString();
                else if (data["action"] == "get_id") return GetLastMessageID().ToString();
                else if (data["action"] == "get")
                {
                    var ans = GetMessagesFrom(int.Parse(data["start"]));
                    if (ans == null) return "";
                    else return JsonConvert.SerializeObject(ans);
                }
                else return "";
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.Message);
                return "-10";
            }
        }
    }
}
