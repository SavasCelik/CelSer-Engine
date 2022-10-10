using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelSerEngine
{
	/// <summary>
	/// SilentObservableCollection is a ObservableCollection with some extensions.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SilentObservableCollection<T> : ObservableCollection<T>
	{
		/// <summary>
		/// Adds a range of items to the observable collection.
		/// Instead of iterating through all elements and adding them
		/// one by one (which causes OnPropertyChanged events), all
		/// the items gets added instantly without firing events.
		/// After adding all elements, the OnPropertyChanged event will be fired.
		/// </summary>
		/// <param name="enumerable"></param>
		public void AddRange(IEnumerable<T> enumerable)
		{
			CheckReentrancy();

			int startIndex = Count;

			foreach (var item in enumerable)
				Items.Add(item);

			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(enumerable), startIndex));
			OnPropertyChanged(new PropertyChangedEventArgs("Count"));
			OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
		}
	}
}
