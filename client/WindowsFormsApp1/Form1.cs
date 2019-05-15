using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApp1
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Service.auth_form = this;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            copy.Text = textBox1.Text;
        }

        void isAuthed(string login, string pass)
        {
            if (Service.off_Auth)
            {
                UserData.user_id = 1;
                return;
            }
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "api");
            data.Add("action", "auth");
            data.Add("login", login);
            data.Add("pass", Service.md5(pass));

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string answer = Service.SendRequestAndGetAnswer(json);
            var tmp = answer.Split(' ');
            if (tmp.Length == 2)
            {
                UserData.user_id = int.Parse(tmp[0]);
                UserData.session_token = tmp[1];
                UserData.login = login;
                UserData.pass = Service.md5(pass);
            }
            else
            {
                UserData.user_id = int.Parse(answer);
            }
            //get nick
            if (UserData.user_id >= 0)
            {
                data = new Dictionary<string, string>();
                data.Add("type", "api");
                data.Add("action", "get_nick");
                data.Add("user_id", UserData.user_id.ToString());
                json = JsonConvert.SerializeObject(data, Formatting.Indented);
                answer = Service.SendRequestAndGetAnswer(json);
                UserData.nick = (answer != "-10") ? answer : "";
            }
        }

        private void klik_Click(object sender, EventArgs e)
        {
            try
            {
                isAuthed(textBox1.Text, textBox2.Text);
            }
            catch
            {
                MessageBox.Show("Ошибка соединения с сервером\nПовторите позже", "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (UserData.user_id >= 0)
            {
                Hide();
                Form2 frm = new Form2();
                //frm.StartPosition = FormStartPosition.CenterScreen;
                frm.ShowDialog();
                //Close();
            }
            else if (UserData.user_id == -10)
            {
                MessageBox.Show("Ошибка сервера\nПовторите позже", "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Text = "";
            }
            else if (UserData.user_id == -2)
            {
                MessageBox.Show("Ошибка сессии\nПопробуйте еще раз", "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Text = "";
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль", "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Text = "";
            }

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Hide();
            Form frm = new Form3();
            frm.ShowDialog();
            Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Hide();
            Form fm = new Form4();
            fm.ShowDialog();
            Close();
        }

        private void button2_keydown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                klik_Click(null, null);
                e.SuppressKeyPress = true;
            }
        }

        private void textBox1_key(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                textBox2.Focus();
                e.SuppressKeyPress = true;
            }
        }
    }
}