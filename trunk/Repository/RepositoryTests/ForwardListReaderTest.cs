using bfs.Repository.Util;
using System;
using System.Collections.Generic;

using NUnit.Framework;



namespace RepositoryTests
{
    
    
    /// <summary>
    ///This is a test class for ForwardListReaderTest and is intended
    ///to contain all ForwardListReaderTest Unit Tests
    ///</summary>
	[TestFixture]
	public class ForwardListReaderTest
	{

		/// <summary>
		///A test for MoveNext
		///</summary>
		[Test]
		public void MoveNextTestHelper()
		{
			const int count = 10;
			IList<int> list = new List<int>(10);
			for (int n = 0; n < count; ++n)
			{
				list.Add(n + 1);
			}

			ForwardListReader<int> target = new ForwardListReader<int>(list);
			target.SetCurrent(9);
			bool result = target.MoveNext();
			Assert.IsFalse(result);
			Assert.IsFalse(target.HasItem);
		}
	}
}
