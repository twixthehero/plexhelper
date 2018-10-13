using System.Collections.ObjectModel;
using System.Linq;

namespace PlexHelper
{
	public class PlexViewModel : NotifyProperty
	{
		public ObservableCollection<Show> Shows { get; } = new ObservableCollection<Show>();

		private Show _selectedShow;
		public Show SelectedShow
		{
			get => _selectedShow;
			set
			{
				if (_selectedShow != value)
				{
					_selectedShow = value;
					OnPropertyChanged();

					if (SelectedShow != null)
					{
						LeftSeason = SelectedShow[0];
						RightSeason = SelectedShow.NumberSeasons > 1 ? SelectedShow[1] : SelectedShow[0];
					}
				}
			}
		}

		private Season _leftSeason;
		public Season LeftSeason
		{
			get => _leftSeason;
			set
			{
				if (_leftSeason != value)
				{
					_leftSeason = value;
					OnPropertyChanged();
				}
			}
		}

		private Season _rightSeason;
		public Season RightSeason
		{
			get => _rightSeason;
			set
			{
				if (_rightSeason != value)
				{
					_rightSeason = value;
					OnPropertyChanged();
				}
			}
		}

		public void SelectShow(int index)
		{
			SelectedShow = Shows[index];
		}

		public void SelectShow(string name)
		{
			SelectedShow = Shows.Single(show => show.Name == name);
		}

		public void RemoveShow(int index)
		{
			if (Shows.Count > 1)
			{
				SelectedShow = Shows[index >= Shows.Count - 1 ? index - 1 : index + 1];
			}
			else
			{
				SelectedShow = null;
			}

			// todo - check if Show is 'dirty' and needs saving
			Shows.RemoveAt(index);
		}
	}
}
