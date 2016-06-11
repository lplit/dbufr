using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace dbufr
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string
            id,
            pass,
            html,
            ue_list,
            grades_list,
            f_dbu,
            ask_info,
            wait_downloading,
            version = "v1.0.7";
        private List<Item> items = new List<Item>();


        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            setup();
        }

        /// <summary>
        /// Saves/reads user/pass from file to login
        /// </summary>
        private async void setup()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile f_dbufr;
            id = "";
            pass = "";
            html = "";
            ue_list = "";
            grades_list = "";
            f_dbu = "dbufr_info.txt";
            ask_info = "Pour visualiser les notes, voir section Infos.";
            wait_downloading = "Patientez...";
            try // File exists, read user/pass, download stuff. 
            {
                f_dbufr = await storageFolder.GetFileAsync(f_dbu);
                string file = await FileIO.ReadTextAsync(f_dbufr);
                string[] t_id = file.Split(' ');
                id = t_id[0];
                pass = t_id[1];
                image_file_state.Source = new BitmapImage(new Uri("ms-appx:///Assets/file_ok.png"));
                setWaitTexts();
                UpdateLayout();
                setTexts();
            }
            catch (FileNotFoundException) // File does not exist ie no data
            {
                resetTexts();
            }

        }

        /// <summary>
        /// Sets HTML code to codeShow textBlock
        /// </summary>
        private async void setTexts()
        {
            html = await httpRequest(id, pass);
            string parsed = parseHTML();
            tb_rawHTML.Text = html;
            tb_listeUE.Text = ue_list;
            tb_grades.Text = grades_list;
            tb_LastUpdate.Text = version + " - MAJ Data: " +  DateTime.Now.ToString();
        }


        /// <summary>
        /// Puts back the placeholder text. Used in case user decides to remove sign in details. 
        /// </summary>
        private void resetTexts()
        {
            tb_rawHTML.Text = ask_info;
            tb_listeUE.Text = ask_info;
            tb_grades.Text = ask_info;
        }

        private void setWaitTexts()
        {
            tb_rawHTML.Text = wait_downloading;
            tb_listeUE.Text = wait_downloading;
            tb_grades.Text = wait_downloading;
        }
        /// <summary>
        /// Grabs raw HTML code from website
        /// </summary>
        /// <returns></returns>
        private async Task<string> httpRequest(string id, string pass)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www-dbufr.ufr-info-p6.jussieu.fr/lmd/2004/master/auths/seeStudentMarks.php");
            request.Credentials = new NetworkCredential(id, pass);
            String ret = "";
            using (var response = (HttpWebResponse)(await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)))
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(responseStream))
                    {
                        ret = await sr.ReadToEndAsync();
                        return ret;
                    }
                }
            }
        }

        private String parseHTML()
        {
            HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            string ret = "";
            int i = 0, j = 0, k = 0;
            foreach (HtmlNode table in doc.DocumentNode.SelectNodes("//table"))
            {
                if (i < 2)
                {
                    i++;
                    continue;
                }
                ret += ("\nTab " + i + table.Id);
                j = 0;
                foreach (HtmlNode row in table.SelectNodes("tr"))
                {
                    if (j < 1)
                    {
                        j++;
                        continue;
                    }
                    if (i == 2)
                        ue_list += "\n";
                    if (i == 3)
                        grades_list += "\n";
                    ret += "\n\t\tRow " + j;
                    k = 0;
                    foreach (HtmlNode cell in row.SelectNodes("th|td"))
                    {
                        if (i == 2)
                            ue_list += "\n" + cell.InnerText;
                        if (i == 3)
                        {  // Notes 
                            grades_list += "\n" + cell.InnerText;
                            /* 
                            chop[0] - UE "4I001-2015oct", "LI115-2011dec"
                                TODO UE 4I001-2015oct needs to be cut down into ue=4I001, year=2015, semestre=oct
                            chop[1] - Type de controle "[examen-reparti-1] creation du 26-11-2015-533161"
                            chop[2] - note "15/20"
                                TODO Note "15/20" cut down to note=15.0;
                            */
                            string[] chop = cell.InnerText.Split('\n');
                            /*Regex.IsMatch(box_etu.Text, @"^\d+$";
                            Regex.
                            */


                        }
                        ret += "\n\t\t\tData " + k + " " + cell.InnerText;
                        k++;
                    }
                    j++;
                }
                i++;
            }
            return ret.TrimStart(' ');
        }

        /************** BUTTONS **************/

        private async void bar_delete_Click(object sender, RoutedEventArgs e)
        {
            id = "";
            pass = "";
            try // Will fail if file doesn't exist
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile f_dbufr = await storageFolder.GetFileAsync(f_dbu);
                await f_dbufr.DeleteAsync();
                resetTexts();
                image_file_state.Source = new BitmapImage(new Uri("ms-appx:///Assets/file_x.png"));
            }
            catch (Exception) { }
            resetTexts();
        }


        private async void bar_save_Click(object sender, RoutedEventArgs e)
        {
            if (Regex.IsMatch(box_etu.Text, @"^\d+$")) // Won't save unless id is digits only
            {
                id = box_etu.Text;
                pass = box_pass.Text;
                box_etu.Text = "";
                box_pass.Text = "";

                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile f_dbufr;
                f_dbufr = await storageFolder.CreateFileAsync(f_dbu, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(f_dbufr, id + " " + pass);
                setup(); // Fetch data once saved.
            }
        }

        private void bar_sync_Click(object sender, RoutedEventArgs e)
        {
            setup();
        }

    }
}
