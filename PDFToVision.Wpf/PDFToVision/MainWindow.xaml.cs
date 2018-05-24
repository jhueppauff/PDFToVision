//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="https://github.com/jhueppauff/PDFToVision">
// Copyright 2018 Jhueppauff
// MIT License
// For licence details visit https://github.com/jhueppauff/PDFToVision/blob/master/LICENSE
// </copyright>
//-----------------------------------------------------------------------

namespace PDFToVision
{
    using System;
    using System.Collections.Generic;
    using System.Drawing.Imaging;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using Newtonsoft.Json;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Button Send Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            List<string> imagePath = this.ConvertToImage();

            if (imagePath != null)
            {
                this.VisionProcessing(imagePath).ConfigureAwait(false).GetAwaiter();
            }
        }

        /// <summary>
        /// Parses the JSON and gets every word from it
        /// </summary>
        /// <param name="result">Json Result from the Vision API</param>
        private void ParseText(string result)
        {
            dynamic json = JsonConvert.DeserializeObject(result);

            StringBuilder builder = new StringBuilder();

            foreach (var region in json["regions"])
            {
                foreach (var line in region["lines"])
                {
                    foreach (var word in line["words"])
                    {
                        builder.AppendLine(word["text"].Value + " ");
                    }
                }
            }

            StringBuilder builder2 = new StringBuilder();
            builder2.Append(TbxTextFiltered.Text);
            builder2.AppendLine("-----------------------------------------");
            builder2.AppendLine("New Page");
            builder2.AppendLine("-----------------------------------------");
            builder2.AppendLine(builder.ToString());
            TbxTextFiltered.Text = builder2.ToString();
        }

        /// <summary>
        /// Converts the pdf to images
        /// </summary>
        /// <returns>List <see cref="List{string}"/> with all paths to the image files</returns>
        private List<string> ConvertToImage()
        {
            try
            {
                using (var document = PdfiumViewer.PdfDocument.Load(TbxFilePath.Text))
                {
                    string filename = System.IO.Path.GetFileName(TbxFilePath.Text).ToString();
                    string currentDirectory = System.IO.Directory.GetCurrentDirectory().ToString();
                    string imageDirectory = System.IO.Path.Combine(currentDirectory, "images");
                    List<string> filePaths = new List<string>();

                    for (int i = 0; i < document.PageCount; i++)
                    {
                        string filePath = System.IO.Path.Combine(imageDirectory, filename + i.ToString() + ".jpeg");
                        var image = document.Render(i, 300, 300, true);

                        if (!System.IO.Directory.Exists(System.IO.Path.Combine(currentDirectory, "images")))
                        {
                            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(currentDirectory, "images"));
                        }

                        if (!System.IO.Directory.Exists(System.IO.Path.Combine(currentDirectory, "images")))
                        {
                            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(currentDirectory, "images"));
                        }

                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }

                        image.Save(filePath, ImageFormat.Jpeg);
                        filePaths.Add(filePath);
                    }

                    return filePaths;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                throw;
            }
        }

        /// <summary>
        /// Processes the Images with Azure Vison API
        /// </summary>
        /// <param name="imagePaths">List <see cref="List{string} with all Image paths"/></param>
        /// <returns>Returns <see cref="Task"/></returns>
        private async Task VisionProcessing(List<string> imagePaths)
        {
            foreach (var imagePath in imagePaths)
            {
                try
                {
                    HttpClient client = new HttpClient();

                    // Request Header
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Properties.Settings.Default.ApiKey);

                    // Request parameters.
                    string requestParameters = "language=unk&detectOrientation=true";

                    string uri = Properties.Settings.Default.EndPointUrl + "?" + requestParameters;

                    HttpResponseMessage response;

                    // Request body. Posts a locally stored JPEG image.
                    byte[] byteData = this.GetImageAsByteArray(imagePath);

                    using (ByteArrayContent content = new ByteArrayContent(byteData))
                    {
                        // This example uses content type "application/octet-stream".
                        // The other content types you can use are "application/json"
                        // and "multipart/form-data".
                        content.Headers.ContentType =
                            new MediaTypeHeaderValue("application/octet-stream");

                        // Make the REST API call.
                        response = await client.PostAsync(uri, content);
                    }

                    // Get the JSON response.
                    string contentString = await response.Content.ReadAsStringAsync();

                    StringBuilder builder = new StringBuilder();
                    builder.Append(this.TbxOutput.Text);
                    builder.Append(this.JsonPrettyPrint(contentString));
                    TbxOutput.Text = builder.ToString();

                    this.ParseText(contentString);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "An Error occured within the Vision API", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                }
            }
        }

        /// <summary>
        /// Get a Byte Array of an Image File
        /// </summary>
        /// <param name="imageFilePath">Path to the file</param>
        /// <returns>Returns <see cref="Byte[]"/></returns>
        private byte[] GetImageAsByteArray(string imageFilePath)
        {
            using (System.IO.FileStream fileStream =
               new System.IO.FileStream(imageFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
        }

        /// <summary>
        /// Formats the given JSON string by adding line breaks and indents.
        /// </summary>
        /// <param name="json">The raw JSON string to format.</param>
        /// <returns>The formatted JSON string.</returns>
        private string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return string.Empty;
            }

            json = json.Replace(Environment.NewLine, string.Empty).Replace("\t", string.Empty);

            string intendString = "    ";
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < json.Length; i++)
            {
                var ch = json[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);

                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(
                                item => sb.Append(intendString));
                        }

                        break;
                    case '}':
                    case ']':

                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(
                                item => sb.Append(intendString));
                        }

                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;

                        while (index > 0 && json[--index] == '\\')
                        {
                            escaped = !escaped;
                        }

                        if (!escaped)
                        {
                            quoted = !quoted;
                        }

                        break;
                    case ',':
                        sb.Append(ch);

                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(
                                item => sb.Append(intendString));
                        }

                        break;
                    case ':':
                        sb.Append(ch);

                        if (!quoted)
                        {
                            sb.Append(" ");
                        }

                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}