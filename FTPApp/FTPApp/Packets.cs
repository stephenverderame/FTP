using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPApp
{
    public struct returnValues
    {
        public static int MAX_CONNECTIONS_REACHED = -20;
        public static int OK = 0;
        public static int CONNECTION_FAILED = -30;
    }
    public struct packetTypes
    {
        public static int p_fileGet = 0;
        public static int p_fileSend = 1;
        public static int p_dirGet = 2;
        public static int p_disconnect = 3;
        public static int p_formatPath = 4;
        public static int p_screenshot = 5;
        public static int p_execute = 6;
        public static int p_isFile = -1;
    }
    public interface IClient
    {
        int connect(string ip);
        int recieveInt(out Int32 data);
        int recieveData(out byte[] data, Int32 size);
        int sendInt(Int32 data);
        int sendData(byte[] data);
        int disconnect();
        bool isConnected();
        void uponConnection(IAsyncResult res);
        void download(int size, string path, Action<byte[], string> callback, Action<float> progressCallback);
        void message(string message);
    }
    public interface ISave
    {
        string getDownloadDirectory(string filename, out string directory);
        void writeFile(string path, byte[] data);
        void writeText(string path, string data);
        void readFile(string path, out byte[] data);
        void readText(string path, out string data);
        List<serverItem> getList();
        void editItem(serverItem item);       
    }
    
}
