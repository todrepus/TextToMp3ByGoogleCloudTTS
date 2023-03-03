using Google.Cloud.TextToSpeech.V1;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TextToMp3.Model;

namespace TextToMp3 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        TTS tts = new TTS();
        Settings settings;
        VoiceStorage voiceStorage;
        VoicePageView voicePageView { get; set; }
        ExcelLoader excelLoader;


        bool bProcessing = false;
        bool bPlayingSound = false;
        WMPLib.WindowsMediaPlayer player;

        public MainWindow() {
            InitializeComponent();

            // Setting 폴더 만들기
            if (!Directory.Exists("Setting")) {
                Directory.CreateDirectory("Setting");
            }


            // Settings 바인딩
            settings = Settings.Instance;
            this.DataContext = settings;

            // 지원언어 불러오기
            voiceStorage = VoiceStorage.Instance; // 내부적으로 불러옴.

            // PageView 셋팅
            voicePageView = VoicePageView.Instance;
            voicePageView.PageIndexChanged += this.PageChanged;

            // 엑셀 로더 셋팅
            excelLoader = ExcelLoader.Instance;

            // Preset 불러오기
            // LoadPreset();

            // PLAYER 셋팅
            player = new WMPLib.WindowsMediaPlayer();
            player.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(OnPlaySoundEnded);


        }

        private void SaveSettings() {
            var result = MessageBox.Show("현재 설정을 저장하시겠습니까?", "알림", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) {
                return;
            }

            if (!Directory.Exists("Setting")) {
                Directory.CreateDirectory("Setting");
            }
            string savePath = System.Environment.CurrentDirectory + @"\Setting";

            if (!string.IsNullOrEmpty(settings.settingPath) && Directory.Exists(System.IO.Path.GetDirectoryName(settings.settingPath))) {
                savePath = System.IO.Path.GetDirectoryName(settings.settingPath);
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "서비스 계정키";
            dlg.Filter = "json files (*.json)|*.json";
            dlg.InitialDirectory = savePath;
            dlg.FileName = System.IO.Path.GetFileNameWithoutExtension(settings.settingPath);

            bool? dlgResult = dlg.ShowDialog();
            if (dlgResult != true) {
                return;
            }



            SettingStorage settingStorage = new SettingStorage();
            settingStorage.settings = settings;
            settingStorage.voicePageView = voicePageView;

            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(dlg.FileName)) {
                using (JsonWriter writer = new JsonTextWriter(sw)) {
                    serializer.Serialize(writer, settingStorage);
                }
            }

            settings.settingPath = dlg.FileName; // 경로가 변경되면, 자동으로 불러오기때문에, settingPath에 마지막에 대입.
        }
        private void BtnCloseApp_Click(object sender, RoutedEventArgs e) {

            // 저장작업
            SaveSettings();


            Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) {
            App.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void OnMouseDown_TitleBar(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {

                App.Current.MainWindow.DragMove();
            }
        }

        private async void BtnTransform_Click(object sender, RoutedEventArgs e) {
            if (bProcessing) {
                return;
            }

            //현재 설정 저장.
            SaveSettings();

            if (!tts.IsAvailable) {
                MessageBox.Show("서비스 계정키부터 입력해주세요.");
                return;
            }
            if (string.IsNullOrEmpty(settings.excelPath) || voicePageView.pages.Count == 0) {
                MessageBox.Show("데이터가 비어있습니다! 엑셀파일을 불러왔는지 확인해주세요.");
                return;
            }

            if (String.IsNullOrEmpty(settings.soundPath)) {
                MessageBox.Show("MP3저장 폴더가 비어있습니다! 저장폴더를 설정했는지 확인해주세요.");
                return;
            }

            foreach (var page in voicePageView.pages) {


                if (page.country == null) {
                    MessageBox.Show(String.Format("{0} - Language 항목이 비었습니다!", page.Category));
                    return;
                }

                if (page.voiceType == null) {
                    MessageBox.Show(String.Format("{0} - Voice Type 항목이 비었습니다!", page.Category));
                    return;
                }

                if (page.voiceName == null) {
                    MessageBox.Show(String.Format("{0} - Voice Name 항목이 비었습니다!", page.Category));
                    return;
                }
            }

            TransformAsync();
        }

        private async Task TransformAsync() {
            await Task.Run(() => {
                bProcessing = true;
                int total = 0;
                foreach (var page in voicePageView.pages) {
                    total += excelLoader.SentenceRows[page.Category].Count;
                }
                settings.Progress = String.Format("0 / {0}", total);

                int current = 0;

                foreach (var page in voicePageView.pages) {
                    string cate = page.Category;
                    int start = cate.IndexOf("(") + 1;
                    int end = cate.IndexOf(")", start);
                    string langCode = cate.Substring(start, end - start);

                    Voice voice = page.voiceName.voice;
                    foreach (var sRow in excelLoader.SentenceRows[page.Category]) {
                        string soundName = String.Format("E{0}_{1}({2}).mp3", sRow.episode, langCode, sRow.order);
                        tts.TransformText(sRow.senetence, page.country.Id, voice, page.Speed, settings.soundPath + "\\" + soundName);
                        settings.Progress = String.Format("{0} / {1}", ++current, total);
                    }
                }
                bProcessing = false;
                MessageBox.Show("변환 완료");
            });
        }
        private void BtnServicePath_Click(object sender, RoutedEventArgs e) {
            //C:\Users\KangHyunWoo\source\repos\ConsoleApp1\ConsoleApp1\bin\Debug
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "서비스 계정키";
            dlg.Multiselect = false;
            dlg.Filter = "json files (*.json)|*.json";
            dlg.InitialDirectory = System.Environment.CurrentDirectory;
            bool? result = dlg.ShowDialog();
            if (result == true) {
                settings.servicePath = dlg.FileNames[0];
            }
        }

        private void BtnExcelPath_Click(object sender, RoutedEventArgs e) {
            if (!tts.IsAvailable) {
                MessageBox.Show("서비스 계정키부터 먼저 등록해주세요.");
                return;
            }
            //C:\Users\KangHyunWoo\source\repos\ConsoleApp1\ConsoleApp1\bin\Debug
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "엑셀 파일";
            dlg.Multiselect = false;
            dlg.Filter = "excel files (*.xls, *.xlsx, *.csv)|*.xls;*.xlsx;*.csv";
            dlg.InitialDirectory = System.Environment.CurrentDirectory;
            bool? result = dlg.ShowDialog();
            if (result == true) {
                settings.excelPath = dlg.FileNames[0];
            }
        }

        private void BtnSoundPath_Click(object sender, RoutedEventArgs e) {
            //C:\Users\KangHyunWoo\source\repos\ConsoleApp1\ConsoleApp1\bin\Debug
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "MP3 저장 폴더";
            dlg.Title = "MP3파일을 저장할 폴더를 선택해주세요.";
            dlg.IsFolderPicker = true;
            dlg.InitialDirectory = System.Environment.CurrentDirectory;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = System.Environment.CurrentDirectory;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok) {
                var folder = dlg.FileName;
                settings.soundPath = folder;
                // Do something with selected folder string
            }
        }

        private void ComboCountry_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ComboCountry.SelectedItem == null) {
                voicePageView.NowPage.voiceTypes = null;
                voicePageView.NowPage.voiceNames = null;
                return;
            }

            VoiceCountryLevel countryLevel = ComboCountry.SelectedItem as VoiceCountryLevel;
            voicePageView.NowPage.voiceTypes = countryLevel.typeLevels;
        }

        private void ComboVTypes_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ComboVTypes.SelectedItem == null) {
                voicePageView.NowPage.voiceNames = null;
                return;
            }

            VoiceTypeLevel voiceTypeLevel = ComboVTypes.SelectedItem as VoiceTypeLevel;
            voicePageView.NowPage.voiceNames = voiceTypeLevel.voiceNameSexs;
        }

        private async void TextBoxServicePath_TextChanged(object sender, TextChangedEventArgs e) {
            //TTS 초기화
            await InitTTS();

            //var countrys = from idLevel in voiceStorage.idRoots
            //               from countryLevel in idLevel.countryLevels
            //               select countryLevel;
            //voicePageView.NowPage.countrys = countrys.ToList();
        }

        private void TextBoxExcelPath_TextChanged(object sender, TextChangedEventArgs e) {
            if (!tts.IsAvailable) {
                return;
            }
            if (string.IsNullOrEmpty(settings.excelPath)) {
                return;
            }
            if (!File.Exists(settings.excelPath)) {
                MessageBox.Show("엑셀 파일 경로를 찾을 수 없습니다.");
                settings.excelPath = null;
                return;
            }

            bool bSame = excelLoader.LoadExcel(settings.excelPath);

            // 기존 페이지 초기화
            VoicePageView voicePageView = VoicePageView.Instance;
            if (!bSame)
                voicePageView.Clear();
            else {
                goto PageLoadExit;
            }

            InitPage();

            PageLoadExit:
            if (voicePageView.pages.Count > 0) {
                voicePageView.PageIndex = 0;
            }

        }

        private void InitPage() {
            for (int i = ExcelLoader.CATEGORY_LANG_START; i < excelLoader.Categories.Count; ++i) {

                VoiceComboList voiceComboList = new VoiceComboList();
                string cate = excelLoader.Categories[i];
                voiceComboList.Category = cate;

                int start = cate.IndexOf("(") + 1;
                int end = cate.IndexOf(")", start);
                string langCode = cate.Substring(start, end - start) + "-";

                var countrys =
                    from idLevel in voiceStorage.idRoots
                    from countryLevel in idLevel.countryLevels
                    select countryLevel;

                voiceComboList.countrys = countrys.ToList();
                // find has same language code
                var linqResult =
                    from countryLevel in countrys
                    where countryLevel.Id.ToLower().Contains(langCode.ToLower())
                    select countryLevel;

                // 맞는 언어가 없다면, 직접 설정해야되니 건너뛴다.
                if (linqResult.Count() == 0) {
                    voicePageView.pages.Add(voiceComboList);
                    continue;
                }

                voiceComboList.country = linqResult.First();
                voicePageView.pages.Add(voiceComboList);
            }
        }

        private void PageChanged(object sendeer, EventArgs e) {
            // 콤보박스 바인딩
            ComboCountry.DataContext = voicePageView.NowPage;
            ComboVTypes.DataContext = voicePageView.NowPage;
            ComboVNames.DataContext = voicePageView.NowPage;

            // 카테고리박스 바인딩
            TxtBlock_Category.DataContext = voicePageView.NowPage;

            // 슬라이더 바인딩
            SliderSpeed.DataContext = voicePageView.NowPage;
            LabelSpeed.DataContext = voicePageView.NowPage;

            btnPrevPage.IsEnabled = btnNextPage.IsEnabled = true;

            {
                Color originColor = new Color();
                //#2D3030
                originColor.R = 0x2D;
                originColor.G = 0x30;
                originColor.B = 0x30;
                originColor.A = 0xFF;

                var originBrush = new SolidColorBrush(originColor);
                btnPrevPage.Foreground = btnNextPage.Foreground = originBrush;
            }

            if (voicePageView.PageIndex == 0) {
                btnPrevPage.IsEnabled = false;
                var brush = Brushes.LightGray;
                btnPrevPage.Foreground = brush;

            }
            if (voicePageView.PageIndex == voicePageView.pages.Count - 1) {
                btnNextPage.IsEnabled = false;
                var brush = Brushes.LightGray;
                btnNextPage.Foreground = brush;
            }
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e) {
            voicePageView.PageIndex -= 1;
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e) {
            voicePageView.PageIndex += 1;
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e) {

            if (bPlayingSound)
                return;

            if (voicePageView.NowPage == null)
                return;

            var page = voicePageView.NowPage;
            if (page.country == null)
                return;

            if (page.voiceType == null)
                return;

            if (page.voiceName == null)
                return;

            bPlayingSound = true;
            string sentence = excelLoader.SentenceRows[page.Category][0].senetence;
            if (tts.SynthesizeText(sentence, page.country.Id, page.voiceName.voice, page.Speed)) {
                player.URL = "output.mp3";
                player.controls.play();
            } else {
                bPlayingSound = false;
            }
        }

        private void OnPlaySoundEnded(int NewState) {

            if (NewState == (int)WMPLib.WMPPlayState.wmppsMediaEnded) {
                bPlayingSound = false;
                player.close();
            }
        }

        private async Task LoadPreset(string fileName = @"Setting\setting.json") {


            if (File.Exists(fileName)) {
                using (StreamReader sr = new StreamReader(fileName)) {
                    string content = sr.ReadToEnd();
                    SettingStorage settingStorage = JsonConvert.DeserializeObject<SettingStorage>(content);
                    Settings.Instance = settingStorage.settings;
                    Settings.Instance.settingPath = settings.settingPath;
                    settings = Settings.Instance;
                    this.DataContext = settings;

                    // tts init
                    if (!String.IsNullOrEmpty(settings.servicePath)) {
                        await InitTTS();
                    } else {
                        return;
                    }

                    // excel init은 TextChanged에서 자동으로 됨.
                    /*if (!(!String.IsNullOrEmpty(settings.excelPath) && File.Exists(settings.excelPath))) {
                        return;
                    }*/




                    VoicePageView vp = settingStorage.voicePageView;
                    VoicePageView nowVp = VoicePageView.Instance;

                    if (string.IsNullOrEmpty(settings.excelPath)) {
                        var linqCategories =
                            from page in vp.pages
                            select page._category;
                        excelLoader.Categories = linqCategories.ToList();
                        excelLoader.Categories.InsertRange(0, new string[] { "에피소드", "문장\n번호", "한국어(원자막)", "한국어(보정)" });
                        InitPage();
                    }
                    vp.CopyTo(ref nowVp);
                    Console.WriteLine("");
                }
            }
        }

        private async Task InitTTS() {
            try {
                tts.Init(settings.servicePath);
            } catch {
                MessageBox.Show("서비스 계정키에 문제가 있습니다. 한번 확인 해주세요.");
                settings.servicePath = null;
                return;
            }

            await tts.UpdateVoiceOnVoiceStorage();
        }

        private void BtnSettingPath_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "설정파일";
            dlg.Multiselect = false;
            dlg.Filter = "json files (*.json)|*.json";
            dlg.InitialDirectory = System.Environment.CurrentDirectory + @"\Setting";
            bool? result = dlg.ShowDialog();
            if (result == true) {
                settings.settingPath = "";
                settings.settingPath = dlg.FileName;
            }
        }

        private void TextBoxSettingPath_TextChanged(object sender, TextChangedEventArgs e) {
            if (string.IsNullOrEmpty(settings.settingPath)) {
                return;
            }
            LoadPreset(settings.settingPath);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e) {
            SaveSettings();
        }
    }

}
