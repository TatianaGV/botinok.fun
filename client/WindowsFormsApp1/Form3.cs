using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if ((textBox3.Text == "") || (textBox4.Text == "") || (textBox1.Text == "") || (textBox2.Text == ""))
            {
                MessageBox.Show("Одно или несколько полей не заполнены");
            }
            else if (textBox1.Text != textBox2.Text)
            {
                MessageBox.Show("Пароли не совпадают");
            }
            else
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("type", "api");
                data.Add("action", "reg");
                data.Add("nick", textBox4.Text);
                data.Add("pass", Service.md5(textBox1.Text));
                data.Add("email", textBox3.Text);
                string answer;
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                try
                {
                    answer = Service.SendRequestAndGetAnswer(json);
                }
                catch
                {
                    MessageBox.Show("Ошибка соединения с сервером\n Повторите позже", "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                int ans = int.Parse(answer);
                string text = "";
                if (ans >= 0)
                {
                    MessageBox.Show("Пользователь успешно зарегистрирован");
                    Hide();
                    Form2 hf = new Form2();
                    hf.ShowDialog();
                    Close();
                }
                else if (ans == -2) text = "Некорректный email";
                else if (ans == -3) text = "Некорректный ник";
                else if (ans == -4) text = "Пользователь с указанным email уже существует";
                else if (ans == -5) text = "Пользователь с указанным ником уже существует";
                else text = "Ошибка сервера\n Повторите позже";
                if (text != "") MessageBox.Show(text, "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Hide();
            Form1 hf = new Form1();
            hf.ShowDialog();
            Close();
        }

        private void press_enter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) Button1_Click(null, null);
        }
    }
}
