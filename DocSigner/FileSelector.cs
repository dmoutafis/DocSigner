using System.Windows.Forms;

namespace DocSigner
{
    class FileSelector
    {
        public string Select()
        {
            string fname;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "pdf files (*.pdf)|*.pdf";
            dialog.InitialDirectory = @"C:\Users\damianos.moutafis\Documents\Visual Studio 2015\Projects\DocSigner\DocSigner";
            dialog.Title = "Select a pdf file";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                fname = dialog.FileName;
            }
            else
            {
                fname = string.Empty;
            }

            return fname;
        }
    }
}