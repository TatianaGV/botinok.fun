using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Player //участник турнира
    {
        public int user_id;
        public int tour_id;
        public int position;
    }

    class TournamentStage
    {
        public int position; //     1/1024 etc
        public List<Player> players = new List<Player>();
    }

    class TournamentMatch
    {
        public int match_number;
        public int user1_id;
        public int user2_id;
        public int winner_id;
    }

    enum TournamentStatus { Registration = 1, Annonced, Playing, Finished, Canceled };

    class Tournament
    {
        public int tour_id;
        public int game_id; //по какой игре турнир?
        public string name;
        public List<TournamentStage> stages = new List<TournamentStage>();
        public List<Player> players = new List<Player>();
        public int players_count;
        public TournamentStatus status;
        public DateTime start;
        API api = new API();
        TournamentSystem tm = new TournamentSystem();
        MatchSystem ms = new MatchSystem();
        public int current_stage; //текущая стадия
        int current_match = 1; //номер текущего матча

        public Tournament(int tour_id) {
            tm.ChangeStatus(tour_id, TournamentStatus.Playing);
            this.tour_id = tour_id;
            //load info from DB
            var info = api.GetSQLQueryResult("SELECT * FROM `tournaments` WHERE `tour_id` = " + tour_id);
            if (info.Count != 1) return;
            game_id = int.Parse(info[0]["game_id"]);
            name = info[0]["name"];
            //players_count = int.Parse(info[0]["players_cnt"]);
            status = (TournamentStatus) int.Parse(info[0]["status"]);
            start = DateTime.Parse(info[0]["date_start"]);

            //create first stage
            stages.Add(new TournamentStage());

            //load players
            var players = api.GetSQLQueryResult("SELECT `user_id` FROM `tour_req` WHERE `tour_id` = " + tour_id);
            players_count = players.Count;

            int pos = (int)Math.Pow(2, Math.Ceiling(Math.Log(players_count) / Math.Log(2)) - 1);
            current_stage = pos;

            foreach (var player in players)
            {
                var pl = new Player();
                pl.tour_id = tour_id;
                pl.user_id = int.Parse(player["user_id"]);
                stages[0].players.Add(pl);
                this.players.Add(pl);
                stages[0].position = pos;
            }

            Console.WriteLine("Начало турнира");
        }

        string GetBotByUserID(int user_id)
        {
            var ans = api.GetSQLQueryResult("SELECT `bot_id` FROM `bots` WHERE `user_id` = " + user_id + " AND `game_id` = " + game_id);
            return "../bots/xo/" + ans[0]["bot_id"] + ".exe"; //load from DB!!!!
        }

        //Перемешивает игроков в списке
        public List<Player> RandomMixPlayers(List<Player> data)
        {
            Random random = new Random();
            for (int k = data.Count - 1; k >= 1; k--)
            {
                int j = random.Next(k + 1);
                var temp = data[j]; // обменять значения data[j] и data[k]
                data[j] = data[k];
                data[k] = temp;
            }
            return data;
        }

        //Разбивает игроков на пары
        List<TournamentMatch> SplitToPairs(List<Player> data)
        {
            List<TournamentMatch> matches = new List<TournamentMatch>();
            for (int i = 0; i < data.Count; i += 2)
            {
                TournamentMatch m = new TournamentMatch();
                m.user1_id = data[i].user_id;
                m.user2_id = (i + 1 < data.Count) ? data[i + 1].user_id : -1;
                m.match_number = current_match;
                current_match++;
                matches.Add(m);
            }
            return matches;
        }

        //Обновляет статистику и рейтинги после каждого матча
        void UpdateStat(int user1_id, int user2_id, int winner_id, bool isTechWin)
        {
            //!!!!!!!!!!!!!!!!!!!!!!!!
        } //TODO !!!!!!!!!!!!!!!

        //Играет все матчи из списка
        public void PlayMatches(ref List<TournamentMatch> matches)
        {
            Game game = null;
            if (game_id == 1) game = new XO(); //крестики-нолики
            //...

            foreach (var x in matches)
            {
                GameStatus st;
                bool isTechWin = false;
                int win_id = 0;
                do
                {
                    if (x.user1_id == -1)
                    {
                        Console.WriteLine("Играем матч " + x.match_number + " (у игрока " + api.GetNick(x.user2_id) + " нет соперника)");
                        st = GameStatus.Win2;
                        x.winner_id = x.user2_id;
                        isTechWin = true;
                    }
                    else if (x.user2_id == -1)
                    {
                        Console.WriteLine("Играем матч " + x.match_number + " (у игрока " + api.GetNick(x.user1_id) + " нет соперника)");
                        st = GameStatus.Win1;
                        x.winner_id = x.user1_id;
                        isTechWin = true;
                    }
                    else
                    {
                        Console.WriteLine("Играем матч " + x.match_number + " (users " + api.GetNick(x.user1_id) +
                            " vs " + api.GetNick(x.user2_id) + ")");
                        st = ms.StartMatch(game, GetBotByUserID(x.user1_id), GetBotByUserID(x.user2_id));
                        if (st == GameStatus.IncorrectMove2 || st == GameStatus.ProcessError2 || st == GameStatus.TimeIsOver2 ||
                            st == GameStatus.Win1) win_id = x.user1_id;
                        else win_id = x.user2_id;
                        x.winner_id = win_id;
                        Console.WriteLine("Выиграл " + api.GetNick(x.winner_id));
                    }
                } while (st == GameStatus.Draw || st == GameStatus.NotStarted); //играем, пока кто-то не выиграет
                UpdateStat(x.user1_id, x.user2_id, x.winner_id, isTechWin);
            }
        }

        //Получает список победителей матчей
        public List<int> GetWinnersFromMatchList(List<TournamentMatch> matches)
        {
            return matches.Select(x => x.winner_id).ToList();
        }

        //Формирует новую стадию по списку победителей прошлой стадии
        public TournamentStage MakeStage(List<int> players)
        {
            //TournamentStage cur_stage = stages.Where(x => x.position == current_stage).First();
            current_stage /= 2;
            TournamentStage stage = new TournamentStage();
            stage.position = current_stage;
            stage.players = new List<Player>();
            foreach (var x in players)
            {
                Player pl = new Player();
                pl.tour_id = tour_id;
                pl.user_id = x;
                stage.players.Add(pl);
            }
            return stage;
        }

        //Записать данные о раунде в БД
        public void WriteStageInfoToDB()
        {

        } //TODO!!!!!!!!!!!!!!

        //Играет все матчи текущей стадии и формирует новую стадию
        public TournamentStage PlayCurrentStage()
        {
            Console.WriteLine("Играем стадию 1/" + current_stage + "\n");
            TournamentStage cur_stage = stages.Find(x => x.position == current_stage);
            cur_stage.players.ForEach(x => x.position = current_stage);
            this.players.ForEach(x => { if (cur_stage.players.Select(y => y.user_id).Contains(x.user_id)) x.position = current_stage; });
            if (cur_stage.position == 0) return cur_stage; //турнир окончен

            cur_stage.players = RandomMixPlayers(cur_stage.players);
            List<TournamentMatch> matches = SplitToPairs(cur_stage.players);
            //cur_stage.players.ForEach(x => { if (x.user_id == m.user1_id || x.user_id == m.user2_id) x.match_number = current_match; });
            PlayMatches(ref matches);

            List<int> winners = GetWinnersFromMatchList(matches);

            WriteStageInfoToDB();
            Console.WriteLine("Стадия 1/" + current_stage + " сыграна\n============================\n");
            return MakeStage(winners);
        }
    }

    class TournamentSystem
    {
        API api = new API();

        string DateToMySQLFormat(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:ii:cc");
        }

        public string AnnonceTournament(string name, int game_id, DateTime date)
        {
            if (date < DateTime.Now) return "-1";
            if (api.CountSQLQueryResult("SELECT `tour_id` FROM `tournaments` WHERE `name` = \"" + name + "\"") == 0)
            {
                string q = "INSERT INTO `tournaments` VALUES (NULL," + game_id + ",\"" + name + "\"," +
                    2 + ", 0, \"" + DateToMySQLFormat(date) + "\")";
                api.MakeSQLQueryWithoutResult(q);
                return (api.CountSQLQueryResult("SELECT `tour_id` FROM `tournaments` WHERE `name` = \"" + name + "\"") == 1) ? "1" : "0";
            }
            return "-2";
        }

        public string AddUserToTournament(int user_id, int tour_id)
        {
            if (api.CountSQLQueryResult("SELECT `req_id` FROM `tour_req` WHERE `tour_id` = " + tour_id + 
                " AND `user_id` = " + user_id) > 0) return "-2"; //уже зарегистрирован
            int status = int.Parse(api.GetSQLQueryResult("SELECT `status` FROM `tournaments` WHERE `tour_id` = " + tour_id)[0]["status"]);
            if (status == 1)
            {
                api.MakeSQLQueryWithoutResult("INSERT INTO `tour_req` VALUES (NULL, " + tour_id + ", " + user_id + ")");
                api.MakeSQLQueryWithoutResult("UPDATE `tournaments` SET `players_cnt` = `players_cnt` + 1 WHERE `tour_id` = " + tour_id);
                return "1"; //ОК
            }
            else return "0"; //регистрация закрыта
        }

        public string RemoveUserFromTournament(int user_id, int tour_id)
        {
            if (api.CountSQLQueryResult("SELECT `req_id` FROM `tour_req` WHERE `tour_id` = " + tour_id +
                " AND `user_id` = " + user_id) == 0) return "-2"; //не зарегистрирован
            int status = int.Parse(api.GetSQLQueryResult("SELECT `status` FROM `tournaments` WHERE `tour_id` = " + tour_id)[0]["status"]);
            if (status == 1)
            {
                api.MakeSQLQueryWithoutResult("DELETE FROM `tour_req` WHERE `user_id` = " + user_id + " AND `tour_id` = " + tour_id);
                api.MakeSQLQueryWithoutResult("UPDATE `tournaments` SET `players_cnt` = `players_cnt` - 1 WHERE `tour_id` = " + tour_id);
                return "1"; //ОК
            }
            else return "0"; //регистрация закрыта
        }

        public string CheckRegistration(int user_id, int tour_id)
        {
            if (api.CountSQLQueryResult("SELECT `req_id` FROM `tour_req` WHERE `tour_id` = " + tour_id +
                " AND `user_id` = " + user_id) > 0) return "1";
            else return "0";
        }

        public bool isTournamentExist(int tour_id)
        {
            return (api.CountSQLQueryResult("SELECT `status` FROM `tournaments` WHERE `tour_id` = " + tour_id) == 1);
        }

        public void ChangeStatus(int tour_id, TournamentStatus status)
        {
            int st = 0;
            switch (status)
            {
                case TournamentStatus.Registration:
                    st = 1;
                    break;
                case TournamentStatus.Annonced:
                    st = 2;
                    break;
                case TournamentStatus.Playing:
                    st = 3;
                    break;
                case TournamentStatus.Finished:
                    st = 4;
                    break;
                case TournamentStatus.Canceled:
                    st = 5;
                    break;
            }
            api.MakeSQLQueryWithoutResult("UPDATE `tournaments` SET `status` = " + st + " WHERE `tour_id` = " + tour_id);
        }

        public struct Tour
        {
            public int tour_id;
            public string name, game, status, players, date;
        }

        string getGameName(int game_id)
        {
            var res = api.GetSQLQueryResult("SELECT `name` FROM `games` WHERE `game_id` = " + game_id);
            if (res.Count == 1) return res[0]["name"];
            else return "";
        }

        public string GetTournaments()
        {
            List<Tour> res = new List<Tour>();
            var tour = api.GetSQLQueryResult("SELECT * FROM `tournaments` WHERE `status` <> 4 AND `status` <> 5");
            foreach (var x in tour)
            {
                Tour tmp = new Tour();
                tmp.tour_id = int.Parse(x["tour_id"]);
                tmp.game = getGameName(int.Parse(x["game_id"]));
                tmp.name = x["name"];
                tmp.status = x["status"];
                tmp.players = x["players_cnt"];
                tmp.date = x["date_start"];
                res.Add(tmp);
            }
            return JsonConvert.SerializeObject(res);
        }

        public string GetBots(string user_id)
        {
            List<Bot> res = new List<Bot>();
            var tour = api.GetSQLQueryResult("SELECT * FROM `bots` WHERE `user_id` = " + user_id);
            foreach (var x in tour)
            {
                Bot tmp = new Bot();
                tmp.bot_id = int.Parse(x["bot_id"]);
                tmp.name = x["name"];
                tmp.game = getGameName(int.Parse(x["game_id"]));
                res.Add(tmp);
            }
            return JsonConvert.SerializeObject(res);
        }

        //сыграть турнир
        public void PlayTournament(int tour_id)
        {
            ChangeStatus(tour_id, TournamentStatus.Playing);
            var t = new Tournament(tour_id);
            do
            {
                t.stages.Add(t.PlayCurrentStage());
            } while (t.stages.Find(x => x.position == t.current_stage).players.Count != 1); //играем, пока не будет победителя
            //подправить места
            foreach (var tt in t.players)
            {
                if (tt.user_id == t.stages.Last().players[0].user_id) tt.position = 1;
                else tt.position *= 2;
            }
            WriteDataToDB(t);
            ChangeStatus(tour_id, TournamentStatus.Finished);
        }

        int cmp(Player x, Player y)
        {
            if (x.position < y.position) return -1;
            else if (x.position > y.position) return 1;
            else return 0;
        }

        //записать данные о турнире в БД
        public void WriteDataToDB(Tournament tour)
        {
            Console.WriteLine("============================\nПобедитель турнира: " + api.GetNick(tour.stages.Last().players[0].user_id));
            Console.WriteLine("=====================\nИтоговые места:\n======================");
            tour.players.Sort(cmp);
            foreach (var x in tour.players)
            {
                Console.WriteLine(x.position + "\t\t" + api.GetNick(x.user_id));
            }
        }

        public string MakeTourQuery(string query)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(query);
                int user_id = int.Parse(data["user_id"]);
                string session = data["session_id"];
                if (!api.isSession(session, user_id)) return "-1";

                if (data["action"] == "annonce") return AnnonceTournament(data["name"], int.Parse(data["game_id"]),
                    DateTime.Parse(data["date"]));
                else if (data["action"] == "reg") return AddUserToTournament(int.Parse(data["user_id"]), int.Parse(data["tour_id"]));
                else if (data["action"] == "unreg") return RemoveUserFromTournament(int.Parse(data["user_id"]), int.Parse(data["tour_id"]));
                else if (data["action"] == "list") return GetTournaments();
                else if (data["action"] == "list_bots") return GetBots(user_id.ToString());
                else if (data["action"] == "is_reg") return CheckRegistration(int.Parse(data["user_id"]), int.Parse(data["tour_id"]));
                else return "";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "-10";
            }
        }
    }

    /*
    TournamentSystem tours = new TournamentSystem();
    tours.AnnonceTournament("test tour", 1, new DateTime(2018, 03, 31, 0, 0, 0));
    tours.ChangeStatus(2, TournamentStatus.Registration);
    tours.AddUserToTournament(18, 2);
    ...
    tours.PlayTournament(2);
    */
}
