using System;
using System.Collections.Generic;


namespace ProjectScanner.Common
{

    [Serializable] // class is serializable for binary serialization over pipes
    public class AgentData
    {
        public string AgentId { get; set; }
        public List<WordIndexEntry> IndexedWords { get; set; }


        public AgentData(string agentId, List<WordIndexEntry> indexedWords)
        {
            AgentId = agentId;
            IndexedWords = indexedWords;
        }
    }
}

