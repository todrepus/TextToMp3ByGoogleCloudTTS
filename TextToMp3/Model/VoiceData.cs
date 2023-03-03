using Google.Cloud.TextToSpeech.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Dynamic;
using Google.Api;
using System.Runtime.InteropServices;
using Google.Apis.Auth.OAuth2;

namespace TextToMp3.Model {
    [Serializable]
    class VoicePageView : INotifyPropertyChanged{
        
        private static VoicePageView _instance = null;
        
        public static VoicePageView Instance {
            get {
                if (_instance == null)
                    _instance = new VoicePageView();
                return _instance;
            }
        }
        
        int _pageIndex = 0;
        
        VoiceComboList _nowPage;

        public List<VoiceComboList> pages = new List<VoiceComboList>();
        
        public VoiceComboList NowPage {
            get {
                return _nowPage;
            }
            set {
                _nowPage = value;
                OnPropertyUpdate("NowPage");
            }
        }
        
        public int PageIndex {
            get {
                return _pageIndex;
            }
            set {
                if (value < 0 || value >= pages.Count) {
                    return;
                }
                if (pages.Count == 0)
                    return;
                _pageIndex = value;
                NowPage = pages[_pageIndex];
                PageIndexChanged?.Invoke(this, null);
                OnPropertyUpdate("PageIndex");
            }
        }

        VoicePageView() {
            
        }


        public event EventHandler PageIndexChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyUpdate(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void CopyTo(ref VoicePageView vp) {

            int i = 0;
            foreach (var page in this.pages) {
                var vpPage = vp.pages[i];
                page.CopyTo(ref vpPage);
                i++;
            }

            vp.PageIndex = 0;
        }

        public void Clear() {
            NowPage = null;
            pages.Clear();
        }

        public void UpdateAll() {
            OnPropertyUpdate("NowPage");
            OnPropertyUpdate("PageIndex");
        }
    }
    class VoiceComboList : INotifyPropertyChanged {

        public VoiceComboList() {
            _countrys = new List<VoiceCountryLevel>();
            _voiceTypes = new List<VoiceTypeLevel>();
            _voiceNames = new List<VoiceNameLevel>();
        }
        List<VoiceCountryLevel> _countrys;
        List<VoiceTypeLevel> _voiceTypes;
        List<VoiceNameLevel> _voiceNames;

        VoiceCountryLevel _country = null;
        VoiceTypeLevel _voiceType = null;
        VoiceNameLevel _voiceName = null;

        double _speed;

        [JsonIgnore]
        public string _category;

        public List<VoiceCountryLevel> countrys { 
            get { return _countrys; }
            set { 
                _countrys = value; 
                OnPropertyUpdate("countrys"); 
            }
        }
        public List<VoiceTypeLevel> voiceTypes { 
            get { return _voiceTypes; } 
            set {
                _voiceTypes = value;
                OnPropertyUpdate("voiceTypes");
            } 
        }
        public List<VoiceNameLevel> voiceNames { 
            get { return _voiceNames; } 
            set {
                _voiceNames = value;
                OnPropertyUpdate("voiceNames");
            } 
        }

        public string Category { get { return _category; } 
            set {
                _category = value;
                OnPropertyUpdate("Category");
            } 
        }
        public VoiceCountryLevel country { get { return _country; } set { _country = value; OnPropertyUpdate("country"); } }
        public VoiceTypeLevel voiceType { get { return _voiceType; } set { _voiceType = value; OnPropertyUpdate("voiceType"); } }
        public VoiceNameLevel voiceName { get { return _voiceName; } set { _voiceName = value; OnPropertyUpdate("voiceName"); } }
        public double Speed { get { return _speed; } set { _speed = Bound(value, 0.25, 4.00); OnPropertyUpdate("Speed"); OnPropertyUpdate("SpeedString"); } }
        public string SpeedString { get { return string.Format("Speed : {0}", _speed); }}

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyUpdate(string propertyName) {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private double Bound(double val, double min, double max) {
            if (val < min)
                val = min;
            else if (val > max)
                val = max;
            return val;
        }

        public void CopyTo(ref VoiceComboList vc) {
            VoiceStorage voiceStorage = VoiceStorage.Instance;

            if (_country?.country != null) {
                var linqResult =
                    from countryLevel in vc.countrys
                    where countryLevel.Id == this.country.Id && countryLevel.country == this.country.country
                    select countryLevel;

                if (linqResult.Count() == 0) {
                    return;
                }
                vc.country = linqResult.First();
            } else {
                return;
            }

            if (_voiceType?.voiceType != null) {
                var linqResult =
                    from voiceTypeLevel in vc.country.typeLevels
                    where voiceTypeLevel.voiceType == this.voiceType.voiceType
                    select voiceTypeLevel;

                if (linqResult.Count() == 0) {
                    return;
                }
                vc.voiceType = linqResult.First();
            } else {
                return;
            }

            if (_voiceName?.voiceName != null) {
                var linqResult =
                    from nameLevel in vc.voiceType.voiceNameSexs
                    where nameLevel.voiceName == this.voiceName.voiceName
                    select nameLevel;
                if (linqResult.Count() == 0)
                    return;

                vc.voiceName = linqResult.First();
            }

            vc.Speed = Speed;
            

            vc.Category = _category;
        }
    }


    class VoiceStorage {
        private static VoiceStorage _instance = null;
        public static VoiceStorage Instance {
            get {
                if (_instance == null) {
                    _instance = new VoiceStorage();
                }
                return _instance;
            }
        }

        public List<VoiceIdLevel> idRoots;

        VoiceStorage() {
            idRoots = new List<VoiceIdLevel>();
            Load();
        }

        public void Load() {
            const string fileName = @"Data\languages.json";
            if (!File.Exists(fileName)) {
                MessageBox.Show("Data\\languages.json이 없습니다. 프로그램을 종료합니다.");
                Application.Current.Shutdown();
                return;
            }

            using(StreamReader file = File.OpenText(fileName)) {
                using (JsonTextReader reader = new JsonTextReader(file)) {
                    JObject json = (JObject)JToken.ReadFrom(reader);
                    foreach(var idPair in json) {
                        VoiceIdLevel voiceIdLevel = new VoiceIdLevel();
                        voiceIdLevel.Id = idPair.Key;
                        foreach(var countryPair in idPair.Value as JObject) {
                            VoiceCountryLevel voiceCountryLevel = new VoiceCountryLevel();
                            voiceCountryLevel.country = countryPair.Key;
                            voiceCountryLevel.Id = idPair.Key;
                            foreach(var vTypePair in countryPair.Value as JObject) {
                                VoiceTypeLevel voiceTypeLevel = new VoiceTypeLevel();
                                voiceTypeLevel.voiceType = vTypePair.Key;
                                foreach(var vNameTuple in vTypePair.Value as JArray) {
                                    string name = vNameTuple["voiceName"].ToString();
                                    string sex = vNameTuple["sex"].ToString();
                                    VoiceNameLevel voiceNameLevel = new VoiceNameLevel(name, sex);
                                    voiceTypeLevel.voiceNameSexs.Add(voiceNameLevel);
                                }
                                voiceCountryLevel.typeLevels.Add(voiceTypeLevel);
                            }
                            voiceIdLevel.countryLevels.Add(voiceCountryLevel);
                        }
                        idRoots.Add(voiceIdLevel);
                    }
                }
            }
        }
    }

    class VoiceIdLevel {
        List<VoiceCountryLevel> _countryLevels;

        public string Id { get; set; }
        public List<VoiceCountryLevel> countryLevels { get { return _countryLevels; } }

        public VoiceIdLevel() {
            _countryLevels = new List<VoiceCountryLevel>();
        }

        public void CopyTo(ref VoiceIdLevel vi) {
            vi.Id = Id;
            int i = 0;
            foreach (var countryLevel in _countryLevels) {
                VoiceCountryLevel voiceCountryLevel = vi._countryLevels[i];
                countryLevel.CopyTo(ref voiceCountryLevel);
                i++;
            }
        }
    }
    class VoiceCountryLevel {

        List<VoiceTypeLevel> _typeLevels;
        public string country { get; set; }
        public string Id { get; set; }
        public List<VoiceTypeLevel> typeLevels { get { return _typeLevels; } }

        public VoiceCountryLevel() {
            _typeLevels = new List<VoiceTypeLevel>();
        }

        public void CopyTo(ref VoiceCountryLevel cl) {
            cl.Id = Id;
            cl.country = country;

            int i = 0;
            foreach (var typeLevel in typeLevels) {
                VoiceTypeLevel voiceTypeLevel = cl._typeLevels[i];
                typeLevel.CopyTo(ref voiceTypeLevel);
                i++;
            }
        }
    }
    class VoiceTypeLevel {
        // voice Type : voice Names
        List<VoiceNameLevel> _voiceNameSexs;

        public string voiceType { get; set; }
        public List<VoiceNameLevel> voiceNameSexs { get { return _voiceNameSexs; } }
        
        public VoiceTypeLevel() {
            _voiceNameSexs = new List<VoiceNameLevel>();
        }

        public void CopyTo(ref VoiceTypeLevel vl) {
            vl.voiceType = voiceType;

            int i = 0;
            foreach (var nameLevel in voiceNameSexs) {
                VoiceNameLevel voiceNameLevel = vl._voiceNameSexs[i];
                nameLevel.CopyTo(ref voiceNameLevel);
                i++;
            }
        }
    }

    class VoiceNameLevel {
        public string sex { get; set; }
        public string voiceName { get; set; }

        public Voice voice { get; set; } = null;
        public VoiceNameLevel(string _voiceName, string _sex) {
            sex = _sex;
            voiceName = _voiceName;
        }

        public void CopyTo(ref VoiceNameLevel nl) {
            nl.sex = sex;
            nl.voiceName = voiceName;
            nl.voice = voice?.Clone();
        }
    }

    
}
