using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System;
using Windows.Storage;
using System.Diagnostics;

namespace PlexHelper
{
	public class Season : NotifyProperty
	{
		private int _number;
		public int Number
		{
			get => _number;
			set
			{
				if (_number != value)
				{
					_number = value;
					OnPropertyChanged();
				}
			}
		}

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

		public Show Show { get; set; }

		public ObservableCollection<Episode> Episodes { get; } = new ObservableCollection<Episode>();
		public int NumberEpisodes { get { return Episodes.Count; } }

		public Season(int number = 0, string name = null)
		{
			Number = number;

			if (name == null)
			{
				Name = GetFolderName();
			}
			else
			{
				Name = name;
			}

			Episodes.CollectionChanged += OnCollectionChanged;
		}

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLine("collectionchanged " + Number);

			if (Number != 0)
			{
				Debug.WriteLine("restoring order " + Number);
				RestoreOrdering();
			}
		}

		public void AddEpisode(Episode episode)
		{
			episode.Season = this;
			Episodes.Add(episode);
		}

		public void InsertEpisode(int index, Episode episode)
		{
			episode.Season = this;
			Episodes.Insert(index, episode);
		}

		public void RemoveEpisode(Episode episode)
		{
			Episodes.Remove(episode);
		}

		public void RestoreOrdering()
		{
			int epNumber = 1;

			for (int i = 0; i < NumberEpisodes; i++)
			{
				if (!Episodes[i].ManuallySet)
				{
					Episodes[i].SetNumber(epNumber++);
				}
				else
				{
					epNumber = Episodes[i].Number + 1;
				}
			}
		}

		public string GetFolderName()
		{
			return string.Format("Season {0:00}", Number);
		}

		public async void Save(StorageFolder showFolder)
		{
			string folderName = GetFolderName();
			StorageFolder seasonFolder;

			try
			{
				seasonFolder = await showFolder.GetFolderAsync(folderName);
			}
			catch (FileNotFoundException)
			{
				seasonFolder = await showFolder.CreateFolderAsync(folderName);
			}

			foreach (Episode episode in Episodes)
			{
				episode.Save(seasonFolder);
			}
		}

		public Episode this[int index]
		{
			get { return Episodes[index]; }
		}

		public override bool Equals(object obj)
		{
			if (obj is Season s)
			{
				return Name == s.Name && Number == s.Number && Episodes.GetHashCode() == s.Episodes.GetHashCode();
			}

			return false;
		}

		public override int GetHashCode()
		{
			int hash = 23;

			hash = (hash * 7) + Name.GetHashCode();
			hash = (hash * 7) + Number.GetHashCode();
			hash = (hash * 7) + Episodes.GetHashCode();

			return hash;
		}
	}
}
