using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bfs.Repository.Interfaces;
using System.Security.Permissions;
using System.Runtime.Serialization;

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
	[Serializable]
	public class TestDataItem : IDataItem
	{
		protected TestDataItem(SerializationInfo info, StreamingContext context)
			: this()
		{
			this.DateTime = info.GetDateTime("DateTime");
			this.ValInt = info.GetInt32("ValInt");
			this.ValString = info.GetString("ValString");
		}

		/// <summary>
		///		Path to the repository folder relative
		/// </summary>
		public string RelativePath
		{ get; set; }

		public DateTime DateTime
		{ get; set; }

		public int ValInt
		{ get; set; }

		public string ValString
		{ get; set; }

		/*[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			//info.AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
			info.AddValue("RelativePath", this.RelativePath);
			info.AddValue("ValInt", this.ValInt);
			info.AddValue("DateTime", this.DateTime);
			info.AddValue("ValString", this.ValString);
		}*/

		private TestDataItem()
		{
			this.RelativePath = string.Empty;
			this.DateTime = DateTime.Now;
		}

		public static TestDataItem GetTestItem(int intVal)
		{
			return new TestDataItem { ValInt = intVal, ValString = string.Format("value of {0}", intVal) };
		}

		internal void Check(int intVal)
		{
			Assert.AreEqual(intVal, this.ValInt);
			Assert.AreEqual(string.Format("value of {0}", ValInt), ValString);
		}

		public int GetBusinessHashCode()
		{
			return this.DateTime.GetHashCode() ^ this.ValString.GetHashCode();
		}

		/// <summary>
		///		Checks equality of data fields: timestamp, ValInt and ValString;
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj))
			{
				return true;
			}
			if (!(obj is TestDataItem))
			{
				return false;
			}
			var that = (TestDataItem)obj;
			return that.DateTime == this.DateTime
				&& that.ValInt == this.ValInt
				&& that.ValString == this.ValString;
		}

		public override int GetHashCode()
		{
			return DateTime.GetHashCode() ^ ValInt.GetHashCode() ^ (ValString == null ? 0 : ValString.GetHashCode());
		}
	}
}
