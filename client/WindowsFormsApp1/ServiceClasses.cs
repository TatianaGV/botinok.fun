using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class UserData
    {
        static public string login;
        static public string nick;
        static public string session_token;
        static public string pass;
        static public int user_id;
    }

    struct ChatMessage
    {
        public int message_id;
        public string nickname;
        public string text;
    }

    public class ServiceReset
    {
        static public string email;
        static public string key;
    }

    public class Service
    {
        const int port = 8888;
        const string address = "localhost";
        static public Thread chat_th;
        static public Thread info_th;
        public const bool off_Auth = false;
        static public Form1 auth_form;

        // Устанавливаем удаленную точку для сокета
        static public IPHostEntry ipHost = Dns.GetHostEntry(address);
        static public IPAddress ipAddr = ipHost.AddressList[0];
        static public IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

        static public string md5(string input)
        {
            byte[] hash = Encoding.UTF8.GetBytes(input);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hashenc = md5.ComputeHash(hash);
            string result = "";
            foreach (var b in hashenc)
            {
                result += b.ToString("x2");
            }
            return result;
        }

        static public string SendRequestAndGetAnswer(string message)
        {
            // Буфер для входящих данных
            byte[] bytes = new byte[1024];

            // Соединяемся с удаленным устройством

            Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Соединяем сокет с удаленной точкой
            sender.Connect(ipEndPoint);

            //Console.WriteLine("Сокет соединяется с {0} ", sender.RemoteEndPoint.ToString());
            byte[] msg = Encoding.UTF8.GetBytes(message);

            // Отправляем данные через сокет
            int bytesSent = sender.Send(msg);

            // Получаем ответ от сервера
            int bytesRec = sender.Receive(bytes);

            string res = Encoding.UTF8.GetString(bytes, 0, bytesRec);

            /*// Используем рекурсию для неоднократного вызова SendMessageFromSocket()
            if (message.IndexOf("<TheEnd>") == -1)
                SendMessageFromSocket();*/

            // Освобождаем сокет
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();

            return res;
        }
    }
}
