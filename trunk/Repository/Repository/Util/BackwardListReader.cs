using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	public class BackwardListReader<T> : IListReader<T>
	{
		private IList<T> _list;

		public BackwardListReader(IList<T> list)
		{
			Check.RequireArgumentNotNull(list, "list");
			_list = list;
			Reset();
		}

		/// <summary>
		///		Get current item
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		The reader is positioned before first or after last item 
		/// </exception>
		public T Current
		{
			get
			{
				Check.RequireLambda(HasItem, () => new InvalidOperationException("The reader is positioned before first or after last item"));
				return _list[CurrentIndex];
			}
		}

		public int CurrentIndex
		{
			get;
			private set;
		}

		public bool HasItem
		{ get { return CurrentIndex >= 0; } }

		public bool HasMore
		{
			get { return CurrentIndex > 0; }
		}

		public void SetCurrent(int index)
		{
			Check.RequireLambda(index >= 0 && index < Count, () => new IndexOutOfRangeException());
			CurrentIndex = index;
		}

		public int Count
		{
			get { return _list.Count; }
		}

		public EnumerationDirection Direction
		{
			get
			{
				return EnumerationDirection.Backwards;
			}
		}

		public bool IsAtStart
		{
			get { return HasItem && CurrentIndex == FirstItemIndex; }
		}

		/// <summary>
		///		Advance current position 
		/// </summary>
		/// <returns>
		///		<see langword="false"/> if end is reached
		///			(<see cref="HasItem"/> will return <see langword="false"/> and <see cref="Current"/> will throw an exception)
		///		<see langword="true"/> if position advanced successfully
		///			(<see cref="HasItem"/> will return <see langword="true"/> and <see cref="Current"/> will return next item)
		/// </returns>
		public bool MoveNext()
		{
			if (HasItem)
			{
				--CurrentIndex;
			}
			return HasItem;
		}

		/// <summary>
		///		Get reader in the opposite direction with the same current position
		/// </summary>
		/// <returns>
		///		<see cref="ForwardListReader&lt;T&gt;"/>
		/// </returns>
		public IListReader<T> Reverse()
		{
			IListReader<T> retval = new ForwardListReader<T>(_list);
			retval.SetCurrent(CurrentIndex);
			return retval;
		}

		/// <summary>
		///		Restart reading the current list
		/// </summary>
		public void Reset()
		{
			CurrentIndex = FirstItemIndex;
		}

		/// <summary>
		///		Get list of all items remaining until the end of list
		/// </summary>
		/// <returns>
		///		<see cref="IList&lt;T&gt;"/>
		/// </returns>
		public IList<T> GetAllRemaining()
		{
			Check.RequireLambda(HasItem, () => new InvalidOperationException("The reader is positioned before first or after last item"));

			IList<T> retval = new List<T>(CurrentIndex);
			while (HasItem)
			{
				retval.Add(Current);
			}
			return retval;
		}

		private int FirstItemIndex
		{ get { return Count - 1; } }
	}
}
