using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.IO;

namespace Server
{
    struct Bot
    {
        public int bot_id;
        public int user_id;
        public string name;
        public string game;
        public List<byte> bytes;
    }
    class Server
    {
        const int port = 8888;
        const bool ENABLE_PRINT = true;
        static API api = new API();
        static Chat gen_chat = new Chat();
        static TournamentSystem ts = new TournamentSystem();
        static List<Bot> uploading_bots = new List<Bot>();

        static int RegisterBot(int user_id, string name, int game_id = 1)
        {
            api.MakeSQLQueryWithoutResult("DELETE FROM `bots` WHERE `user_id` = " + user_id + " AND `game_id` = " + game_id);
            api.MakeSQLQueryWithoutResult("INSERT INTO `bots` VALUES (NULL," + user_id + "," + game_id + ",\"" + name + "\")");
            var res = api.GetSQLQueryResult("SELECT `bot_id` FROM `bots` WHERE `user_id` = " + user_id + " AND `game_id` = " + game_id);
            Bot t = new Bot();
            t.user_id = user_id;
            t.bot_id = int.Parse(res[0]["bot_id"]);
            t.bytes = new List<byte>();
            uploading_bots.Add(t);
            return t.bot_id;
        }

        static void RunServer()
        {
            api.ConnectToDB();
            // Устанавливаем для сокета локальную конечную точку
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 8888);

            // Создаем сокет Tcp/Ip
            Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Назначаем сокет локальной конечной точке и слушаем входящие сокеты
            try
            {
                sListener.Bind(ipEndPoint);
                sListener.Listen(10);

                // Начинаем слушать соединения
                while (true)
                {
                    if (ENABLE_PRINT) Console.WriteLine("Ожидаем соединение через порт {0}", ipEndPoint);

                    // Программа приостанавливается, ожидая входящее соединение
                    Socket handler = sListener.Accept();
                    string data = "";

                    // Мы дождались клиента, пытающегося с нами соединиться

                    byte[] bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);

                    if (bytes[0] == 1) //если идет загрузка бота
                    {
                        if (ENABLE_PRINT) Console.WriteLine("Дата запроса: " + DateTime.Now.ToString("dd MMMM yyyy | HH:mm:ss"));
                        if (ENABLE_PRINT) Console.Write("Получены бинарные данные\n\n");

                        int bot_id, size;
                        bot_id = BitConverter.ToInt32(bytes, 1);
                        size = BitConverter.ToInt32(bytes, 5);
                        Bot bot = uploading_bots.Find(x => x.bot_id == bot_id);
                        if (bot.bot_id != 0 && bot.user_id != 0) {
                            for (int i = 0; i < size; i++) bot.bytes.Add(bytes[i + 9]);
                            if (size < 1015) //последний пакет
                            {
                                File.WriteAllBytes("../bots/xo/" + bot_id + ".exe", bot.bytes.ToArray());
                                uploading_bots.Remove(bot);
                            }
                        }
                    }
                    else //если запросы API и чата
                    {
                        data += Encoding.UTF8.GetString(bytes, 0, bytesRec);

                        if (ENABLE_PRINT) Console.WriteLine("Дата запроса: " + DateTime.Now.ToString("dd MMMM yyyy | HH:mm:ss"));
                        // Показываем данные на консоли
                        if (ENABLE_PRINT) Console.Write("Полученный текст:\n" + data + "\n\n");


                        //var json = Json.Decode(data);
                        var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                        if (json["type"] == "api")
                        {
                            data = api.MakeAPIQuery(data);
                        }
                        else if (json["type"] == "general_chat")
                        {
                            data = gen_chat.MakeChatQuery(data);
                        }
                        else if (json["type"] == "tours")
                        {
                            data = ts.MakeTourQuery(data);
                        }
                        else if (json["type"] == "bots" && json["action"] == "reg")
                        {
                            if (api.isSession(json["session_id"]))
                            {
                                Bot t = new Bot();
                                t.user_id = int.Parse(json["user_id"]);
                                t.name = json["name"];
                                t.bot_id = RegisterBot(t.user_id, t.name);
                                data = t.bot_id.ToString();
                            }
                        }
                    }

                    byte[] msg = Encoding.UTF8.GetBytes(data);
                    try
                    {
                        handler.Send(msg);
                    }
                    catch
                    {
                        if (ENABLE_PRINT) Console.WriteLine("Клиент разорвал соединение");
                    }
                    if (data.IndexOf("<TheEnd>") > -1) if (ENABLE_PRINT) Console.WriteLine("Сервер завершил соединение с клиентом.");

                    try
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                    }
                    catch
                    {
                        if (ENABLE_PRINT) Console.WriteLine("Подключение было разорвано");
                    }
                }
            }
            catch (Exception ex)
            {
                if (ENABLE_PRINT) Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }

        static void PlayTournaments()
        {
            //проверять текущее время и запускать турниры, если пришло время
            while (true)
            {
                var info = api.GetSQLQueryResult("SELECT `tour_id` FROM `tournaments` WHERE `date_start` >= NOW() AND `status` = 1");
                foreach (var t in info) ts.PlayTournament(int.Parse(t["tour_id"]));
                Thread.Sleep(10000);
            }
        }

        static void Main(string[] args)
        {
            Thread th_server = new Thread(x => RunServer());
            th_server.Start();
            Thread th_tours = new Thread(x => PlayTournaments());
            th_tours.Start();
            while (th_server.ThreadState != ThreadState.Running) Thread.Sleep(2);

            while (true)
            {
                if (th_server.ThreadState != ThreadState.Running) //если поток сервера упал, поднимем его
                {
                    th_server.Abort();
                    while (th_server.ThreadState != ThreadState.Stopped) Thread.Sleep(2);
                    th_server.Start();
                }
                Thread.Sleep(5000);
            }
        }
    }
}
