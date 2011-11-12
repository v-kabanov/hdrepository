/*
 * User: vasily
 * Date: 31/03/2011
 * Time: 21:00
 * 
 */

using System;
using System.Transactions;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Storage;
using bfs.Repository.Storage.FileSystem;
using bfs.Repository.IO;
using NUnit.Framework;

namespace RepositoryTests
{
	[TestFixture]
	public class LongSlaveTransactionManagerTest : TestBase
	{
		[Test]
		public void TestRepeatedReuseSlave()
		{
			if (!EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test transactions in this environment");
			}
			else
			{
				Assume.That(!FileSystemProvider.IsStorageAmbientTransactionActive);

				Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.RequireTransactions;

				TransactionSubscriber subscriber = new TransactionSubscriber();
				IFileSystemTransaction transaction;

				LongSlaveTransactionManager tman = new LongSlaveTransactionManager(Repository, subscriber);

				using (TransactionScope masterScope = new TransactionScope())
				{
					using (var scope = tman.GetTransactionScope())
					{
						Assert.IsTrue(scope.HasChangedContext);
						Assert.IsFalse(scope.IsNullScope);
						Assert.IsFalse(scope.IsTransactionOwner);
						Assert.IsFalse(scope.NoTransaction);
						Assert.AreSame(AmbientTransaction, tman.PendingTransaction);
						Assert.AreSame(scope.UnderlyingTransaction, tman.PendingTransaction);

						Assert.IsTrue(tman.CanIOTransactionSpanMultipleRepositoryCalls);

						Assert.IsTrue(tman.IsTransactionPending());

						Assert.AreEqual(1, StorageTransactionScope.ScopeNestLevel);
						transaction = AmbientTransaction;
					}

					Assert.AreEqual(0, StorageTransactionScope.ScopeNestLevel);
					Assert.IsNull(AmbientTransaction);
					Assert.IsNotNull(tman.PendingTransaction);
					Assert.IsFalse(tman.CanIOTransactionSpanMultipleRepositoryCalls, "No ambientr transaction here");

					for (int n = 0; n < 10; ++n)
					{
						using (var scope = tman.GetTransactionScope())
						{
							Assert.IsTrue(scope.HasChangedContext);
							Assert.IsFalse(scope.IsNullScope);
							Assert.IsFalse(scope.IsTransactionOwner);
							Assert.IsFalse(scope.NoTransaction);

							Assert.AreSame(transaction, tman.PendingTransaction);
							Assert.AreSame(AmbientTransaction, tman.PendingTransaction);
							Assert.AreSame(scope.UnderlyingTransaction, tman.PendingTransaction);

							Assert.IsTrue(tman.CanIOTransactionSpanMultipleRepositoryCalls);

							Assert.IsTrue(tman.IsTransactionPending());

							Assert.AreEqual(1, StorageTransactionScope.ScopeNestLevel);
							transaction = AmbientTransaction;
						}
						Assert.AreEqual(0, StorageTransactionScope.ScopeNestLevel);
						Assert.IsNull(AmbientTransaction);
						Assert.IsNotNull(tman.PendingTransaction);
						Assert.IsFalse(tman.CanIOTransactionSpanMultipleRepositoryCalls, "No ambientr transaction here");
					}

					//masterScope.Complete();

				} // using (TransactionScope masterScope = new TransactionScope())

				System.Threading.Thread.Sleep(50);

				Console.WriteLine("About to test times notified");

				Assert.AreEqual(1, subscriber.TimesNotified, "Only 1 storage transaction must have been instantiated and therefore 1 notification received");
				Assert.IsFalse(subscriber.Committed, "Master scope has not been completed");
				//Assert.IsFalse(transaction.IsActive);

				Assert.IsNull(tman.PendingTransaction, "Must have received notification and reset pending transaction");
				Assert.IsFalse(tman.CanIOTransactionSpanMultipleRepositoryCalls, "There should be no ambient transaction here at all");

				tman.Dispose();

				Assert.Throws<ObjectDisposedException>(() => tman.GetTransactionScope());
			}

		}
	}
}
