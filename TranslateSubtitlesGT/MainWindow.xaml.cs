using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Printing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
using Path = System.IO.Path;

namespace TranslateSubtitlesGT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string inputDir = "";
        private string outputDir = "";
        private string supportedExtensions = "";
        private string fromLang = "";
        private string toLang = "";

        public MainWindow()
        {
            InitializeComponent();
            CommandBinding cb = new CommandBinding(ApplicationCommands.Copy, CopyCmdExecuted, CopyCmdCanExecute);

            this.listBoxMsg.CommandBindings.Add(cb);
        }

        private void InputPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderPicker();
            if (dlg.ShowDialog() == true)
            {
                TxtBxInput.Text = dlg.ResultPath;
            }
        }

        private void OutputPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderPicker();
            if (dlg.ShowDialog() == true)
            {
                TxtBxOutput.Text = dlg.ResultPath;
            }
        }

        private void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            inputDir = TxtBxInput.Text;
            outputDir = TxtBxOutput.Text;
            supportedExtensions = TxtBxSupportedExtensions.Text;
            fromLang = TxtBxFromLang.Text;
            toLang = TxtBxToLang.Text;
            if (inputDir == "" || outputDir == "" || supportedExtensions == "" || fromLang == "" || toLang == "")
            {
                return;
            }

            CopyAndTranslate();
        }

        private void CopyAndTranslate()
        {
            var allFiles = Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories).Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()));

            if (allFiles.Count()>0)
            {
                listBoxMsg.Items.Clear();

                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;
                worker.DoWork += worker_DoWork;
                worker.ProgressChanged += worker_ProgressChanged;

                worker.RunWorkerAsync();

            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
            listBoxMsg.Items.Add(e.UserState);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var allFiles = Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories).Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()));
            var counter = 0;
            var allFilesCount = allFiles.Count();
            foreach (var fileInput in allFiles)
            {
                try
                {
                    var fileOutput = fileInput.Replace(inputDir, outputDir);
                    if (File.Exists(fileOutput))
                    {
                        File.Delete(fileOutput);
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(fileOutput));
                    TranslateSubs(fileInput, fileOutput);
                    counter++;
                    var progressBarPercent = counter * 100 / allFilesCount;
                    (sender as BackgroundWorker).ReportProgress(progressBarPercent, fileOutput);
                }
                catch (Exception)
                {

                    throw;
                }
            }
            (sender as BackgroundWorker).ReportProgress(100, "Success!");
        }

        private void TranslateSubs(string fileInput, string fileOutput)
        {
            string[] lines = File.ReadAllLines(fileInput);
            foreach (var line in lines)
            {
                using (var tw = new StreamWriter(fileOutput, true))
                {
                    if (line.Length > 0 && !char.IsDigit(line[0]))
                    {
                        var translatedstring = Translate(line);
                        tw.WriteLine(translatedstring);
                    }
                    else
                    {
                        tw.WriteLine(line);
                    }
                }
            }
        }

        private String Translate(String stringToTranslate)
        {
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLang}&tl={toLang}&dt=t&q={HttpUtility.UrlEncode(stringToTranslate)}";

            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = httpClient.Send(request);
            using var reader = new StreamReader(response.Content.ReadAsStream());
            var responseBody = reader.ReadToEnd();
            try
            {
                responseBody = responseBody.Substring(4, responseBody.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                return responseBody;
            }
            catch
            {
                return stringToTranslate;
            }
        }

        void CopyCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            ListBox lb = e.OriginalSource as ListBox;
            string copyContent = String.Empty;
   
            foreach (var item in lb.SelectedItems)
            {
                copyContent += item.ToString();
                // Add a NewLine for carriage return   
                copyContent += Environment.NewLine;
            }
            Clipboard.SetText(copyContent);
        }
        void CopyCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            ListBox lb = e.OriginalSource as ListBox;
            // CanExecute only if there is one or more selected Item.   
            if (lb.SelectedItems.Count > 0)
                e.CanExecute = true;
            else
                e.CanExecute = false;
        }

    }
}
