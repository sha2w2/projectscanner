# ProjectScanner expected outcome & structurte

## Project Structure

The solution is composed of three console applications and one class library:

### 1. Master Application (`ProjectScanner.Master`)
This is a C# console application responsible for:
* Waiting for connections from both Agent (Scanner) programs via named pipes.
* Receiving and processing indexed word data from each agent.
* Collecting the data from both agents.
* Displaying the final consolidated word index .
* It uses multiple threads to handle each pipe concurrently.
* Configured to run on a separate CPU core using `ProcessorAffinity`.

### 2. Agent A Application (`ProjectScanner.ScannerA`)
This is a C# console application representing the first Scanner agent. Its responsibilities include:
* Receiving a directory path containing `.txt` files as input.
* Reading the content of each file and indexing words.
* Sending this indexed information to the Master process using a unique named pipe ("agent1").
* It uses multiple threads (e.g., one for reading files, another for sending data).
* Configured to run on a separate CPU core using `ProcessorAffinity`.

### 3. Agent B Application (`ProjectScanner.ScannerB`)
This is a C# console application representing the second Scanner agent. Its responsibilities are identical to Agent A:
* Receiving a directory path containing `.txt` files as input.
* Reading the content of each file and indexing words .
* Sending this indexed information to the Master process using a unique named pipe ("agent2").
* It uses multiple threads (e.g., one for reading files, another for sending data).
* Configured to run on a separate CPU core using `ProcessorAffinity`.

### 4. Common Class Library (`ProjectScanner.Common`)
This is a C# Class Library project that contains shared code components used by the Master and Agent applications. This includes:
* **`DataTransferObjects.cs`**: Defines the data structures (DTOs) used for communication between the agents and the master via named pipes, ensuring consistent data serialization and deserialization.
* **`PipeUtils.cs`**: Provides utility methods for handling the serialization and deserialization of data transferred over named pipes.

## Communication Method
Inter-process communication (IPC) between the agents and the master is exclusively handled using **Named Pipes**.
* The Master uses `NamedPipeServerStream`.
* Each Agent uses `NamedPipeClientStream`.
* Each agent connects to the master through a unique named pipe.

## Multithreading
All applications (Master, Scanner A, Scanner B) use multithreading to perform their tasks at the same time, for example, one thread for reading files and another for sending data in Agents, and separate threads for handling each pipe in the Master.

## CPU Core Affinity
Each executable (`ScannerA.exe`, `ScannerB.exe`, `Master.exe`) must be configured to run on a separate CPU core using `ProcessorAffinity` to ensure optimal performance and resource isolation
