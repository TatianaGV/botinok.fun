using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Web.Helpers;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;
using System.Threading;

namespace Server
{
    class API
    {
        MySqlConnection conn;

        bool isSQLBusy = false;

        public API()
        {
            ConnectToDB();
        }

        public static string md5(string input)
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

        public void ConnectToDB()
        {
            string connStr = "server=users.mmcs.sfedu.ru;user=nikitaodnorob;password=mexmat248;database=nikitaodnorob";
            // создаём объект для подключения к БД
            conn = new MySqlConnection(connStr);
            // устанавливаем соединение с БД
            conn.Open();
        }

        public void MakeSQLQueryWithoutResult(string query)
        {
            while (isSQLBusy) Thread.Sleep(2);
            using (MySqlCommand command = new MySqlCommand(query, conn))
            {
                isSQLBusy = true;
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                }
            }
            isSQLBusy = false;
        }

        public List<Dictionary<string, string>> GetSQLQueryResult(string query)
        {
            List<Dictionary<string, string>> res = new List<Dictionary<string, string>>();
            while (isSQLBusy) Thread.Sleep(2);
            using (MySqlCommand command = new MySqlCommand(query, conn))
            {
                isSQLBusy = true;
                using (MySqlDataReader reader = command.ExecuteReader()) {
                    while (reader.Read())
                    {
                        Dictionary<string, string> d = new Dictionary<string, string>();
                        for (int i = 0; i < reader.FieldCount; i++) d.Add(reader.GetName(i), reader[i].ToString());
                        res.Add(d);
                    }
                }
            }
            isSQLBusy = false;
            return res;
        }

        public int CountSQLQueryResult(string query)
        {
            int cnt = 0;
            while (isSQLBusy) Thread.Sleep(2);
            using (MySqlCommand command = new MySqlCommand(query, conn))
            {
                isSQLBusy = true;
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) cnt++;
                }
            }
            isSQLBusy = false;
            return cnt;
        }

        public bool isSession(string token)
        {
            return GetSQLQueryResult("SELECT `session_id` FROM `sessions` WHERE `token` = \"" + token + "\"").Count == 1;
        }

        public bool isSession(string token, int user_id)
        {
            //return true; //ТОЛЬКО ДЛЯ ТЕСТА!!!
            return GetSQLQueryResult("SELECT `session_id` FROM `sessions` WHERE `token` = \"" + token + 
                "\" AND `user_id` = " + user_id).Count == 1;
        }

        public string Auth(string login, string password)
        {
            string id = "-1";
            string query = "SELECT `user_id` FROM `auth` WHERE (`email` = \"" + login +
                "\" OR `nick` = \"" + login + "\") AND `pass` = \"" + password + "\"";

            //Console.WriteLine(query);
            var ans = GetSQLQueryResult(query);
            if (ans.Count == 1)
            {
                id = ans[0]["user_id"];
                query = "SELECT `session_id` FROM `sessions` WHERE `user_id` = " + id;
                ans = GetSQLQueryResult(query);
                if (ans.Count == 0) //сессии еще нет, нужно сгенерировать токен
                {
                    string token = "";
                    int count = 1;
                    query = "SELECT `session_id` FROM `sessions` WHERE `token` = \"";
                    while (true)
                    {
                        token = md5(login + count.ToString());
                        if (CountSQLQueryResult(query + token + "\"") != 0) count++;
                        else break;
                        if (count == 100) return "-10";
                    }
                    MakeSQLQueryWithoutResult("INSERT INTO `sessions` VALUES (NULL, " + id + ", \"" + token + "\")");
                    return id + " " + token;
                }
                else //сессия уже есть
                {
                    query = "SELECT `token` FROM `sessions` WHERE `user_id` = " + id;
                    ans = GetSQLQueryResult(query);
                    return id + " " + ans[0]["token"];
                    //return "-2";
                }
            }
            else return "-1";
        }

        public string GetNick(int user_id)
        {
            string query = "SELECT `nick` FROM `auth` WHERE `user_id` = " + user_id.ToString();
            var ans = GetSQLQueryResult(query);
            if (ans.Count == 1) return ans[0]["nick"];
            else return "-1";
        }

        public int GetUserID(string login)
        {
            string query = "SELECT `user_id` FROM `auth` WHERE (`email` = \"" + login + "\" OR `nick` = \"" + login + "\")";
            var ans = GetSQLQueryResult(query);
            if (ans.Count == 1) return int.Parse(ans[0]["user_id"]);
            else return -1;
        }

        static bool IsValidEmail(string strIn)
        {
            return Regex.IsMatch(strIn,
                   @"^(?("")("".+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-zA-Z])@))" +
                   @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,6}))$");
        }

        public int Register(string email, string nick, string pass)
        {
            int res = -1;
            if (!IsValidEmail(email)) return -2; //incorrect email
            if (!Regex.IsMatch(nick, "^([0-9a-zA-Z_]){3,20}$")) return -3; //incorrect nick

            if (GetUserID(email) > 0) res = -4; //user with this email is existing
            else if (GetUserID(nick) > 0) res = -5; //user with this nick is existing

            if (res != -1) return res;

            string query = "INSERT INTO `auth` VALUES (NULL, \"" + email + "\", \"" + nick + "\", \"" + pass + "\")";
            MakeSQLQueryWithoutResult(query);

            SendMail("smtp.mail.ru", "botinok.fun@mail.ru", "botfun123", email, "Добро пожаловать!",
                "Добро пожаловать на botinok.fun! Скорее создавайте своих ботов и загружайте к нам! ;)");

            return GetUserID(email);
        }

        public string ChangeNick(string oldnick, string newnick, string pass)
        {
            int uid = GetUserID(oldnick);
            if (uid < 0) return "-1"; //такого ника нет
            if (int.Parse(Auth(oldnick, pass)) < 0) return "-2"; //неверный пароль
            if (GetUserID(newnick) > 0) return "-3"; //новый ник уже есть
            if (!Regex.IsMatch(newnick, "^([0-9a-zA-Z_]){3,20}$")) return "-4";

            MakeSQLQueryWithoutResult("UPDATE `auth` SET `nick` = \"" + newnick + "\" WHERE `nick` = \"" + oldnick + "\"");

            return (GetNick(uid) == newnick) ? "1" : "0";
        }

        public string SendRequestForResetPassword(string email)
        {
            Random rand = new Random();
            int uid = GetUserID(email);
            int count = 0;
            string token = "";
            if (uid < 0) return "-1";
            string query = "SELECT `user_id` FROM `reset_pass` WHERE `user_id` = " + uid.ToString() + " AND `token` = \"";
            while (true)
            {
                token = rand.Next(10000000, 100000000).ToString();
                if (CountSQLQueryResult(query + token + "\"") != 0) continue;
                else break;
                if (count == 10) return "-10";
            }
            MakeSQLQueryWithoutResult("INSERT INTO `reset_pass` VALUES (NULL, " + uid.ToString() + ", \"" + token + "\")");
            SendMail("smtp.mail.ru", "botinok.fun@mail.ru", "botfun123", email, "Сброс пароля",
                "Для сброса пароля введите следующий код:\n\n" + token);
            return "1";
        }

        public string ResetPassword(string email, string newpass, string key)
        {
            int uid = GetUserID(email);
            if (uid < 0) return "-1";

            string query = "SELECT `reset_id` FROM `reset_pass` WHERE `user_id` = " + uid.ToString() + " AND `token` = \"" + key + "\"";

            if (CountSQLQueryResult(query) != 1) return "-10";

            if (newpass != "")
            {
                query = "UPDATE `auth` SET `pass` = \"" + newpass + "\" WHERE `user_id` = " + uid.ToString() + " AND `email` = \"" +
                    email + "\"";
                MakeSQLQueryWithoutResult(query);

                query = "DELETE FROM `reset_pass` WHERE `user_id` = " + uid.ToString() + " AND `token` = \"" + key + "\"";
                MakeSQLQueryWithoutResult(query);

                SendMail("smtp.mail.ru", "botinok.fun@mail.ru", "botfun123", email, "Успешный сброс пароля",
                    "Вы успешно сбросили пароль и можете входить в систему по новому паролю");
            }
            return "1";
        }

        public int SendRequestToFriend(string token, int user_id1, int user_id2) //1 -> 2
        {
            if (!isSession(token, user_id1)) return -1;
            string query;
            if (CountSQLQueryResult("SELECT `friend_id` FROM `friends` WHERE `first_id` = " + user_id1 + " AND `second_id` = " +
                +user_id2) == 0)
            {
                query = "INSERT INTO `friends` VALUES (NULL, " + user_id1.ToString() + "," + user_id2.ToString() + ", TRUE,FALSE,NULL)";
                MakeSQLQueryWithoutResult(query);
            }

            query = "INSERT INTO `events` VALUES (NULL," + user_id1.ToString() + ",1," + user_id2.ToString() + ",NULL,NULL,NULL,NULL)";
            MakeSQLQueryWithoutResult(query);

            return 1;
        }

        public string GetRequestsForFriend(string token, int user_id)
        {
            if (!isSession(token, user_id)) return "-1";
            string res = "";
            string query = "SELECT * FROM `events` WHERE `event_type` = 1 AND `param1` = " + user_id;
            var ans = GetSQLQueryResult(query);
            if (ans.Count == 0) return "0";
            foreach (var x in ans) res += x["user_id"] + " ";
            string last_event_id = ans[ans.Count - 1]["event_id"];
            MakeSQLQueryWithoutResult("DELETE FROM `events` WHERE `event_type` = 1 AND `event_id` <= " + last_event_id +
                " AND `param1` = " + user_id);
            return res;
        }

        public string GetMatches(string token, int user_id)
        {
            if (!isSession(token)) return "-1";
            string query = "SELECT * FROM `matches` WHERE `user1_id` = " + user_id + " OR `user2_id` = " + user_id + " LIMIT 10";
            var ans = GetSQLQueryResult(query);
            if (ans.Count == 0) return "0";
            List<Profile.Matches> matches = new List<Profile.Matches>();
            foreach (var x in ans)
            {
                var m = new Profile.Matches();
                m.date = DateTime.Parse(x["date"]);
                m.name = x["name"];
                matches.Add(m);
            }
            return JsonConvert.SerializeObject(matches);
        }

        public string ApplyRequestToFriend(string token, int user_id1, int user_id2)
        {
            if (!isSession(token, user_id2)) return "-1";
            if (CountSQLQueryResult("SELECT `friend_id` FROM `friends` WHERE `first_id` = " + user_id1 + " AND `second_id` = " +
                +user_id2) == 1)
            {
                MakeSQLQueryWithoutResult("UPDATE `friends` SET `confirm2` = 1 WHERE `first_id` = " + user_id1 + " AND `second_id` = " +
                +user_id2);
                return "1";
            }
            else return "0";
        }

        public string GetFriends(string token, int user_id)
        {
            if (!isSession(token)) return "-1";
            var ans = GetSQLQueryResult("SELECT `first_id`, `second_id` FROM `friends` WHERE `confirm1` = 1 AND `confirm2` = 1 AND " +
                "(`first_id` = " + user_id + " OR `second_id` = " + user_id + ")");
            if (ans.Count == 0) return "0";
            List<Profile.Friend> fr = new List<Profile.Friend>();
            foreach (var x in ans)
            {
                Profile.Friend t = new Profile.Friend();
                int id1 = int.Parse(x["first_id"]); int id2 = int.Parse(x["second_id"]);
                t.id = (id1 == user_id) ? id2 : id1;
                t.name = GetNick(t.id);
                fr.Add(t);
            }
            return JsonConvert.SerializeObject(fr);
        }

        public string GetCurrentTournaments(string token) { 
            if (!isSession(token)) return "-1";
            var ans = GetSQLQueryResult("SELECT *` FROM `tournaments`");
            if (ans.Count == 0) return "0";
            List<Profile.Tournaments> tr = new List<Profile.Tournaments>();
            foreach (var x in ans)
            {
                Profile.Tournaments t = new Profile.Tournaments();
                t.id = int.Parse(x["tour_id"]);
                t.name = x["name"];
                t.status = int.Parse(x["status"]);
                t.min_lvl = int.Parse(x["min_lvl"]);
                t.prvt = bool.Parse(x["private"]);
                t.admin_id = int.Parse(x["admin_id"]);
                tr.Add(t);
            }
            return JsonConvert.SerializeObject(tr);
        }

        public string GetAchievements(string token, int user_id)
        {
            if (!isSession(token)) return "-1";
            var ans = GetSQLQueryResult("SELECT * FROM `achievements` WHERE `user_id` = " + user_id);
            if (ans.Count == 0) return "0";
            List<Profile.Achievements> ac = new List<Profile.Achievements>();
            foreach (var x in ans)
            {
                Profile.Achievements t = new Profile.Achievements();
                t.id = int.Parse(x["ach_id"]);
                t.name = x["name"];
                t.date = DateTime.Parse(x["date"]);
                ac.Add(t);
            }
            return JsonConvert.SerializeObject(ac);
        }

        public string GetUserInfo(string token, int user_id)
        {
            if (!isSession(token)) return "-1";
            var ans = GetSQLQueryResult("SELECT * FROM `user_info` WHERE `user_id` = " + user_id);
            if (ans.Count != 1) return "0";
            List<Profile.UserInfo> ac = new List<Profile.UserInfo>();
            foreach (var x in ans)
            {
                Profile.UserInfo t = new Profile.UserInfo();
                t.id = user_id;
                t.name = x["name"];
                t.fname = x["fname"];
                t.ffname = x["ffname"];
                t.sex = x["sex"];
                t.info = x["info"];
                t.status = char.Parse(x["status"]);
                t.lvl = int.Parse(x["level"]);
                t.warnings = float.Parse(x["warnings"]);
                ac.Add(t);
            }
            return JsonConvert.SerializeObject(ac);
        }

        public string MakeAPIQuery(string json_data)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json_data);
                if (data["action"] == "auth") return Auth(data["login"], data["pass"]).ToString(); //doc
                else if (data["action"] == "get_nick") return GetNick(int.Parse(data["user_id"])); //doc
                else if (data["action"] == "get_id") return GetUserID(data["login"]).ToString(); //doc
                else if (data["action"] == "reg") return Register(data["email"], data["nick"], data["pass"]).ToString(); //doc
                else if (data["action"] == "change_nick") return ChangeNick(data["oldnick"], data["newnick"], data["pass"]); //doc
                else if (data["action"] == "send_req_reset_pass") return SendRequestForResetPassword(data["email"]); //doc
                else if (data["action"] == "reset_pass") return ResetPassword(data["email"], data["newpass"], data["key"]); //doc
                else if (data["action"] == "send_to_friend") return SendRequestToFriend(data["session"],
                    int.Parse(data["user1"]), int.Parse(data["user2"])).ToString(); //doc
                else if (data["action"] == "get_to_friend") return GetRequestsForFriend(data["session"], int.Parse(data["user_id"]));
                else if (data["action"] == "get_matches") return GetMatches(data["session"], int.Parse(data["user_id"]));
                else if (data["action"] == "apply_to_friend") return ApplyRequestToFriend(data["session"],
                    int.Parse(data["user1"]), int.Parse(data["user2"]));
                else if (data["action"] == "get_friends") return GetFriends(data["session"], int.Parse(data["user_id"]));
                else if (data["action"] == "get_tours") return GetCurrentTournaments(data["session"]);
                else if (data["action"] == "get_achievs") return GetAchievements(data["session"], int.Parse(data["user_id"]));
                else if (data["action"] == "get_info") return GetUserInfo(data["session"], int.Parse(data["user_id"]));
                else return "";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "-10";
            }
        }

        public void SendMail(string smtpServer, string from, string password, string mailto, string caption, string message, string attachFile = null)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(from);
                mail.To.Add(new MailAddress(mailto));
                mail.Subject = caption;
                mail.Body = message;
                if (!string.IsNullOrEmpty(attachFile)) mail.Attachments.Add(new Attachment(attachFile));
                SmtpClient client = new SmtpClient();
                client.Host = smtpServer;
                client.Port = 587;
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(from.Split('@')[0], password);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Send(mail);
                mail.Dispose();
            }
            catch
            {
                //throw new Exception("Mail.Send: " + e.Message);
            }
        }

        public void CloseDB()
        {
            conn.Close();
        }
    }

    class Program
    {
        static void Main2(string[] args) //for test
        {
        }
    }
}
