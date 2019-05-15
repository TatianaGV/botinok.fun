using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// Статус завершения игры: Win - победа одного из ботов, Draw - ничья, TimeIsOver - лимит времени у одного из
    /// ботов, IncorrectMove - запрещенный ход одного из ботов, ProcessError - RunTime Error у одного из ботов (или ошибка при запуске
    /// процесса), Playing - игра не завершена, NotStarted - игра еще не началась
    /// </summary>
    public enum GameStatus
    {
        Win1 = 0,
        Win2,
        Draw,
        TimeIsOver1,
        TimeIsOver2,
        IncorrectMove1,
        IncorrectMove2,
        ProcessError1,
        ProcessError2,
        Playing,
        NotStarted
    };

    public enum RunBotStatus
    {
        Successfully = 0,
        Bot1Error,
        Bot2Error
    }

    /// <summary>
    /// Абстрактный класс игры, для каждой игры необходимо переопределить GetMove
    /// </summary>
    abstract class Game
    {
        //Служебные поля, для запуска процессов
        protected Process[] BotProcess = new Process[2];
        protected StreamReader[] writer = new StreamReader[2];
        protected StreamWriter[] reader = new StreamWriter[2];
        protected const bool ENABLE_PRINT = false;
        protected GameStatus status = GameStatus.NotStarted;
        public int match_id;
        DateTime time_start = DateTime.Now;

        protected List<string> logs = new List<string>();

        public RunBotStatus RunBots(string bot1, string bot2) //run bots
        {
            BotProcess[0] = new Process();
            BotProcess[1] = new Process();
            BotProcess[0].StartInfo.FileName = bot1;
            BotProcess[1].StartInfo.FileName = bot2;
            BotProcess[0].StartInfo.RedirectStandardInput = true;
            BotProcess[1].StartInfo.RedirectStandardInput = true;
            BotProcess[0].StartInfo.RedirectStandardOutput = true;
            BotProcess[1].StartInfo.RedirectStandardOutput = true;
            BotProcess[0].StartInfo.UseShellExecute = false;
            BotProcess[1].StartInfo.UseShellExecute = false;
            BotProcess[0].Exited += XO_Bot1_Exited;
            BotProcess[1].Exited += XO_Bot2_Exited;

            if (!BotProcess[0].Start()) return RunBotStatus.Bot1Error;
            else if (!BotProcess[1].Start()) return RunBotStatus.Bot2Error;

            reader[0] = BotProcess[0].StandardInput;
            reader[1] = BotProcess[1].StandardInput;

            writer[0] = BotProcess[0].StandardOutput;
            writer[1] = BotProcess[1].StandardOutput;

            if (ENABLE_PRINT) Console.WriteLine("Процессы запущены");

            status = GameStatus.Playing;
            return RunBotStatus.Successfully;
        }

        private void XO_Bot1_Exited(object sender, EventArgs e)
        {
            status = GameStatus.ProcessError1;
        }

        private void XO_Bot2_Exited(object sender, EventArgs e)
        {
            status = GameStatus.ProcessError2;
        }

        public void FinishGame()
        {
            for (int i = 0; i < 2; i++)
            {
                reader[i].Close();
                writer[i].Close();
                BotProcess[i].Kill();
                BotProcess[i].Close();
            }
            if (ENABLE_PRINT) Console.WriteLine("Процессы уничтожены");
        }

        public void SendInfo(bool op, string info)
        {
            reader[(op) ? 0 : 1].WriteLine(info);
        }

        public string GetInfo(bool op)
        {
            return writer[(op) ? 0 : 1].ReadLine();
        }

        public GameStatus Status()
        {
            return status;
        }

        public bool SaveLog(string file)
        {
            try
            {
                var stream = File.CreateText(file);
                stream.WriteLine("Матч # " + match_id);
                stream.WriteLine("Время начала матча: " + time_start.ToString("dd MMMM yyyy | HH:mm:ss"));
                foreach (var line in logs) stream.WriteLine(line);
                stream.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        abstract public void GetMove(bool op);

        virtual public void SendFirstInfo(bool op) { } //информирует бота о том, что он ходит первым
        virtual public void SendSecondInfo(bool op) { } //информирует бота о том, что он ходит вторым

        //Следующие поля для контроля времени
        static bool flag;
        static string answ;
        static void makeFlag()
        {
            Thread.Sleep(3000);
            flag = true;
        }

        public bool GetAnswerFor3Seconds(bool op, ref string answer, ref long time)
        {
            Stopwatch sw;
            if (ENABLE_PRINT)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            flag = false;
            Task t = Task.Run(() => answ = GetInfo(op));
            var th = new Thread(makeFlag); th.Start();
            while (!t.IsCompleted && !flag) { }
            th.Abort();
            if (!t.IsCompleted) return false;

            if (ENABLE_PRINT)
            {
                sw.Stop();
                time = sw.ElapsedMilliseconds;
            }
            answer = answ;
            return true;
        }
    }

    class MatchSystem
    {
        public GameStatus StartMatch(Game game, string bot1, string bot2)
        {
            //const bool ENABLE_PRINT = true;
            game.RunBots(bot1, bot2); //run bots' processes
            Random r = new Random();
            //int num_op = r.Next(0, 2); //кто первый ходит
            bool[] bools = new bool[2] { false, true };
            bool first = bools[r.Next(0, 2)]; //true, если сейчас ходит первый бот

            //if (ENABLE_PRINT) Console.WriteLine("Первым ходит Бот {0}", (first) ? 1 : 2);

            //отправляем ботам инфу, кто есть кто
            game.SendFirstInfo(first);
            game.SendSecondInfo(!first);

            while(game.Status() == GameStatus.Playing) {
                game.GetMove(first);
                first = !first;
            }

            game.FinishGame();
            return game.Status();
        }
    }

    class Match
    {
        public MatchSystem system = new MatchSystem();
        public Game game;
        public int bot1_id, bot2_id;
        public string bot1_exe, bot2_exe;
        public GameStatus status;

        public Match(int bot1_id, int bot2_id, Game game)
        {
            //Проверить ID ботов
            this.bot1_id = bot1_id;
            this.bot2_id = bot2_id;
            this.game = game;

            game.match_id = 1; //TODO!!!

            //TODO: по ID ботов получить пути к программам ботов
            bot1_exe = "../bots/xo/bot1.exe";
            bot2_exe = "../bots/xo/bot2.exe";
            
        }

        public GameStatus Play()
        {
            status = system.StartMatch(game, bot1_exe, bot2_exe);
            return status;
        }

        public bool SaveLog(string path)
        {
            string file = bot1_id.ToString() + "_" + bot2_id + DateTime.Now.ToString("_dd_MM_yyyy_HH_mm_ss") + ".txt";
            return game.SaveLog((path + "/" + file).Replace("//", "/"));
        }
    }

}
