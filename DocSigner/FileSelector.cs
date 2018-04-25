﻿using System.Windows.Forms;

namespace DocSigner
{
    class FileSelector
    {
        public string Select()
        {
            string filename;

            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "pdf files (*.pdf)|*.pdf",
                InitialDirectory =
                @"C:\Users\damianos.moutafis\Documents\" +
                @"Visual Studio 2015\Projects\DocSigner\DocSigner",

                Title = "Select a pdf file"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                filename = dialog.FileName;
            }
            else
            {
                filename = string.Empty;
            }

            return filename;
        }
    }
}