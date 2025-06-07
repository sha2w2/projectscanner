using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using ProjectScanner.Common; // Import the common library

namespace ProjectScanner.ScannerA
{
    class Program
    {
        private const string AgentId = "agent1";
        private const string PipeName = "agent1"; // Master listens on this name

        static async Task Main(string[] args)
        {
            Console.WriteLine($"ProjectScanner.ScannerA (Agent ID: {AgentId}) starting...");

            if (args.Length == 0)
            {
                // Corrected: Using verbatim string literal for the path to avoid "Unrecognized escape sequence"
                Console.WriteLine(@"Usage: ScannerA.exe C:\ScanData\AgentA");
                return;
            }

            string directoryPath = args[0];
            if (!System.IO.Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Error: The specified directory does not exist: {directoryPath}");
                return;
            }

            // Set processor affinity to core 1 (bitmask 2)
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.ProcessorAffinity = new IntPtr(2); // Core 1 (0-indexed)
                Console.WriteLine($"Processor affinity set for ScannerA to core 1.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not set processor affinity: {ex.Message}");
                Console.WriteLine("This might happen if you have fewer than 2 CPU cores or insufficient permissions.");
            }

            List<WordIndexEntry> indexedWords = new List<WordIndexEntry>();
            bool scanCompleted = false;
            // dataSent is used to determine if data was attempted to be sent, not necessarily successfully
            bool dataSent = false; // Added dataSent variable for consistency

            ManualResetEvent scanDoneEvent = new ManualResetEvent(false); // Used to synchronize tasks

            // This is the 'scanTask' definition, ensure all braces are correctly matched.
            Task scanTask = Task.Run(async () =>
            {
                try
                {
                    FileScanner scanner = new FileScanner(directoryPath); // Uses FileScanner from Common
                    indexedWords = await scanner.ScanFilesAsync();
                    scanCompleted = true;
                    scanDoneEvent.Set(); // Signal completion
                    Console.WriteLine("File scanning task completed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during file scanning: {ex.Message}");
                    scanCompleted = true; // Still signal completion to unblock send task
                    scanDoneEvent.Set();
                }
            });

            Task sendTask = Task.Run(async () =>
            {
                scanDoneEvent.WaitOne(); // Wait for scanning to finish

                if (!scanCompleted)
                {
                    Console.WriteLine("Scanning did not complete successfully. Skipping data sending.");
                    return;
                }

                if (indexedWords.Count == 0)
                {
                    Console.WriteLine("No words indexed. Nothing to send to Master.");
                    dataSent = true; // Mark as sent even if nothing was sent
                    return;
                }

                Console.WriteLine($"Attempting to connect to Master via pipe: {PipeName}");
                using (NamedPipeClientStream clientPipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    try
                    {
                        // Increased timeout to 10 seconds to reduce premature TimeoutException
                        await clientPipe.ConnectAsync(10000);
                        Console.WriteLine($"Connected to Master pipe: {PipeName}");

                        AgentData dataToSend = new AgentData(AgentId, indexedWords); // Uses AgentData from Common
                        await PipeUtils.SendDataAsync(clientPipe, dataToSend); // Uses PipeUtils from Common
                        Console.WriteLine($"Successfully sent {indexedWords.Count} indexed words to Master.");
                        dataSent = true; // Mark as sent upon successful transmission
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine($"Error: Connection to pipe '{PipeName}' timed out. Is Master running?");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending data through pipe: {ex.Message}");
                    }
                    finally
                    {
                        clientPipe.Close();
                    }
                }
            });

            await Task.WhenAll(scanTask, sendTask); // Wait for both tasks to complete

            Console.WriteLine($"ProjectScanner.ScannerA (Agent ID: {AgentId}) finished.");
            // Added Console.ReadKey() to keep the console window open after completion for easy viewing
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
