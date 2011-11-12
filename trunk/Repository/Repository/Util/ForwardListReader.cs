using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	public class ForwardListReader<T> : IListReader<T>
	{
		private IList<T> _list;

		public ForwardListReader(IList<T> list)
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
		{ get { return CurrentIndex < Count; } }

		public bool HasMore
		{
			get { return CurrentIndex < (Count - 1); }
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
				return EnumerationDirection.Forwards;
			}
		}

		public bool IsAtStart
		{
			get { return HasItem && CurrentIndex == FirstItemIndex; }
		}

		public bool MoveNext()
		{
			if (HasItem)
			{
				++CurrentIndex;
			}
			return HasItem;
		}

		/// <summary>
		///		Get reader in the opposite direction with the same current position
		/// </summary>
		/// <returns>
		///		<see cref="BackwardListReader&lt;T&gt;"/>
		/// </returns>
		public IListReader<T> Reverse()
		{
			IListReader<T> retval = new BackwardListReader<T>(_list);
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

			if (IsAtStart)
			{
				return _list;
			}

			IList<T> retval = new List<T>(Count - CurrentIndex);
			while (HasItem)
			{
				retval.Add(Current);
				MoveNext();
			}

			return retval;
		}

		private int FirstItemIndex
		{ get { return 0; } }
	}
}
