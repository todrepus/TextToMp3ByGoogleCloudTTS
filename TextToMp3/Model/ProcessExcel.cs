using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;

namespace TextToMp3.Model {
    class SentenceRow{
        public int episode;
        public int order;
        public string senetence = null;

        public SentenceRow Clone() {
            SentenceRow s = new SentenceRow();
            s.episode = this.episode;
            s.order = this.order;
            s.senetence = this.senetence;
            return s;
        }
    }
    class ExcelLoader {
        private static ExcelLoader _instance = null;

        private Dictionary<string, List<SentenceRow>> _sentenceRows = new Dictionary<string, List<SentenceRow>>();
        private List<string> _categories = new List<string>();

        public Dictionary<string, List<SentenceRow>> SentenceRows { get { return _sentenceRows; } set { _sentenceRows = value; } }
        public List<string> Categories { get { return _categories; } set { _categories = value; } }
        public const int CATEGORY_LANG_START = 4;
        public static ExcelLoader Instance {
            get {
                if (_instance == null)
                    _instance = new ExcelLoader();
                return _instance;
            }
        }

        ExcelLoader() {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public bool LoadExcel(string excelPath) {
            // List 비우기
            this.SentenceRows.Clear();
            bool bSame = false; // Category 가 같은경우인가?

            List<string> tempCategories = new List<string>();
            using(FileStream stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                ExcelPackage excel = new ExcelPackage(stream);
                var sheet = excel.Workbook.Worksheets[0];

                var start = sheet.Dimension.Start;
                var end = sheet.Dimension.End;

                List<int> passIdxList = new List<int>();
                for (int col = start.Column; col <= end.Column; col++) {
                    string cell = sheet.Cells[start.Row, col].Text;

                    if (cell.Trim() == "") {
                        passIdxList.Add(col);
                        continue;
                    }
                    tempCategories.Add(cell);
                }

                if (tempCategories.Count == Categories.Count && tempCategories.SequenceEqual(Categories)) {
                    bSame = true;
                } else {
                    Categories = tempCategories;
                }

                for (int row=start.Row+1; row <=end.Row; row++) {
                    SentenceRow sentenceRow = new SentenceRow();
                    int diff = 0; // 인덱스를 앞으로 밀어주는 역할
                    for (int col = start.Column; col <= end.Column; col++) {
                        string cell = sheet.Cells[row, col].Text;
                        /*
                         0 : 에피소드, 1: 문장번호, 2,3 : 한국어, 4 ~ :각종언어들.
                         */
                        if (passIdxList.Contains(col)) {
                            diff += 1;
                            continue;
                        }
                        if (String.IsNullOrEmpty(cell)) {
                            break;
                        }

                        if (col == start.Column) {
                            sentenceRow.episode = Int32.Parse(cell);
                        }else if (col == start.Column + 1) {
                            sentenceRow.order = Int32.Parse(cell);
                        } else {
                            SentenceRow s = sentenceRow.Clone();
                            s.senetence = cell;
                            if (!SentenceRows.ContainsKey(Categories[col-start.Column - diff]))
                                SentenceRows[Categories[col-start.Column]] = new List<SentenceRow>();
                            SentenceRows[Categories[col-start.Column]].Add(s);
                        }
                    }
                }

            }
            return bSame;
        }
        
    }
}
