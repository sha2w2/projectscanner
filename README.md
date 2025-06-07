# ProjectScanner

## Project Overview
This project implements a distributed system in C# using .NET Framework for efficient file content indexing. It features a central Master process and two Agent (Scanner) processes that communicate via named pipes, demonstrating inter-process communication, multithreading, and CPU core affinity for parallel processing.

## Project Structure
The solution is composed of three console applications and one class library:

### 1. Master Application (ProjectScanner.Master)
This is a C# console application responsible for:

- Waiting for connections from both Agent (Scanner) programs via named pipes
- Receiving and processing indexed word data from each agent
- Collecting the data from both agents
- Displaying the final consolidated word index
- Uses multiple threads to handle each pipe concurrently
- Configured to run on a separate CPU core using ProcessorAffinity

**Files:**
- `Program.cs`: Contains the main logic for starting pipe servers, handling agent connections, and consolidating results

### 2. Agent A Application (ProjectScanner.ScannerA)
This is a C# console application representing the first Scanner agent. Its responsibilities include:

- Receiving a directory path containing .txt files as input
- Reading the content of each file and indexing words
- Sending this indexed information to the Master process using a unique named pipe ("agent1")
- Uses multiple threads (e.g., one for reading files, another for sending data)
- Configured to run on a separate CPU core using ProcessorAffinity

**Files:**
- `Program.cs`: Contains the main logic for scanning files, connecting to the Master, and sending data

### 3. Agent B Application (ProjectScanner.ScannerB)
This is a C# console application representing the second Scanner agent. Its responsibilities are identical to Agent A:

- Receiving a directory path containing .txt files as input
- Reading the content of each file and indexing words
- Sending this indexed information to the Master process using a unique named pipe ("agent2")
- Uses multiple threads (e.g., one for reading files, another for sending data)
- Configured to run on a separate CPU core using ProcessorAffinity

**Files:**
- `Program.cs`: Contains the main logic for scanning files, connecting to the Master, and sending data

### 4. Common Class Library (ProjectScanner.Common)
This is a C# Class Library project that contains shared code components used by the Master and Agent applications.

**Files:**
- `WordIndexEntry.cs`: Defines the data structure for a single indexed word (filename, word, count)
- `AgentData.cs`: Defines the data package containing agent ID and a list of WordIndexEntry objects for transmission
- `PipeUtils.cs`: Provides static utility methods for handling the serialization and deserialization of data transferred over named pipes
- `FileScanner.cs`: Contains the core logic for reading .txt files from a given directory and counting words

## Order of Project Creation
The projects were created in the following order to establish dependencies and functionality iteratively:

1. `ProjectScanner.ScannerA`: Created first to establish a base
2. `ProjectScanner.Common`: Created to define all shared data structures and utility methods
3. `ProjectScanner.ScannerB`: Created as a duplicate of ScannerA
4. `ProjectScanner.Master`: Created last

## Communication Method
Inter-process communication (IPC) between the agents and the master is exclusively handled using Named Pipes:

- The Master uses `NamedPipeServerStream`
- Each Agent uses `NamedPipeClientStream`
- Each agent connects to the master through a unique named pipe (`agent1`, `agent2`)

## Multithreading
All applications (Master, Scanner A, Scanner B) use multithreading to perform their tasks concurrently.

## CPU Core Affinity
Each executable is configured to run on a separate dedicated CPU core using `Process.ProcessorAffinity` for optimal performance and resource isolation.

## Key Challenges & Solutions
Developing this distributed system involved overcoming several common challenges:
9860
- **Project File Configuration (.csproj)**: Resolved issues with missing compilation entries by manually editing .csproj files in Notepad
- **C# Syntax & Compatibility**: Corrected string path issues and .NET Framework compatibility
- **PowerShell Execution**: Addressed "command not found" errors by using `.\` prefix when running program
- **Client Communication Clarity**: Improved timeout handling and console window persistence
