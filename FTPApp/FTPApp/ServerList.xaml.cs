using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FTPApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ServerList : ContentPage
    {
        IClient client;
        MainPage page;
        public ServerList(IClient client, MainPage page)
        {
            InitializeComponent();
            serverView.ItemsSource = DependencyService.Get<ISave>().getList();
            this.client = client;
            this.page = page;
        }
        void onTap(object sender, ItemTappedEventArgs e)
        {
            if (e.Item == null) return;
            var item = (serverItem)e.Item;
            if (client.connect(item.ip) == returnValues.CONNECTION_FAILED)
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
                    DisplayAlert("FTP", "Failed to connect to server. Is it a valid ip address?", "OK");
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
                    return;
                }
                else
                {
                    byte[] ip = new byte[size];
                    client.recieveData(out ip, size);
                    string ipStr = Encoding.UTF8.GetString(ip, 0, size);
                    page.changeId("Id: " + ipStr);
                }
                DisplayAlert("FTP", "Connected to: " + item.ip, "OK");
            }
        }
        void addServer(object sender, EventArgs e)
        {
            var item = new serverItem();
            item.name = nameField.Text;
            item.ip = ipField.Text;
            DependencyService.Get<ISave>().editItem(item);
            nameField.Text = ""; ipField.Text = "";
            serverView.ItemsSource = DependencyService.Get<ISave>().getList();
        }
    }
}