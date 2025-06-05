using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using ProjectScanner.Common; 

namespace ProjectScanner.ScannerB
{
    class Program
    {
        private const string AgentId = "agent2";
        private const string PipeName = "agent2";

        static async Task Main(string[] args)
        {
            Console.WriteLine($"ProjectScanner.ScannerB (Agent ID: {AgentId}) starting...");

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: ScannerB.exe C:\\ScanData\\AgentB");
                return;
            }

            string directoryPath = args[0];
            
            if (!System.IO.Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Error: The specified directory does not exist: {directoryPath}");
                return;
            }

         
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.ProcessorAffinity = new IntPtr(4);
                Console.WriteLine($"Processor affinity set for ScannerB to core 2.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not set processor affinity: {ex.Message}");
                Console.WriteLine("This might happen if you have fewer than 3 CPU cores or insufficient permissions.");
            }

            List<WordIndexEntry> indexedWords = new List<WordIndexEntry>();
            bool scanCompleted = false;
            bool dataSent = false;

            ManualResetEvent scanDoneEvent = new ManualResetEvent(false);

            Task scanTask = Task.Run(async () =>
            {
                try
                {
                    FileScanner scanner = new FileScanner(directoryPath);
                    indexedWords = await scanner.ScanFilesAsync();
                    scanCompleted = true;
                    scanDoneEvent.Set(); 
                    Console.WriteLine("File scanning task completed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during file scanning: {ex.Message}");
                    scanCompleted = true; 
                    scanDoneEvent.Set();
                }
            });

           
            Task sendTask = Task.Run(async () =>
            {
               
                scanDoneEvent.WaitOne();

                if (!scanCompleted)
                {
                    Console.WriteLine("Scanning did not complete successfully. Skipping data sending.");
                    return;
                }

                if (indexedWords.Count == 0)
                {
                    Console.WriteLine("No words indexed. Nothing to send to Master.");
                    dataSent = true; // !Mark as sent even if nothing was sent
                    return;
                }

                Console.WriteLine($"Attempting to connect to Master via pipe: {PipeName}");
                using (NamedPipeClientStream clientPipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    try
                    {
                        await clientPipe.ConnectAsync(5000); 
                        Console.WriteLine($"Connected to Master pipe: {PipeName}");

                        AgentData dataToSend = new AgentData(AgentId, indexedWords);
                        await PipeUtils.SendDataAsync(clientPipe, dataToSend);
                        Console.WriteLine($"Successfully sent {indexedWords.Count} indexed words to Master.");
                        dataSent = true;
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
          
            await Task.WhenAll(scanTask, sendTask);

            Console.WriteLine($"ProjectScanner.ScannerB (Agent ID: {AgentId}) finished.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

