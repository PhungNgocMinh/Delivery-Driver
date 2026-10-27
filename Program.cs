using System;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AudioManager.Initialize();
            AudioManager.StartMusic();
            try
            {
                Application.Run(new MainMenuForm());   // not new GameForm()
            }
            finally
            {
                AudioManager.Shutdown();
            }
        }
    }
}
