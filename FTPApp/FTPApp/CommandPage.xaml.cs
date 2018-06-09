using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FTPApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CommandPage : ContentPage
    {
        List<string> commands = new List<string>()
        {
            "Screenshot",
            "Execute"
        };
        IClient client;
        public CommandPage(IClient client)
        {
            InitializeComponent();
            commandPicker.ItemsSource = commands;
            this.client = client;
        }
        void callbackSave(byte[] data, string path)
        {
            string fileName = path.Substring(path.LastIndexOf(@"\") + 1);
            string saveTitle = "";
            string saveLocation = DependencyService.Get<ISave>().getDownloadDirectory(fileName, out saveTitle);
            if (fileName.Contains(".txt"))
            {
                DependencyService.Get<ISave>().writeText(saveLocation, Encoding.UTF8.GetString(data, 0, data.Length));
            }
            else
            {
                DependencyService.Get<ISave>().writeFile(saveLocation, data);
            }
            DisplayAlert("FTP", fileName + " was saved to " + saveTitle, "OK");
            progressBar.IsVisible = false;
        }
        void setProgress(float progress)
        {
            this.progressBar.Progress = progress;
        }
        void onCommand(object sender, EventArgs e)
        {
            if(commandPicker.SelectedIndex == 0)
            {
                client.sendInt(packetTypes.p_screenshot);
                int size;
                client.recieveInt(out size);
                string nill;
                progressBar.IsVisible = true;
                client.message("Starting screenshot download...");
                client.download(size, DependencyService.Get<ISave>().getDownloadDirectory("screenshot.bmp", out nill), callbackSave, setProgress);
            }else if(commandPicker.SelectedIndex == 1)
            {
                Navigation.PushAsync(new FileList("C:", client, tasks.execute));
            }
        }
    }
}