using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using bfs.Repository.Storage;
using bfs.Repository.Interfaces;
using bfs.Repository.Events;
using RepositoryTests.Mock;
using bfs.Repository.Interfaces.Infrastructure;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace RepositoryTests
{
	[TestFixture]
	public class FolderReadingPositionTest
	{
		[Test]
		public void TestSerialisation()
		{
			const string folderKey = "jdosdv/vdvsd/dvsdv";
			DateTime positionTime = DateTime.Now;
			int numberOfItems = new Random().Next(10) + 1;
			int verificationHash = new Random().Next();

			FolderReadingPosition position = new FolderReadingPosition(folderKey: folderKey);
			position.Time = positionTime;
			position.NumberOfItemsWithTheTimestampRead = numberOfItems;
			position.VerificationLastReadItemHash = verificationHash;

			FolderReadingPosition copy = (FolderReadingPosition)position.Clone();

			Assert.AreEqual(position, copy);

			Assert.AreEqual(folderKey, copy.FolderKey);
			Assert.AreEqual(positionTime, copy.Time);
			Assert.AreEqual(numberOfItems, copy.NumberOfItemsWithTheTimestampRead);
			Assert.AreEqual(verificationHash, copy.VerificationLastReadItemHash);

			Assert.IsTrue(copy.IsExact);

			using (var stream = new MemoryStream(512))
			{
				BinaryFormatter fmt = new BinaryFormatter();
				fmt.Serialize(stream, position);
				stream.Position = 0;
				copy = (FolderReadingPosition)fmt.Deserialize(stream);
			}

			Assert.AreEqual(position, copy);
		}


		// this test is a proof of concept
		// I do not want to allow setting the version of the real FolderReadingPosition from outside
		// and do not have time to do it using reflection; that would be on the TODO list
		[Test]
		public void TestSerialisationNewerVersionConcept()
		{
			const string folderKey = "jdosdv/vdvsd/dvsdv";
			DateTime positionTime = DateTime.Now;
			int numberOfItems = new Random().Next(10) + 1;
			int verificationHash = new Random().Next();

			FolderReadingPositionMock position = new FolderReadingPositionMock(folderKey: folderKey);
			FolderReadingPositionMock.PositionVersion = FolderReadingPosition.PositionVersion;

			position.Time = positionTime;
			position.NumberOfItemsWithTheTimestampRead = numberOfItems;
			position.VerificationLastReadItemHash = verificationHash;

			Assert.AreEqual(folderKey, position.FolderKey);
			Assert.AreEqual(positionTime, position.Time);
			Assert.AreEqual(numberOfItems, position.NumberOfItemsWithTheTimestampRead);
			Assert.AreEqual(verificationHash, position.VerificationLastReadItemHash);

			Assert.IsTrue(position.IsExact);

			using (var stream = new MemoryStream(512))
			{
				BinaryFormatter fmt = new BinaryFormatter();

				FolderReadingPositionMock.PositionVersion = new Version(1, 0);
				fmt.Serialize(stream, position);
				stream.Position = 0;
				FolderReadingPositionMock.PositionVersion = FolderReadingPosition.PositionVersion;

				try
				{
					FolderReadingPositionMock deserializedPos = (FolderReadingPositionMock)fmt.Deserialize(stream);
					Assert.Fail("Version mismatch not detected");
				}
				catch (System.Reflection.TargetInvocationException e)
				{
					Assert.IsInstanceOf<SerializationException>(e.InnerException);
					String expectedMsg = string.Format(
						StorageResources.IncompatibleVersion, typeof(FolderReadingPositionMock).Name
						, FolderReadingPosition.PositionVersion, new Version(1, 0));
					Assert.AreEqual(expectedMsg, e.InnerException.Message);
				}
			}

		}
	}
}
