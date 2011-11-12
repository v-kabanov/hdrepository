using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;

using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestContext = System.String;
using ClassInitialize = NUnit.Framework.TestFixtureSetUpAttribute;
using ClassCleanup = NUnit.Framework.TestFixtureTearDownAttribute;


namespace RepositoryTests.Mock
{
	class NumberedDataRouter : IDataRouter
	{
		private int _subfolderCount;
		public NumberedDataRouter(int subfolderCount)
		{
			Assert.IsTrue(subfolderCount > 1, "Internal test error");
			_subfolderCount = subfolderCount;
		}

		public string GetRelativePath(IDataItem dataItem)
		{
			return (((Mock.TestDataItem)dataItem).ValInt % _subfolderCount).ToString();
		}
	}
}
