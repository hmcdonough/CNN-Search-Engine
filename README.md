CNN Search Engine

This project combines several different components to create a search engine.
	1. A query suggestion service that reads and stores Wikipedia's top page title names into a custom Trie data structure. This allows it to give users suggestions for search keywords that match any input.
	2. A web crawler that crawls CNN.com and BleacherReport.com and their sitemaps. Any URLs are processed and put into Azure storage to be accessed with each search.
	3. A retrieval service that grabs URLs, titles, and their date published, ordered by matching keywords in the title and then by date.

Since this was a school project I was given credits on Microsoft Azure to host it, however those credits have since run up and I am no longer able to do so. 