using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Profile
    {
        public struct Tournaments //+
        {
            public int id; // id of this Trnmt in bd
            public string name;
            public DateTime date;
            public int status; // Status of this T (close/future)
            public bool prvt; // Is it tournament private?
            public int min_lvl; // min lvl for join
            public int admin_id; // judge of this trmnt
        }

        public struct Social  
        {
            string vk;
            string facebook;
            string Telegram;
            string twitter;
        }

        public struct Matches //+
        {
            public string name; // Exmpl: Hidanio vs Jesus [hyperlink]
            public DateTime date;
            public int user1_id, user2_id;
            public string DwnlLog; // Download log file of this match
        }

        public struct Achievements //+
        {
            public int id; // id of this achv (for picture and bd)
            public string name;
            public DateTime date;
        }

        public struct Friend //+
        {
            public int id; // for bd
            public string name;
        }

        public struct UserInfo
        {
            public int id; // his id in bd
            public string name;
            public string fname;
            public string ffname;
            public string sex;
            public string info;

            public char status; // user rights
            public float warnings; // in %
            public int lvl; // 1-10
        }

        public struct User
        {
            UserInfo info;

            List<Achievements> achivements;
            List<Matches> matches;
            List<Social> social;
            List<Friend> friends;
            List<Tournaments> tournaments;
            
        }

    }
}
