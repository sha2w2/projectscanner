using System;


namespace ProjectScanner.Common
{
    [Serializable]
    public class WordIndexEntry
    {
        public string FileName { get; set; }
        public string Word { get; set; }
        public int Count { get; set; }



        public WordIndexEntry(string fileName, string word, int count)
        {
            FileName = fileName;
            Word = word;
            Count = count;
        }



        public override string ToString()
        {
            return $"{FileName}:{Word}:{Count}";
        }
    }
}

