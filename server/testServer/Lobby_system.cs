using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /*public struct Game_info
    {
        public User user1;
        public User user2;
        public Tournament_Match info; // info about this match in tournament

        public int id; // game id
        public string game_name;
        public bool IsT; // Is it trnmt?
    }

    public struct User
    {
        public int id; // user id
        public string name;
        public score x;
    }

    public struct score
    {
        public int Fh; // first half
        public int Sh; // second half
    }

    public struct Tournament_Match
    {
        public User winner;
        public User loser;
        public int position; // ?position in tournament? (1/1024 etc)
    }

    partial class TournamentSystem
    {
        public Tournament_Match update_position(Game_info x, Tournament_Match y)
        {
            if (x.IsT == false)
            {
                return null;
            }
            else
            {
                if ((x.user1.x.Fh + x.user1.x.Sh) > (x.user2.x.Fh + x.user2.x.Sh))
                {
                    y.winner = x.user1;
                    y.loser = x.user2;
                    if (y.position != 1)
                    {
                        y.position /= 2;
                    }

                    return y;
                }
                else
                {
                    y.winner = x.user2;
                    y.loser = x.user1;
                    if (y.position != 1)
                    {
                        y.position /= 2;
                    }
                    return y;
                }
            }
        }
    }*/
}
