using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Diagnostics;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using System.Threading;

namespace FTPApp
{
    public partial class MainPage : ContentPage
    {
        IClient client; 
        public MainPage()
        {
            InitializeComponent();
            client = DependencyService.Get<IClient>();
        }
        public void changeId(string id)
        {
            ipLabel.Text = id;
        }
        void onDisconnect(object sender, EventArgs e)
        {
            if (client.isConnected())
            {
                client.disconnect();
                ipLabel.Text = "Id: ";
                ipField.Text = "";
            }else
            {
                DisplayAlert("FTP", "The client was never connected to a server", "OK");
            }
        }
        void onSendFiles(object sender, EventArgs e)
        {
            if (!client.isConnected())
            {
                DisplayAlert("FTP", "No server connected", "OK");
                return;
            }
            FileData data = null;
            Task.Run(async () =>
            {
                data = await CrossFilePicker.Current.PickFile();
                sendFiles(data);
            });            

            
        }
        void sendFiles(FileData data)
        {
            if(!client.isConnected())
            {
                DisplayAlert("FTP", "No server connected", "OK");
                return;
            }
            Debug.WriteLine("File chosen: " + data.FileName);
            client.sendInt(packetTypes.p_fileSend);
            client.sendInt(data.FileName.Length);
            byte[] buffer = new byte[data.FileName.Length];
            buffer = Encoding.UTF8.GetBytes(data.FileName);
            client.sendData(buffer);
            client.sendInt(data.DataArray.Length);
            client.sendData(data.DataArray);
            DisplayAlert("FTP", "File sent to server!", "OK");
        }
        void viewServerList(object sender, EventArgs e)
        {
            Navigation.PushAsync(new ServerList(client, this));
        }
        void onGetFiles(object sender, EventArgs e)
        {
            if (!client.isConnected())
            {
                DisplayAlert("FTP", "No server connected", "OK");
                return;
            }
            client.sendInt(packetTypes.p_dirGet);
            string path = "c:";
            client.sendInt(path.Length);
            client.sendData(Encoding.UTF8.GetBytes(path));
            int amountOfFiles;
            client.recieveInt(out amountOfFiles);
            Debug.WriteLine("Amount of files: " + amountOfFiles);
            if(amountOfFiles == packetTypes.p_isFile)
            {
                DisplayAlert("FTP", "Cannot get directory of file", "OK");
                return;
            }
            List<listItem> files = new List<listItem>();
            for(int i = 0; i < amountOfFiles; i++)
            {
                Debug.WriteLine("Getting file");
                int size;
                client.recieveInt(out size);
                Debug.WriteLine("Got size: " + size);
                byte[] name = new byte[size];
                client.recieveData(out name, size);
                Debug.WriteLine("Recieved data!");
                string nameStr = Encoding.UTF8.GetString(name, 0, size);
                Debug.WriteLine("Got name: " + nameStr);
                listItem item = new FTPApp.listItem(nameStr, path + "\\" + nameStr);
                if(nameStr.Contains(".png") || nameStr.Contains(".jpg") || nameStr.Contains(".bmp") || nameStr.Contains(".jpeg"))
                {
                    item.image = "image.png";
                }else if (!nameStr.Contains("."))
                {
                    item.image = "folderIcon.png";
                }else
                {
                    item.image = "file.png";
                }
                files.Add(item);
            }
            Navigation.PushAsync(new FileList(files, path, client));
        }
        void onCommands(object sender, EventArgs e)
        {
            if (!client.isConnected())
            {
                DisplayAlert("FTP", "Not connected to  a server", "OK");
                return;
            }
            Navigation.PushAsync(new CommandPage(client));
        }
        void connectToServer(object sender, EventArgs e)
        {
            if(client.connect(ipField.Text) == returnValues.CONNECTION_FAILED)
            {
                DisplayAlert("FTP", "Connection failed. Is it a valid ip address?", "OK");
                return;
            }
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (!client.isConnected())
            {
                if(timer.ElapsedMilliseconds >= 3000)
                {
                    DisplayAlert("FTP", "Timeout reached. Failed to connect to server. Is it a valid ip?", "OK");
                    break;
                }
            }
            if (client.isConnected())
            {
                Int32 size;
                client.recieveInt(out size);
                if (size == returnValues.MAX_CONNECTIONS_REACHED)
                {
                    DisplayAlert("FTP", "Failed to connect to server: maximum clients reached", "OK");
                }
                else
                {
                    byte[] ip = new byte[size];
                    client.recieveData(out ip, size);
                    Debug.WriteLine("Recieving message of size: " + size);
                    string ipStr = Encoding.UTF8.GetString(ip, 0, size);
                    ipLabel.Text = "Id: " + ipStr;
                }
            }
        }
    }
}
