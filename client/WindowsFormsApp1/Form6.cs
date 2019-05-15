using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == textBox2.Text)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("type", "api");
                data.Add("action", "reset_pass");
                data.Add("key", ServiceReset.key);
                data.Add("newpass", Service.md5(textBox1.Text));
                data.Add("email", ServiceReset.email);

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string answer;
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

                if (ans == 1)
                {
                    MessageBox.Show("Пароль изменен");
                    Hide();
                    Form fm = new Form1();
                    fm.ShowDialog();
                    Close();
                }
                else MessageBox.Show("Ошибка сервера\n Повторите позже", "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Пароли не совпадают");
            }
        }

        private void press_enter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) button1_Click(null, null);
        }
    }
}
