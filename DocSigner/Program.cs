using System;
using System.Windows.Forms;

namespace DocSigner
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            // Start the signing process
            var signingProcess = new ProcessCore();
            signingProcess.Execute();

            Application.Exit();
        }
    }
}