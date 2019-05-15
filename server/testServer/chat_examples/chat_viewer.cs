using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Threading;

namespace testClient
{
    struct ChatMessage
    {
        public int message_id;
        public string nickname;
        public string text;
    }

    class chat_viewer
    {
        const int port = 8888;
        const string address = "localhost";
        static IPHostEntry ipHost = Dns.GetHostEntry(address);
        static IPAddress ipAddr = ipHost.AddressList[0];
        static IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

        static string SendRequestAndGetAnswer(string message)
        {
            // Буфер для входящих данных
            byte[] bytes = new byte[1024];

            //Соединяем сокет с удаленной точкой
            Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(ipEndPoint);

            //Console.WriteLine("Сокет соединяется с {0} ", sender.RemoteEndPoint.ToString());
            byte[] msg = Encoding.UTF8.GetBytes(message);

            // Отправляем данные через сокет
            int bytesSent = sender.Send(msg);

            // Получаем ответ от сервера
            int bytesRec = sender.Receive(bytes);

            string ans = Encoding.UTF8.GetString(bytes, 0, bytesRec);
            sender.Close();
            return ans;
        }

        static int getLastMessageID()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "general_chat");
            data.Add("action", "get_id");
            data.Add("user_id", "18");
            data.Add("session_id", "a4c43ad86d5ad2ec3db7297442ce8a2d");
            string answer = "";
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                answer = SendRequestAndGetAnswer(json);
                return int.Parse(answer);
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        static void Main(string[] args)
        {
            bool first = true;
            int mes_id = -1;
            while (true)
            {
                if (first)
                {
                    mes_id = getLastMessageID();
                    first = false;
                }
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("type", "general_chat");
                data.Add("action", "get");
                data.Add("user_id", "18");
                data.Add("session_id", "a4c43ad86d5ad2ec3db7297442ce8a2d");
                data.Add("start", mes_id.ToString());
                string answer = "";
                try
                {
                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    answer = SendRequestAndGetAnswer(json);
                    if (answer == "-10")
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    var messages = JsonConvert.DeserializeObject<List<ChatMessage>>(answer);
                    if (messages != null && messages.Count != 0)
                    {
                        foreach (var x in messages)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write("{0}: ", x.nickname);
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine(x.text);
                        }
                        mes_id = messages.Last().message_id;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Thread.Sleep(1000);
            }
        }
    }
}
