using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        struct Friend
        {
            public int id;
            public string name;
        }

        List<Tour> Tours = new List<Tour>();
        List<Bot> Bots = new List<Bot>();
        List<Panel> FriendsList = new List<Panel>();
        List<Label> FriendsName = new List<Label>();

        void UpdateFriends()
        {
            //friends
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "api");
            data.Add("action", "get_friends");
            data.Add("session", UserData.session_token);
            data.Add("user_id", UserData.user_id.ToString());
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string answer = Service.SendRequestAndGetAnswer(json);
            if (answer == "0") return;
            if (answer == "-1") { return; } //заставить авторизоваться заново
            else if (answer == "-10")
            {
                MessageBox.Show("Botinok", "Не удалось загрузить список друзей!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var ans = JsonConvert.DeserializeObject<List<Friend>>(answer);

            int col = 0;
            
            foreach (var x in ans)
            {
                if (col < 4) FriendsName[col].Invoke(new Action(()=> FriendsName[col].Text = x.name));
                else
                {
                    //Добавить прокрутку, если друзей больше 4
                }
                col++;
            }
        }

        struct Tour
        {
            public int tour_id;
            public string name, game, status, players, date;
        }

        string getTourStatus(int status)
        {
            switch (status)
            {
                case 1:
                    return "Регистрация";
                case 2:
                    return "Анонсирован";
                case 3:
                    return "Идет";
                case 4:
                    return "Завершен";
                case 5:
                    return "Отменен";
                default:
                    return "<Неизвестно>";
            }
        }
        
        private void UpdateTournaments()
        {
            Tours.Clear();
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("user_id", UserData.user_id.ToString());
            data.Add("type", "tours");
            data.Add("action", "list");
            data.Add("session_id", UserData.session_token);
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string answer = Service.SendRequestAndGetAnswer(json);
                if (answer == "") return;
                int sel_ind = -1;
                listView1.Invoke(new Action(() => sel_ind = (listView1.SelectedIndices.Count > 0) ? listView1.SelectedIndices[0] : -1));
                listView1.Invoke(new Action(() => listView1.Items.Clear()));
                foreach (var x in JsonConvert.DeserializeObject<List<Tour>>(answer))
                {
                    var item = new string[] { x.name, x.game, getTourStatus(int.Parse(x.status)), x.players, x.date };
                    listView1.Invoke(new Action(()=> listView1.Items.Add(new ListViewItem(item))));
                    Tours.Add(x);
                }
                if (sel_ind >= 0) listView1.Invoke(new Action(() => listView1.Items[sel_ind].Selected = true));
            }
            catch
            {
                MessageBox.Show("Не удается загрузить список турниров", "Botinok", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        struct Bot
        {
            public int bot_id;
            public int user_id;
            public string name;
            public string game;
            public List<byte> bytes;
        }

        private void UpdateBots()
        {
            Bots.Clear();
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("user_id", UserData.user_id.ToString());
            data.Add("type", "tours");
            data.Add("action", "list_bots");
            data.Add("session_id", UserData.session_token);
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string answer = Service.SendRequestAndGetAnswer(json);
                if (answer == "") return;
                int sel_ind = -1;
                listView2.Invoke(new Action(() => sel_ind = (listView2.SelectedIndices.Count > 0) ? listView2.SelectedIndices[0] : -1));
                listView2.Invoke(new Action(() => listView2.Items.Clear()));
                foreach (var x in JsonConvert.DeserializeObject<List<Bot>>(answer))
                {
                    var item = new string[] { x.name, x.game };
                    listView2.Invoke(new Action(() => listView2.Items.Add(new ListViewItem(item))));
                    Bots.Add(x);
                }
                if (sel_ind >= 0) listView2.Invoke(new Action(() => listView2.Items[sel_ind].Selected = true));
            }
            catch
            {
                MessageBox.Show("Не удается загрузить список турниров", "Botinok", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            //nick
            linkLabel1.Text = UserData.nick;

            FriendsList.Add(friend1); FriendsList.Add(friend2); FriendsList.Add(friend3); FriendsList.Add(friend4);
            for (int col = 0; col < 4; col++)
            {
                Label lab = new Label();
                lab.Text = "";
                lab.Font = new Font("Arial", 12);
                lab.Location = new Point(25, 5);
                lab.Width = FriendsList[col].Width - 50;
                FriendsList[col].Controls.Add(lab);
                FriendsName.Add(lab);
            }

            Service.chat_th = new Thread(() => UpdateChat());
            Service.info_th = new Thread(new ThreadStart (() => UpdateInfo()));
            Service.chat_th.Start();
            Service.info_th.Start();

            //listView1.Items.Add(new ListViewItem( new string[] { "XO Championship", "Крестики-нолики", "Идет", "17", "10.03.2018 00:00" }));
            //listView1.Items.Add(new ListViewItem(new string[] { "Battleship Championship", "Морской бой", "Регистрация", "5", "25.03.2018 00:00" }));
            //listView1.Items.Add(new ListViewItem(new string[] { "Domino Championship", "Домино", "Анонсирован", "0", "31.03.2018 00:00" }));

            //listView2.Items.Add(new ListViewItem(new string[] { "my_xo_bot", "Крестики-нолики", "05.03.2018 22:46", "Проверен", "6" }));
            //listView2.Items.Add(new ListViewItem(new string[] { "my_domino_bot", "Домино", "85.03.2018 00:39", "Проверен", "1" }));
            //listView2.Items.Add(new ListViewItem(new string[] { "my_battleship_bot", "Морской бой", "12.03.2018 23:45", "Проверяется", "0" }));
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            Service.chat_th.Abort();
            Service.info_th.Abort();
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Hide();
            Service.chat_th.Abort();
            Service.info_th.Abort();
            Form1 hf = new Form1();
            hf.ShowDialog();
            Close();
        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            Form7 hf = new Form7();
            hf.ShowDialog();
        }

        private int getLastMessageID()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "general_chat");
            data.Add("action", "get_id");
            data.Add("user_id", UserData.user_id.ToString());
            data.Add("session_id", UserData.session_token);
            string answer = "";
            try
            {
                string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                answer = Service.SendRequestAndGetAnswer(json);
                return int.Parse(answer);
            }
            catch
            {
                return 0;
            }
        }

        public void UpdateInfo()
        {
            while (true)
            {
                UpdateFriends();
                UpdateTournaments();
                UpdateBots();
                Thread.Sleep(5000);
            }
        }

        public void UpdateChat()
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
                data.Add("user_id", UserData.user_id.ToString());
                data.Add("session_id", UserData.session_token);
                data.Add("start", mes_id.ToString());
                string answer = "";
                try
                {
                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    answer = Service.SendRequestAndGetAnswer(json);
                    if (answer == "-10")
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    var messages = JsonConvert.DeserializeObject<List<ChatMessage>>(answer);
                    if (messages != null && messages.Count != 0)
                    {
                        foreach (var x in messages) { richTextBox3.AppendText(x.nickname + " : " + x.text + "\n"); }
                        richTextBox3.ScrollToCaret();
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

        private void button4_Click(object sender, EventArgs e)
        {
            string text = TextMsg.Text;
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "general_chat");
            data.Add("action", "write");
            data.Add("user_id", UserData.user_id.ToString());
            data.Add("session_id", UserData.session_token);
            data.Add("text", text);

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string answer = Service.SendRequestAndGetAnswer(json);
            if (answer != "1") MessageBox.Show("Error!");
            TextMsg.Clear();
        }

        private void enter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button4_Click(null, null);
                e.SuppressKeyPress = true;
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            string tour_name = listView1.SelectedItems[0].SubItems[0].Text;
            int tour_id = -1;
            foreach (var xx in Tours)
            {
                if (xx.name == tour_name)
                {
                    tour_id = xx.tour_id;
                    break;
                }
            }
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "tours");
            data.Add("action", (isReg(tour_id)) ? "unreg" : "reg");
            data.Add("user_id", UserData.user_id.ToString());
            data.Add("tour_id", tour_id.ToString());
            data.Add("session_id", UserData.session_token);
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string answer = Service.SendRequestAndGetAnswer(json);
                if (answer == "0" && data["action"] == "unreg") MessageBox.Show("Ошибка, регистрация уже закрыта");
            }
            catch
            {
                return;
            }
            button10.Text = (isReg(tour_id)) ? "Отменить заявку" : "Регистрация";
            listView1.Items[listView1.SelectedIndices[0]].Selected = false;
        }

        bool isReg(int tour_id)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "tours");
            data.Add("action", "is_reg");
            data.Add("user_id", UserData.user_id.ToString());
            data.Add("tour_id", tour_id.ToString());
            data.Add("session_id", UserData.session_token);
            string answer = "";
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                answer = Service.SendRequestAndGetAnswer(json);
                if (answer == "1") return true;
                else return false;
            }
            catch
            {
                return false;
            }
        }

        private void select_changed(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            var x = e.Item.SubItems;
            int t_id = -1;
            foreach (var xx in Tours)
            {
                if (xx.name == x[0].Text)
                {
                    t_id = xx.tour_id;
                    break;
                }
            }
            button10.Enabled = true;
            if (!isReg(t_id)) button10.Text = "Регистрация";
            else button10.Text = "Отменить заявку";
        }

        void UploadBot(string exe, string name)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "bots");
            data.Add("action", "reg");
            data.Add("name", name);
            data.Add("user_id", UserData.user_id.ToString());
            data.Add("session_id", UserData.session_token);
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string answer = Service.SendRequestAndGetAnswer(json);
                if (answer == "-1") MessageBox.Show("Ошибка загрузки");

                int bot_id = int.Parse(answer);
                var bid = BitConverter.GetBytes(bot_id);

                var bb = File.ReadAllBytes(exe);
                int cur_pos = 0;

                while (true)
                {
                    byte[] bytes = new byte[1024];
                    bytes[0] = 1; //маркер загрузки бота
                    for (int i = 0; i < 4; i++) bytes[i + 1] = bid[i]; //следующие 4 байта - id бота
                    int size = 1015;
                    if (cur_pos + 1015 >= bb.Length) size = bb.Length - cur_pos;
                    if (size <= 0) break;
                    var size_bits = BitConverter.GetBytes(size);
                    for (int i = 0; i < 4; i++) bytes[i + 5] = size_bits[i]; //следующие 4 байта - размер блока
                    for (int i = 0; i < size; i++) bytes[i + 9] = bb[cur_pos++]; //остальные 1015 байт - бинарные данные

                    Socket sender = new Socket(Service.ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    sender.Connect(Service.ipEndPoint);
                    int bytesSent = sender.Send(bytes);
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
            }
            catch
            {
                return;
            }
        }

        private void button1_Click(object sender, EventArgs e) //load bot
        {
            var fd = new OpenFileDialog();
            fd.CheckFileExists = true;
            fd.Multiselect = false;
            fd.Filter = "EXE files (*.exe)|*.exe";
            if (fd.ShowDialog() == DialogResult.OK)
            {
                InputBox inputBox = new InputBox();
                UploadBot(fd.FileName, inputBox.getString());
            }
        }
    }
}
