using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class Trie
    {
        public TrieNode root;
        private char[] baseChars;
        private List<string> results = new List<string>();

        public Trie()
        {
            root = new TrieNode();
        }

        //adds a word to the trie, non recursively
        public void AddTitle(string title)
        {
            int count = 0;
            TrieNode placeholder = root;
            foreach (char ch in title.ToLower())
            {
                if (!placeholder.childNodes.ContainsKey(ch))
                {
                    TrieNode newNode = new TrieNode();
                    placeholder.childNodes.Add(ch, newNode);
                }
                placeholder = placeholder.childNodes[ch];
                if (count == title.Length - 1)
                {
                    placeholder.word = true;
                }
                count++;
            }
        }

        //searches through the trie until it gets to the endpoint of the passed word
        //then calls the recursive depth search method
        public List<string> SearchForWord(string wordPassed)
        {
            baseChars = wordPassed.ToLower().ToCharArray();
            TrieNode placeholder = root;
            results.Clear();

            foreach (char ch in wordPassed.ToLower())
            {
                if (!placeholder.childNodes.ContainsKey(ch))
                {
                    return results;
                }
                else
                {
                    placeholder = placeholder.childNodes[ch];
                }
            }
            if (placeholder.word)
            {
                results.Add(wordPassed);
            }
            DepthSearch(placeholder, baseChars);
            return results;
        }


        //recursively searches through the child nodes of a place holder and adds the word to total list
        public void DepthSearch(TrieNode placeholder, char[] chars)
        {
            var list = placeholder.childNodes.Keys.ToList();
            list.Sort();
            if (list.Contains(' '))
            {
                list.Remove(' ');
                list.Add(' ');
            }
            foreach (var key in list)
            {
                if (results.Count != 10)
                {
                    string temp = new string(chars);
                    temp += key.ToString();
                    chars = temp.ToCharArray();
                    if (placeholder.childNodes[key].word)
                    {
                        results.Add(temp);
                    }
                    DepthSearch(placeholder.childNodes[key], chars);
                    temp = new string(chars);
                    temp = temp.Remove(temp.Length - 1);
                    chars = temp.ToCharArray();
                }
            }
        }
    }
}