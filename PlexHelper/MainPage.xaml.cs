using System.Collections.Generic;
using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Search;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace PlexHelper
{
	public sealed partial class MainPage : Page
	{
		public PlexViewModel PlexViewModel { get; private set; } = new PlexViewModel();

		private Data data = new Data();

		public static readonly List<string> VideoExtensions = new List<string>()
		{
			".mp4", ".mkv"
		};

		private ListView dragStart;

		public MainPage()
        {
            InitializeComponent();
			Init();
		}

		private void Init()
		{
			data.LoadAsync(PlexViewModel);

			if (PlexViewModel.Shows.Count > 0)
			{
				PlexViewModel.SelectShow(0);
				RenameButton.IsEnabled = true;
			}
		}

		private void ShowSearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{

		}

		private void LeftButton_Click(object sender, RoutedEventArgs e)
		{
			if (PlexViewModel.LeftSeason == PlexViewModel.RightSeason)
			{
				return;
			}

			IList<object> selectedItems = RightEpisodeList.SelectedItems;
			List<Episode> selectedEpisodes = new List<Episode>();

			foreach (object selected in selectedItems)
			{
				selectedEpisodes.Add(selected as Episode);
			}

			if (selectedEpisodes.Count > 0)
			{
				foreach (Episode episode in selectedEpisodes)
				{
					PlexViewModel.RightSeason.RemoveEpisode(episode);
					PlexViewModel.LeftSeason.AddEpisode(episode);
				}

				PlexViewModel.SelectedShow.MarkDirty();
			}
		}

		private void RightButton_Click(object sender, RoutedEventArgs e)
		{
			if (PlexViewModel.LeftSeason == PlexViewModel.RightSeason)
			{
				return;
			}

			IList<object> selectedItems = LeftEpisodeList.SelectedItems;
			List<Episode> selectedEpisodes = new List<Episode>();

			foreach (object selected in selectedItems)
			{
				selectedEpisodes.Add(selected as Episode);
			}

			if (selectedEpisodes.Count > 0)
			{
				foreach (Episode episode in selectedEpisodes)
				{
					PlexViewModel.LeftSeason.RemoveEpisode(episode);
					PlexViewModel.RightSeason.AddEpisode(episode);
				}

				PlexViewModel.SelectedShow.MarkDirty();
			}
		}

		private void Show_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
		{
			showList.SelectedItem = ((FrameworkElement)sender).DataContext;
		}

		private async void OpenShow_ClickAsync(object sender, RoutedEventArgs e)
		{
			FolderPicker picker = new FolderPicker
			{
				SuggestedStartLocation = PickerLocationId.Desktop
			};
			picker.FileTypeFilter.Add("*");

			StorageFolder folder = await picker.PickSingleFolderAsync();
			if (folder != null)
			{
				Show show = new Show(null, folder.Name, folder.Path);

				// add to list of places allowed
				Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace(show.Id, folder);

				QueryOptions queryOptions = new QueryOptions(CommonFileQuery.OrderByName, VideoExtensions)
				{
					FolderDepth = FolderDepth.Shallow
				};
				StorageFileQueryResult queryResult = folder.CreateFileQueryWithOptions(queryOptions);

				IReadOnlyList<StorageFile> unsortedEpisodeFiles = await queryResult.GetFilesAsync();
				foreach (StorageFile unsortedEpisodeFile in unsortedEpisodeFiles)
				{
					Episode episode = new Episode(-1, unsortedEpisodeFile.Path);
					show.UnsortedSeason.AddEpisode(episode);
				}

				IReadOnlyList<StorageFolder> folders = await folder.GetFoldersAsync();
				foreach (StorageFolder seasonFolder in folders)
				{
					Season season;

					if (IsCorrectSeasonName(seasonFolder.Name))
					{
						season = new Season(GetSeasonNumberFromFolder(seasonFolder.Name), seasonFolder.Name);
						show.AddSeason(season);
					}
					else
					{
						season = show.UnsortedSeason;
					}

					queryResult = seasonFolder.CreateFileQueryWithOptions(queryOptions);
					IReadOnlyList<StorageFile> episodeFiles = await queryResult.GetFilesAsync();
					foreach (StorageFile episodeFile in episodeFiles)
					{
						Episode episode = new Episode(-1, episodeFile.Path);
						season.AddEpisode(episode);
					}
				}

				PlexViewModel.Shows.Add(show);
			}
		}

		private int GetSeasonNumberFromFolder(string name)
		{
			return int.Parse(name.Split(new char[] { ' ' })[1]);
		}

		private bool IsCorrectSeasonName(string folderName)
		{
			if (!folderName.StartsWith("Season "))
			{
				return false;
			}

			return int.TryParse(folderName.Substring(7), out int value);
		}

		private void SaveShowIcon_Click(object sender, RoutedEventArgs e)
		{
			if (showList.SelectedItem == null)
			{
				return;
			}

			SaveShow((Show)showList.SelectedItem);
		}

		private void ShowList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateControlState();
		}

		private void SaveShow_Click(object sender, RoutedEventArgs e)
		{
			SaveShow((Show)showList.SelectedItem);
		}

		private void RemoveShow_Click(object sender, RoutedEventArgs e)
		{
			if (PlexViewModel.SelectedShow.IsDirty)
			{
				// todo - ask to save before removing
			}

			data.RemoveShowData((Show)showList.SelectedItem);
			PlexViewModel.RemoveShow(showList.SelectedIndex);
			UpdateControlState();
		}

		private async void SaveShow(Show show)
		{
			await show.SaveAsync();
			data.SaveShowData(show);
		}

		private void UpdateControlState()
		{
			if (PlexViewModel.Shows.Count > 0)
			{
				Searchbar.IsEnabled = true;
				RenameButton.IsEnabled = true;
				PathButton.IsEnabled = true;
				AddSeasonButton.IsEnabled = true;
				LeftSeasonList.IsEnabled = true;
				RightSeasonList.IsEnabled = true;
				LeftButton.IsEnabled = true;
				RightButton.IsEnabled = true;
			}
			else
			{
				Searchbar.IsEnabled = false;
				RenameButton.IsEnabled = false;
				PathButton.IsEnabled = false;
				AddSeasonButton.IsEnabled = false;
				LeftSeasonList.IsEnabled = false;
				RightSeasonList.IsEnabled = false;
				LeftButton.IsEnabled = false;
				RightButton.IsEnabled = false;

				LeftSeasonList.SelectedItem = null;
				RightSeasonList.SelectedItem = null;

				LeftEpisodeList.ItemsSource = null;
				RightEpisodeList.ItemsSource = null;
			}
		}

		private void RenameBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				if (!string.IsNullOrEmpty(RenameText.Text))
				{
					// invalid file name characters
					if (RenameText.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
					{
						return;
					}

					// show with that name already exists
					if (File.Exists(Path.Combine(Directory.GetParent(PlexViewModel.SelectedShow.ShowPath).FullName, RenameText.Text)))
					{
						return;
					}

					PlexViewModel.SelectedShow.Name = RenameText.Text;
					RenameButton.Flyout.Hide();
				}
			}
		}

		private void RenameFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
		{
			RenameText.Text = "";
		}

		private void LeftEpisodeList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			dragStart = LeftEpisodeList;
			e.Data.Properties.Add("episodes", e.Items);
			e.Data.RequestedOperation = DataPackageOperation.Move;
		}

		private void RightEpisodeList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			dragStart = RightEpisodeList;
			e.Data.Properties.Add("episodes", e.Items);
			e.Data.RequestedOperation = DataPackageOperation.Move;
		}

		private void LeftEpisodeList_DragOver(object sender, DragEventArgs e)
		{
			if (dragStart == RightEpisodeList)
			{
				e.AcceptedOperation = DataPackageOperation.Move;
			}
			else
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
		}

		private void RightEpisodeList_DragOver(object sender, DragEventArgs e)
		{
			if (dragStart == LeftEpisodeList)
			{
				e.AcceptedOperation = DataPackageOperation.Move;
			}
			else
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
		}

		private void LeftEpisodeList_Drop(object sender, DragEventArgs e)
		{
			DragOperationDeferral def = e.GetDeferral();

			if (e.Data.Properties.TryGetValue("episodes", out object items))
			{
				Point pos = e.GetPosition(LeftEpisodeList.ItemsPanelRoot);
				ListViewItem lvi = (ListViewItem)LeftEpisodeList.ContainerFromIndex(0);
				int index = 0;

				if (lvi != null)
				{
					double itemHeight = lvi.ActualHeight + lvi.Margin.Top + lvi.Margin.Bottom;
					index = Math.Min(LeftEpisodeList.Items.Count - 1, (int)(pos.Y / itemHeight));

					ListViewItem targetItem = (ListViewItem)LeftEpisodeList.ContainerFromIndex(index);
					Point positionInItem = e.GetPosition(targetItem);

					if (positionInItem.Y > itemHeight / 2)
					{
						index++;
					}

					index = Math.Min(LeftEpisodeList.Items.Count, index);
				}

				IList<object> objects = (IList<object>)items;
				if (objects.Count > 0)
				{
					PlexViewModel.SelectedShow.MarkDirty();
				}

				foreach (object o in objects)
				{
					Episode episode = (Episode)o;
					PlexViewModel.RightSeason.RemoveEpisode(episode);
					PlexViewModel.LeftSeason.InsertEpisode(index++, episode);
				}

				e.AcceptedOperation = DataPackageOperation.Move;
			}

			def.Complete();
		}

		private void RightEpisodeList_Drop(object sender, DragEventArgs e)
		{
			DragOperationDeferral def = e.GetDeferral();

			if (e.Data.Properties.TryGetValue("episodes", out object items))
			{
				Point pos = e.GetPosition(RightEpisodeList.ItemsPanelRoot);
				ListViewItem lvi = (ListViewItem)RightEpisodeList.ContainerFromIndex(0);
				int index = 0;

				if (lvi != null)
				{
					double itemHeight = lvi.ActualHeight + lvi.Margin.Top + lvi.Margin.Bottom;
					index = Math.Min(LeftEpisodeList.Items.Count - 1, (int)(pos.Y / itemHeight));
					ListViewItem targetItem = (ListViewItem)RightEpisodeList.ContainerFromIndex(index);
					Point positionInItem = e.GetPosition(targetItem);

					if (positionInItem.Y > itemHeight / 2)
					{
						index++;
					}

					index = Math.Min(RightEpisodeList.Items.Count, index);
				}

				IList<object> objects = (IList<object>)items;
				if (objects.Count > 0)
				{
					PlexViewModel.SelectedShow.MarkDirty();
				}

				foreach (object o in objects)
				{
					Episode episode = (Episode)o;
					PlexViewModel.LeftSeason.RemoveEpisode(episode);
					PlexViewModel.RightSeason.InsertEpisode(index++, episode);
				}

				e.AcceptedOperation = DataPackageOperation.Move;
			}

			def.Complete();
		}

		private void EpisodeNumberTextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			HashSet<char> toRemove = new HashSet<char>();
			foreach (char c in sender.Text)
			{
				if (!char.IsDigit(c))
				{
					toRemove.Add(c);
				}
			}

			char[] invalid = new char[toRemove.Count];
			toRemove.CopyTo(invalid);
			int index = sender.Text.IndexOfAny(invalid);
			while (index != -1)
			{
				sender.Text = sender.Text.Remove(index, 1);
				index = sender.Text.IndexOfAny(invalid);
			}
		}

		private void NumberAutomatically_Click(object sender, RoutedEventArgs e)
		{
			Button button = (Button)e.OriginalSource;
			Episode episode = (Episode)button.DataContext;
			episode.ManuallySet = false;
			episode.Season.RestoreOrdering();
		}

		private void AddSeasonButton_Click(object sender, RoutedEventArgs e)
		{
			Season season = new Season(PlexViewModel.SelectedShow.NumberSeasons);
			PlexViewModel.SelectedShow.AddSeason(season);
		}
	}
}
