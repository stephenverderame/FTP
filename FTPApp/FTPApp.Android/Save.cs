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
using System.IO;

[assembly: Dependency(typeof(FTPApp.Droid.Save))]
namespace FTPApp.Droid
{
    class Save : ISave
    {
        public string getDownloadDirectory(string filename, out string directory)
        {
/*            if(filename.ToLower().Contains(".png") || filename.ToLower().Contains(".jpg") || filename.ToLower().Contains(".bmp") || filename.ToLower().Contains(".jpeg"))
            {
                directory = "Pictures";
                return Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).AbsolutePath, filename);
            }else if(filename.ToLower().Contains(".wav") || filename.ToLower().Contains(".mp3") || filename.ToLower().Contains(".mpeg"))
            {
                directory = "Music";
                return Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath, filename);
            }else if (filename.ToLower().Contains(".mp4") || filename.ToLower().Contains(".avi") || filename.ToLower().Contains(".mov"))
            {
                directory = "Movies";
                return Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).AbsolutePath, filename);
            }else if (filename.ToLower().Contains(".txt"))
            {
                directory = "Documents";
                return Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath, filename);
            }
            */
            directory = "Downloads";
            return Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, filename);
        }

        public void writeFile(string path, byte[] data)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(data);
                //the using statement auto closes things that implement IDisposable
            }

        }
        public List<serverItem> getList()
        {
            var filename = "servers.dat";
            var fpath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            var path = Path.Combine(fpath, filename);
            List<serverItem> servers = new List<serverItem>();
            try
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        string _name = reader.ReadString();
                        string _ip = reader.ReadString();
                        servers.Add(new serverItem() { name = _name, ip = _ip });
                    }

                }
            }
            catch (System.IO.FileNotFoundException)
            {

            }
            return servers;
        }
        public void editItem(serverItem item)
        {
            var filename = "servers.dat";
            var fpath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            var path = Path.Combine(fpath, filename);
            List<serverItem> servers = new List<serverItem>();
            try
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open)))
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        string _name = reader.ReadString();
                        string _ip = reader.ReadString();
                        servers.Add(new serverItem() { name = _name, ip = _ip });
                    }

                }
                using (BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Append)))
                {
                    writer.Write(servers.Count + 1);
                    for (int i = 0; i < servers.Count; i++)
                    {
                        writer.Write(servers[i].name);
                        writer.Write(servers[i].ip);
                    }
                    writer.Write(item.name);
                    writer.Write(item.ip);
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                using(BinaryWriter writer = new BinaryWriter(new FileStream(path, FileMode.Create)))
                {
                    writer.Write(1);
                    writer.Write(item.name);
                    writer.Write(item.ip);
                }
            }
        }
        public void readFile(string path, out byte[] data)
        {
            int size = (int)(new FileInfo(path).Length);
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                byte[] buffer = new byte[size];
                reader.Read(buffer, 0, size);
                data = buffer;
            }
        }
        public void readText(string path, out string data)
        {
            using(StreamReader file = new StreamReader(path))
            {
                data = file.ReadToEnd();
            }
        }
        public void writeText(string path, string data)
        {
            System.Diagnostics.Debug.WriteLine("Read data: " + data);
            using(StreamWriter file = new StreamWriter(path))
            {
                file.WriteLine(data);
            }
        }
    }
}