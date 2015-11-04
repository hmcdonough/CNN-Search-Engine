using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieNode
    {
        public Dictionary<char, TrieNode> childNodes;
        public bool word;
        public TrieNode()
        {
            this.childNodes = new Dictionary<char, TrieNode>();
            this.word = false;
        }
    }
}