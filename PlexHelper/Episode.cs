using System.IO;
using System;
using Windows.Storage;

namespace PlexHelper
{
	public class Episode : NotifyProperty
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
					ManuallySet = true;
					OnPropertyChanged();
				}
			}
		}

		private bool _manual = false;
		public bool ManuallySet
		{
			get => _manual;
			set
			{
				if (_manual != value)
				{
					_manual = value;
					OnPropertyChanged();

					if (Season != null)
					{
						Season.RestoreOrdering();
					}
				}
			}
		}

		private string _episodePath;
		public string EpisodePath
		{
			get => _episodePath;
			set
			{
				if (_episodePath != value)
				{
					_episodePath = value;
					OnPropertyChanged();

					Filename = Path.GetFileName(EpisodePath);
					FilenameNoExtension = Path.GetFileNameWithoutExtension(Filename);
				}
			}
		}

		public string OriginalFilename { get; private set; }

		private string ShowName { get { return Season.Show.Name; } }

		public string Filename { get; private set; }
		public string FilenameNoExtension { get; private set; }

		public Episode(int number, string episodePath = null, string originalFilename = null)
		{
			EpisodePath = episodePath;
			_number = number;

			if (originalFilename == null)
			{
				OriginalFilename = FilenameNoExtension;
			}
			else
			{
				OriginalFilename = originalFilename;
			}
		}

		private Season _season;
		public Season Season
		{
			get => _season;
			set
			{
				if (_season != value)
				{
					_season = value;

					TryGetEpisodeNumberFromFilename();
				}
			}
		}

		private bool TryGetEpisodeNumberFromFilename(bool setEpisodeNumber = true)
		{
			if (!FilenameNoExtension.StartsWith(ShowName))
			{
				return false;
			}

			if (FilenameNoExtension[ShowName.Length] != ' ' ||
				FilenameNoExtension[ShowName.Length + 1] != 's')
			{
				return false;
			}

			string seasonNumber = "";
			int i = ShowName.Length + 2;
			while (i < FilenameNoExtension.Length)
			{
				if (char.IsDigit(FilenameNoExtension[i]))
				{
					seasonNumber += FilenameNoExtension[i];
				}
				else if (FilenameNoExtension[i] != 'e')
				{
					return false;
				}
				else
				{
					i++;
					break;
				}

				i++;
			}

			int season = int.Parse(seasonNumber);

			string episodeNumber = "";
			while (i < FilenameNoExtension.Length)
			{
				if (char.IsDigit(FilenameNoExtension[i]))
				{
					episodeNumber += FilenameNoExtension[i];
				}
				else
				{
					break;
				}

				i++;
			}

			if (setEpisodeNumber)
			{
				_number = int.Parse(episodeNumber);
			}

			return true;
		}

		public void SetNumber(int newNumber)
		{
			Number = newNumber;
			ManuallySet = false;
		}

		public string GetCorrectName()
		{
			return string.Format(ShowName + " s{0:00}e{1:00}{2}", Season.Number, Number, Path.GetExtension(EpisodePath));
		}

		public async void Save(StorageFolder seasonFolder)
		{
			StorageFile original = await StorageFile.GetFileFromPathAsync(EpisodePath);
			string correctName = GetCorrectName();
			await original.MoveAsync(seasonFolder, correctName);
			EpisodePath = Path.Combine(seasonFolder.Path, correctName);
		}

		public override bool Equals(object obj)
		{
			if (obj is Episode e)
			{
				return Number == e.Number && EpisodePath == e.EpisodePath;
			}

			return false;
		}

		public override int GetHashCode()
		{
			int hash = 23;

			hash = (hash * 7) + Number.GetHashCode();
			hash = (hash * 7) + EpisodePath.GetHashCode();

			return hash;
		}

		public override string ToString()
		{
			return $"Episode[Number={Number},EpisodePath={EpisodePath}]";
		}
	}
}
