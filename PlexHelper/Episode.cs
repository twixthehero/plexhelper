using System.IO;
using System;
using Windows.Storage;
using System.Threading.Tasks;

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

					Season?.RestoreOrdering();
				}
			}
		}

		private string _tempPath;

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

		private string _filename;
		public string Filename
		{
			get => _filename;
			set
			{
				if (_filename != value)
				{
					_filename = value;
					OnPropertyChanged();
				}
			}
		}
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

					if (!ManuallySet)
					{
						TryGetEpisodeNumberFromFilename();
					}
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

		/// <summary>
		/// Moves this episode to a temp filename
		/// </summary>
		public async Task MoveToTemp()
		{
			StorageFile original = await StorageFile.GetFileFromPathAsync(EpisodePath);
			string tmpName = GetCorrectName() + ".tmp";
			_tempPath = Path.Combine(Path.GetDirectoryName(original.Path), tmpName);
			await original.RenameAsync(tmpName);
		}

		public async void Save(StorageFolder seasonFolder)
		{
			StorageFile original = await StorageFile.GetFileFromPathAsync(_tempPath ?? EpisodePath);
			string correctName = GetCorrectName();
			string destPath = Path.Combine(seasonFolder.Path, correctName);

			if (original.Path != destPath)
			{
				try
				{
					StorageFile dest = await StorageFile.GetFileFromPathAsync(destPath);

					// move ep to tmp first
					Episode destEp = Season?.GetEpisodeWithPath(destPath);
					if (destEp == null)
					{
						foreach (Season season in Season.Show.Seasons)
						{
							destEp = season.GetEpisodeWithPath(destPath);

							if (destEp != null)
							{
								break;
							}
						}
					}

					await destEp.MoveToTemp();
				}
				catch (FileNotFoundException)
				{
					// no problem, ok to move
				}

				await original.MoveAsync(seasonFolder, correctName);
				EpisodePath = destPath;
			}
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
