using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Server
{
    class XO : Game
    { 
        const int EMPTY = 0;
        const int X = 1;
        const int O = 2;

        int[,] board = new int[3, 3] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };

        override public void SendFirstInfo(bool op) //информирует бота о том, что он ходит первым
        {
            SendInfo(op, "X");
        } 
        override public void SendSecondInfo(bool op) //информирует бота о том, что он ходит вторым
        {
            SendInfo(op, "Y");
        }

        public override void GetMove(bool op) //TODO: сделать логи
        {
            #region Когда боту нужно сделать ход, ему отсылается строка "move\n". После этого у бота есть 3 секунды на отправку ответа
            SendInfo(op, "move");

            string answer = ""; long time = 0;
            //string answer = GetInfo(op); //работает быстрее, но не контролирует время
            if (!GetAnswerFor3Seconds(op, ref answer, ref time))
            {
                logs.Add(string.Format("Бот {0} превысил время ожидания", (op) ? 1 : 2));
                //if (ENABLE_PRINT) Console.WriteLine("Бот {0} превысил время ожидания", (op) ? 1 : 2);
                status = (op) ? GameStatus.TimeIsOver1 : GameStatus.TimeIsOver2;
                return;
            }

            //if (ENABLE_PRINT) Console.WriteLine("Бот {0} сделал ход \"{1}\", время {2} ms", (op) ? 1 : 2, answer, time);
            logs.Add(string.Format("Бот {0} сделал ход \"{1}\", время {2} ms", (op) ? 1 : 2, answer, time));
            #endregion

            #region Проверка корректности хода
            int x, y;
            var t = answer.Split();
            if (!int.TryParse(t[0], out x) || !int.TryParse(t[1], out y) || x > 3 || y > 3 || x < 0 || y < 0 //неверный формат
                || board[x, y] != 0) //такая клетка уже занята
            {
                status = (op) ? GameStatus.IncorrectMove1 : GameStatus.IncorrectMove2;
                logs.Add(string.Format("Бот {0} сделал некорректный ход", (op) ? 1 : 2));
                return;
            }
            #endregion

            //допустимый ход, делаем его
            board[x, y] = (op) ? X : O;

            #region Другому боту так же отсылается информация в таком же формате о ходе оппонента, а перед этим - команда "opp"
            SendInfo(!op, "opp");
            SendInfo(!op, x.ToString() + " " + y.ToString());
            #endregion

            #region Проверяем, не была ли выиграна партия после этого хода
            if ((board[x, 1] != 0 && board[x, 0] == board[x, 1] && board[x, 1] == board[x, 2]) ||
                (board[1, y] != 0 && board[0, y] == board[1, y] && board[1, y] == board[2, y])) //собрали 3 по горизонтали или вертикали
            {
                status = (op) ? GameStatus.Win1 : GameStatus.Win2;
                logs.Add(string.Format("Бот {0} выиграл", (op) ? 1 : 2));
                return;
            }
            if ((x == y && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2] && board[1, 1] != 0) || //собрали на побочной диагонали
                (x + y == 2 && board[2, 0] == board[1, 1] && board[1, 1] == board[0, 2]) && board[1, 1] != 0) //собрали на главной диагонали
            {
                status = (op) ? GameStatus.Win1 : GameStatus.Win2;
                logs.Add(string.Format("Бот {0} выиграл", (op) ? 1 : 2));
                return;
            }

            //если пришли сюда - еще никто не выиграл
            int cnt = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (board[i, j] != 0) cnt++;

            if (cnt == 9) //все поля заполнены, а победителя нет
            {
                status = GameStatus.Draw;
                logs.Add("Ничья");
            }
            #endregion
            //иначе продолжаем дальше
        }
    }
}
