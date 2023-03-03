using Google.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextToMp3.Model {
    class Settings : INotifyPropertyChanged {
        private static Settings _instance = null;
        private string _settingPath;
        private string _servicePath;
        private string _excelPath;
        private string _soundPath;
        private string _progress;
        public string servicePath { get { return _servicePath; } set { _servicePath = value; OnPropertyUpdate("servicePath"); } }
        public string excelPath { get { return _excelPath; } set { _excelPath = value; OnPropertyUpdate("excelPath"); } }
        public string soundPath { get { return _soundPath; } set { _soundPath = value; OnPropertyUpdate("soundPath"); } }
        public string settingPath { get { return _settingPath; } set { _settingPath = value; OnPropertyUpdate("settingPath"); } }

        [JsonIgnore]
        public string Progress { get { return _progress; } set { _progress = value; OnPropertyUpdate("Progress"); } }


        public static Settings Instance {
            get {
                if (_instance == null)
                    _instance = new Settings();
                return _instance;
            }
            set {
                _instance = value;
            }
        }
        Settings() {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyUpdate(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    class SettingStorage {
        public VoicePageView voicePageView { get; set; }
        public Settings settings { get; set; }
    }


}
