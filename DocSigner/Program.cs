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

            // Perform signing of the file
            var file = new FileSelector();
            var pdf = new PdfManipulator();

            pdf.PerformSign(file.Select());
            MessageBox.Show("Signed file created!", "Information", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            Application.Exit();
        }
    }
}