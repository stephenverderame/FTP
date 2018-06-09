using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace FTPApp
{
    public enum tasks
    {
        download,
        execute
    }
    public class listItem
    {
        public listItem(string name, string path)
        {
            fileName = name;
            this.path = path;
        }
        public string fileName { get; set; }
        public string path { get; set; }
        public string image { get; set; }
    }
    public class serverItem
    {
        public string ip { get; set; }
        public string name { get; set; }
    }
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new FTPApp.MainPage());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }
        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
