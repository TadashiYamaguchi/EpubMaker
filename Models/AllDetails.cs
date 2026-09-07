namespace EpubMaker
{
	public class AllDetails : BindableBase
	{
		#region AllDetails プロパティ

		// シリーズ名
		private string series = string.Empty;
		public string Series { get => series; set => SetProperty(ref series, value); }

		// 著者名
		private string author = string.Empty;
		public string Author { get => author; set => SetProperty(ref author, value); }

		// 出版社名
		private string publisher = string.Empty;
		public string Publisher { get => publisher; set => SetProperty(ref publisher, value); }

		#endregion
	}
}