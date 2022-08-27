using Helper_1080.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using ZXing.QrCode;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using ZXing.Windows.Compatibility;
using System.Text.RegularExpressions;
using Windows.Services.Store;
using System.Text.Json;
using Microsoft.UI.Input;
using System.Reflection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Helper_1080.ContentPages.Tools
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class rar : Page
    {
        ObservableCollection<rarFileInfo> rarFileNameList = new();
        List<string> shareBaiduLinkList = new();
        List<string> share115LinkList = new();
        List<string> down115LinkList = new();

        //保存路径
        string SaveRAReXtractFilesPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "eXtract");

        public rar()
        {
            this.InitializeComponent();
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Link;

            e.DragUIOverride.Caption = "拖拽获取压缩包内文件信息";

        }

        private async void tryAddtoClipboard(string ClipboardText)
        {
            //创建一个数据包
            DataPackage dataPackage = new DataPackage();
            //设置创建包里的文本内容
            //string ClipboardText = JsonSerializer.Serialize(content);
            dataPackage.SetText(ClipboardText);

            //把数据包放到剪贴板里
            Clipboard.SetContent(dataPackage);


            DataPackageView dataPackageView = Clipboard.GetContent();
            string text = await dataPackageView.GetTextAsync();
            if (text == ClipboardText)
            {
                LightDismissTeachingTip.Content = @"已添加到剪贴板";
                LightDismissTeachingTip.IsOpen = true;

                await Task.Delay(1000);
                LightDismissTeachingTip.IsOpen = false;
            }
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            //获取拖入文件信息
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                List<string> filesPath = new();

                await Task.Delay(1000);

                rarFileNameList.Clear();

                foreach (var item in items)
                {
                    //文件
                    if (item.IsOfType(StorageItemTypes.File))
                    {
                        var File = item as StorageFile;

                        //rar压缩包
                        if (File.FileType == ".rar")
                        {
                            rarFileNameList.Add(new rarFileInfo() { Name = File.DisplayName, Path = File.Path });
                        };

                    }
                }

                infoBar.Severity = InfoBarSeverity.Success;
                infoBar.Message = $"拖入{items.Count}个文件";
            }

            if (rarFileNameList.Count == 0) return;

            VisualStateManager.GoToState(this, "ShowDropResult", true);

            foreach (var item in rarFileNameList)
            {
                string FileName = Path.GetFileNameWithoutExtension(item.Path);
                string outPath = Path.Combine(SaveRAReXtractFilesPath, FileName);

                if (!Directory.Exists(outPath))
                {
                    Helper.local.ProgressRun("bz", $"x -y -o:{outPath} {item.Path}");
                }

                DirectoryInfo TheFolder = new DirectoryInfo(outPath);
                var filesList = TheFolder.GetFiles();

                //fc2969832.txt
                var detailFile = filesList.Where(x => Regex.Match(x.Name, @"[a-z0-9]\.txt",RegexOptions.IgnoreCase).Success).FirstOrDefault();
                if (detailFile == null) continue;

                //读取文件
                using (StreamReader sr = new StreamReader(detailFile.FullName))
                {
                    string originalContent = sr.ReadToEnd();

                    item.DetailFileContent = originalContent;

                    //获取下载链接
                    var contentLine = originalContent.Split('\n');
                    for (int i = 0; i < contentLine.Count(); i++)
                    {
                        var line = contentLine[i].Trim();
                        if (line.Contains("解壓密碼："))
                        {
                            var passwdResult = Regex.Match(line, @"解壓密碼：(.*)");
                            if (passwdResult.Success)
                            {
                                item.CompressedPassword = passwdResult.Groups[1].Value;
                            }
                        }
                        else if (line.Contains("magnet"))
                        {
                            string link = line;
                            if (item.Links.ContainsKey("magnet"))
                            {
                                item.Links["magnet"].Add(link);
                            }
                            else
                            {
                                item.Links.Add("magnet", new List<string> { link });
                            }
                        }
                        else if (line.Contains("ed2k://"))
                        {
                            string link = line;
                            if (item.Links.ContainsKey("ed2k"))
                            {
                                item.Links["ed2k"].Add(link);
                            }
                            else
                            {
                                item.Links.Add("ed2k", new List<string> { link });
                            }
                        }
                        else if (line.Contains('|') && Regex.Match(line, @"\w.*\|\d.*?\|\w{40}\|\w{40}").Success)
                        {
                            string link = line;
                            if (item.Links.ContainsKey("115转存链接"))
                            {
                                item.Links["115转存链接"].Add(link);
                            }
                            else
                            {
                                item.Links.Add("115转存链接", new List<string> { link });
                            }
                        }
                        else if (line.Contains("http"))
                        {
                            if (line.Contains("1fichier"))
                            {
                                var linkResult = Regex.Match(line, @"[:： ]+(https?:.*)");
                                if (linkResult.Success)
                                {
                                    string link = linkResult.Groups[1].Value;
                                    if (item.Links.ContainsKey("1fichier"))
                                    {
                                        item.Links["1fichier"].Add(link);
                                    }
                                    else
                                    {
                                        item.Links.Add("1fichier", new List<string> { link });
                                    }
                                }
                            }
                            else
                            {
                                var linkResult = Regex.Match(line, @"^http.*");
                                if (linkResult.Success)
                                {
                                    string link = linkResult.Value;
                                    if (item.Links.ContainsKey("直链"))
                                    {
                                        item.Links["直链"].Add(link);
                                    }
                                    else
                                    {
                                        item.Links.Add("直链", new List<string> { link });
                                    }
                                }
                            }
                        }
                        else if (line.Contains("http"))
                        {
                            var linkResult = Regex.Match(line, @"^http.*");
                            if (linkResult.Success)
                            {
                                string link = linkResult.Groups[1].Value;
                                if (item.Links.ContainsKey("直链"))
                                {
                                    item.Links["直链"].Add(link);
                                }
                                else
                                {
                                    item.Links.Add("直链", new List<string> { link });
                                }
                            }
                        }
                    }
                }

                //百度轉存二維碼,提取碼(注意區分數字1和字母l)：877a.png
                var shareImage = filesList.Where(x => x.Extension == ".png").FirstOrDefault();
                if (shareImage == null) continue;

                item.QRcodeImagePath = shareImage.FullName;

                Bitmap image;
                image = (Bitmap)Bitmap.FromFile(shareImage.FullName);
                LuminanceSource source;
                source = new BitmapLuminanceSource(image);
                BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(source));
                Result result = new MultiFormatReader().decode(bitmap);

                string SharePassword = "解压密码";
                var passwordResult = Regex.Match(shareImage.Name, @"百度轉存二維碼,提取碼\(注意區分數字1和字母l\)：(\w*)");
                if (passwordResult.Success)
                {
                    SharePassword = passwordResult.Groups[1].Value;
                }

                item.baiduShareInfo = new() { shareLink = result.Text.Trim(), sharePassword = SharePassword };
            }

            GetLinkFromRarInfo();
            //shareBaiduLinkList = rarFileNameList.Where(x => !string.IsNullOrEmpty(x.baiduShareInfo.shareLink)).ToList();
            //down115LinkList = rarFileNameList.Where(x => x.Links.ContainsKey("ed2k") || x.Links.ContainsKey("magnet") || x.Links.ContainsKey("直链")).ToList();
            //share115LinkList = rarFileNameList.Where(x => x.Links.ContainsKey("115转存链接")).ToList();



            shareBaiduLinkCount_TextBlock.Text = shareBaiduLinkList.Count().ToString();
            down115LinkCount_TextBlock.Text = down115LinkList.Count().ToString();
            share115LinkCount_TextBlock.Text = share115LinkList.Count().ToString();



            FileListView.SelectedIndex = 0;
        }

        private void GetLinkFromRarInfo()
        {
            shareBaiduLinkList.Clear();
            down115LinkList.Clear();
            share115LinkList.Clear();
            foreach (var item in rarFileNameList)
            {
                if (item.baiduShareInfo == null) continue;

                //百度网盘分享链接
                if (!string.IsNullOrEmpty(item.baiduShareInfo.shareLink))
                {
                    shareBaiduLinkList.Add(item.baiduShareInfo.shareLinkWithPwd);
                }
                //其他链接
                foreach(var linkDict in item.Links)
                {
                    switch (linkDict.Key)
                    {
                        case "ed2k" or "magnet" or "直链":
                            down115LinkList.Add(String.Join('\n', linkDict.Value));
                            break;
                        case "115转存链接":
                            share115LinkList.Add(String.Join('\n', linkDict.Value));
                            break;

                    }
                }
            }
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var fileInfo = (sender as ListView).SelectedItem as rarFileInfo;

            if(fileInfo == null || fileInfo.QRcodeImagePath == null) return;

            OriginalContent_Text.Text = fileInfo.DetailFileContent;

            QRcodeImage.Source = new BitmapImage(new Uri(fileInfo.QRcodeImagePath));

            FormatContentGrid.RowDefinitions.Clear();
            FormatContentGrid.Children.Clear();

            //解压密码
            FormatContentGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            //var passwdTitleTextBlock = new TextBlock() { Text = "解压密码"};

            Controls.TitleTextBlock passwdTitleTextBlock = new Controls.TitleTextBlock("解压密码");
            FormatContentGrid.Children.Add(passwdTitleTextBlock);

            var passwdValueTextBlock = new TextBlock() { Text = fileInfo.CompressedPassword, TextWrapping = TextWrapping.Wrap };
            passwdValueTextBlock.Tapped += TextBlock_Tapped;

            passwdValueTextBlock.PointerEntered += TextBlock_PointerEntered;
            passwdValueTextBlock.PointerExited += TextBlock_PointerExited;

            passwdValueTextBlock.SetValue(Grid.ColumnProperty, 1);
            FormatContentGrid.Children.Add(passwdValueTextBlock);

            //百度网盘分享
            FormatContentGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            //var baiduShareLinkTitle_TextBlock = new TextBlock() { Text = "百度分享链接"};

            Controls.TitleTextBlock baiduShareLinkTitle_TextBlock = new Controls.TitleTextBlock("百度分享链接");
            baiduShareLinkTitle_TextBlock.SetValue(Grid.RowProperty, 1);
            FormatContentGrid.Children.Add(baiduShareLinkTitle_TextBlock);

            var baiduShareLinkValue_TextBlock = new TextBlock() { Text = fileInfo.baiduShareInfo.shareLinkWithPwd, TextWrapping = TextWrapping.Wrap };
            baiduShareLinkValue_TextBlock.Tapped += TextBlock_Tapped;

            baiduShareLinkValue_TextBlock.PointerEntered += TextBlock_PointerEntered;
            baiduShareLinkValue_TextBlock.PointerExited += TextBlock_PointerExited;

            baiduShareLinkValue_TextBlock.SetValue(Grid.RowProperty, 1);
            baiduShareLinkValue_TextBlock.SetValue(Grid.ColumnProperty, 1);
            FormatContentGrid.Children.Add(baiduShareLinkValue_TextBlock);

            baiduSharePassword_TextBlock.Text = fileInfo.baiduShareInfo.sharePassword;

            for (int i = 0;i< fileInfo.Links.Count; i++)
            {
                //第一行为解压密码
                int rowIndex = i + 2;

                var item = fileInfo.Links.ToArray()[i];
                FormatContentGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                //var titleTextBlock = new TextBlock() { Text = item.Key};
                Controls.TitleTextBlock titleTextBlock = new Controls.TitleTextBlock(item.Key);
                titleTextBlock.SetValue(Grid.RowProperty, rowIndex);
                FormatContentGrid.Children.Add(titleTextBlock);


                var valueTextBlock = new TextBlock() { Text = String.Join('\n', item.Value), TextWrapping = TextWrapping.Wrap };
                valueTextBlock.Tapped += TextBlock_Tapped;
                valueTextBlock.PointerEntered += TextBlock_PointerEntered;
                valueTextBlock.PointerExited += TextBlock_PointerExited;

                valueTextBlock.SetValue(Grid.RowProperty, rowIndex);
                valueTextBlock.SetValue(Grid.ColumnProperty, 1);
                FormatContentGrid.Children.Add(valueTextBlock);
            }
        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var text = (sender as TextBlock).Text;
            tryAddtoClipboard(text);
        }

        private void TextBlock_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }

        private void TextBlock_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        private void CopyBaiduLink_Click(object sender, RoutedEventArgs e)
        {
            if (shareBaiduLinkList.Count > 0)
            {
                tryAddtoClipboard(String.Join('\n', shareBaiduLinkList));
            }
        }

        private void Copy115DownLink_Click(object sender, RoutedEventArgs e)
        {
            if (down115LinkList.Count > 0)
            {
                tryAddtoClipboard(String.Join('\n', down115LinkList));
            }
        }

        private void Copy115ShareLink_Click(object sender, RoutedEventArgs e)
        {
            if (share115LinkList.Count > 0)
            {
                tryAddtoClipboard(String.Join('\n', share115LinkList));
            }
        }
    }
    public class rarFileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string QRcodeImagePath { get; set; }
        public string DetailFileContent { get; set; }
        public baiduShareInfo baiduShareInfo { get; set; }



        public Dictionary<string, List<string>> Links { get; set; } = new();

        public string CompressedPassword {get;set;}
    }

    public class baiduShareInfo
    {
        public string shareLink { get; set; }
        public string sharePassword { get; set; }
        public string shareLinkWithPwd
        {
            get
            {
                return $"{shareLink}?pwd={sharePassword}";
            }
        }

    }
}
