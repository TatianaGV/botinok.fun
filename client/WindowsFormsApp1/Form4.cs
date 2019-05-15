using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApp1
{

    //форма сброса пароля
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "api");
            data.Add("action", "send_req_reset_pass");
            data.Add("email", textBox1.Text);

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
                MessageBox.Show("Проверочный код отправлен на указанный email");
                ServiceReset.email = textBox1.Text;
                Hide();
                Form fm = new Form5();
                
                fm.ShowDialog();
                Close();
            }
            else if (ans == -1) MessageBox.Show("Пользователя с таким email не существует");
            else MessageBox.Show("Ошибка сервера\n Повторите позже", "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Hide();
            Form fn = new Form1();
            fn.ShowDialog();
            Close();
        }

        private void key_pressed(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) button1_Click(null, null);
        }
    }
}
