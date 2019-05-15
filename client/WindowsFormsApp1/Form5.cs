using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form5 : Form
    {
        public Form5()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("type", "api");
            data.Add("action", "reset_pass");
            data.Add("key", textBox1.Text);
            data.Add("newpass", "");
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
                ServiceReset.key = textBox1.Text;
                Hide();
                Form fm = new Form6();
                fm.ShowDialog();
                Close();
            }
            else MessageBox.Show("Ошибка сервера\n Повторите позже", "Botinok.fun", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void press_enter(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) button1_Click(null, null);
        }
    }
}
