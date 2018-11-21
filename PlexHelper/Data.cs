using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Search;

namespace PlexHelper
{
	public class Data
	{
		private const string KEY_VERSION = "k_version";

		private readonly ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

		private int version;
		private PlexViewModel viewModel;

		public async void LoadAsync(PlexViewModel viewModel)
		{
			version = settings.Values[KEY_VERSION] as int? ?? 1;
			this.viewModel = viewModel;

			foreach (string showKey in settings.Containers.Keys)
			{
				ApplicationDataContainer showContainer = settings.Containers[showKey];
				Debug.WriteLine($"Show: {showContainer.Values["name"] as string}|{showContainer.Values["showPath"] as string}");
				Show show = new Show(showKey, showContainer.Values["name"] as string, showContainer.Values["showPath"] as string);

				StorageFolder showFolder;

				try
				{
					showFolder = await StorageFolder.GetFolderFromPathAsync(show.ShowPath);
				}
				catch (FileNotFoundException)
				{
					settings.DeleteContainer(showKey);
					continue;
				}
				catch (UnauthorizedAccessException)
				{
					settings.DeleteContainer(showKey);
					continue;
				}

				viewModel.Shows.Add(show);

				SortedList<int, Season> seasons = new SortedList<int, Season>();
				Dictionary<string, HashSet<string>> filenames = new Dictionary<string, HashSet<string>>();
				
				foreach (string seasonKey in showContainer.Containers.Keys)
				{
					ApplicationDataContainer seasonContainer = showContainer.Containers[seasonKey];
					Debug.WriteLine($"Season: {seasonContainer.Values["number"] as int? ?? -1}|{seasonKey}");
					Season season = new Season(seasonContainer.Values["number"] as int? ?? -1, seasonKey)
					{
						Show = show
					};

					StorageFolder seasonFolder;

					try
					{
						seasonFolder = await showFolder.GetFolderAsync(season.GetFolderName());
					}
					catch (FileNotFoundException)
					{
						showContainer.DeleteContainer(seasonKey);
						continue;
					}

					filenames.Add(seasonFolder.Name, new HashSet<string>());
					SortedList<int, Episode> episodes = new SortedList<int, Episode>();

					foreach (string episodeKey in seasonContainer.Containers.Keys)
					{
						ApplicationDataContainer episodeContainer = seasonContainer.Containers[episodeKey];
						Debug.WriteLine($"Episode: {episodeContainer.Values["number"] as int? ?? -1}|{episodeContainer.Values["episodePath"] as string}|{episodeContainer.Values["originalFilename"] as string}");
						Episode episode = new Episode(episodeContainer.Values["number"] as int? ?? -1, episodeContainer.Values["episodePath"] as string, episodeContainer.Values["originalFilename"] as string)
						{
							ManuallySet = episodeContainer.Values["manuallySet"] as bool? ?? false
						};

						StorageFile file;

						try
						{
							file = await seasonFolder.GetFileAsync(episode.Filename);
						}
						catch (FileNotFoundException)
						{
							seasonContainer.DeleteContainer(episodeKey);
							continue;
						}

						filenames[season.Name].Add(episode.Filename);

						if (!episodes.ContainsKey(episode.Number))
						{
							episodes.Add(episode.Number, episode);
						}
					}

					foreach (Episode episode in episodes.Values)
					{
						season.AddEpisode(episode);
					}

					seasons.Add(season.Number, season);
				}

				foreach (Season season in seasons.Values)
				{
					show.AddSeason(season);
				}

				// check for unsorted eps
				QueryOptions queryOptions = new QueryOptions(CommonFileQuery.OrderByName, MainPage.VideoExtensions)
				{
					FolderDepth = FolderDepth.Shallow
				};
				StorageFileQueryResult queryResult = showFolder.CreateFileQueryWithOptions(queryOptions);

				IReadOnlyList<StorageFile> unsortedEpisodeFiles = await queryResult.GetFilesAsync();
				foreach (StorageFile episodeFile in unsortedEpisodeFiles)
				{
					if (IsEpisodeNamedCorrectly(episodeFile.Name, show.Name))
					{
						string correctFolderName = GetCorrectFolderName(episodeFile.Name);
						Episode episode = new Episode(GetEpisodeNumber(episodeFile.Name), episodeFile.Path, episodeFile.Name);

						StorageFolder correctSeasonFolder;

						try
						{
							correctSeasonFolder = await showFolder.GetFolderAsync(correctFolderName);
						}
						catch (FileNotFoundException)
						{
							correctSeasonFolder = await showFolder.CreateFolderAsync(correctFolderName);
						}

						// move to correct folder
						await episodeFile.MoveAsync(correctSeasonFolder);

						// restore to season
						int seasonNumber = GetSeasonNumber(episode.Filename);

						if (!seasons.ContainsKey(seasonNumber))
						{
							Season newSeason = new Season(seasonNumber, correctFolderName);
							seasons.Add(seasonNumber, newSeason);

							show.InsertSeason(newSeason);

							showContainer.CreateContainer(correctSeasonFolder.Name, ApplicationDataCreateDisposition.Always);
						}

						Season season = seasons[seasonNumber];

						int i;
						for (i = 0; i < season.NumberEpisodes; i++)
						{
							if (episode.Number < season.Episodes[i].Number)
							{
								break;
							}
						}

						season.InsertEpisode(i, episode);

						// save ep data
						ApplicationDataContainer episodeContainer = showContainer.Containers[correctSeasonFolder.Name].CreateContainer(episode.Filename, ApplicationDataCreateDisposition.Always);
						episodeContainer.Values["number"] = episode.Number;
						episodeContainer.Values["manuallySet"] = episode.ManuallySet;
						episodeContainer.Values["episodePath"] = episode.EpisodePath;
						episodeContainer.Values["originalFilename"] = episode.OriginalFilename;
					}
					else
					{
						Debug.WriteLine($"UnsortedEpisode: -1 {episodeFile.Path}");
						Episode episode = new Episode(-1, episodeFile.Path);
						show.UnsortedSeason.AddEpisode(episode);
					}
				}

				IReadOnlyList<StorageFolder> folders = await showFolder.GetFoldersAsync();
				foreach (StorageFolder seasonFolder in folders)
				{
					if (!filenames.ContainsKey(seasonFolder.Name))
					{
						filenames.Add(seasonFolder.Name, new HashSet<string>());
					}

					queryResult = seasonFolder.CreateFileQueryWithOptions(queryOptions);
					IReadOnlyList<StorageFile> episodeFiles = await queryResult.GetFilesAsync();
					foreach (StorageFile episodeFile in episodeFiles)
					{
						if (!filenames[seasonFolder.Name].Contains(episodeFile.Name))
						{
							if (IsEpisodeNamedCorrectly(episodeFile.Name, show.Name))
							{
								string correctFolderName = GetCorrectFolderName(episodeFile.Name);
								Episode episode = new Episode(GetEpisodeNumber(episodeFile.Name), episodeFile.Path, episodeFile.Name);

								StorageFolder correctSeasonFolder;
								if (correctFolderName != seasonFolder.Name)
								{
									try
									{
										correctSeasonFolder = await showFolder.GetFolderAsync(correctFolderName);
									}
									catch (FileNotFoundException)
									{
										correctSeasonFolder = await showFolder.CreateFolderAsync(correctFolderName);
									}

									// move to correct folder
									await episodeFile.MoveAsync(correctSeasonFolder);
								}
								else
								{
									correctSeasonFolder = seasonFolder;
								}

								// restore to season
								int seasonNumber = GetSeasonNumber(episode.Filename);

								if (!seasons.ContainsKey(seasonNumber))
								{
									Season newSeason = new Season(seasonNumber, correctFolderName);
									seasons.Add(seasonNumber, newSeason);

									show.InsertSeason(newSeason);

									showContainer.CreateContainer(correctSeasonFolder.Name, ApplicationDataCreateDisposition.Always);
								}

								Season season = seasons[seasonNumber];

								int i;
								for (i = 0; i < season.NumberEpisodes; i++)
								{
									if (episode.Number < season.Episodes[i].Number)
									{
										break;
									}
								}

								season.InsertEpisode(i, episode);

								// save ep data
								ApplicationDataContainer episodeContainer = showContainer.Containers[correctSeasonFolder.Name].CreateContainer(Path.GetFileNameWithoutExtension(episode.Filename), ApplicationDataCreateDisposition.Always);
								episodeContainer.Values["number"] = episode.Number;
								episodeContainer.Values["manuallySet"] = episode.ManuallySet;
								episodeContainer.Values["episodePath"] = episode.EpisodePath;
								episodeContainer.Values["originalFilename"] = episode.OriginalFilename;
							}
							else
							{
								Debug.WriteLine($"UnsortedEpisode: -1 {episodeFile.Path}");
								Episode episode = new Episode(-1, episodeFile.Path);
								show.UnsortedSeason.AddEpisode(episode);
							}
						}
					}
					}
			}

			Debug.WriteLine("done loading");
		}

		public static bool IsEpisodeNamedCorrectly(string filename, string showName)
		{
			if (!filename.StartsWith(showName))
			{
				return false;
			}

			if (filename[showName.Length] != ' ' ||
				filename[showName.Length + 1] != 's')
			{
				return false;
			}

			string seasonNumber = "";
			int i = showName.Length + 2;
			while (i < filename.Length)
			{
				if (char.IsDigit(filename[i]))
				{
					seasonNumber += filename[i];
				}
				else if (filename[i] != 'e')
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
			while (i < filename.Length)
			{
				if (char.IsDigit(filename[i]))
				{
					episodeNumber += filename[i];
				}
				else
				{
					break;
				}

				i++;
			}

			return true;
		}

		public static int GetEpisodeNumber(string filename)
		{
			string[] pieces = Path.GetFileNameWithoutExtension(filename).Split(new char[] { ' ' });
			return int.Parse(pieces[pieces.Length - 1].Split(new char[] { 'e' })[1]);
		}

		public static int GetSeasonNumber(string filename)
		{
			string[] pieces = Path.GetFileNameWithoutExtension(filename).Split(new char[] { ' ' });
			return int.Parse(pieces[pieces.Length - 1].Split(new char[] { 'e' })[0].Substring(1));
		}

		public static string GetCorrectFolderName(string filename)
		{
			string[] pieces = Path.GetFileNameWithoutExtension(filename).Split(new char[] { ' ' });
			return string.Format("Season {0:00}", int.Parse(pieces[pieces.Length - 1].Split(new char[] { 'e' })[0].Substring(1)));
		}

		public void RemoveShowData(Show show)
		{
			settings.DeleteContainer(show.Id);

			if (StorageApplicationPermissions.FutureAccessList.ContainsItem(show.Id))
			{
				StorageApplicationPermissions.FutureAccessList.Remove(show.Id);
			}
		}

		public void SaveShowData(Show show)
		{
			ApplicationDataContainer showContainer = settings.CreateContainer(show.Id, ApplicationDataCreateDisposition.Always);
			showContainer.Values["name"] = show.Name;
			showContainer.Values["showPath"] = show.ShowPath;

			foreach (Season season in show.Seasons)
			{
				if (season.Number == 0)
				{
					continue;
				}

				ApplicationDataContainer seasonContainer = showContainer.CreateContainer(season.Name, ApplicationDataCreateDisposition.Always);
				seasonContainer.Values["number"] = season.Number;

				foreach (Episode episode in season.Episodes)
				{
					ApplicationDataContainer episodeContainer = seasonContainer.CreateContainer(episode.Filename, ApplicationDataCreateDisposition.Always);
					episodeContainer.Values["number"] = episode.Number;
					episodeContainer.Values["manuallySet"] = episode.ManuallySet;
					episodeContainer.Values["episodePath"] = episode.EpisodePath;
					episodeContainer.Values["originalFilename"] = episode.OriginalFilename;
				}
			}
		}

		public void SaveAllData()
		{
			settings.Values[KEY_VERSION] = version;

			foreach (Show show in viewModel.Shows)
			{
				SaveShowData(show);
			}
		}
	}
}
