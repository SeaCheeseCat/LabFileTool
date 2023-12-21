using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LabTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ApiUrl = "http://59.110.231.16:7070/listFiles";
        private readonly string apiUrl = "http://59.110.231.16:7070/upload";  
        private readonly string apibaseUrl = "http://59.110.231.16:7070";
        private string savepath = "";
        public TreeViewItem selectedItem;


        public MainWindow()
        {
            InitializeComponent();
            LoadDataAndBuildTreeView();
            filetree.SelectedItemChanged += TreeView_SelectedItemChanged;
            savepath = ConfigurationManager.AppSettings["path"];
            filetree.ContextMenu = CreateContextMenu();
        }

        private ContextMenu CreateContextMenu()
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem copyMenuItem = new MenuItem
            {
                Header = "复制到本地"
            };
            MenuItem pastMenuItem = new MenuItem
            {
                Header = "粘贴"
            };

            MenuItem DeleteItem = new MenuItem
            {
                Header = "删除"
            };
            contextMenu.Items.Add(copyMenuItem);
            contextMenu.Items.Add(pastMenuItem);
            contextMenu.Items.Add(DeleteItem);
            // 初始时启用按钮
            copyMenuItem.IsEnabled = true;
            // 处理菜单项点击事件
            copyMenuItem.Click += CopyMenuItem_Click;
            pastMenuItem.Click += PasteToServer_Click;
            DeleteItem.Click += DeleteFileButton_Click;
            return contextMenu;
        }

        
        private async void LoadDataAndBuildTreeView()
        {
            filetree.Items.Clear();
            try
            {
                string apiResponse = await GetApiResponseAsync(ApiUrl);
                Debug.WriteLine("开始调用API获取数据...", apiResponse);
                // 使用正则表达式提取目录结构
                List<TreeNode> treeNodes = ExtractDirectoryStructure(apiResponse);
                foreach (var node in treeNodes)
                {
                    TreeViewItem treeViewItem = new TreeViewItem();
                    treeViewItem.Header = node.Name;
                    // 如果有子节点，递归添加子节点
                    if (node.Children != null && node.Children.Count > 0)
                    {
                        AddChildNodes(treeViewItem, node.Children);
                    }
                    filetree.Items.Add(treeViewItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async Task<string> GetApiResponseAsync(string apiUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private void AddChildNodes(TreeViewItem parentItem, List<TreeNode> childNodes)
        {
            foreach (var childNode in childNodes)
            {
                TreeViewItem childItem = new TreeViewItem();
                childItem.Header = childNode.Name;

                // 如果有子节点，递归添加子节点
                if (childNode.Children != null && childNode.Children.Count > 0)
                {
                    AddChildNodes(childItem, childNode.Children);
                }

                parentItem.Items.Add(childItem);
            }
        }

        private List<TreeNode> ExtractDirectoryStructure(string apiResponse)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            MatchCollection matches = Regex.Matches(apiResponse, @"([^\r\n]+)");
            TreeNode currentNode = null;
            foreach (Match match in matches)
            {
                string path = match.Groups[1].Value;
                int depth = path.Split('\\').Length - 1;
                TreeNode newNode = new TreeNode { Name = Path.GetFileName(path) };
                // 确定节点的层次关系
                if (depth == 0)
                {
                    // 根节点
                    nodes.Add(newNode);
                    currentNode = newNode;
                }
                else
                {
                    // 子节点
                    if (currentNode != null)
                    {
                        // 寻找父节点
                        while (depth <= currentNode.Depth && currentNode.Parent != null)
                        {
                            currentNode = currentNode.Parent;
                        }
                        newNode.Parent = currentNode;
                        currentNode.Children.Add(newNode);
                        currentNode = newNode;
                    }
                }
            }
            return nodes;
        }

      
        private async void DownloadFile(string fileUrl, string localFilePath)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(fileUrl);
                    response.EnsureSuccessStatusCode();

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                  stream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await contentStream.CopyToAsync(stream);
                    }

                    Debug.WriteLine($"文件已下载到：{localFilePath}");
                    progressbar.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"下载文件时出错：{ex.Message}");
                MessageBox.Show($"下载文件时出错：{ex.Message}");
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // 在此处理选择事件，触发下载操作
            this.selectedItem = e.NewValue as TreeViewItem;
        }

        private string GetFullPath(TreeViewItem item)
        {
            if (item == null)
            {
                return "";
            }
            string path = item.Header.ToString();
            return path;
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string selectedFilePath = GetFullPath(selectedItem);
            if (selectedFilePath == "" || !selectedFilePath.Contains("."))
                return;
            progressbar.Visibility = Visibility.Visible;
            if (selectedFilePath.Contains("\\"))
            {
                selectedFilePath = selectedFilePath.Replace("\\", "&");
            }
            Debug.WriteLine("解析完成路径", selectedFilePath);
            string fileUrl = "http://59.110.231.16:7070/download/" + selectedFilePath; // 替换为实际的文件基础 URL
            string localFilePath = savepath  + selectedFilePath; // 替换为你想保存的本地路径
            DownloadFile(fileUrl, localFilePath);
        }

        
        private async void PasteToServer_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                var serverpath = "";
                if (selectedItem != null && this.selectedItem.Header.ToString() != "" && !this.selectedItem.Header.ToString().Contains("."))
                {
                    serverpath = selectedItem.Header.ToString();
                }

                if (selectedItem  != null&& this.selectedItem.Header.ToString().Contains("."))
                {
                    // 获取父项的名字
                    if (selectedItem.Parent is TreeViewItem parentItem)
                    {
                        string parentItemName = parentItem.Header.ToString();
                        serverpath = parentItemName;
                    }
                }
                Debug.WriteLine("开始尝试传输文件", filePath+serverpath);
                await UploadFileAsync(filePath, serverpath);
            }
        }


        private async void DeleteFileButton_Click(object sender, RoutedEventArgs e)
        {
            string fileNameToDelete = ""; // 替换成要删除的文件名
            if (selectedItem != null && selectedItem.Header.ToString().Contains("."))
            {
                fileNameToDelete = selectedItem.Header.ToString();
                if (selectedItem.Parent != null )
                {
                    if (selectedItem.Parent is TreeViewItem parentItem)
                    {
                        string parentItemName = parentItem.Header.ToString();
                        fileNameToDelete = parentItemName+"&"+ selectedItem.Header.ToString();
                        Debug.WriteLine("返回掉了", fileNameToDelete);
                    }
                }
            }
            else
            {
               
                return;
            }
            try
            {
                using (var httpClient = new HttpClient())
                {
                    HttpResponseMessage response = await httpClient.DeleteAsync($"{apibaseUrl}/deleteFile/{fileNameToDelete}");
                    if (response.IsSuccessStatusCode)
                    {
                        LoadDataAndBuildTreeView();
                        //MessageBox.Show("File deleted successfully!");
                    }
                    else
                    {
                        MessageBox.Show($"Failed to delete file. Status Code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }



    private async Task UploadFileAsync(string filePath, string subDir)
    {
            progressbar.Visibility = Visibility.Visible;
            Debug.WriteLine("传输的本地位置",filePath);
            try
            {
                using (var httpClient = new HttpClient())
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var streamContent = new StreamContent(fileStream);

                    using (var formData = new MultipartFormDataContent())
                    {
                        formData.Add(streamContent, "file", Path.GetFileName(filePath));
                        if(subDir != "")
                            formData.Add(new StringContent(subDir), "subDir");

                        var response = await httpClient.PostAsync(apiUrl, formData);

                        if (response.IsSuccessStatusCode)
                        {
                            LoadDataAndBuildTreeView();
                            progressbar.Visibility = Visibility.Hidden;
                            //MessageBox.Show("File uploaded successfully!");
                        }
                        else
                        {
                            progressbar.Visibility = Visibility.Hidden;
                            MessageBox.Show($"File upload failed. Status Code: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                progressbar.Visibility = Visibility.Hidden;
                MessageBox.Show($"HttpRequestException: {ex.Data} {ex.Message}");
                Debug.WriteLine($"HttpRequestException: {ex.Data} {ex.Message}");
            }
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadDataAndBuildTreeView();
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            InputDialog inputDialog = new InputDialog();
            if (inputDialog.ShowDialog() == true)
            {
                string userInput = inputDialog.UserInput;
                savepath = userInput;
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            LoadDataAndBuildTreeView();
        }
    }




















  

}


public class TreeNode
{
    public string Name { get; set; }
    public List<TreeNode> Children { get; set; } = new List<TreeNode>();
    public TreeNode Parent { get; set; }
    public int Depth => Parent == null ? 0 : Parent.Depth + 1;
}


public class RelayCommand : ICommand
{
    private readonly Action<object> execute;
    private readonly Func<object, bool> canExecute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter)
    {
        return canExecute == null || canExecute(parameter);
    }

    public void Execute(object parameter)
    {
        execute(parameter);
    }
}