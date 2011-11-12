using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bfs.Repository.Util
{
	public interface IListReader<T>
	{
		/// <summary>
		///		Get current item
		/// </summary>
		/// <exception cref="InvalidOperationException">
		///		The reader is positioned before first or after last item 
		/// </exception>
		T Current
		{ get; }

		int CurrentIndex
		{ get; }

		bool HasItem
		{ get; }

		bool HasMore
		{ get; }

		//IList<T> List
		//{ get; }

		int Count
		{ get; }

		EnumerationDirection Direction
		{ get; }

		bool IsAtStart
		{ get; }

		/// <summary>
		///		Advance current position 
		/// </summary>
		/// <returns>
		///		<see langword="false"/> if end is reached
		///			(<see cref="HasItem"/> will return <see langword="false"/> and <see cref="Current"/> will throw an exception)
		///		<see langword="true"/> if position advanced successfully
		///			(<see cref="HasItem"/> will return <see langword="true"/> and <see cref="Current"/> will return next item)
		/// </returns>
		bool MoveNext();

		void SetCurrent(int index);

		/// <summary>
		///		Get reader in the opposite direction with the same current position
		/// </summary>
		/// <returns>
		///		<see cref="IListReader&lt;T&gt;"/>
		/// </returns>
		IListReader<T> Reverse();

		/// <summary>
		///		Restart reading the current list
		/// </summary>
		void Reset();

		/// <summary>
		///		Get list of all items remaining until the end of list
		/// </summary>
		/// <returns>
		///		<see cref="IList&lt;T&gt;"/>
		/// </returns>
		IList<T> GetAllRemaining();
	}
}
