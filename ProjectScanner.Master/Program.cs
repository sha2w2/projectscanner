using System;
using System.Collections.Concurrent; // For thread-safe collection
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProjectScanner.Common; // Import the common library (ensure this project references ProjectScanner.Common)

namespace ProjectScanner.Master
{
    class Program
    {
        // Thread-safe list to store all received WordIndexEntry objects from all agents
        private static ConcurrentBag<WordIndexEntry> _consolidatedWordIndex = new ConcurrentBag<WordIndexEntry>();

        // CountdownEvent to signal when all expected agents have sent their data
        private static CountdownEvent _agentCountdown;

        static async Task Main(string[] args)
        {
            Console.WriteLine("ProjectScanner.Master starting...");

            // Expected pipe names as command-line arguments (e.g., "agent1", "agent2")
            if (args.Length == 0)
            {
                // Corrected: Using verbatim string literal for file path to avoid "Unrecognized escape sequence"
                Console.WriteLine(@"Usage: Master.exe agent1 agent2");
                Console.WriteLine("Please provide the pipe names for agents to listen on.");
                return;
            }

            string[] pipeNames = args; // The pipe names the master will listen on
            _agentCountdown = new CountdownEvent(pipeNames.Length); // Initialize countdown with the number of expected agents

            // Attempt to set processor affinity for the Master process
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                // Set affinity to core 0 (using bitmask 1)
                currentProcess.ProcessorAffinity = new IntPtr(1);
                Console.WriteLine($"Processor affinity set for Master to core 0.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not set processor affinity: {ex.Message}");
                Console.WriteLine("This might happen if you have fewer than 1 CPU core or insufficient permissions.");
            }

            List<Task> listenTasks = new List<Task>();

            Console.WriteLine($"Master is listening for {pipeNames.Length} agent connections...");

            // Create and start a listening task for each expected pipe name
            foreach (string pipeName in pipeNames)
            {
                // Each agent connection is handled in a separate, asynchronous task
                listenTasks.Add(HandleAgentConnectionAsync(pipeName));
            }
            // Removed the lone dot '.' that was causing a syntax error here
            Console.WriteLine("Waiting for agents to connect and send data...");
            _agentCountdown.Wait(); // Blocks until countdown reaches zero

            Console.WriteLine("\n--- All expected agents have sent their data ---");
            Console.WriteLine("\nConsolidated Word Index:");

            // Display the aggregated results
      
            if (_consolidatedWordIndex.IsEmpty)
            {
                Console.WriteLine("No data received from agents.");
            }
            else
            {
                // Order the results for consistent output
                var orderedResults = _consolidatedWordIndex
                                        .OrderBy(entry => entry.FileName)
                                        .ThenBy(entry => entry.Word)
                                        .ToList();

                foreach (var entry in orderedResults)
                {
                    Console.WriteLine($"{entry.FileName}:{entry.Word}:{entry.Count}");
                }
            }

            Console.WriteLine("\nProjectScanner.Master finished. Press any key to exit.");
            Console.ReadKey(); // Keep the console open until a key is pressed
        }

        /// <summary>
        /// Handles a single incoming agent connection on a specific named pipe.
        /// This method runs asynchronously for each agent.
        /// </summary>
        /// <param name="pipeName">The name of the pipe to listen on for this agent.</param>
        private static async Task HandleAgentConnectionAsync(string pipeName)
        {
            // Use a try-finally block to ensure CountdownEvent is decremented
            // even if an error occurs during connection or data reception.
            try
            {
                Console.WriteLine($"Master: Setting up pipe server for '{pipeName}'...");
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.In,
                    // Corrected: Replaced NamedPipeServerStream.MaxAllowedInstances with a literal int for .NET Framework
                    // Since you have 2 agents, 2 is a suitable max.
                    2,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous))
                {
                    Console.WriteLine($"Master: Waiting for client connection on pipe '{pipeName}'...");
                    // Wait for an agent to connect
                    await pipeServer.WaitForConnectionAsync();
                    Console.WriteLine($"Master: Client connected on pipe '{pipeName}'.");

                    // Receive AgentData using the common PipeUtils
                    AgentData agentData = await PipeUtils.ReceiveDataAsync<AgentData>(pipeServer);

                    if (agentData != null)
                    {
                        Console.WriteLine($"Master: Received {agentData.IndexedWords.Count} entries from Agent '{agentData.AgentId}'.");
                        // Add received word entries to the consolidated list
                        foreach (var entry in agentData.IndexedWords)
                        {
                            _consolidatedWordIndex.Add(entry);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Master: No data received from Agent on pipe '{pipeName}'.");
                    }
                } // This '}' closes the 'using (NamedPipeServerStream pipeServer = ...)' block
            } // This '}' closes the 'try' block
            catch (Exception ex)
            {
                Console.WriteLine($"Master: Error handling pipe '{pipeName}': {ex.Message}");
            }
            finally
            {
                // Signal that this agent's data handling is complete or failed
                _agentCountdown.Signal();
                Console.WriteLine($"Master: Agent '{pipeName}' handling finished. Remaining agents to wait for: {_agentCountdown.CurrentCount}");
            }
        } // 'HandleAgentConnectionAsync' method
    } //  'class Program'
} // 'namespace ProjectScanner.Master'
