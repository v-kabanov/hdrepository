using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;

namespace bfs.Repository.Storage
{
	public class DataItemRead : IDataItemRead
	{

		public IDataItem DataItem
		{
			get;
			internal set;
		}

		public IRepositoryFolder RepositoryFolder
		{
			get;
			internal set;
		}
	}
}
