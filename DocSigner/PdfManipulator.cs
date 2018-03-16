using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Security;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;
using System.Windows.Forms;

namespace DocSigner
{
    public class PdfManipulator
    {
        private string _signedFile { get; set; }
        private IList<ICrlClient> _crlList { get; set; }
        private ITSAClient _tsaClient { get; set; }
        private IOcspClient _ocspClient { get; set; }
        private IList<X509Certificate> _chain { get; set; }
        private X509Certificate2 _pk { get; set; }
        private X509Certificate2Collection _collection { get; set; }
        private X509Certificate _cert { get; set; }
        private string _thumbprint { get; set; }
        private string _destPath { get; set; }

        public PdfManipulator()
        {
            _pk = null;
            _collection = null;
            _ocspClient = null;
            _signedFile = string.Empty;
            _thumbprint = Folders.certificateThumbprint;
            _chain = new List<X509Certificate>();
        }

        public void PerformSign(string fileToBeSigned)
        {
            _destPath = Path.GetDirectoryName(fileToBeSigned) + "\\";

            try
            {
                // Pick a signing certificate from specific thumbprint in 'my store'
                GetCertificate(Folders.certificateThumbprint);

                // Getting the timestamp from ERMIS.
                if (_tsaClient == null) GetTimestamp(_chain);

                // Perform the signing if file exists && there is a certificate
                ProcessSigning(fileToBeSigned,_destPath,_pk,_chain,_collection,_crlList,_ocspClient,_tsaClient);
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception:" + Environment.NewLine + e.Message);
            }
        }

        private void GetCertificate(string _thumbprint)
        {
            X509Store x509Store = new X509Store(StoreName.My,StoreLocation.CurrentUser);
            x509Store.Open(OpenFlags.ReadOnly | OpenFlags.IncludeArchived);

            _collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint,_thumbprint,false);

            // If there is at least one certificate
            if (_collection.Count > 0)
            {
                X509Certificate2Enumerator certificatesEn = _collection.GetEnumerator();
                certificatesEn.MoveNext();
                _pk = certificatesEn.Current;
                X509Chain x509chain = new X509Chain();
                x509chain.Build(_pk);

                foreach (X509ChainElement x509ChainElement in x509chain.ChainElements)
                {
                    _chain.Add(DotNetUtilities.FromX509Certificate(x509ChainElement.Certificate));
                }
            }

            x509Store.Close();
        }

        private void GetTimestamp(IList<X509Certificate> _chain)
        {
            for (int i = 0; i < _chain.Count; i++)
            {
                _cert = _chain[i];
                string tsaUrl = Folders.ermisLink; // The link is in the 'folders.resx' file
                if (tsaUrl != null) _tsaClient = new TSAClientBouncyCastle(tsaUrl);
            }

            _crlList = new List<ICrlClient>
                {
                new CrlClientOnline(_chain)
                };
        }

        private void ProcessSigning(string fileToBeSigned,
                                    string _destPath,X509Certificate2 _pk,
                                    IList<X509Certificate> _chain,X509Certificate2Collection _collection,
                                    IList<ICrlClient> _crlList,IOcspClient _ocspClient,ITSAClient _tsaClient)
        {
            if (OkToSign(fileToBeSigned))
            {
                // Set the signed file's full path+filename
                _signedFile = (_destPath + Path.GetFileNameWithoutExtension(fileToBeSigned) + "_s.pdf").ToLower();

                Sign(fileToBeSigned,_signedFile,_chain,_pk,DigestAlgorithms.SHA1,CryptoStandard.CMS,
                                        "Electronic invoicing",null,_crlList,_ocspClient,_tsaClient,0);

                // Logging the operation to txt file
                var log = new Logger();
                log.ToFile("Signed " + Path.GetFileNameWithoutExtension(fileToBeSigned) + " using " + 
                    ((_pk.FriendlyName == "") ? _pk.Subject : _pk.FriendlyName),true);
            }
            else
            {
                _signedFile = string.Empty;
            }
        }

        private void Sign(string src,string dest,
                         ICollection<X509Certificate> _chain,X509Certificate2 _pk,
                         string digestAlgorithm,CryptoStandard subfilter,
                         string reason,string location,
                         ICollection<ICrlClient> _crlList,
                         IOcspClient _ocspClient,
                         ITSAClient _tsaClient,
                         int estimatedSize)
        {
            // Creating the reader and the stamper
            PdfReader reader = null;
            PdfStamper stamper = null;
            FileStream fs = null;
            try
            {
                reader = new PdfReader(src);
                fs = new FileStream(dest,FileMode.Create);
                stamper = PdfStamper.CreateSignature(reader,fs,'\0');

                // Creating the appearance
                PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                appearance.Reason = reason;
                appearance.SetVisibleSignature(new Rectangle(400,40,480,70),1,"signature");

                // Creating the signature
                IExternalSignature pks = new X509Certificate2Signature(_pk,digestAlgorithm);
                MakeSignature.SignDetached(appearance,pks,_chain,_crlList,_ocspClient,_tsaClient,estimatedSize,subfilter);
            }
            finally
            {
                if (reader != null) reader.Close();
                if (stamper != null) stamper.Close();
                if (fs != null) fs.Close();
            }
        }

        public void ProtectWithPassword(string file,string cardcode)
        {
            var source = file;
            var dest = Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file) + "_pwd.pdf";

            using (Stream input = new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read))
            using (Stream output = new FileStream(dest,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                PdfReader reader = new PdfReader(input);
                PdfEncryptor.Encrypt(reader,output,true,cardcode,cardcode,PdfWriter.AllowCopy);
            }
        }

        private bool OkToSign(string fileToBeSigned)
        {
            return (!string.IsNullOrWhiteSpace(fileToBeSigned) && 
                    File.Exists(fileToBeSigned) && _collection.Count > 0);
        }
    }
}