# hdrepository
Repository stores serializable timestamped data items in compressed files structured in a way that allows to quickly find data by time, quickly read and write data items in chronological order.

Key features:
- all data is stored under a single root folder; like file system, repository can contain a tree of folders each of which contains its own data stream; when reading, multiple streams can be combined into a single output stream maintaining chronological order; the tree structure can be created manually or dynamically, on the fly, from incoming data item attributes;
- data can quickly be found by stream (path to the repository folder) and time;
- open architecture; users can provide their own implementation of most components;
- data integrity; data corruption due to unexpected termination of the client application can be prevented with file system transactions when they are supported (Windows Vista, Server 2008 and above);
- support for reading positions; the repository reader supports reading positions which can be saved and later used to continue interrupted reading

Advantages:
- simplicity and low cost of data storage; you do not need expensive software, hardware and expertise to store and access large volumes of data as compared to traditional relational database solutions;
- scalability; the performance of sequential reads and writes does not depend on the size of the repository; this is often difficult to achieve with relational databases;
