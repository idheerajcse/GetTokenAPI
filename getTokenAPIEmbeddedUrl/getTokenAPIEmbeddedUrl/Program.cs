using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace getTokenAPIEmbeddedUrl
{
    class Program
    {
        static void Main(string[] args)
        {
            var scopes = new List<string>
                {
                    "signature",
                    "impersonation",
                };
            ApiClient2 c = new ApiClient2();
            var _authToken = c.RequestJWTUserToken("1ab08a59-8428-4126-8530-e136bca0603d",
                 "35204a64-8aae-43d3-a9a3-72dc0e5a34c8",
                "account-d.docusign.com",
                 ReadFileContent(PrepareFullPrivateKeyFilePath("private.key")), 1, scopes);


            //create Envelope

            string signerEmail = "dhiru9570@gmail.com";
            string signerName = "Raj s";
            string signerClientId = "1000";
            string docPdf = "World_Wide_Corp_lorem.pdf";
            string accountId = "90527da0-2f34-42d3-aaf9-8f8e52842676";
            // Step 1. Create the envelope definition
            EnvelopeDefinition envelope = MakeEnvelope(signerEmail, signerName, signerClientId, docPdf);
            //prepare the params
            string basePath = "https://demo.docusign.net/restapi";
            var apiClient = new ApiClient(basePath);
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + _authToken.access_token);
            EnvelopesApi envelopesApi = new EnvelopesApi(apiClient);
            EnvelopeSummary results = envelopesApi.CreateEnvelope(accountId, envelope);
            string envelopeId = results.EnvelopeId;

            //Embedded Url
            string returnUrl = "www.google.com";
            string pingUrl = null;
            RecipientViewRequest viewRequest = MakeRecipientViewRequest(signerEmail, signerName, returnUrl, signerClientId, pingUrl);
            // call the CreateRecipientView API
            ViewUrl results1 = envelopesApi.CreateRecipientView(accountId, envelopeId, viewRequest);

            // Step 4. Redirect the user to the Signing Ceremony
            // Don't use an iFrame!
            // State can be stored/recovered using the framework's session or a
            // query parameter on the returnUrl (see the makeRecipientViewRequest method)
            string redirectUrl = results1.Url;

        }

        private static EnvelopeDefinition MakeEnvelope(string signerEmail, string signerName, string signerClientId, string docPdf)
        {
            // Data for this method
            // signerEmail 
            // signerName
            // signerClientId -- class global
            // Config.docPdf


            byte[] buffer = System.IO.File.ReadAllBytes(docPdf);

            EnvelopeDefinition envelopeDefinition = new EnvelopeDefinition();
            envelopeDefinition.EmailSubject = "Please sign this document";
            Document doc1 = new Document();

            String doc1b64 = Convert.ToBase64String(buffer);

            doc1.DocumentBase64 = doc1b64;
            doc1.Name = "Lorem Ipsum"; // can be different from actual file name
            doc1.FileExtension = "pdf";
            doc1.DocumentId = "3";

            // The order in the docs array determines the order in the envelope
            envelopeDefinition.Documents = new List<Document> { doc1 };

            // Create a signer recipient to sign the document, identified by name and email
            // We set the clientUserId to enable embedded signing for the recipient
            // We're setting the parameters via the object creation
            Signer signer1 = new Signer
            {
                Email = signerEmail,
                Name = signerName,
                ClientUserId = signerClientId,
                RecipientId = "1"
            };

            // Create signHere fields (also known as tabs) on the documents,
            // We're using anchor (autoPlace) positioning
            //
            // The DocuSign platform seaches throughout your envelope's
            // documents for matching anchor strings.
            SignHere signHere1 = new SignHere
            {
                AnchorString = "/sn1/",
                AnchorUnits = "pixels",
                AnchorXOffset = "10",
                AnchorYOffset = "20"
            };
            // Tabs are set per recipient / signer
            Tabs signer1Tabs = new Tabs
            {
                SignHereTabs = new List<SignHere> { signHere1 }
            };
            signer1.Tabs = signer1Tabs;

            // Add the recipient to the envelope object
            Recipients recipients = new Recipients
            {
                Signers = new List<Signer> { signer1 }
            };
            envelopeDefinition.Recipients = recipients;

            // Request that the envelope be sent by setting |status| to "sent".
            // To request that the envelope be created as a draft, set to "created"
            envelopeDefinition.Status = "sent";

            return envelopeDefinition;
        }
        private static RecipientViewRequest MakeRecipientViewRequest(string signerEmail, string signerName, string returnUrl, string signerClientId, string pingUrl = null)
        {
            // Data for this method
            // signerEmail 
            // signerName
            // dsPingUrl -- class global
            // signerClientId -- class global
            // dsReturnUrl -- class global


            RecipientViewRequest viewRequest = new RecipientViewRequest();
            // Set the url where you want the recipient to go once they are done signing
            // should typically be a callback route somewhere in your app.
            // The query parameter is included as an example of how
            // to save/recover state information during the redirect to
            // the DocuSign signing ceremony. It's usually better to use
            // the session mechanism of your web framework. Query parameters
            // can be changed/spoofed very easily.
            viewRequest.ReturnUrl = returnUrl + "?state=123";

            // How has your app authenticated the user? In addition to your app's
            // authentication, you can include authenticate steps from DocuSign.
            // Eg, SMS authentication
            viewRequest.AuthenticationMethod = "none";

            // Recipient information must match embedded recipient info
            // we used to create the envelope.
            viewRequest.Email = signerEmail;
            viewRequest.UserName = signerName;
            viewRequest.ClientUserId = signerClientId;

            // DocuSign recommends that you redirect to DocuSign for the
            // Signing Ceremony. There are multiple ways to save state.
            // To maintain your application's session, use the pingUrl
            // parameter. It causes the DocuSign Signing Ceremony web page
            // (not the DocuSign server) to send pings via AJAX to your
            // app,
            // NOTE: The pings will only be sent if the pingUrl is an https address
            if (pingUrl != null)
            {
                viewRequest.PingFrequency = "600"; // seconds
                viewRequest.PingUrl = pingUrl; // optional setting
            }

            return viewRequest;
        }

        internal static string PrepareFullPrivateKeyFilePath(string fileName)
        {
            const string DefaultRSAPrivateKeyFileName = "docusign_private_key.txt";

            var fileNameOnly = Path.GetFileName(fileName);
            if (string.IsNullOrEmpty(fileNameOnly))
            {
                fileNameOnly = DefaultRSAPrivateKeyFileName;
            }

            var filePath = Path.GetDirectoryName(fileName);
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Directory.GetCurrentDirectory();
            }

            return Path.Combine(filePath, fileNameOnly);
        }
        internal static byte[] ReadFileContent(string path)
        {
            return File.ReadAllBytes(path);
        }
    }
}
