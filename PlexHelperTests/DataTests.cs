
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlexHelper;

namespace PlexHelperTests
{
    [TestClass]
    public class DataTests
    {
		[TestMethod]
		public void TestIsEpisodeNamedCorrectly()
		{
			Assert.IsTrue(Data.IsEpisodeNamedCorrectly("myshow s01e22.mkv", "myshow"));
			Assert.IsTrue(Data.IsEpisodeNamedCorrectly("myshow s11e22.mkv", "myshow"));

			Assert.IsFalse(Data.IsEpisodeNamedCorrectly("myshows1e22.mkv", "myshow"));
			Assert.IsFalse(Data.IsEpisodeNamedCorrectly("myshow 1e22.mkv", "myshow"));
			Assert.IsFalse(Data.IsEpisodeNamedCorrectly("myshow s122.mkv", "myshow"));
			Assert.IsFalse(Data.IsEpisodeNamedCorrectly("myshow 122.mkv", "myshow"));
		}

		[TestMethod]
		public void TestGetEpisodeNumber()
		{
			Assert.AreEqual(22, Data.GetEpisodeNumber("Anime 1 s01e22.mkv"));
			Assert.AreEqual(1, Data.GetEpisodeNumber("myshow s11e1.mkv"));
		}

		[TestMethod]
		public void TestGetSeasonNumber()
		{
			Assert.AreEqual(1, Data.GetSeasonNumber("Anime 1 s01e01.mkv"));
			Assert.AreEqual(11, Data.GetSeasonNumber("myshow s11e22.mkv"));
		}

		[TestMethod]
		public void TestGetCorrectFolderName()
		{
			Assert.AreEqual("Season 01", Data.GetCorrectFolderName("Anime 1 s01e01.mkv"));
			Assert.AreEqual("Season 11", Data.GetCorrectFolderName("myshow s11e22.mkv"));
		}
	}
}
