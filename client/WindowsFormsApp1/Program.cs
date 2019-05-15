using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form0 fm = new Form0();
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(3);
            fm.Show();
            while (end > DateTime.Now)
            {
                Application.DoEvents();
            }
            fm.Close();
            fm.Dispose();
            Application.Run(new Form1());
        }
    }
}
