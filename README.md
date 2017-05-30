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

Reading:

```C#
	IRepositoryFolder targetFolder;
	// ...
	// getting reader from the root folder from which we want to read, recursively reading all descendant folders
	IRepositoryReader reader = targetFolder.GetReader(DateTime.Now.AddDays(-1), true);

	// adding folder from outside the subtree; all the data streams will be merged
	IRepositoryFolder anotherFolder;
	// ...
	reader.AddFolder(anotherFolder);

	IDataItemRead ritem = null;
	int n = 0;

	IRepositoryReader altReader = null;
	SeekStatusListener seekStatusListener = new SeekStatusListener();

	while (reader.HasData)
	{
		// periodically creating new reader from position returned by the first one and checking that they are
		// positioned at exactly the same point
		if (n > 0 && n % 100 == 0)
		{
			// using position of the first reader to re-create another reader instance in exectly the same state
			// the position may be persisted (serializable) and used later to continue [interrupted] reading in the same way
			altReader = Repository.ObjectFactory.GetReader(reader.Position, seekStatusListener.HanldeStatus);
		}
		ritem = reader.Read();

		if (altReader != null)
		{
			// checking that the reader recreated from position is indeed in the same state
			IDataItemRead altItem = altReader.Read();
			Assert.AreEqual(ritem.DataItem.DateTime, altItem.DataItem.DateTime);
			Assert.AreEqual(0, seekStatusListener.Statuses.Count);
		}

		++n;
	}

	// you can start reading again
	reader.Seek(DateTime.Now.AddDays(-1));

	for (n = 0; n < 1000 && reader.HasData; ++n)
	{
		Assert.IsNotNull(reader.Read());
	}

	// you can change reading direction most of the time except sometimes after Seek() due to deferred file loading
	// (until deferred data files are loaded)
	if (reader.CanChangeDirection)
	{
		reader.Direction = bfs.Repository.Util.EnumerationDirection.Backwards;
		// now read the same items in the opposite order
		for (n = 0; n < 1000 && reader.HasData; ++n)
		{
			Assert.IsNotNull(reader.Read());
		}
	}
```

Writing

```C#
using(IRepository repository = new RepositoryManager(@"c:\data\repositories\test"))
{
	// injecting our implementations; the default (gzip) compressor is registered and used by default
	repository.ObjectFactory.AddEncryptor(new MyEncryptor(), false);
	repository.ObjectFactory.AddCompressor(new MyCompressor(), false);
				
	IRepositoryFolder folder = repository.RootFolder.CreateSubfolder("MyFolder");
	// configure parameters of the folder and its descendants
	folder.Properties.DesiredItemsPerFile = 500; // will write very big items
	folder.Properties.EnableEncryption = true;
	folder.Properties.Encryptor = MyEncryptor.CODE; // writer will grab our registered encryptor from the object factory
	folder.Properties.Compressor = MyCompressor.CODE; // writer will grab our registered compressor from the object factory
	
	using (IRepositoryWriter writer = folder.GetWriter())
	{
		// writing some data
		for (int n = 0; n < 100; ++n)
		{
			Mock.TestDataItem item = Mock.TestDataItem.GetTestItem(n);
			item.DateTime = DateTime.UtcNow;
			writer.Write(item);
		}
		// flushing buffers to disk
		writer.Flush();
	}
	// now read something written 10 milliseconds back or later, only from the target folder (not reading descendants)
	using (IRepositoryReader reader = folder.GetReader(DateTime.UtcNow.AddMilliseconds(-10), false))
	{
		IDataItemRead dataItem = reader.Read();
	}
}
```
