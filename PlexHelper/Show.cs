using System;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace PlexHelper
{
	/// <summary>
	/// string name
	/// string path
	/// set<Season>
	/// </summary>
	public class Show : NotifyProperty
	{
		public string Id { get; private set; }

		private string _name;
		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged();
				}
			}
		}

		private string _showPath;
		public string ShowPath
		{
			get => _showPath;
			set
			{
				if (_showPath != value)
				{
					_showPath = value;
					OnPropertyChanged();
				}
			}
		}

		public ObservableCollection<Season> Seasons { get; } = new ObservableCollection<Season>();
		public int NumberSeasons { get { return Seasons.Count; } }

		public Season UnsortedSeason { get; private set; }

		private bool _isDirty;
		public bool IsDirty
		{
			get => _isDirty;
			set
			{
				if (_isDirty != value)
				{
					_isDirty = value;
					OnPropertyChanged();
				}
			}
		}

		public Show(string id, string name = "Default Show Name", string showPath = null)
		{
			if (id == null)
			{
				Id = Guid.NewGuid().ToString();
			}
			else
			{
				Id = id;
			}

			Name = name;
			ShowPath = showPath;

			UnsortedSeason = new Season(0, "Unsorted");
			AddSeason(UnsortedSeason);
		}

		public void MarkDirty()
		{
			IsDirty = true;
		}

		public void MarkClean()
		{
			IsDirty = false;
		}

		public void AddSeason(Season season)
		{
			season.Show = this;
			Seasons.Add(season);
		}

		public void InsertSeason(Season season)
		{
			int i;
			for (i = 1; i < NumberSeasons; i++)
			{
				if (season.Number < Seasons[i].Number)
				{
					break;
				}
			}

			season.Show = this;
			Seasons.Insert(i, season);
		}

		public async System.Threading.Tasks.Task SaveAsync()
		{
			StorageFolder showFolder = await StorageFolder.GetFolderFromPathAsync(ShowPath);

			foreach (Season season in Seasons)
			{
				if (season.Number != 0)
				{
					season.Save(showFolder);
				}
			}
		}

		public Season this[int index]
		{
			get { return Seasons[index]; }
		}
	}
}
