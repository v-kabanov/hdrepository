/*
 * User: vasily
 * Date: 20/03/2011
 * Time: 11:15 AM
 * 
 */
using System;
using System.Transactions;
using bfs.Repository.Interfaces;
using bfs.Repository.Interfaces.Infrastructure;
using bfs.Repository.Storage;
using bfs.Repository.Storage.FileSystem;
using NUnit.Framework;
using System.Threading;
using bfs.Repository.IO.WinNtfs;

namespace RepositoryTests
{
	internal class TransactionSubscriber : ITransactionNotification
	{
		private volatile int _timesNotified;
		private volatile bool _committed;

		public int TimesNotified
		{ get { return _timesNotified; } }

		public bool Committed
		{ get { return _committed; } }

		public void TransactionCompleted(IFileSystemTransaction transaction, bool committed)
		{
			Console.WriteLine("TransactionSubscriber notified");
			Interlocked.Increment(ref _timesNotified);
			_committed = committed;
		}

		public void Prepare()
		{
		}
	}
	
	[TestFixture]
	public class StorageTransactionScopeTest : TestBase
	{
		[Test]
		public void TestAmbientManagedFirstEntry()
		{
			if (!EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test transactions in this environment");
				return;
			}
			Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.RequireTransactions;
				
			TransactionSubscriber subscriber = new TransactionSubscriber();
			IFileSystemTransaction transaction;
				
			using (TransactionScope masterScope = new TransactionScope())
			{
				using (var scope = StorageTransactionScope.Create(Repository))
				{
					Assert.AreEqual(1, StorageTransactionScope.ScopeNestLevel);

					Assert.IsTrue(scope.HasChangedContext, "Scope must have changed context");
					Assert.IsFalse(scope.IsTransactionOwner, "Transaction must be slave under managed");
					Assert.IsFalse(scope.ToBeDisposed, "Scope having created slave transaction must not be disposing");
					Assert.IsNotNull(AmbientTransaction, "Context not set");
					Assert.AreSame(AmbientTransaction, scope.UnderlyingTransaction, "Underlying transaction is not in sync with context");
					Assert.IsTrue(scope.UnderlyingTransaction.IsActive, "Transaction must be active in this scope");
						
					Assert.IsFalse(scope.UnderlyingTransaction.Unsubscribe(subscriber), "Unsubscribing not yet subscribed returned true");
					scope.UnderlyingTransaction.Subscribe(subscriber);
					transaction = scope.UnderlyingTransaction;
						
					Assert.Throws(
						Is.InstanceOf<InvalidOperationException>()
						, () => transaction.Commit(), "Committing slave transaction must throw InvalidOperationException");
						
					Assert.IsFalse(scope.ToBeDisposed, "Scope turned disposing after transaction detached");
					Assert.IsFalse(scope.IsTransactionOwner);
				}

				Assert.AreEqual(0, StorageTransactionScope.ScopeNestLevel);
					
				Assert.IsTrue(transaction.IsActive, "Detached transaction must remain active after disposing scope");
				Assert.AreEqual(0, subscriber.TimesNotified, "Disposing detached scope must not result in notification");
				Assert.IsNull(KtmTransaction.Current, "The scope was disposed and must have restored context");
				
				using (var scope = StorageTransactionScope.Create(Repository, transaction))
				{
					Assert.AreEqual(1, StorageTransactionScope.ScopeNestLevel);
						
					Assert.AreSame(transaction, scope.UnderlyingTransaction, "Underlying transaction is not what was explicitly assigned");
					Assert.IsTrue(scope.HasChangedContext, "Scope must have changed context");
					Assert.IsFalse(scope.IsTransactionOwner, "Transaction must be slave under managed");
					Assert.IsFalse(scope.ToBeDisposed, "Scope must not be disposing as explicitly specified when creating");
					Assert.IsNotNull(AmbientTransaction, "Context not set");
					Assert.AreSame(AmbientTransaction, scope.UnderlyingTransaction, "Underlying transaction is not in sync with context");
					Assert.IsTrue(scope.UnderlyingTransaction.IsActive, "Transaction must be active in this scope");
						
					scope.UnderlyingTransaction.Subscribe(subscriber);
				}
					
				int level = StorageTransactionScope.ScopeNestLevel;
				Assert.AreEqual(0, StorageTransactionScope.ScopeNestLevel);
					
				Assert.IsTrue(transaction.IsActive, "Detached transaction must remain active after disposing scope");
				Assert.AreEqual(0, subscriber.TimesNotified, "Scope was not to dispose or commit transaction");
				Assert.IsNull(AmbientTransaction, "The scope was disposed and must have restored context");
					
				masterScope.Complete();

				Assume.That(transaction.IsActive && subscriber.TimesNotified == 0);
			}

			System.Threading.Thread.Sleep(50);

			Assert.IsFalse(transaction.IsActive, "Master transaction was committed, KTM must be committed too");
			Assert.AreEqual(1, subscriber.TimesNotified, "Master transaction was committed, must have recieved 1 notification");
			Assert.IsTrue(subscriber.Committed, "Transaction must have beed reported committed.");
				
			Assert.Throws(
				Is.InstanceOf<InvalidOperationException>()
				, () => transaction.Subscribe(subscriber), "Subscribing to inactive transaction must throw exception");
		}

		[Test]
		public void DetachTransactionTest()
		{
			if (!EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test transactions in this environment");
				return;
			}
			Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.RequireTransactions | StorageTransactionSettings.DisallowJoiningAmbientManaged;

			Assume.That(AmbientTransaction == null);

			IFileSystemTransaction transaction;
			using (var scope = StorageTransactionScope.Create(Repository))
			{
				var underlyingTRansaction = scope.UnderlyingTransaction;
				Assert.IsNotNull(underlyingTRansaction);
				Assert.AreSame(AmbientTransaction, underlyingTRansaction, "Underlying transaction must be installed into the context");

				transaction = scope.DetachTransaction();
				Assert.AreSame(AmbientTransaction, transaction, "Detaching transaction must not change context");
			}

			Assert.IsNull(AmbientTransaction, "Detached scope must still restore context when disposed");
			Assert.IsTrue(transaction.IsActive, "Detached context must not end transaction when disposed");

			transaction.Dispose();
		}

		/// <summary>
		///		Test usage [allowed] of external KTM transaction in eager and lazy scope.
		/// </summary>
		[Test]
		public void TestAmbientKtmTransaction()
		{
			if (!bfs.Repository.Util.SystemInfo.IsAnyWindows || !EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test in this environment");
				return;
			}
			using (var scope = CreateStandaloneKtmTransactionScope(true))
			{
				// thus allowing to use ambient storage transaction
				Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.DisallowJoiningAmbientManaged;

				KtmTransaction ambientTransaction = (KtmTransaction)AmbientTransaction;

				// eager scope
				using (StorageTransactionScope target = StorageTransactionScope.Create(Repository))
				{
					Assert.IsFalse(target.IsNullScope, "Eager scope must not produce null scope");
					Assert.IsFalse(target.IsTransactionOwner, "There's external transaction to be used");
					Assert.IsFalse(target.HasChangedContext);
					Assert.IsFalse(target.NoTransaction);
					Assert.IsFalse(target.ToBeDisposed);

					Assert.AreSame(ambientTransaction, AmbientTransaction);
					Assert.AreSame(ambientTransaction, target.UnderlyingTransaction);
					Assert.AreSame(ambientTransaction, target.PreviousTransaction);
				}

				Assert.IsNotNull(AmbientTransaction);
				Assert.AreSame(ambientTransaction, AmbientTransaction);
				// lazy scope
				using (StorageTransactionScope target = StorageTransactionScope.CreateLazy(Repository, null))
				{
					Assert.IsFalse(target.IsNullScope, "Because the \"always start new\" option is OFF");
					Assert.IsFalse(target.IsTransactionOwner, "Lazy scope must never be transaction owner");
					Assert.IsFalse(target.HasChangedContext, "Lazy scope can only change context to NULL and only when ambient transaction is present but its usage prohibited");
					Assert.IsFalse(target.NoTransaction, "Lazy scope can result in no ambient transaction when ambient transaction is present but its usage prohibited or there's "
						+ "no ambient transaction at all");
					Assert.IsFalse(target.ToBeDisposed, "Lazy scope must not own or dispose underlying transaction");

					Assert.AreSame(ambientTransaction, AmbientTransaction);
					Assert.AreSame(ambientTransaction, target.UnderlyingTransaction);
					Assert.AreSame(ambientTransaction, target.PreviousTransaction);
				}

				Assert.IsNotNull(AmbientTransaction);
				Assert.AreSame(ambientTransaction, AmbientTransaction);
			}

			Assert.IsNull(AmbientTransaction);
		}

		/// <summary>
		///		Tests lazy scope enforcing NULL scope (clearing transactional context).
		/// </summary>
		[Test]
		public void TestLazyNullScope()
		{
			if (!EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test in this environment");
				return;
			}

			Assume.That(AmbientTransaction == null, "Leftovers are not expected here");

			// thus allowing to use ambient storage transaction
			Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.DisallowJoiningAmbientManaged;
			// no ambient
			using (var scope = StorageTransactionScope.CreateLazy(Repository, null))
			{
				Assert.IsTrue(scope.IsNullScope);
				Assert.IsFalse(scope.HasChangedContext);
			}

			// now with ambient

			// thus disallowing to use ambient storage transaction
			Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.DisallowJoiningAmbientManaged | StorageTransactionSettings.AlwaysStartNew;

			using (var outerScope = FileSystemProvider.CreateStandaloneTransactionScope(true))
			{
				Assert.IsNotNull(AmbientTransaction);

				using (var scope = StorageTransactionScope.CreateLazy(Repository, null))
				{
					Assert.IsTrue(scope.IsNullScope);
					Assert.IsTrue(scope.HasChangedContext);
					Assert.IsNull(AmbientTransaction);
				}
			}
			Assert.IsNull(AmbientTransaction);
		}

		[Test]
		public void TestLazyWithAmbientManaged()
		{
			if (!EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test in this environment");
				return;
			}

			Assume.That(AmbientTransaction == null, "Leftovers are not expected here");
			// allowing both managed and storage external transactions
			Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.RequireTransactions;

			using (var managedScope = new TransactionScope())
			{
				Assume.That(Transaction.Current != null);

				using (var scope = StorageTransactionScope.CreateLazy(Repository, null))
				{
					Assert.IsNotNull(AmbientTransaction);
					Assert.IsTrue(AmbientTransaction.IsPartOfManagedAmbient);
					Assert.AreEqual(scope.UnderlyingTransaction, AmbientTransaction);
					Assert.AreSame(Transaction.Current, AmbientTransaction.MasterTransaction);
					Assert.IsFalse(scope.ToBeDisposed);
				}

				Assert.IsNull(AmbientTransaction);

				using (var ktmScope = CreateStandaloneKtmTransactionScope(true))
				{
					var ktmTransaction = ktmScope.UnderlyingTransaction;

					Assert.IsFalse(ktmTransaction.IsPartOfManagedAmbient);
					Assert.IsTrue(ktmScope.IsTransactionOwner);

					using (var scope = StorageTransactionScope.CreateLazy(Repository, null))
					{
						Assert.AreSame(ktmTransaction, scope.UnderlyingTransaction, "External transactions are allowed and preference must be given to existing "
							+ "external ambient storage transaction");
						Assert.AreSame(ktmTransaction, AmbientTransaction);
						Assert.IsFalse(scope.HasChangedContext);
					}
				}
			}
		}

		/// <summary>
		///		Test usage [disallowed] of external KTM transaction in an eager scope.
		/// </summary>
		[Test]
		public void TestEagerWithAmbientKtmTransactionProhibited()
		{
			if (!bfs.Repository.Util.SystemInfo.IsAnyWindows || !EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test in this environment");
				return;
			}

			Assume.That(AmbientTransaction == null, "Leftovers are not expected here");
			// thus not allowing to use ambient storage transaction
			Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.DisallowJoiningAmbientManaged | StorageTransactionSettings.AlwaysStartNew;

			using (var scope_ = CreateStandaloneKtmTransactionScope(true))
			{
				KtmTransaction ambientTransaction = (KtmTransaction)AmbientTransaction;

				// eager scope
				using (StorageTransactionScope target = StorageTransactionScope.Create(Repository))
				{
					Assert.IsFalse(target.IsNullScope, "Eager scope must not produce null scope");
					Assert.IsTrue(target.IsTransactionOwner, "The use of external transaction is prohibited by Repository.Settings.StorageTransactionSettings");
					Assert.IsTrue(target.HasChangedContext);
					Assert.IsFalse(target.NoTransaction);
					Assert.IsTrue(target.ToBeDisposed);

					Assert.AreNotSame(ambientTransaction, AmbientTransaction);
					Assert.AreNotSame(ambientTransaction, target.UnderlyingTransaction);
					Assert.AreSame(ambientTransaction, target.PreviousTransaction);
				}

				Assert.IsNotNull(AmbientTransaction);
				Assert.AreSame(ambientTransaction, AmbientTransaction);

			}

			Assert.IsNull(AmbientTransaction);
		}

		/// <summary>
		///		Testing scope wrapping pending transaction.
		/// </summary>
		[Test]
		public void PendingTransactionTest()
		{
			if (!EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test in this environment");
				return;
			}

			Assume.That(AmbientTransaction == null, "Leftovers are not expected here");
			// thus not allowing to use ambient storage transaction
			Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.DisallowJoiningAmbientManaged | StorageTransactionSettings.AlwaysStartNew;

			using (var standaloneTransaction = GetStandaloneTransaction())
			{
				Assume.That(standaloneTransaction != null && standaloneTransaction.IsActive);

				using (var scope = StorageTransactionScope.Create(Repository, standaloneTransaction))
				{
					Assert.IsFalse(scope.IsNullScope);
					Assert.IsFalse(scope.IsTransactionOwner);
					Assert.IsFalse(scope.NoTransaction);
					Assert.AreSame(standaloneTransaction, scope.UnderlyingTransaction);
				}
				Assert.IsTrue(standaloneTransaction.IsActive);
				Assert.IsNull(AmbientTransaction);
			}
		}

		[Test]
		public void TestLoopingAndNesting()
		{
			if (!EnvironmentSupportsTransactions)
			{
				Assert.Inconclusive("Cannot test in this environment");
				return;
			}

			Assume.That(AmbientTransaction == null, "Leftovers are not expected here");
			// thus allowing to use ambient storage transaction
			Repository.Settings.StorageTransactionSettings = StorageTransactionSettings.DisallowJoiningAmbientManaged;


			using (var standaloneTransaction = GetStandaloneTransaction())
			{
				for (int n = 0; n < 100; ++n)
				{
					using (var scope = StorageTransactionScope.CreateLazy(Repository, standaloneTransaction))
					{
						Assert.AreSame(standaloneTransaction, scope.UnderlyingTransaction);
						Assert.IsFalse(scope.ToBeDisposed);
						Assert.AreSame(standaloneTransaction, AmbientTransaction);

						using (var nestedScope = StorageTransactionScope.Create(Repository, standaloneTransaction))
						{
							Assert.IsFalse(nestedScope.HasChangedContext);
							Assert.AreSame(standaloneTransaction, scope.UnderlyingTransaction);
							Assert.IsFalse(scope.ToBeDisposed);
							Assert.AreSame(standaloneTransaction, AmbientTransaction);

							using (var anotherTransaction = GetStandaloneTransaction())
							{
								Assert.AreSame(standaloneTransaction, AmbientTransaction);
								Assert.Throws(
									Is.InstanceOf<InvalidOperationException>()
									, () => StorageTransactionScope.Create(Repository, anotherTransaction));
							}

							nestedScope.Complete();
						}
					}
					Assert.IsNull(AmbientTransaction);
					Assert.IsTrue(standaloneTransaction.IsActive);
				}
			}
		}

		#region helper methods

		private IFileSystemTransaction GetStandaloneTransaction()
		{
			IFileSystemTransaction standaloneTransaction;
			using (var scopeTemp = FileSystemProvider.CreateStandaloneTransactionScope(true))
			{
				standaloneTransaction = scopeTemp.DetachTransaction();
			}
			Assert.IsNotNull(standaloneTransaction);
			Assert.IsTrue(standaloneTransaction.IsActive);
			return standaloneTransaction;
		}

		private KtmTransactionScope CreateStandaloneKtmTransactionScope(bool dispose)
		{
			var scope_ = FileSystemProvider.CreateStandaloneTransactionScope(dispose);
			Assert.IsInstanceOf<KtmTransactionScope>(scope_);
			var scope = (KtmTransactionScope)scope_;

			Assert.IsTrue(scope.IsTransactionOwner);
			Assert.IsTrue(scope.ToBeDisposed);
			Assert.IsTrue(scope.HasChangedContext);

			Assert.AreSame(scope.UnderlyingTransaction, AmbientTransaction);
			Assert.IsInstanceOf<KtmTransaction>(AmbientTransaction);

			Assert.IsFalse(scope.UnderlyingTransaction.IsPartOfManagedAmbient);

			return scope;
		}

		#endregion helper methods
	}
}
