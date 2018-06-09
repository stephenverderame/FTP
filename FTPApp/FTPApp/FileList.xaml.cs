using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Diagnostics;
using System.Threading;

namespace FTPApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FileList : ContentPage
    {
        IClient client;
        tasks task;
        List<string> jumps = new List<string>()
        {
            "Desktop",
            "Documents",
            "Downloads",
            "Music",
            "Pictures",
            "Videos",
            "Local Disk"
        };
        public FileList(List<listItem> source, string title, IClient client, tasks task = tasks.download)
        {
            InitializeComponent();
            fileSystem.ItemsSource = source;
            Title = title;
            this.client = client;
            jump.ItemsSource = jumps;
            this.task = task;
        }
        public FileList(string title, IClient client, tasks task)
        {
            InitializeComponent();
            Title = title;
            this.client = client;
            jump.ItemsSource = jumps;
            this.task = task;
            openDirectory("c:");
        }
        void onItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if(e.SelectedItem == null)
            {
                return;
            }
            var item = (listItem)e.SelectedItem;
            openDirectory(item.path);
            ((ListView)sender).SelectedItem = null;
        }
        void newJump(object sender, EventArgs e)
        {
            if(jump.SelectedIndex == 6)
            {
                openDirectory("c:");
            }else
            {
                string path = "XF!" + jumps[jump.SelectedIndex] + "-";
                openDirectory(path);
            }
        }
        void openDirectory(string path)
        {
            switch (task)
            {
                case tasks.download:
                    client.sendInt(packetTypes.p_dirGet);
                    break;
                case tasks.execute:
                    client.sendInt(packetTypes.p_execute);
                    break;
            }           
            client.sendInt(path.Length);
            client.sendData(Encoding.UTF8.GetBytes(path));
            Debug.WriteLine("Sent path: " + path);
            int amountOfFiles;
            client.recieveInt(out amountOfFiles);
            if (amountOfFiles == packetTypes.p_isFile)
            {
                if (task == tasks.download)
                {
                    saveFile(path);
                }else if(task == tasks.execute)
                {
                    DisplayAlert("FTP", "File opened or ran", "OK");
                }
                return;
            }
            List<listItem> files = new List<listItem>();
            for (int i = 0; i < amountOfFiles; i++)
            {
                int size;
                client.recieveInt(out size);
                byte[] name = new byte[size];
                if(client.recieveData(out name, size) == -1)
                {
                    DisplayAlert("FTP", "An error has occured. Please try again", "OK");
                    break;
                }
                string nameStr = Encoding.UTF8.GetString(name, 0, size);
                listItem item = new FTPApp.listItem(nameStr, path + "\\" + nameStr);
                if (nameStr.ToLower().Contains(".png") || nameStr.ToLower().Contains(".jpg") || nameStr.ToLower().Contains(".bmp") || nameStr.ToLower().Contains(".jpeg"))
                {
                    item.image = "image.png";
                }
                else if (nameStr.Equals("."))
                {
                    item.image = "refreshIcon.png";
                }
                else if (nameStr.Equals(".."))
                {
                    item.image = "backArrow.png";
                }
                else if (nameStr.ToLower().Contains(".wav") || nameStr.ToLower().Contains(".mp3") || nameStr.ToLower().Contains(".mp4") || nameStr.ToLower().Contains(".aiff") || nameStr.ToLower().Contains(".mov") || nameStr.ToLower().Contains(".wmo"))
                {
                    item.image = "mediaIcon.png";
                }
                else if (!nameStr.Contains("."))
                {
                    item.image = "folderIcon.png";
                }
                else
                {
                    item.image = "file.png";
                }
                files.Add(item);
            }
            fileSystem.ItemsSource = files;
            Title = path;
        }
        void saveFile(string path)
        {
            int fileSize;
            client.recieveInt(out fileSize);
            client.message("Starting file download...");
            this.progress.IsVisible = true;
            this.progress.Progress = 0;
            client.download(fileSize, path, callbackSave, setProgress);

        }
        void setProgress(float progress)
        {
            this.progress.Progress = progress;
            if (progress == 1) this.progress.IsVisible = false;
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
                Debug.WriteLine("Writing binary data of size: " + data.Length);
                DependencyService.Get<ISave>().writeFile(saveLocation, data);
            }
            DisplayAlert("FTP", fileName + " was saved to " + saveTitle, "OK");
            progress.IsVisible = false;
        }
    }
}