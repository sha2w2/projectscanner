using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace ProjectScanner.Common
{

    public class FileScanner
    {
        private readonly string _directoryPath;


        public FileScanner(string directoryPath)
        {
            // The characters `1 at the end of this line were causing the error.
            // They have been removed.
            _directoryPath = directoryPath;
        }


        public async Task<List<WordIndexEntry>> ScanFilesAsync()
        {
            List<WordIndexEntry> indexedWords = new List<WordIndexEntry>();
            string[] textFiles = Directory.GetFiles(_directoryPath, "*.txt");


            Console.WriteLine($"Scanning {textFiles.Length} .txt files in '{_directoryPath}'...");


            foreach (string filePath in textFiles)
            {
                string fileName = Path.GetFileName(filePath);
                try
                {
                    // Read file content asynchronously
                    string content = await Task.Run(() => File.ReadAllText(filePath));
                    Dictionary<string, int> wordCounts = CountWords(content);


                    foreach (var entry in wordCounts)
                    {
                        indexedWords.Add(new WordIndexEntry(fileName, entry.Key, entry.Value));
                    }
                    Console.WriteLine($"  - Scanned '{fileName}' and found {wordCounts.Count} unique words.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning file '{fileName}': {ex.Message}");
                }
            }


            return indexedWords;
        }

        private Dictionary<string, int> CountWords(string content)
        {
            Dictionary<string, int> wordCounts = new Dictionary<string, int>();


            // Use Regex to find alphanumeric characters
            MatchCollection matches = Regex.Matches(content, @"\b\w+\b");


            foreach (Match match in matches)
            {
                string word = match.Value.ToLowerInvariant(); // for case-insensitive counting
                if (wordCounts.ContainsKey(word))
                {
                    wordCounts[word]++;
                }
                else
                {
                    wordCounts[word] = 1;
                }
            }
            return wordCounts;
        }
    }
}
