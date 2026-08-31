using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.IO;
using System.Windows;

namespace EpubMaker
{
	public class DragAndDropBehavior : Behavior<FrameworkElement>
	{
		#region DragAndDropBehavior 関係依存プロパティ

		public static readonly DependencyProperty DropFilesProperty =
			DependencyProperty.Register( nameof(DropFiles), typeof(IList), typeof(DragAndDropBehavior), new PropertyMetadata(null) );

		public static readonly DependencyProperty AllowedExtensionsProperty =
			DependencyProperty.Register( nameof(AllowedExtensions), typeof(string), typeof(DragAndDropBehavior), new PropertyMetadata(null) );

		public IList DropFiles
		{
			get => (IList)GetValue(DropFilesProperty);
			set => SetValue(DropFilesProperty, value);
		}

		public string AllowedExtensions
		{
			get => (string)GetValue(AllowedExtensionsProperty);
			set => SetValue(AllowedExtensionsProperty, value);
		}

		#endregion

		#region DragAndDropBehavior メソッド

		protected override void OnAttached()
		{
			base.OnAttached();

			// ドラッグ＆ドロップにメソッド処理を追加
			AssociatedObject.PreviewDragOver += OnPreviewDragOver;
			AssociatedObject.Drop += OnDrop;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			// ドラッグ＆ドロップにメソッド処理を解除
			AssociatedObject.PreviewDragOver -= OnPreviewDragOver;
			AssociatedObject.Drop -= OnDrop;
		}

		private void OnPreviewDragOver(object sender, DragEventArgs e)
		{
			// ドラッグデータがファイルフォーマットかチェック
			if ( e.Data.GetDataPresent(DataFormats.FileDrop, true) && IsAllAcceptable( (string[] )e.Data.GetData(DataFormats.FileDrop) ) )
			{
				// ファイルならコピー形式でドロップ
				e.Effects = DragDropEffects.Copy;
			}
			else
			{
				// ドロップ処理をキャンセル
				e.Effects = DragDropEffects.None;
			}

			// ドラッグ操作のキャンセル
			e.Handled = true;
		}

		private void OnDrop(object sender, DragEventArgs e)
		{
			if ( e.Data.GetDataPresent(DataFormats.FileDrop, true) )
			{
				string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
				if ( IsAllAcceptable(fileNames) )
				{
					// ドロップの情報をIListに変換
					DropFiles = fileNames;
				}
			}
		}

		private bool IsAllAcceptable(string[] fileNames)
		{
			bool bAcceptable = true;

			if ( !string.IsNullOrWhiteSpace(AllowedExtensions) )
			{
				IEnumerable<string> allowed = AllowedExtensions.Split(',').Select( ext => ext.Trim().ToLowerInvariant() );
				bAcceptable = fileNames.All( fileName => Directory.Exists(fileName) || allowed.Contains( Path.GetExtension(fileName).ToLowerInvariant() ) );
			}

			return bAcceptable;
		}

		#endregion
	}
}