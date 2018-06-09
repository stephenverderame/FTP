using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

[assembly: Dependency(typeof(FTPApp.Droid.Connection))]
namespace FTPApp.Droid
{
    public class Connection : IClient
    {
        TcpClient client;
        NetworkStream stream;
        bool connected = false;
        string _ip;
        public Connection()
        {
            _ip = "";
        }
        public void uponConnection(IAsyncResult res)
        {
            if (client.Connected)
            {
                connected = true;
                stream = client.GetStream();
            }
        }
        int IClient.connect(string ip)
        {

            Ping ping = new Ping();
            PingReply reply = ping.Send(ip, 2000);
            if(reply.RoundtripTime < 0 || reply.RoundtripTime > 2000)
            {
                return returnValues.CONNECTION_FAILED;
            }
            client = new TcpClient();
            AsyncCallback call = new AsyncCallback(uponConnection);
            client.BeginConnect(ip, 3250, call, client);
            return returnValues.OK;
        }
        int IClient.recieveInt(out Int32 data)
        {
            byte[] number = new byte[4];
            int bytesSent = 0;
            while (bytesSent < 4)
            {
                int readByte = stream.Read(number, bytesSent, 4 - bytesSent);
                bytesSent += readByte;
            }
            int dataBuffer = BitConverter.ToInt32(number, 0);
            data = IPAddress.NetworkToHostOrder(dataBuffer);
            return 0;
        }
        public async void download(int size, string path, Action<byte[], string> callback, Action<float> progressCallback)
        {
            byte[] buffer = new byte[size];
            int bytesRead = 0;
            while(bytesRead < size)
            {
                int readByte = stream.Read(buffer, bytesRead, size - bytesRead);
                bytesRead += readByte;
                float p = (float)bytesRead / size;
                progressCallback(p);
                await Task.Delay(5);
            }
            callback(buffer, path);
        }
        int IClient.recieveData(out byte[] data, Int32 size)
        {
            try
            {
                byte[] buffer = new byte[size];
                int bytesRead = 0;
                while (bytesRead < size)
                {
                    int readByte = stream.Read(buffer, bytesRead, size - bytesRead);
                    bytesRead += readByte;
                }
                data = buffer;
            }
            catch (System.OutOfMemoryException e)
            {
                data = Encoding.UTF8.GetBytes("null");
                return -1;
            }
            return 0;
        }
        int IClient.sendInt(Int32 data)
        {
            byte[] number = new byte[4];
//          Int32 htonl = IPAddress.HostToNetworkOrder(data);
            number = BitConverter.GetBytes(data);
            stream.Write(number, 0, 4);
            return 0;
        }
        int IClient.sendData(byte[] data)
        {
            stream.Write(data, 0, data.Length);
            return 0;
        }
        int IClient.disconnect()
        {
            byte[] packet = new byte[4];
            packet = BitConverter.GetBytes(packetTypes.p_disconnect);
            stream.Write(packet, 0, 4);
            connected = false;
            return 0;
        }

        public bool isConnected()
        {
            if(connected && client.Connected && stream == null)
            {
                stream = client.GetStream();
            }
            return connected;
        }

        public void message(string message)
        {
            Toast.MakeText(Android.App.Application.Context, message, ToastLength.Long).Show();
        }
    }
}